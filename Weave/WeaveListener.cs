using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Antlr4.Runtime.Tree;


namespace Weave;


internal class WeaveListener : WeaveParserBaseListener {
    private readonly WeaveLibrary        _globalLibrary;
    private readonly WeaveFileDefinition _file;

    public WeaveListener(WeaveFileDefinition file, WeaveLibrary globalLibrary) {
        _file          = file;
        _globalLibrary = globalLibrary;
    }

    private class Scope {
        private readonly Dictionary<string, ParameterExpression> _variables = new();
        private readonly Scope?                                  _parent;

        public Scope(Scope? parent) => _parent = parent;

        public Expression GetVariable(string name) => _variables.TryGetValue(name, out var value) ? value : _parent!.GetVariable(name);

        public IEnumerable<ParameterExpression> GetAllVariables() => _variables.Values;

        public void AddLocalVariable(string name, ParameterExpression value) => _variables[name] = value;
    }

    public override void EnterImportStatement(WeaveParser.ImportStatementContext context) {
        var path = context.identifier().Select(i => i.Start.Text).Aggregate((a, b) => $"{a}/{b}");
        Console.WriteLine($"Importing {path}");
        var name = context.identifier().Last().Start.Text;
        _file.Library[name] = _globalLibrary[path];
    }

    public override void ExitListener(WeaveParser.ListenerContext context) {
        var id = context.identifier()[0].Start.Text;
        if (!_file.Library.TryGet(id, out var itemInfo)) throw new($"Tried to subscribe to event \"{id}\" but it does not exist in this scope. Did you forget to import it?");

        if (itemInfo is not WeaveEventInfo eventInfo) throw new($"Tried to subscribe to event \"{id}\" but it is not an event.");

        var scope = new Scope(null);

        var parametersToUse = context.identifier().Skip(1).Select(i => i.Start.Text);
        var parameters = eventInfo.Parameters.Where(p => parametersToUse.Contains(p.Key)).Select(p => {
            var param = Expression.Parameter(p.Value, p.Key);
            scope.AddLocalVariable(p.Key, param);
            return param;
        }).ToArray();

        _file.AddListener(eventInfo, Expression.Lambda(ParseTree(context.block(), scope), parameters).Compile());
    }

    private Expression ParseTree(IParseTree context, Scope scope) {
        switch (context) {
            case WeaveParser.BlockContext block:
                var newScope    = new Scope(scope);
                var expressions = block.statement().Select(s => ParseTree(s, newScope)).ToList();
                return Expression.Block(newScope.GetAllVariables(), expressions);
            case WeaveParser.StatementContext statement: return ParseTree(statement.GetChild(0), scope);
            case WeaveParser.PrintContext print:
                var valueToPrint = ParseTree(print.expression(), scope);

                var writeLineMethod = typeof(Console).GetRuntimeMethod("WriteLine",
                    new[] {
                        valueToPrint.Type,
                    });

                return Expression.Call(writeLineMethod, valueToPrint);
            case WeaveParser.ExpressionContext expression: return ParseExpression(expression, scope);
            case WeaveParser.IfContext ifContext:          return ParseIf(ifContext, scope);
            case WeaveParser.WhileContext whileContext:    throw new NotImplementedException("While loops are not implemented yet.");
            case WeaveParser.ForContext forContext:        throw new NotImplementedException("For loops are not implemented yet.");
            case WeaveParser.TempContext temp:
                var variableName = temp.identifier().Start.Text;
                var value        = ParseTree(temp.expression(), scope);
                var variable     = Expression.Variable(value.Type, variableName);
                scope.AddLocalVariable(variableName, variable);
                return Expression.Assign(variable, value);
            case WeaveParser.AssignmentContext assignment: return Expression.Assign(scope.GetVariable(assignment.identifier().Start.Text), ParseTree(assignment.expression(), scope));
            case WeaveParser.LiteralContext literal:       return ParseLiteral(literal.Start.Type, literal.Start.Text);
            case WeaveParser.IdentifierContext identifier: { return scope.GetVariable(identifier.Start.Text); }
            default:
                Console.WriteLine($"Unknown context: {context.GetType().Name}");
                return Expression.Constant(0);
        }
    }

