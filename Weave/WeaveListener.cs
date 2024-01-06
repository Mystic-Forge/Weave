﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;


namespace Weave;


internal class WeaveListener : WeaveParserBaseListener {
    private readonly WeaveLibrary          _globalLibrary;
    private readonly WeaveScriptDefinition _script;

    public WeaveListener(WeaveScriptDefinition script, WeaveLibrary globalLibrary) {
        _script        = script;
        _globalLibrary = globalLibrary;
    }

    internal class Scope {
        private readonly Dictionary<string, ParameterExpression> _identifierExpressions = new();
        private readonly Scope?                         _parent;
        private readonly ParameterExpression?           _selfParameter;

        public LabelTarget? ExitTarget { get; set; }

        public LabelTarget? ReturnTarget { get; set; }

        public Scope(Scope? parent, ParameterExpression? selfParameter) {
            _parent        = parent;
            _selfParameter = selfParameter;
            ExitTarget     = parent?.ExitTarget;
            ReturnTarget   = parent?.ReturnTarget;
        }

        public Expression GetIdentifier(string name) {
            if (name == "self") {
                if (_selfParameter != null)
                    return _selfParameter;
                else
                    throw new("self is not defined in this scope");
            }

            return _identifierExpressions.TryGetValue(name, out var value) ? value : _parent!.GetIdentifier(name);
        }

        public IEnumerable<ParameterExpression> GetAllVariables() => _identifierExpressions.Values.OfType<ParameterExpression>();

        public void AddLocalVariable(string name, ParameterExpression value) => _identifierExpressions[name] = value;

        public bool TryGetLocalVariable(string name, out ParameterExpression value) {
            if(_identifierExpressions.TryGetValue(name, out value)) return true;
            if (_parent != null) return _parent.TryGetLocalVariable(name, out value);
            
            value = null!;
            return false;
        }

        public ParameterExpression? GetSelfParameter() => _selfParameter;
    }

    public override void EnterImportStatement(WeaveParser.ImportStatementContext context) => DoImport(_script, _globalLibrary, context, t => t is not WeaveType);

    internal static void DoImport(WeaveScriptDefinition script, WeaveLibrary globalLibrary, WeaveParser.ImportStatementContext context, Predicate<WeaveLibraryEntry>? filter = null) {
        if (context.AS() is not null && context.MULTIPLY() is not null) throw new WeaveTokenException(script, context.MULTIPLY().Symbol, "Cannot use wildcard (*) and alias (as) in the same import statement");

        var pathParts   = context.import_identifier().Select(i => i.GetText()).ToArray();
        var path        = pathParts.Aggregate("", (current, part) => current + $"{part}/");

        if (context.MULTIPLY() is not null)
            path += "*";
        else
            path += context.identifier().First().Start.Text;
        
        var nameOverride = context.AS() is not null ? context.identifier().Last().Start.Text : null;

        IEnumerable<WeaveLibraryEntry> entries; 
        if(pathParts.Length > 0 && pathParts[0] == "..") {
            var root = script.Library;
            entries = root.Get(path).ToArray();
        } else {
            entries = globalLibrary.Get(path).ToArray();
        }

        foreach (var entry in entries) {
            if (filter != null && !filter(entry)) continue;
            // Console.WriteLine($"Importing {entry.Name} at {path} as {nameOverride ?? entry.Name}");
            script.LocalLibrary.Set(nameOverride ?? entry.Name, entry);
        }
    }

    public override void EnterMemory(WeaveParser.MemoryContext context) {
        var id         = context.identifier().First().Start.Text;
        var type       = ParseType(context.identifier().Last().Start.Text, _script.LocalLibrary);
        var memoryInfo = new WeaveMemoryInfo(id, type);
        _script.LocalLibrary.Set(id, memoryInfo);
    }

