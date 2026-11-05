// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Expression = System.Linq.Expressions.Expression;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

/// <summary>
/// Resolves a bounded expression of unchanged locals and deterministic framework values.
/// It never loads a project assembly or invokes a member supplied by the project.
/// </summary>
internal sealed class CSharpFormatStringValueEvaluator(SemanticModel semanticModel, CancellationToken cancellationToken) : IDisposable
{
    // Bound recursive work and intermediate values before they can expand into expensive operations.
    internal const int OperationBudget = 64;
    internal const int RequestOperationBudget = 4096;
    internal const int ScopeNodeLimit = 2048;
    internal const int DataFlowLimit = 16;
    internal const int LocalLimit = 128;
    internal const int StringLimit = 256;
    private const int IntegerByteLimit = 512;
    private readonly PooledDictionary<ILocalSymbol, CSharpFormatStringValue> _locals = PooledDictionary<ILocalSymbol, CSharpFormatStringValue>.GetInstance();
    private readonly PooledDictionary<SyntaxNode, FormatStringValueReason> _scopes = PooledDictionary<SyntaxNode, FormatStringValueReason>.GetInstance();
    private int _requestRemaining = RequestOperationBudget;
    private int _dataFlowRemaining = DataFlowLimit;
    private FormatStringValueReason _reason;
    private int _remaining;
    private bool _exhausted;

    internal bool IsRequestExhausted => _requestRemaining <= 0;

    public void Dispose()
    {
        _locals.Free();
        _scopes.Free();
    }

    public CSharpFormatStringValue GetValue(IOperation? operation)
    {
        _remaining = OperationBudget;
        _exhausted = false;
        _reason = FormatStringValueReason.UnknownInput;
        try
        {
            var value = Evaluate(operation);
            return new(value, operation?.Type, value.HasValue ? FormatStringValueReason.Known : _reason);
        }
        catch (Exception e) when (e is ArgumentException or ArithmeticException or InvalidOperationException ||
            e is TargetInvocationException { InnerException: ArgumentException or ArithmeticException or FormatException })
        {
            return new(default, operation?.Type, FormatStringValueReason.InvalidValue);
        }
    }

    private Optional<object?> Unknown(FormatStringValueReason reason)
    {
        _reason = reason;
        _exhausted |= reason == FormatStringValueReason.WorkLimit;
        return default;
    }

    private Optional<object?> Evaluate(IOperation? operation)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (operation is null)
            return default;
        if (_remaining-- <= 0 || _requestRemaining-- <= 0)
            return Unknown(FormatStringValueReason.WorkLimit);
        if (operation.ConstantValue.HasValue)
            return Bounded(operation.ConstantValue.Value);