    private Expression ParseExpression(WeaveParser.ExpressionContext expressionContext, Scope scope) {
        var first = expressionContext.GetChild(0);

        switch (first) {
            case WeaveParser.IfContext ifContext:          return ParseIf(ifContext, scope);
            case WeaveParser.LiteralContext literal:       return ParseLiteral(literal.Start.Type, literal.Start.Text);
            case WeaveParser.IdentifierContext identifier: { return scope.GetVariable(identifier.Start.Text); }
            case TerminalNodeImpl terminal:
                return terminal.Payload.Type switch {
                    WeaveParser.NOT    => Expression.Not(ParseTree(expressionContext.expression(0),    scope)),
                    WeaveParser.MINUS  => Expression.Negate(ParseTree(expressionContext.expression(0), scope)),
                    WeaveParser.LPAREN => ParseTree(expressionContext.expression(0), scope),
                    _                  => throw new($"Unknown terminal: {terminal.Payload.Type}"),
                };
            case WeaveParser.ExpressionContext:
                var left  = ParseTree(expressionContext.expression(0), scope);
                var right = ParseTree(expressionContext.expression(1), scope);
                return ParseBinaryExpression(left, right, expressionContext.GetChild(1));
        }

        return Expression.Constant(0);
    }

    private static Expression ParseBinaryExpression(Expression left, Expression right, IParseTree middle) {
        var type = ((TerminalNodeImpl)middle).Payload.Type;

        return type switch {
            WeaveParser.MULTIPLY      => Expression.Multiply(left, right),
            WeaveParser.SLASH         => Expression.Divide(left, right),
            WeaveParser.PLUS          => Expression.Add(left, right),
            WeaveParser.MINUS         => Expression.Subtract(left, right),
            WeaveParser.IS            => Expression.Equal(left, right),
            WeaveParser.IS_NOT        => Expression.NotEqual(left, right),
            WeaveParser.GREATER       => Expression.GreaterThan(left, right),
            WeaveParser.LESS          => Expression.LessThan(left, right),
            WeaveParser.GREATER_EQUAL => Expression.GreaterThanOrEqual(left, right),
            WeaveParser.LESS_EQUAL    => Expression.LessThanOrEqual(left, right),
            _                         => throw new($"Unknown binary expression: {type}"),
        };
    }

    private Expression ParseIf(WeaveParser.IfContext context, Scope scope, int elseDepth = 0) {
        var condition = ParseTree(context.expression(elseDepth), scope);
        var trueBlock = context.block(elseDepth);

        if (context.ELSE().Length > elseDepth) {
            if (context.IF().Length > elseDepth + 1) { return Expression.IfThenElse(condition, ParseTree(trueBlock, scope), ParseIf(context, scope, elseDepth + 1)); } else {
                var falseBlock = context.block(elseDepth + 1);
                return Expression.IfThenElse(condition, ParseTree(trueBlock, scope), ParseTree(falseBlock, scope));
            }
        }

        return Expression.IfThen(condition, ParseTree(trueBlock, scope));
    }

    private static Expression ParseLiteral(int type, string text) {
        switch (type) {
            case WeaveParser.BOOL:
                var b = bool.Parse(text);
                return Expression.Constant(b, typeof(bool));
            case WeaveParser.INT:
                var i = int.Parse(text);
                return Expression.Constant(i, typeof(int));
            case WeaveParser.FLOAT:
                var f = float.Parse(text);
                return Expression.Constant(f, typeof(float));
            case WeaveParser.STRING:
                var s = text.Substring(1, text.Length - 2);
                return Expression.Constant(s, typeof(string));
        }

        throw new NotImplementedException($"Unknown literal type: {type}");
    }

    internal static Type ParseType(int type) {
        return type switch {
            WeaveParser.BOOL_TYPE   => typeof(bool),
            WeaveParser.INT_TYPE    => typeof(int),
            WeaveParser.FLOAT_TYPE  => typeof(float),
            WeaveParser.STRING_TYPE => typeof(string),
            _                       => throw new NotImplementedException($"Unknown type: {type}"),
        };
    }
}