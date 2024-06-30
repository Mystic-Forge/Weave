using System;
using System.Collections;
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
        private readonly Scope?                                  _parent;
        private readonly ParameterExpression?                    _selfParameter;

        public LabelTarget? ExitTarget { get; set; }

        public LabelTarget? ReturnTarget { get; set; }

        public Scope(Scope? parent, ParameterExpression? selfParameter) {
            _parent        = parent;
            _selfParameter = selfParameter;
            ExitTarget     = parent?.ExitTarget;
            ReturnTarget   = parent?.ReturnTarget;
        }

        public ParameterExpression GetIdentifier(WeaveParser.IdentifierContext identifier, WeaveScriptDefinition script) {
            if (TryGetIdentifier(identifier, script, out var expression)) return expression;

            throw new WeaveTokenException(script, identifier.Start, $"Identifier {identifier.Start.Text} is not defined in this scope. Did you forget to declare/load/import it?");
        }

        public bool TryGetIdentifier(WeaveParser.IdentifierContext identifier, WeaveScriptDefinition script, out ParameterExpression expression) {
            var name = identifier.Start.Text;

            if (name == "self") {
                if (_selfParameter != null) {
                    expression = _selfParameter;
                    return true;
                } else {
                    expression = null!;
                    return false;
                }
            }

            if (_identifierExpressions.TryGetValue(name, out var value)) {
                expression = value;
                return true;
            }

            if (_parent is not null) return _parent.TryGetIdentifier(identifier, script, out expression);

            expression = null!;
            return false;
        }

        public IEnumerable<ParameterExpression> GetAllVariables() => _identifierExpressions.Values;

        public void AddLocalVariable(string name, ParameterExpression value) => _identifierExpressions[name] = value;

        public ParameterExpression? GetSelfParameter() => _selfParameter;
    }

    public override void EnterImportStatement(WeaveParser.ImportStatementContext context) => DoImport(_script, _globalLibrary, context, t => t is not WeaveType);

    internal static void DoImport(WeaveScriptDefinition script, WeaveLibrary globalLibrary, WeaveParser.ImportStatementContext context, Predicate<WeaveLibraryEntry>? filter = null) {
        if (context.AS() is not null && context.MULTIPLY() is not null) throw new WeaveTokenException(script, context.MULTIPLY().Symbol, "Cannot use wildcard (*) and alias (as) in the same import statement");

        var pathParts = context.import_identifier().Select(i => i.GetText()).ToArray();
        var path      = pathParts.Aggregate("", (current, part) => current + $"{part}/");

        if (context.MULTIPLY() is not null)
            path += "*";
        else
            path += context.identifier().First().Start.Text;

        var nameOverride = context.AS() is not null ? context.identifier().Last().Start.Text : null;

        IEnumerable<WeaveLibraryEntry> entries;

        if (pathParts.Length > 0 && pathParts[0] == "..") {
            var root = script.Library;
            entries = root.Get(path.Substring(3)).ToArray();
        } else { entries = globalLibrary.Get(path).ToArray(); }

        foreach (var entry in entries) {
            if (filter != null && !filter(entry)) continue;

            script.LocalLibrary.Set(nameOverride ?? entry.Name, entry);
        }
    }

    public override void ExitListener(WeaveParser.ListenerContext context) {
        var id = context.identifier()[0].Start.Text;
        if (!_script.LocalLibrary.TryGetFirst(id, out var itemInfo)) throw new WeaveTokenException(_script, context.identifier()[0].Start, $"Tried to subscribe to event \"{id}\" but it does not exist in this scope. Did you forget to import it?");

        if (itemInfo is not WeaveEventInfo eventInfo) throw new($"Tried to subscribe to event \"{id}\" but it is not an event.");
        
        var selfParameter = Expression.Parameter(_script.SelfType, "self");
        var scope         = new Scope(null, selfParameter);
        var returnLabel   = Expression.Label(eventInfo.ReturnType ?? typeof(void), "return label");
        scope.ReturnTarget = returnLabel;
        var parametersToUse = context.identifier().Skip(1).Select(i => i.Start.Text);

        var parameters = eventInfo.Parameters
            .Select(p => {
                var param = Expression.Parameter(p.Value, p.Key);
                scope.AddLocalVariable(p.Key, param);
                return param;
            })
            .Prepend(selfParameter)
            .ToArray();

        _script.AddListener(eventInfo,
            Expression.Lambda(
                Expression.Block(
                    ParseTree(_script, context.block(), scope),
                    Expression.Label(returnLabel, Expression.Default(eventInfo.ReturnType ?? typeof(void)))
                ),
                parameters).Compile()
        );
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

                var writeLineMethod = typeof(Console).GetRuntimeMethod("WriteLine",
                    new[] {
                        typeof(object),
                    })!;

                var getPrintStringMethod = typeof(WeaveListener).GetRuntimeMethods().First(m => m.Name == nameof(GetPrintString)); // I have no idea why you can't just use "GetRuntimeMethod" here

                var getStringMethod = Expression.Call(getPrintStringMethod, Expression.Convert(valueToPrint, typeof(object)));
                return Expression.Call(writeLineMethod, getStringMethod);
            case WeaveParser.ExpressionContext expression: return ParseExpression(script, expression, scope);
            case WeaveParser.IfContext ifContext:          return ParseIf(script, ifContext, scope);
            case WeaveParser.WhileContext whileContext: {
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

                var returnValueExpression = returnContext.expression() != null ? ParseTree(script, returnContext.expression(), scope) : null;
                var targetReturnType      = scope.ReturnTarget.Type;

                if ((returnValueExpression?.Type ?? typeof(void)) != targetReturnType) throw new WeaveTokenException(script, returnContext.Start, $"Return type mismatch. Expected {targetReturnType} but got {returnValueExpression?.Type ?? typeof(void)}.");

                return Expression.Return(scope.ReturnTarget!, returnValueExpression);
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

                var rightSide = Expression.ConvertChecked(ParseTree(script, assignment.expression().Last(), scope), leftSide.Type);
                return Expression.Assign(leftSide, rightSide);
            }
            case WeaveParser.LiteralContext literal:       return ParseLiteral(script, literal, scope);
            case WeaveParser.IdentifierContext identifier: return scope.GetIdentifier(identifier, script);
            case WeaveParser.SaveContext save: {
                var valueToSave = save.expression() is not null
                    ? ParseTree(script, save.expression(), scope)
                    : scope.GetIdentifier(save.identifier().First(), script);

                var memoryId   = save.identifier().Last().Start.Text;
                var memoryInfo = script.LocalLibrary.GetFirst(memoryId);
                if (memoryInfo is not WeaveMemoryInfo) throw new($"Save statement points to \"{memoryId}\" but it is not a memory.");

                var setMemoryMethod = typeof(WeaveInstance).GetMethod("SetMemory")!; // TODO: Cache this
                return Expression.Call(scope.GetSelfParameter(), setMemoryMethod, Expression.Constant(memoryInfo), Expression.Convert(valueToSave, typeof(object)));
            }
            case WeaveParser.LoadContext load: {
                var memoryId = load.identifier().First().Start.Text;

                if (!script.LocalLibrary.TryGetFirst<WeaveMemoryInfo>(memoryId, out var memoryInfo)) throw new WeaveTokenException(script, load.identifier().First().Start, $"A memory with the identifier {memoryId} was not available in this file.");

                var                 getMemoryMethod    = typeof(WeaveInstance).GetMethod("GetMemory")!; // TODO: Cache this
                var                 rawValueExpression = Expression.Call(scope.GetSelfParameter(), getMemoryMethod, Expression.Constant(memoryInfo));
                var                 targetVariable     = load.identifier().Last();
                ParameterExpression variableToSet;

                if (!scope.TryGetIdentifier(targetVariable, script, out variableToSet)) {
                    variableToSet = Expression.Variable(memoryInfo.Type, targetVariable.Start.Text);
                    scope.AddLocalVariable(targetVariable.Start.Text, variableToSet);
                }

                var valueExpression = Expression.Convert(rawValueExpression, memoryInfo.Type);

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
        if (expressionContext.list_prefix_function().Length > 0) return ParseListPrefixFunctionExpression(script, expressionContext, scope);

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
            case WeaveParser.IdentifierContext identifier: { return scope.GetIdentifier(identifier, script); }
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

                if (script.LocalLibrary.TryGetFirst(id, out var itemInfo)) {
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

    private static Expression ParseListPrefixFunctionExpression(WeaveScriptDefinition script, WeaveParser.ExpressionContext expressionContext, Scope scope) {
        var prefixes   = expressionContext.list_prefix_function();
        var expression = expressionContext.expression().Last();

        var value                              = ParseTree(script, expression, scope);
        foreach (var prefix in prefixes) value = ParseListPrefixFunction(script, prefix, value, scope);

        // ToList
        if (typeof(IEnumerable).IsAssignableFrom(value.Type)) {
            var toListMethod = typeof(Enumerable).GetMethod("ToList")!.MakeGenericMethod(value.Type.GetGenericArguments()[0]);
            value = Expression.Call(toListMethod, value);
        }

        return value;
    }

    private static Expression ParseListPrefixFunction(WeaveScriptDefinition script, WeaveParser.List_prefix_functionContext prefixContext, Expression value, Scope scope) {
        switch (prefixContext.GetChild(0)) {
            case WeaveParser.List_indexContext listIndexContext: {
                var index = ParseTree(script, listIndexContext.expression(), scope);

                // var x = new List<int>();
                // ((IEnumerable<int>)x).ToArray()

                var toArrayMethod = typeof(Enumerable).GetMethod("ToArray")!.MakeGenericMethod(value.Type.GetGenericArguments()[0]);
                return Expression.ArrayAccess(Expression.Call(toArrayMethod, value), index);
            }
            case WeaveParser.List_skipContext listSkipContext: {
                var skip = ParseTree(script, listSkipContext.expression(), scope);

                return Expression.Call(typeof(Enumerable),
                    "Skip",
                    new[] {
                        value.Type.GetGenericArguments()[0],
                    },
                    value,
                    skip);
            }
            case WeaveParser.List_takeContext listTakeContext: {
                var take = ParseTree(script, listTakeContext.expression(), scope);

                return Expression.Call(typeof(Enumerable),
                    "Take",
                    new[] {
                        value.Type.GetGenericArguments()[0],
                    },
                    value,
                    take);
            }
            case WeaveParser.List_whereContext listWhereContext: {
                var whereExpression = ParseTree(script, listWhereContext.expression(), scope);

                return Expression.Call(typeof(Enumerable),
                    "Where",
                    new[] {
                        value.Type.GetGenericArguments()[0],
                    },
                    value,
                    whereExpression);
            }
        }

        throw new WeaveTokenException(script, prefixContext.Start, $"Unknown list prefix function: {prefixContext.GetChild(0).GetType().Name}");
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
            WeaveParser.AND           => Expression.AndAlso(left, right),
            WeaveParser.OR            => Expression.OrElse(left, right),
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
            case WeaveParser.NIL: return Expression.Constant(null, typeof(object));
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

        if (literal.@enum() is not null) {
            var enumType  = ParseType(literal.@enum().identifier().First().Start.Text, script.LocalLibrary);
            var enumValue = Enum.Parse(enumType, literal.@enum().identifier().Last().Start.Text);
            return Expression.Constant(enumValue, enumType);
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
        if (localLibrary.TryGetFirst<WeaveType>(type, out var libraryType)) return libraryType.Type;

        throw new($"Unknown type: {type}");
    }

    internal static string GetPrintString(object? value) {
        if (value is null) return "nil";
        if (value is string s) return s;
        if (value is IEnumerable enumerable) return $"[{string.Join(", ", enumerable.Cast<object>().Select(GetPrintString))}]";

        return value.ToString();
    }
}

internal class WeaveTokenException : Exception {
    public WeaveTokenException(WeaveScriptDefinition script, IToken token, string message) : base($"{script.Name}.wv:{token.Line}:{token.Column} : {message}") { }
}