        switch (operation)
        {
            case ILocalReferenceOperation local:
                return EvaluateLocal(local);
            case IParenthesizedOperation parentheses:
                return Evaluate(parentheses.Operand);
            case IConversionOperation conversion:
                {
                    // Unboxing depends on the boxed runtime type, which this value domain does not retain.
                    if (conversion.Operand.Type?.SpecialType == SpecialType.System_Object || conversion.Operand.Type?.TypeKind == TypeKind.Interface)
                        return default;
                    var value = Evaluate(conversion.Operand);
                    if (!value.HasValue)
                        return default;
                    if (conversion.OperatorMethod is { } method)
                        return Invoke(method, null, [value.Value]);
                    return ConvertValue(value.Value, conversion.Type, conversion.IsChecked);
                }
            case IDefaultValueOperation:
                {
                    var type = RuntimeType(operation.Type);
                    return type is null ? default : Bounded(type.IsValueType ? Activator.CreateInstance(type) : null);
                }
            case IConditionalOperation conditional:
                {
                    var condition = Evaluate(conditional.Condition);
                    return condition is { HasValue: true, Value: bool test }
                        ? Evaluate(test ? conditional.WhenTrue : conditional.WhenFalse) : default;
                }
            case ICoalesceOperation coalesce:
                {
                    var value = Evaluate(coalesce.Value);
                    return !value.HasValue ? default : value.Value is null ? Evaluate(coalesce.WhenNull)
                        : coalesce.ValueConversion.IsUserDefined ? Unknown(FormatStringValueReason.UnsupportedOperation)
                        : ConvertValue(value.Value, coalesce.Type, isChecked: false);
                }
            case IUnaryOperation unary:
                {
                    var value = Evaluate(unary.Operand);
                    if (!value.HasValue)
                        return default;
                    if (unary.OperatorMethod is { } method)
                        return value.Value is null ? new Optional<object?>(null) : Invoke(method, null, [value.Value]);
                    var operand = Typed(value.Value, RuntimeType(unary.Operand.Type));
                    if (operand is null)
                        return default;
                    var expression = unary.OperatorKind switch
                    {
                        UnaryOperatorKind.Plus => operand,
                        UnaryOperatorKind.Minus => unary.IsChecked ? Expression.NegateChecked(operand) : Expression.Negate(operand),
                        UnaryOperatorKind.Not or UnaryOperatorKind.BitwiseNegation => Expression.Not(operand),
                        _ => null,
                    };
                    return expression is null ? default : Bounded(Interpret(expression));
                }
            case IBinaryOperation binary:
                return EvaluateBinary(binary);
            case IObjectCreationOperation { Constructor: { } constructor, Initializer: null } creation:
                return EvaluateCall(constructor, null, creation.Arguments);
            case IInvocationOperation invocation:
                return EvaluateCall(invocation.TargetMethod, invocation.Instance, invocation.Arguments);
            case IPropertyReferenceOperation property:
                return property.Property.GetMethod is { } getter
                    ? EvaluateCall(getter, property.Instance, property.Arguments) : default;
            case IFieldReferenceOperation { Field.IsStatic: true } field:
                {
                    var type = RuntimeType(field.Field.ContainingType);
                    if (type is null || field.Field.Name is not ("MinValue" or "MaxValue" or "Empty" or "Zero" or "One" or "ImaginaryOne" or "UnixEpoch"))
                        return default;
                    var member = type.GetField(field.Field.Name, BindingFlags.Public | BindingFlags.Static);
                    return member is { IsInitOnly: true } or { IsLiteral: true } ? Bounded(member.GetValue(null)) : default;
                }
            default:
                return Unknown(FormatStringValueReason.UnsupportedOperation);
        }
    }

    private Optional<object?> EvaluateLocal(ILocalReferenceOperation operation)
    {
        var local = operation.Local;
        if (_locals.TryGetValue(local, out var cached))
        {
            if (!cached.IsKnown)
                _reason = cached.Reason;
            return cached.Value;
        }
        if (_locals.Count >= LocalLimit || _dataFlowRemaining <= 0)
            return Unknown(FormatStringValueReason.WorkLimit);
        if (local.RefKind != RefKind.None || local.DeclaringSyntaxReferences is not [var reference] ||
            reference.SyntaxTree != semanticModel.SyntaxTree ||
            reference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax
            { Parent: VariableDeclarationSyntax { Variables.Count: 1, Parent: LocalDeclarationStatementSyntax declaration } } declarator ||
            declaration.Parent is not (BlockSyntax or GlobalStatementSyntax) ||
            declarator.Span.End > operation.Syntax.SpanStart ||
            semanticModel.GetOperation(declarator, cancellationToken) is not IVariableDeclaratorOperation { Initializer.Value: { } initializer })
        {
            return Unknown(FormatStringValueReason.UnsupportedOperation);
        }

        var scope = declaration.SyntaxTree.GetRoot(cancellationToken);
        foreach (var ancestor in declaration.Ancestors())
        {
            if (ancestor is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax)
            {
                scope = ancestor;
                break;
            }
        }
        if (!_scopes.TryGetValue(scope, out var scopeReason))
        {
            scopeReason = FormatStringValueReason.Known;
            var remaining = ScopeNodeLimit;
            foreach (var node in scope.DescendantNodes())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (--remaining < 0)
                {
                    scopeReason = FormatStringValueReason.WorkLimit;
                    break;
                }
                // A jump can bypass an initializer that lexically precedes its use.
                if (node is GotoStatementSyntax)
                {
                    scopeReason = FormatStringValueReason.UnsupportedOperation;
                    break;
                }
            }
            _scopes.Add(scope, scopeReason);
        }
        if (scopeReason != FormatStringValueReason.Known)
            return Unknown(scopeReason);

        var value = Evaluate(initializer);
        if (value.HasValue)
        {
            if (_dataFlowRemaining <= 0)
                return Unknown(FormatStringValueReason.WorkLimit);
            _dataFlowRemaining--;
            cancellationToken.ThrowIfCancellationRequested();
            var flow = semanticModel.AnalyzeDataFlow(declaration);
            cancellationToken.ThrowIfCancellationRequested();
            // A preceding block initializer with no jumps establishes assignment. Avoid another whole-method
            // definite-assignment walk; these read/write facts share the compiler's existing analysis.
            if (flow is not { Succeeded: true } || flow.WrittenOutside.Contains(local) ||
                flow.UnsafeAddressTaken.Contains(local) || flow.Captured.Contains(local))
            {
                value = Unknown(FormatStringValueReason.MutationOrEscape);
            }
            else
            {
                foreach (var node in scope.DescendantNodes())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (node is IdentifierNameSyntax name && name.Identifier.ValueText == local.Name &&
                        semanticModel.GetOperation(name, cancellationToken) is ILocalReferenceOperation used &&
                        SymbolEqualityComparer.Default.Equals(used.Local, local))
                    {
                        IOperation use = used;
                        while (use.Parent is IConditionalOperation { IsRef: true } conditional)
                            use = conditional;
                        SyntaxNode syntax = name;
                        while (syntax.Parent is ParenthesizedExpressionSyntax parentheses)
                            syntax = parentheses;
                        if (use.Parent is IArgumentOperation { Parameter.RefKind: not RefKind.None } || syntax.Parent is RefExpressionSyntax)
                        {
                            value = Unknown(FormatStringValueReason.MutationOrEscape);
                            break;
                        }
                    }
                }
            }
        }
        if (!_exhausted && _locals.Count < LocalLimit)
            _locals[local] = new(value, local.Type, value.HasValue ? FormatStringValueReason.Known : _reason);
        return value;
    }

    private Optional<object?> EvaluateBinary(IBinaryOperation operation)
    {
        var left = Evaluate(operation.LeftOperand);
        if (!left.HasValue)
            return default;
        if (left.Value is bool test &&
            (operation.OperatorKind == BinaryOperatorKind.ConditionalAnd && !test || operation.OperatorKind == BinaryOperatorKind.ConditionalOr && test))
        {
            return left;
        }
        var right = Evaluate(operation.RightOperand);
        if (!right.HasValue)
            return default;
        if (operation.OperatorKind == BinaryOperatorKind.Add && operation.Type?.SpecialType == SpecialType.System_String &&
            left.Value is null or string && right.Value is null or string)
        {
            return (((string?)left.Value)?.Length ?? 0) + (((string?)right.Value)?.Length ?? 0) > StringLimit
                ? Unknown(FormatStringValueReason.WorkLimit) : Bounded((string?)left.Value + (string?)right.Value);
        }
        if (left.Value is BigInteger && right.Value is int shift &&
            operation.OperatorKind is BinaryOperatorKind.LeftShift or BinaryOperatorKind.RightShift && Math.Abs((long)shift) > IntegerByteLimit * 8)
        {
            return default;
        }
        if (operation.IsLifted && (left.Value is null || right.Value is null))
        {
            return operation.OperatorKind switch
            {
                BinaryOperatorKind.Equals => new Optional<object?>(left.Value is null && right.Value is null),
                BinaryOperatorKind.NotEquals => new Optional<object?>(left.Value is not null || right.Value is not null),
                BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual => new Optional<object?>(false),
                BinaryOperatorKind.And when left.Value is bool a && !a || right.Value is bool b && !b => new Optional<object?>(false),
                BinaryOperatorKind.Or when left.Value is bool a && a || right.Value is bool b && b => new Optional<object?>(true),
                _ => new Optional<object?>(null),
            };
        }
        if (operation.OperatorMethod is { } method)
            return left.Value is null || right.Value is null ? default : Invoke(method, null, [left.Value, right.Value]);
        if (operation.LeftOperand.Type?.SpecialType is SpecialType.System_Single or SpecialType.System_Double)
            return Unknown(FormatStringValueReason.UnsupportedOperation);
        var leftExpression = Typed(left.Value, RuntimeType(operation.LeftOperand.Type));
        var rightExpression = Typed(right.Value, RuntimeType(operation.RightOperand.Type));
        if (leftExpression is null || rightExpression is null)
            return default;
        var kind = operation.OperatorKind switch
        {
            BinaryOperatorKind.Add => operation.IsChecked ? ExpressionType.AddChecked : ExpressionType.Add,
            BinaryOperatorKind.Subtract => operation.IsChecked ? ExpressionType.SubtractChecked : ExpressionType.Subtract,
            BinaryOperatorKind.Multiply => operation.IsChecked ? ExpressionType.MultiplyChecked : ExpressionType.Multiply,
            BinaryOperatorKind.Divide => ExpressionType.Divide,
            BinaryOperatorKind.Remainder => ExpressionType.Modulo,
            BinaryOperatorKind.LeftShift => ExpressionType.LeftShift,
            BinaryOperatorKind.RightShift => ExpressionType.RightShift,
            BinaryOperatorKind.And => ExpressionType.And,
            BinaryOperatorKind.Or => ExpressionType.Or,
            BinaryOperatorKind.ExclusiveOr => ExpressionType.ExclusiveOr,
            BinaryOperatorKind.ConditionalAnd => ExpressionType.AndAlso,
            BinaryOperatorKind.ConditionalOr => ExpressionType.OrElse,
            BinaryOperatorKind.Equals => ExpressionType.Equal,
            BinaryOperatorKind.NotEquals => ExpressionType.NotEqual,
            BinaryOperatorKind.LessThan => ExpressionType.LessThan,
            BinaryOperatorKind.LessThanOrEqual => ExpressionType.LessThanOrEqual,
            BinaryOperatorKind.GreaterThan => ExpressionType.GreaterThan,
            BinaryOperatorKind.GreaterThanOrEqual => ExpressionType.GreaterThanOrEqual,
            _ => (ExpressionType?)null,
        };
        return kind is null ? default : Bounded(Interpret(Expression.MakeBinary(kind.Value, leftExpression, rightExpression)));
    }

    private Optional<object?> EvaluateCall(IMethodSymbol method, IOperation? instance, ImmutableArray<IArgumentOperation> arguments)
    {
        if (!IsAllowed(method) || arguments.Length != method.Parameters.Length)
            return Unknown(FormatStringValueReason.UnsupportedOperation);
        object? receiver = null;
        if (instance is not null)
        {
            var value = Evaluate(instance);
            if (!value.HasValue || value.Value is null)
                return default;
            receiver = value.Value;
        }
        var values = new object?[arguments.Length];
        foreach (var argument in arguments)
        {
            if (argument.Parameter is not { RefKind: RefKind.None } parameter)
                return default;
            var value = Evaluate(argument.Value);
            var type = RuntimeType(parameter.Type);
            if (!value.HasValue || type is null)
                return default;
            values[parameter.Ordinal] = value.Value is null || type.IsInstanceOfType(value.Value)
                ? value.Value : type.IsEnum ? Enum.ToObject(type, value.Value) : Interpret(Expression.Convert(Expression.Constant(value.Value), type));
        }
        return Invoke(method, receiver, values);
    }

    private Optional<object?> Invoke(IMethodSymbol method, object? receiver, object?[] arguments)
    {
        if (!IsAllowed(method))
            return default;
        var type = RuntimeType(method.ContainingType);
        if (type is null)
            return default;
        var parameters = new Type[method.Parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = RuntimeType(method.Parameters[i].Type);
            if (parameter is null || method.Parameters[i].RefKind != RefKind.None)
                return default;
            parameters[i] = parameter;
        }

        if (Array.Exists(arguments, value => value is StringComparison comparison && comparison != StringComparison.Ordinal))
            return Unknown(FormatStringValueReason.UnsupportedOperation);

        if (type == typeof(Guid) && arguments.Length is 1 or 2 && arguments[0] is string input)
        {
            var format = input.Length switch
            {
                32 => "N",
                36 => "D",
                38 when input[0] == '{' => "B",
                38 when input[0] == '(' => "P",
                68 => "X",
                _ => null,
            };
            if (format is null || arguments.Length == 2 && !string.Equals(format, arguments[1] as string, StringComparison.OrdinalIgnoreCase))
                return Unknown(FormatStringValueReason.UnsupportedOperation);
            return Guid.TryParseExact(input, format, out var guid) && input.Equals(guid.ToString(format), StringComparison.OrdinalIgnoreCase)
                ? Bounded(guid) : Unknown(FormatStringValueReason.InvalidValue);
        }

        // Integral time units have the same exact tick result on both sides of .NET 7's precision change.
        var ticksPerUnit = method.Name switch
        {
            "AddDays" or "FromDays" => TimeSpan.TicksPerDay,
            "AddHours" or "FromHours" => TimeSpan.TicksPerHour,
            "AddMinutes" or "FromMinutes" => TimeSpan.TicksPerMinute,
            "AddSeconds" or "FromSeconds" => TimeSpan.TicksPerSecond,
            "AddMilliseconds" or "FromMilliseconds" => TimeSpan.TicksPerMillisecond,
            "AddMicroseconds" or "FromMicroseconds" => 10L,
            _ => 0,
        };
        if (ticksPerUnit != 0 && type.Name is "DateTime" or "DateTimeOffset" or "TimeSpan" &&
            parameters is [var unitType] && unitType == typeof(double) && arguments is [double amount])
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount != Math.Truncate(amount) ||
                Math.Abs(amount) > (1L << 53) / ticksPerUnit)
            {
                return Unknown(FormatStringValueReason.UnsupportedOperation);
            }
            var ticks = checked((long)amount * ticksPerUnit);
            return Bounded(receiver is DateTime date ? date.AddTicks(ticks) :
                receiver is DateTimeOffset offset ? offset.AddTicks(ticks) : (object)new TimeSpan(ticks));
        }
        if (type == typeof(string))
        {
            long length = 0;
            if (method.Name == "Concat")
            {
                foreach (var argument in arguments)
                    length += (argument as string)?.Length ?? 0;
            }
            else if (method.Name == "Replace" && receiver is string text && arguments[0] is string oldValue && oldValue.Length > 0)
            {
                length = text.Length;
                for (var position = 0; (position = text.IndexOf(oldValue, position, StringComparison.Ordinal)) >= 0; position += oldValue.Length)
                    length += ((arguments[1] as string)?.Length ?? 0) - oldValue.Length;
            }
            if (length > StringLimit)
                return Unknown(FormatStringValueReason.WorkLimit);
        }
        if (type == typeof(BigInteger) && arguments.Length > 0 && arguments[0] is BigInteger integer)
        {
            var bytes = integer.ToByteArray().Length;
            var maximum = method.MetadataName switch
            {
                "op_Multiply" when arguments[1] is BigInteger other => bytes + other.ToByteArray().Length,
                "op_Addition" or "op_Subtraction" when arguments[1] is BigInteger other => Math.Max(bytes, other.ToByteArray().Length) + 1,
                "op_LeftShift" when arguments[1] is int shift => bytes + (Math.Max(0L, shift) + 7) / 8,
                "op_RightShift" when arguments[1] is int shift => bytes + (Math.Max(0L, -(long)shift) + 7) / 8,
                _ => (long)bytes,
            };
            if (maximum > IntegerByteLimit)
                return Unknown(FormatStringValueReason.WorkLimit);
        }

        if (type == typeof(DateTimeOffset))
        {
            // DateTime-to-offset construction can consult the machine's local time zone.
            for (var i = 0; i < arguments.Length; i++)
            {
                if (method.MethodKind is MethodKind.Constructor or MethodKind.Conversion && arguments[i] is DateTime date &&
                    (date.Kind == DateTimeKind.Local || date.Kind == DateTimeKind.Unspecified && Array.IndexOf(parameters, typeof(TimeSpan)) < 0))
                {
                    return default;
                }
            }
        }
        if (method.MethodKind == MethodKind.Constructor)
        {
            var constructor = type.GetConstructor(parameters);
            return constructor is not null ? Bounded(constructor.Invoke(arguments)) :
                parameters.Length == 0 && type.IsValueType ? Bounded(Activator.CreateInstance(type)) : default;
        }
        MethodInfo? member;
        if (method.MethodKind == MethodKind.Conversion)
        {
            // CLR conversion overloads can differ only by return type.
            var returnType = RuntimeType(method.ReturnType);
            member = type.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(candidate =>
                candidate.Name == method.MetadataName && candidate.ReturnType == returnType &&
                candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameters));
        }
        else
        {
            member = type.GetMethod(method.MetadataName, BindingFlags.Public | (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance), null, parameters, null);
        }
        return member is null ? default : Bounded(member.Invoke(receiver, arguments));
    }

    private bool IsAllowed(IMethodSymbol method)
    {
        var type = RuntimeType(method.ContainingType);
        if (type is null)
            return false;
        if (type.Name == "TimeOnly" && method.Parameters.Any(p => p.Type.SpecialType is SpecialType.System_Single or SpecialType.System_Double))
            return false;
        if (method.MethodKind == MethodKind.Constructor)
        {
            if (type == typeof(Guid))
                return method.Parameters.All(p => p.Type.SpecialType == SpecialType.System_String || IsIntegral(p.Type));
            if (type == typeof(decimal) || type == typeof(BigInteger) || type == typeof(TimeSpan))
                return method.Parameters.All(p => IsIntegral(p.Type));
            if (type == typeof(Complex))
                return method.Parameters.Length == 2 && method.Parameters.All(p => p.Type.SpecialType == SpecialType.System_Double);
            return type.Name is "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly" &&
                method.Parameters.All(p => IsIntegral(p.Type) || RuntimeType(p.Type)?.Name is "DateTimeKind" or "DateTime" or "TimeSpan" or "DateOnly" or "TimeOnly");
        }
        if (method.MethodKind is MethodKind.Conversion or MethodKind.UserDefinedOperator)
        {
            if (type == typeof(decimal) || type == typeof(BigInteger) || type.Name is "Int128" or "UInt128")
                return method.Parameters.All(p => IsIntegral(p.Type) || RuntimeType(p.Type) is { } parameter && (parameter == type || parameter == typeof(decimal)));
            return type.Name is "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly" or "TimeSpan" &&
                method.Parameters.All(p => RuntimeType(p.Type)?.Name is "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly" or "TimeSpan") &&
                method.ReturnType.SpecialType != SpecialType.System_Double;
        }
        if (method.MethodKind == MethodKind.PropertyGet)
        {
            if (type == typeof(string))
                return method.AssociatedSymbol?.Name == "Length";
            if (type == typeof(Complex))
                return method.AssociatedSymbol?.Name is "Real" or "Imaginary";
            return type.Name is "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly" or "TimeSpan" &&
                method.AssociatedSymbol?.Name is
                "MinValue" or "MaxValue" or "UnixEpoch" or "Date" or "DateTime" or "UtcDateTime" or
                "Year" or "Month" or "Day" or "DayOfWeek" or "DayOfYear" or "Hour" or "Minute" or "Second" or
                "Millisecond" or "Microsecond" or "Nanosecond" or "Ticks" or "UtcTicks" or "Kind" or "TimeOfDay" or
                "Offset" or "DayNumber" or "Days" or "Hours" or "Minutes" or "Seconds" or "Milliseconds" or
                "Microseconds" or "Nanoseconds" or "TotalDays" or "TotalHours" or "TotalMinutes" or "TotalSeconds" or
                "TotalMilliseconds" or "TotalMicroseconds" or "TotalNanoseconds";
        }
        if (type == typeof(Math))
        {
            return method.IsStatic && method.Name is "Abs" or "Min" or "Max" or "Clamp" or "Sign" or "Ceiling" or "Floor" or "Round" or "Truncate" &&
                method.Parameters.Length > 0 && method.Parameters[0].Type.SpecialType is not (SpecialType.System_Single or SpecialType.System_Double);
        }
        return type.Name switch
        {
            "Guid" => method.IsStatic && method.Name is "Parse" or "ParseExact" && method.Parameters.Length is 1 or 2 &&
                method.Parameters.All(p => p.Type.SpecialType == SpecialType.System_String),
            "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly" => method.Name is
                "Add" or "AddTicks" or "AddMicroseconds" or "AddMilliseconds" or "AddSeconds" or "AddMinutes" or
                "AddHours" or "AddDays" or "AddMonths" or "AddYears" or "Subtract" or "SpecifyKind" or
                "DaysInMonth" or "IsLeapYear" or "FromDateTime" or "FromDayNumber" or "FromTimeSpan" or
                "ToDateTime" or "ToTimeSpan" or "ToOffset" or "FromUnixTimeSeconds" or "FromUnixTimeMilliseconds" or
                "ToUnixTimeSeconds" or "ToUnixTimeMilliseconds" or "IsBetween",
            "TimeSpan" => method.Name is "Add" or "Subtract" or "Duration" or "Negate" or
                "FromTicks" or "FromMicroseconds" or "FromMilliseconds" or "FromSeconds" or "FromMinutes" or "FromHours" or "FromDays",
            "String" => method.Name is "Substring" or "Replace" ||
                method.Name == "Concat" && method.Parameters.All(p => p.Type.SpecialType == SpecialType.System_String),
            "Decimal" => method.Name is "Add" or "Subtract" or "Multiply" or "Divide" or "Remainder" or "Negate" or
                "Abs" or "Ceiling" or "Floor" or "Round" or "Truncate" or "Min" or "Max" or "Clamp" or "Sign",
            "BigInteger" => method.Name is "Abs" or "Negate" or "Min" or "Max",
            _ => false,
        };
    }

    private static bool IsIntegral(ITypeSymbol type)
        => type.SpecialType is SpecialType.System_Char or SpecialType.System_SByte or SpecialType.System_Byte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64;

    private Type? RuntimeType(ITypeSymbol? symbol)
    {
        if (symbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            var underlying = RuntimeType(nullable.TypeArguments[0]);
            return underlying is null ? null : typeof(Nullable<>).MakeGenericType(underlying);
        }
        var core = semanticModel.Compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;
        if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumeration &&
            !SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, core))
        {
            return RuntimeType(enumeration.EnumUnderlyingType);
        }
        if (symbol is not INamedTypeSymbol type)
            return null;
        if (type.MetadataName is "BigInteger" or "Complex" &&
            type.ContainingNamespace is { Name: "Numerics", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } &&
            type.ContainingAssembly.Identity.Name is "System.Numerics" or "System.Runtime.Numerics" &&
            !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, semanticModel.Compilation.Assembly))
        {
            return type.MetadataName == "BigInteger" ? typeof(BigInteger) : typeof(Complex);
        }
        if (type.ContainingNamespace is not { Name: "System", ContainingNamespace.IsGlobalNamespace: true } ||
            !SymbolEqualityComparer.Default.Equals(type, core.GetTypeByMetadataName("System." + type.MetadataName)))
        {
            return null;
        }
        return type.MetadataName switch
        {
            "Boolean" => typeof(bool),
            "Char" => typeof(char),
            "String" => typeof(string),
            "Object" => typeof(object),
            "SByte" => typeof(sbyte),
            "Byte" => typeof(byte),
            "Int16" => typeof(short),
            "UInt16" => typeof(ushort),
            "Int32" => typeof(int),
            "UInt32" => typeof(uint),
            "Int64" => typeof(long),
            "UInt64" => typeof(ulong),
            "Single" => typeof(float),
            "Double" => typeof(double),
            "Decimal" => typeof(decimal),
            "Guid" => typeof(Guid),
            "DateTime" => typeof(DateTime),
            "DateTimeOffset" => typeof(DateTimeOffset),
            "TimeSpan" => typeof(TimeSpan),
            "Math" => typeof(Math),
            "Half" or "Int128" or "UInt128" or "DateOnly" or "TimeOnly" or "MathF" => typeof(object).Assembly.GetType("System." + type.MetadataName),
            _ => type.TypeKind == TypeKind.Enum ? typeof(object).Assembly.GetType("System." + type.MetadataName) : null,
        };
    }

    private Optional<object?> ConvertValue(object? value, ITypeSymbol? target, bool isChecked)
    {
        var type = RuntimeType(target);
        if (type is null)
            return default;
        if (value is null)
            return type.IsValueType && Nullable.GetUnderlyingType(type) is null ? default : new Optional<object?>(null);
        if (type.IsInstanceOfType(value))
            return Bounded(value);
        // Nonconstant floating conversions can depend on the target runtime's overflow behavior.
        if (value is float or double)
            return Unknown(FormatStringValueReason.UnsupportedOperation);
        if (type.IsEnum)
            return Bounded(Enum.ToObject(type, value));
        return Bounded(Interpret(isChecked ? Expression.ConvertChecked(Expression.Constant(value), type)
            : Expression.Convert(Expression.Constant(value), type)));
    }

    private static Expression? Typed(object? value, Type? type)
        => type is null ? null : Expression.Convert(Expression.Constant(value), type);

    private static object? Interpret(Expression expression)
        => Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile(preferInterpretation: true)();

    private Optional<object?> Bounded(object? value)
    {
        if (value is DateTime { Kind: DateTimeKind.Local })
            return Unknown(FormatStringValueReason.UnsupportedOperation);
        if (value is string { Length: > StringLimit } || value is BigInteger integer && integer.ToByteArray().Length > IntegerByteLimit)
            return Unknown(FormatStringValueReason.WorkLimit);
        // Roslyn enum constants use the underlying numeric value, including for format X.
        return new Optional<object?>(value is Enum enumeration
            ? Convert.ChangeType(enumeration, Enum.GetUnderlyingType(enumeration.GetType()), CultureInfo.InvariantCulture) : value);
    }
}