    public override void ExitListener(WeaveParser.ListenerContext context) {
        var id = context.identifier()[0].Start.Text;
        if (!_script.LocalLibrary.TryGet(id, out var itemInfo)) throw new($"Tried to subscribe to event \"{id}\" but it does not exist in this scope. Did you forget to import it?");

        if (itemInfo is not WeaveEventInfo eventInfo) throw new($"Tried to subscribe to event \"{id}\" but it is not an event.");

        var selfParameter   = Expression.Parameter(typeof(WeaveInstance), "self");
        var scope           = new Scope(null, selfParameter);
        var parametersToUse = context.identifier().Skip(1).Select(i => i.Start.Text);

        var parameters = eventInfo.Parameters.Where(p => parametersToUse.Contains(p.Key))
            .Select(p => {
                var param = Expression.Parameter(p.Value, p.Key);
                scope.AddLocalVariable(p.Key, param);
                return param;
            })
            .Prepend(selfParameter)
            .ToArray();

        _script.AddListener(eventInfo, Expression.Lambda(ParseTree(_script, context.block(), scope), parameters).Compile());
    }

    internal static Expression ParseTree(WeaveScriptDefinition script, IParseTree context, Scope scope) {
        switch (context) {
            case WeaveParser.BlockContext block: {
                var newScope    = new Scope(scope, scope.GetSelfParameter());
                var expressions = block.statement().Select(s => ParseTree(script, s, newScope)).ToList();
                if (expressions.Count == 0) return Expression.Empty();

                var blockExpression = Expression.Block(newScope.GetAllVariables(), expressions);
                return blockExpression;
            }
            case WeaveParser.StatementContext statement: { return ParseTree(script, statement.GetChild(0), scope); }
            case WeaveParser.PrintContext print:
                var valueToPrint = ParseTree(script, print.expression(), scope);

                var toStringMethod = valueToPrint.Type.GetRuntimeMethod("ToString", Array.Empty<Type>()); // TODO: Cache this

                var writeLineMethod = typeof(Console).GetRuntimeMethod("WriteLine",
                    new[] {
                        typeof(string),
                    }); // TODO: Cache this

                return Expression.Call(writeLineMethod, Expression.Call(valueToPrint, toStringMethod));
            case WeaveParser.ExpressionContext expression: return ParseExpression(script, expression, scope);
            case WeaveParser.IfContext ifContext:          return ParseIf(script, ifContext, scope);
            case WeaveParser.WhileContext whileContext: {
                Console.WriteLine("While");
                var condition = ParseTree(script, whileContext.expression(), scope);
                scope.ExitTarget = Expression.Label();
                var body = ParseTree(script, whileContext.block(), scope);
                return Expression.Loop(Expression.IfThenElse(condition, body, Expression.Break(scope.ExitTarget)), scope.ExitTarget);
            }
            case WeaveParser.ExitContext exit: {
                if (scope.ExitTarget is null) throw new WeaveTokenException(script, exit.Start, "Exit statement used outside of a appropriate context.");

                return Expression.Break(scope.ExitTarget!);
            }
            case WeaveParser.ReturnContext returnContext: {
                if (scope.ReturnTarget is null) throw new WeaveTokenException(script, returnContext.Start, "Return statement used outside of a appropriate context.");

                var returnExpression = returnContext.expression() != null ? ParseTree(script, returnContext.expression(), scope) : null;
                var targetReturnType = scope.ReturnTarget.Type;

                if ((returnExpression?.Type ?? typeof(void)) != targetReturnType) throw new WeaveTokenException(script, returnContext.Start, $"Return type mismatch. Expected {targetReturnType} but got {returnExpression?.Type ?? typeof(void)}.");

                return Expression.Return(scope.ReturnTarget!, returnExpression);
            }
            case WeaveParser.ForContext forContext: {
                var variableName = forContext.identifier().Start.Text;
                var list         = ParseTree(script, forContext.expression(), scope);

                var enumeratorType     = typeof(List<>.Enumerator).MakeGenericType(list.Type.GenericTypeArguments[0]);
                var enumeratorVariable = Expression.Variable(enumeratorType, $"{variableName}Enumerator");
                var moveNextMethod     = enumeratorType.GetRuntimeMethod("MoveNext", Array.Empty<Type>());
                var currentProperty    = enumeratorType.GetRuntimeProperty("Current");
                var enumeratorDispose  = enumeratorType.GetRuntimeMethod("Dispose", Array.Empty<Type>());
                var enumeratorAssign   = Expression.Assign(enumeratorVariable, Expression.Call(list, list.Type.GetRuntimeMethod("GetEnumerator", Array.Empty<Type>())));
                var condition          = Expression.Call(enumeratorVariable, moveNextMethod);

                var variable = Expression.Variable(currentProperty.PropertyType, variableName);
                var newScope = new Scope(scope, scope.GetSelfParameter());
                newScope.AddLocalVariable(variableName, variable);

                var body = Expression.Block(newScope.GetAllVariables(),
                    Expression.Assign(variable, Expression.Property(enumeratorVariable, currentProperty)),
                    ParseTree(script, forContext.block(), newScope));

                var breakLabel = Expression.Label();
                newScope.ExitTarget = breakLabel;

                return Expression.Block(new[] {
                        enumeratorVariable,
                        variable,
                    },
                    enumeratorAssign,
                    Expression.Loop(Expression.IfThenElse(condition, body, Expression.Break(newScope.ExitTarget)), newScope.ExitTarget),
                    Expression.Call(enumeratorVariable, enumeratorDispose));
            }
            case WeaveParser.TempContext temp: {
                var variableName = temp.identifier().Start.Text;
                var value        = ParseTree(script, temp.expression(), scope);
                var variable     = Expression.Variable(value.Type, variableName);
                scope.AddLocalVariable(variableName, variable);
                return Expression.Assign(variable, value);
            }
            case WeaveParser.AssignmentContext assignment: {
                var leftSide = ParseTree(script, assignment.expression().First(), scope);
                if (leftSide is not (ParameterExpression or MemberExpression)) throw new WeaveTokenException(script, assignment.expression().First().Start, $"Left side of assignment must be a variable or a property but got {leftSide.GetType()}.");

                var rightSide = ParseTree(script, assignment.expression().Last(), scope);
                return Expression.Assign(leftSide, rightSide);
            }
            case WeaveParser.LiteralContext literal:       return ParseLiteral(script, literal, scope);
            case WeaveParser.IdentifierContext identifier: return scope.GetIdentifier(identifier.Start.Text);
            case WeaveParser.SaveContext save: {
                var valueToSave = save.expression() is not null
                    ? ParseTree(script, save.expression(), scope)
                    : scope.GetIdentifier(save.identifier().First().Start.Text);

                var memoryId   = save.identifier().Last().Start.Text;
                var memoryInfo = script.LocalLibrary.GetFirst(memoryId);
                if (memoryInfo is not WeaveMemoryInfo) throw new($"Save statement points to \"{memoryId}\" but it is not a memory.");

                var setMemoryMethod = typeof(WeaveInstance).GetMethod("SetMemory")!; // TODO: Cache this
                return Expression.Call(scope.GetSelfParameter(), setMemoryMethod, Expression.Constant(memoryInfo), Expression.Convert(valueToSave, typeof(object)));
            }
            case WeaveParser.LoadContext load: {
                var memoryId     = load.identifier().First().Start.Text;
                var libraryEntry = script.LocalLibrary.GetFirst(memoryId);
                if (libraryEntry is not WeaveMemoryInfo memoryInfo) throw new($"Load statement points to \"{memoryId}\" but it is not a memory.");

                var        getMemoryMethod    = typeof(WeaveInstance).GetMethod("GetMemory")!; // TODO: Cache this
                var        rawValueExpression = Expression.Call(scope.GetSelfParameter(), getMemoryMethod, Expression.Constant(libraryEntry));
                var        targetVariable     = load.identifier().Last().Start.Text;
                ParameterExpression variableToSet;
                if (!scope.TryGetLocalVariable(targetVariable, out variableToSet)) {
                    variableToSet = Expression.Variable(memoryInfo.Type, targetVariable);
                    scope.AddLocalVariable(targetVariable, variableToSet);
                }

                var valueExpression    = Expression.Convert(rawValueExpression, memoryInfo.Type);
                var assignExpression = Expression.IfThenElse(Expression.Equal(rawValueExpression, Expression.Constant(null)),
                    Expression.Assign(variableToSet, Expression.Default(memoryInfo.Type)),
                    Expression.Assign(variableToSet, valueExpression));

                return assignExpression;
            }
            default: {
                if (context is ParserRuleContext parserRuleContext) throw new WeaveTokenException(script, parserRuleContext.Start, $"Unknown context: {context.GetType().Name}");

                throw new($"Unknown context: {context.GetType().Name}");
            }
        }
    }

