using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Antlr4.Runtime.Tree;


namespace Weave;


internal class WeaveListener : WeaveParserBaseListener {
    public WeaveFileDefinition FileDefinition { get; } = new();

    internal readonly WeaveLibrary                       _library        = new();
    internal readonly Dictionary<string, WeaveLibraryEntry> imports = new();

    public WeaveListener(WeaveLibrary library) => _library = library;

    private class Scope {
        private readonly Dictionary<string, ParameterExpression> _variables = new();
        private readonly Scope?                                  _parent;

        public Scope(Scope? parent) => _parent = parent;

        public Expression GetVariable(string name) {
            if (_variables.TryGetValue(name, out var value)) return value;

            return _parent!.GetVariable(name);
        }

        public IEnumerable<ParameterExpression> GetAllVariables() => _variables.Values;

        public void AddLocalVariable(string name, ParameterExpression value) => _variables[name] = value;
    }

    public override void EnterImportStatement(WeaveParser.ImportStatementContext context) {
        var path = context.identifier().Select(i => i.Start.Text).Aggregate((a, b) => $"{a}/{b}");
        Console.WriteLine($"Importing {path}");
        
    }

    public override void ExitListener(WeaveParser.ListenerContext context) {
        var id = context.identifier()[0].Start.Text;
        if (!_library.TryGet(id, out var itemInfo)) throw new($"Tried to subscribe to event \"{id}\" but it does not exist in this scope. Did you forget to import it?");

        if(itemInfo is not WeaveEventInfo eventInfo) throw new($"Tried to subscribe to event \"{id}\" but it is not an event.");
        
        var scope = new Scope(null);
        var parameters = eventInfo.ParameterTypes.Zip(context.identifier().Skip(1),
            (type, identifier) => {
                var name     = identifier.Start.Text;
                var variable = Expression.Variable(type, name);
                scope.AddLocalVariable(name, variable);
                return variable;
            }).ToArray(); // ToArray is needed to force the evaluation of the zip

        FileDefinition.AddListener(eventInfo, Expression.Lambda(ParseTree(context.block(), scope), parameters).Compile());
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
            case WeaveParser.ExpressionContext expression: return ParseTree(expression.GetChild(0), scope);
            case WeaveParser.IfContext ifContext:          return ParseIf(ifContext, scope);
            case WeaveParser.TempContext temp:
                var variableName = temp.identifier().Start.Text;
                var value        = ParseTree(temp.expression(), scope);
                var variable     = Expression.Variable(value.Type, variableName);
                scope.AddLocalVariable(variableName, variable);
                return Expression.Assign(variable, value);
            case WeaveParser.AssignmentContext assignment: return Expression.Assign(scope.GetVariable(assignment.identifier().Start.Text), ParseTree(assignment.expression(), scope));
            case WeaveParser.LiteralContext literal:       return ParseLiteral(literal.Start.Type, literal.Start.Text);
            case WeaveParser.IdentifierContext identifier: return scope.GetVariable(identifier.Start.Text);
            default:
                Console.WriteLine($"Unknown context: {context.GetType().Name}"); // "Unknown context: BlockContext
                return Expression.Constant(0);
        }
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
}