    private static Expression ParseExpression(WeaveScriptDefinition script, WeaveParser.ExpressionContext expressionContext, Scope scope) {
        var first = expressionContext.GetChild(0);

        switch (first) {
            case WeaveParser.IfContext ifContext: return ParseIf(script, ifContext, scope);
            case WeaveParser.Property_accessContext propertyAccessContext: {
                var property = propertyAccessContext.identifier().Start.Text;
                var instance = ParseTree(script, propertyAccessContext.expression(), scope);

                var propertyInfo = instance.Type.GetProperty(property);

                if (propertyInfo is null) {
                    var fieldInfo = instance.Type.GetField(property);
                    if (fieldInfo is null) throw new($"Property or field \"{property}\" does not exist on type \"{instance.Type}\".");

                    return Expression.Field(instance, fieldInfo);
                }

                return Expression.Property(instance, propertyInfo);
            }
            case WeaveParser.LiteralContext literal:       return ParseLiteral(script, literal, scope);
            case WeaveParser.IdentifierContext identifier: { return scope.GetIdentifier(identifier.Start.Text); }
            case TerminalNodeImpl terminal:
                return terminal.Payload.Type switch {
                    WeaveParser.NOT    => Expression.Not(ParseTree(script,    expressionContext.expression(0), scope)),
                    WeaveParser.MINUS  => Expression.Negate(ParseTree(script, expressionContext.expression(0), scope)),
                    WeaveParser.LPAREN => ParseTree(script, expressionContext.expression(0), scope),
                    _                  => throw new($"Unknown terminal: {terminal.Payload.Type}"),
                };
            case WeaveParser.ExpressionContext:
                var left  = ParseTree(script, expressionContext.expression(0), scope);
                var right = ParseTree(script, expressionContext.expression(1), scope);
                return ParseBinaryExpression(left, right, expressionContext.GetChild(1));
            case WeaveParser.Function_callContext function: {
                var id = function.identifier().Start.Text;

                if (script.LocalLibrary.TryGet(id, out var itemInfo)) {
                    if (itemInfo is WeaveFunctionInfo functionInfo) {
                        var arguments = function.expression().Select(e => Expression.Convert(ParseTree(script, e, scope), typeof(object))).ToArray();
                        functionInfo.CompileExpression();
                        var paramArgs      = Expression.NewArrayInit(typeof(object), arguments);
                        var callExpression = Expression.Call(Expression.Constant(functionInfo), WeaveFunctionInfo.InvokeMethodInfo, paramArgs);

                        if (functionInfo.ReturnType == typeof(void) || functionInfo.ReturnType == null) return callExpression;

                        return Expression.Convert(callExpression, functionInfo.ReturnType!);
                    }

                    throw new WeaveTokenException(script, function.identifier().Start, $"Tried to call function \"{id}\" but it is not a function.");
                }

                throw new WeaveTokenException(script, function.identifier().Start, $"Tried to call function \"{id}\" but it does not exist.");
            }
        }

        throw new($"Unknown expression: {first.GetType().Name}");
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

    private static Expression ParseIf(WeaveScriptDefinition script, WeaveParser.IfContext context, Scope scope, int elseDepth = 0) {
        var returnIsUsed = IsReturnValueUsed(context);

        if (returnIsUsed)
            if (context.IF().Length > context.ELSE().Length)
                throw new WeaveTokenException(script, context.Start, "If statement is non-exhaustive in a context where it must be.");

        var condition = ParseTree(script, context.expression(elseDepth), scope);
        var trueBlock = ParseTree(script, context.block(elseDepth),      scope);

        if (context.ELSE().Length > elseDepth) {
            if (context.IF().Length > elseDepth + 1)
                return ParseIf(returnIsUsed, scope, condition, trueBlock, ParseIf(script, context, scope, elseDepth + 1));
            else
                return ParseIf(returnIsUsed, scope, condition, trueBlock, ParseTree(script, context.block(elseDepth + 1), scope));
        }

        return ParseIf(returnIsUsed, scope, condition, trueBlock);
    }

    private static Expression ParseIf(bool returnIsUsed, Scope scope, Expression condition, Expression trueBlock, Expression? falseBlock = null) {
        if (returnIsUsed) {
            var returnVariable = Expression.Variable(trueBlock.Type, "returnVariable");

            return Expression.Block(
                scope.GetAllVariables().Append(returnVariable),
                falseBlock is null
                    ? Expression.IfThen(condition, Expression.Assign(returnVariable,     trueBlock))
                    : Expression.IfThenElse(condition, Expression.Assign(returnVariable, trueBlock), Expression.Assign(returnVariable, falseBlock)),
                returnVariable);
        }

        if (falseBlock is null) return Expression.IfThen(condition, trueBlock);

        return Expression.IfThenElse(condition, trueBlock, falseBlock);
    }

    private static bool IsReturnValueUsed(RuleContext context) {
        var user = FindUsageOfReturnValue(context);
        if (user is WeaveParser.ListenerContext or WeaveParser.FunctionContext or WeaveParser.StatementContext) return false;

        return true;
    }

    private static RuleContext FindUsageOfReturnValue(RuleContext context) {
        var parent = context.Parent;
        if (parent is WeaveParser.BlockContext or WeaveParser.IfContext) return FindUsageOfReturnValue(parent);

        return parent;
    }

    private static Expression ParseLiteral(WeaveScriptDefinition script, WeaveParser.LiteralContext literal, Scope scope) {
        switch (literal.Start.Type) {
            case WeaveParser.NIL:
                return Expression.Constant(null, typeof(object));
            case WeaveParser.BOOL:
                var b = bool.Parse(literal.Start.Text);
                return Expression.Constant(b, typeof(bool));
            case WeaveParser.INT:
                var i = int.Parse(literal.Start.Text);
                return Expression.Constant(i, typeof(int));
            case WeaveParser.FLOAT:
                var f = float.Parse(literal.Start.Text);
                return Expression.Constant(f, typeof(float));
            case WeaveParser.STRING:
                var s = literal.Start.Text.Substring(1, literal.Start.Text.Length - 2);
                return Expression.Constant(s, typeof(string));
        }

        if (literal.list() is not null) {
            var expressions = literal.list().expression().Select(expression => ParseTree(script, expression, scope)).ToArray();

            if (expressions.Length > 1) {
                var type = expressions.First().Type;
                if (expressions.Skip(1).Any(expression => expression.Type != type)) throw new WeaveTokenException(script, literal.Start, "List elements must be of the same type.");
            }

            var elementType = literal.list().identifier() is not null ? ParseType(literal.list().identifier().Start.Text, script.LocalLibrary) : expressions.First().Type;
            var listType    = typeof(List<>).MakeGenericType(elementType);

            var addMethod = listType.GetMethod("Add",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] {
                    elementType,
                },
                null)!;

            var variable = Expression.Variable(listType, "list");

            var addExpressions = expressions.Select(expression => Expression.Call(variable, addMethod, expression))
                .Cast<Expression>()
                .Prepend(Expression.Assign(variable,
                    Expression.New(listType
                        .GetConstructor(BindingFlags.Public | BindingFlags.Instance,
                            null,
                            new Type[] { },
                            null)!)))
                .Append(variable);

            return Expression.Block(
                new[] {
                    variable,
                },
                addExpressions);
        }

        throw new WeaveTokenException(script, literal.Start, $"Unknown literal type: {literal.Start.Type}");
    }

    private static Dictionary<string, Type> BuiltInTypes = new() {
        {
            "Bool", typeof(bool)
        }, {
            "Int", typeof(int)
        }, {
            "Float", typeof(float)
        }, {
            "String", typeof(string)
        },
    };

    internal static Type ParseType(string type, WeaveLibrary localLibrary) {
        if (BuiltInTypes.TryGetValue(type, out var builtInType)) return builtInType;
        if (localLibrary.TryGet<WeaveType>(type, out var libraryType)) return libraryType.Type;

        throw new($"Unknown type: {type}");
    }
}

internal class WeaveTokenException : Exception {
    public WeaveTokenException(WeaveScriptDefinition script, IToken token, string message) : base($"{script.Name}.wv:{token.Line}:{token.Column} : {message}") { }
}