// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal static partial class CSharpOperationFactory
    {
        private static Optional<object> ConvertToOptional(ConstantValue value)
        {
            return value != null ? new Optional<object>(value.Value) : default(Optional<object>);
        }

        private static ImmutableArray<IOperation> ToStatements(BoundStatement statement)
        {
            BoundStatementList statementList = statement as BoundStatementList;
            if (statementList != null)
            {
                return statementList.Statements.SelectAsArray(n => Create(n));
            }
            else if (statement == null)
            {
                return ImmutableArray<IOperation>.Empty;
            }

            return ImmutableArray.Create<IOperation>(Create(statement));
        }

        private static readonly ConditionalWeakTable<BoundIncrementOperator, ILiteralExpression> s_incrementValueMappings = new ConditionalWeakTable<BoundIncrementOperator, ILiteralExpression>();

        private static ILiteralExpression CreateIncrementOneLiteralExpression(BoundIncrementOperator boundIncrementOperator)
        {
            return s_incrementValueMappings.GetValue(boundIncrementOperator, (increment) =>
            {
                string text = increment.Syntax.ToString();
                bool isInvalid = false;
                SyntaxNode syntax = increment.Syntax;
                ITypeSymbol type = increment.Type;
                Optional<object> constantValue = ConvertToOptional(Semantics.Expression.SynthesizeNumeric(increment.Type, 1));
                return new LiteralExpression(text, isInvalid, syntax, type, constantValue);
            });
        }

        private static readonly ConditionalWeakTable<BoundExpression, IArgument> s_argumentMappings = new ConditionalWeakTable<BoundExpression, IArgument>();

        private static IArgument DeriveArgument(
            int parameterIndex,
            int argumentIndex,
            ImmutableArray<BoundExpression> boundArguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            ImmutableArray<CSharp.Symbols.ParameterSymbol> parameters,
            SyntaxNode invocationSyntax)
        {
            if ((uint)argumentIndex >= (uint)boundArguments.Length)
            {
                // Check for an omitted argument that becomes an empty params array.
                if (parameters.Length > 0)
                {
                    IParameterSymbol lastParameter = parameters[parameters.Length - 1];
                    if (lastParameter.IsParams)
                    {
                        var value = CreateParamArray(lastParameter, boundArguments, argumentIndex, invocationSyntax);
                        return new Argument(
                            argumentKind: ArgumentKind.ParamArray,
                            parameter: lastParameter,
                            value: value,
                            inConversion: null,
                            outConversion: null,
                            isInvalid: lastParameter == null || value.IsInvalid,
                            syntax: value.Syntax,
                            type: null,
                            constantValue: default(Optional<object>));
                    }
                }

                // There is no supplied argument and there is no params parameter. Any action is suspect at this point.
                var invalid = OperationFactory.CreateInvalidExpression(invocationSyntax, ImmutableArray<IOperation>.Empty);
                return new Argument(
                    argumentKind: ArgumentKind.Explicit,
                    parameter: null,
                    value: invalid,
                    inConversion: null,
                    outConversion: null,
                    isInvalid: true,
                    syntax: null,
                    type: null,
                    constantValue: default(Optional<object>));
            }

            return s_argumentMappings.GetValue(
                boundArguments[argumentIndex],
                (argument) =>
                {
                    string nameOpt = !argumentNamesOpt.IsDefaultOrEmpty ? argumentNamesOpt[argumentIndex] : null;
                    IParameterSymbol parameterOpt = (uint)parameterIndex < (uint)parameters.Length ? parameters[parameterIndex] : null;

                    if ((object)nameOpt == null)
                    {
                        RefKind refMode = argumentRefKindsOpt.IsDefaultOrEmpty ? RefKind.None : argumentRefKindsOpt[argumentIndex];

                        if (refMode != RefKind.None)
                        {
                            var value = Create(argument);
                            return new Argument(
                                argumentKind: ArgumentKind.Explicit,
                                parameter: parameterOpt,
                                value: value,
                                inConversion: null,
                                outConversion: null,
                                isInvalid: parameterOpt == null || value.IsInvalid,
                                syntax: argument.Syntax,
                                type: null,
                                constantValue: default(Optional<object>));
                        }

                        if (argumentIndex >= parameters.Length - 1 &&
                            parameters.Length > 0 &&
                            parameters[parameters.Length - 1].IsParams &&
                            // An argument that is an array of the appropriate type is not a params argument.
                            (boundArguments.Length > argumentIndex + 1 ||
                             ((object)argument.Type != null && // If argument type is null, we are in an error scenario and cannot tell if it is a param array, or not. 
                              (argument.Type.TypeKind != TypeKind.Array ||
                              !argument.Type.Equals((CSharp.Symbols.TypeSymbol)parameters[parameters.Length - 1].Type, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds)))))
                        {
                            var parameter = parameters[parameters.Length - 1];
                            var value = CreateParamArray(parameter, boundArguments, argumentIndex, invocationSyntax);

                            return new Argument(
                                argumentKind: ArgumentKind.ParamArray,
                                parameter: parameter,
                                value: value,
                                inConversion: null,
                                outConversion: null,
                                isInvalid: parameter == null || value.IsInvalid,
                                syntax: value.Syntax,
                                type: null,
                                constantValue: default(Optional<object>));
                        }
                        else
                        {
                            var value = Create(argument);
                            return new Argument(
                                argumentKind: ArgumentKind.Explicit,
                                parameter: parameterOpt,
                                value: value,
                                inConversion: null,
                                outConversion: null,
                                isInvalid: parameterOpt == null || value.IsInvalid,
                                syntax: value.Syntax,
                                type: null,
                                constantValue: default(Optional<object>));
                        }
                    }

                    var operation = Create(argument);
                    return new Argument(
                        argumentKind: ArgumentKind.Explicit,
                        parameter: parameterOpt,
                        value: operation,
                        inConversion: null,
                        outConversion: null,
                        isInvalid: parameterOpt == null || operation.IsInvalid,
                        syntax: operation.Syntax,
                        type: null,
                        constantValue: default(Optional<object>));
                });
        }

        private static IOperation CreateParamArray(IParameterSymbol parameter, ImmutableArray<BoundExpression> boundArguments, int firstArgumentElementIndex, SyntaxNode invocationSyntax)
        {
            if (parameter.Type.TypeKind == TypeKind.Array)
            {
                IArrayTypeSymbol arrayType = (IArrayTypeSymbol)parameter.Type;
                ArrayBuilder<IOperation> builder = ArrayBuilder<IOperation>.GetInstance(boundArguments.Length - firstArgumentElementIndex);

                for (int index = firstArgumentElementIndex; index < boundArguments.Length; index++)
                {
                    builder.Add(Create(boundArguments[index]));
                }

                var paramArrayArguments = builder.ToImmutableAndFree();

                // Use the invocation syntax node if there is no actual syntax available for the argument (because the paramarray is empty.)
                return OperationFactory.CreateArrayCreationExpression(arrayType, paramArrayArguments, paramArrayArguments.Length > 0 ? paramArrayArguments[0].Syntax : invocationSyntax);
            }

            return OperationFactory.CreateInvalidExpression(invocationSyntax, ImmutableArray<IOperation>.Empty);
        }

        private static IOperation GetDelegateCreationInstance(BoundDelegateCreationExpression expression)
        {
            BoundMethodGroup methodGroup = expression.Argument as BoundMethodGroup;
            if (methodGroup != null)
            {
                return Create(methodGroup.InstanceOpt);
            }

            return null;
        }

        private static readonly ConditionalWeakTable<BoundObjectCreationExpression, object> s_memberInitializersMappings =
            new ConditionalWeakTable<BoundObjectCreationExpression, object>();

        private static ImmutableArray<ISymbolInitializer> GetObjectCreationMemberInitializers(BoundObjectCreationExpression expression)
        {
            return (ImmutableArray<ISymbolInitializer>)s_memberInitializersMappings.GetValue(expression,
                objectCreationExpression =>
                {
                    var objectInitializerExpression = expression.InitializerExpressionOpt as BoundObjectInitializerExpression;
                    if (objectInitializerExpression != null)
                    {
                        var builder = ArrayBuilder<ISymbolInitializer>.GetInstance(objectInitializerExpression.Initializers.Length);
                        foreach (var memberAssignment in objectInitializerExpression.Initializers)
                        {
                            var assignment = memberAssignment as BoundAssignmentOperator;
                            var leftSymbol = (assignment?.Left as BoundObjectInitializerMember)?.MemberSymbol;

                            if ((object)leftSymbol == null)
                            {
                                continue;
                            }

                            switch (leftSymbol.Kind)
                            {
                                case SymbolKind.Field:
                                    {
                                        var value = Create(assignment.Right);
                                        builder.Add(new FieldInitializer(
                                            ImmutableArray.Create((IFieldSymbol)leftSymbol),
                                            value,
                                            OperationKind.FieldInitializerInCreation,
                                            value.IsInvalid || leftSymbol == null,
                                            assignment.Syntax,
                                            type: null,
                                            constantValue: default(Optional<object>)));
                                        break;
                                    }
                                case SymbolKind.Property:
                                    {
                                        var value = Create(assignment.Right);
                                        builder.Add(new PropertyInitializer(
                                            (IPropertySymbol)leftSymbol,
                                            value,
                                            OperationKind.PropertyInitializerInCreation,
                                            value.IsInvalid || leftSymbol == null,
                                            assignment.Syntax,
                                            type: null,
                                            constantValue: default(Optional<object>)));
                                        break;
                                    }
                            }
                        }

                        return builder.ToImmutableAndFree();
                    }

                    return ImmutableArray<ISymbolInitializer>.Empty;
                });
        }

        private static ConversionKind GetConversionKind(CSharp.ConversionKind kind)
        {
            switch (kind)
            {
                case CSharp.ConversionKind.ExplicitUserDefined:
                case CSharp.ConversionKind.ImplicitUserDefined:
                    return Semantics.ConversionKind.OperatorMethod;

                case CSharp.ConversionKind.ExplicitReference:
                case CSharp.ConversionKind.ImplicitReference:
                case CSharp.ConversionKind.Boxing:
                case CSharp.ConversionKind.Unboxing:
                case CSharp.ConversionKind.Identity:
                    return Semantics.ConversionKind.Cast;

                case CSharp.ConversionKind.AnonymousFunction:
                case CSharp.ConversionKind.ExplicitDynamic:
                case CSharp.ConversionKind.ImplicitDynamic:
                case CSharp.ConversionKind.ExplicitEnumeration:
                case CSharp.ConversionKind.ImplicitEnumeration:
                case CSharp.ConversionKind.ImplicitThrow:
                case CSharp.ConversionKind.ImplicitTupleLiteral:
                case CSharp.ConversionKind.ImplicitTuple:
                case CSharp.ConversionKind.ExplicitTupleLiteral:
                case CSharp.ConversionKind.ExplicitTuple:
                case CSharp.ConversionKind.ExplicitNullable:
                case CSharp.ConversionKind.ImplicitNullable:
                case CSharp.ConversionKind.ExplicitNumeric:
                case CSharp.ConversionKind.ImplicitNumeric:
                case CSharp.ConversionKind.ImplicitConstant:
                case CSharp.ConversionKind.IntegerToPointer:
                case CSharp.ConversionKind.IntPtr:
                case CSharp.ConversionKind.DefaultOrNullLiteral:
                case CSharp.ConversionKind.NullToPointer:
                case CSharp.ConversionKind.PointerToInteger:
                case CSharp.ConversionKind.PointerToPointer:
                case CSharp.ConversionKind.PointerToVoid:
                    return Semantics.ConversionKind.CSharp;

                default:
                    return Semantics.ConversionKind.Invalid;
            }
        }

        private static ITypeSymbol GetArrayCreationElementType(BoundArrayCreation creation)
        {
            IArrayTypeSymbol arrayType = creation.Type as IArrayTypeSymbol;
            if ((object)arrayType != null)
            {
                return arrayType.ElementType;
            }

            return null;
        }

        private static readonly ConditionalWeakTable<BoundBlock, object> s_blockStatementsMappings =
            new ConditionalWeakTable<BoundBlock, object>();

        private static ImmutableArray<IOperation> GetBlockStatement(BoundBlock block)
        {
            // This is to filter out operations of kind None.
            return (ImmutableArray<IOperation>)s_blockStatementsMappings.GetValue(block,
                blockStatement =>
                {
                    return blockStatement.Statements.Select(s => Create(s)).Where(s => s.Kind != OperationKind.None).ToImmutableArray();
                });
        }

        private static readonly ConditionalWeakTable<BoundSwitchStatement, object> s_switchSectionsMappings =
            new ConditionalWeakTable<BoundSwitchStatement, object>();

        private static ImmutableArray<ISwitchCase> GetSwitchStatementCases(BoundSwitchStatement statement)
        {
            return (ImmutableArray<ISwitchCase>)s_switchSectionsMappings.GetValue(statement,
                switchStatement =>
                {
                    return switchStatement.SwitchSections.SelectAsArray(switchSection =>
                    {
                        var clauses = switchSection.SwitchLabels.SelectAsArray(s => (ICaseClause)Create(s));
                        var body = switchSection.Statements.SelectAsArray(s => Create(s));

                        return (ISwitchCase)new SwitchCase(clauses, body, switchSection.HasErrors, switchSection.Syntax, type: null, constantValue: default(Optional<object>));
                    });
                });
        }

        private static BinaryOperationKind GetLabelEqualityKind(BoundSwitchLabel label)
        {
            BoundExpression caseValue = label.ExpressionOpt;
            if (caseValue != null)
            {
                switch (caseValue.Type.SpecialType)
                {
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int16:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Char:
                        return BinaryOperationKind.IntegerEquals;

                    case SpecialType.System_Boolean:
                        return BinaryOperationKind.BooleanEquals;

                    case SpecialType.System_String:
                        return BinaryOperationKind.StringEquals;
                }

                if (caseValue.Type.TypeKind == TypeKind.Enum)
                {
                    return BinaryOperationKind.EnumEquals;
                }

                return BinaryOperationKind.Invalid;
            }

            // Return None for `default` case.
            return BinaryOperationKind.None;
        }

        private static readonly ConditionalWeakTable<BoundLocalDeclaration, object> s_variablesMappings =
            new ConditionalWeakTable<BoundLocalDeclaration, object>();

        private static ImmutableArray<IVariableDeclaration> GetVariableDeclarationStatementVariables(BoundLocalDeclaration decl)
        {
            return (ImmutableArray<IVariableDeclaration>)s_variablesMappings.GetValue(decl,
                declaration => ImmutableArray.Create<IVariableDeclaration>(
                    OperationFactory.CreateVariableDeclaration(declaration.LocalSymbol, Create(declaration.InitializerOpt), declaration.Syntax)));
        }

        private static readonly ConditionalWeakTable<BoundMultipleLocalDeclarations, object> s_multiVariablesMappings =
            new ConditionalWeakTable<BoundMultipleLocalDeclarations, object>();

        private static ImmutableArray<IVariableDeclaration> GetVariableDeclarationStatementVariables(BoundMultipleLocalDeclarations decl)
        {
            return (ImmutableArray<IVariableDeclaration>)s_multiVariablesMappings.GetValue(decl,
                multipleDeclarations =>
                    multipleDeclarations.LocalDeclarations.SelectAsArray(declaration =>
                        OperationFactory.CreateVariableDeclaration(declaration.LocalSymbol, Create(declaration.InitializerOpt), declaration.Syntax)));
        }

        // TODO: We need to reuse the logic in `LocalRewriter.MakeArguments` instead of using private implementation. 
        //       Also. this implementation here was for the (now removed) API `ArgumentsInParameter`, which doesn't fulfill
        //       the contract of `ArgumentsInEvaluationOrder` plus it doesn't handle various scenarios correctly even for parameter order, 
        //       e.g. default arguments, erroneous code, etc. 
        //       https://github.com/dotnet/roslyn/issues/18549
        internal static ImmutableArray<IArgument> DeriveArguments(
            ImmutableArray<BoundExpression> boundArguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<int> argumentsToParametersOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            ImmutableArray<CSharp.Symbols.ParameterSymbol> parameters,
            SyntaxNode invocationSyntax)
        {
            ArrayBuilder<IArgument> arguments = ArrayBuilder<IArgument>.GetInstance(boundArguments.Length);
            for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                int argumentIndex = -1;
                if (argumentsToParametersOpt.IsDefault)
                {
                    argumentIndex = parameterIndex;
                }
                else
                {
                    argumentIndex = argumentsToParametersOpt.IndexOf(parameterIndex);
                }

                if ((uint)argumentIndex >= (uint)boundArguments.Length)
                {
                    // No argument has been supplied for the parameter at `parameterIndex`:
                    // 1. `argumentIndex == -1' when the arguments are specified out of parameter order, and no argument is provided for parameter corresponding to `parameters[parameterIndex]`.
                    // 2. `argumentIndex >= boundArguments.Length` when the arguments are specified in parameter order, and no argument is provided at `parameterIndex`.

                    var parameter = parameters[parameterIndex];
                    if (parameter.HasExplicitDefaultValue)
                    {
                        // The parameter is optional with a default value.
                        arguments.Add(new Argument(
                            ArgumentKind.DefaultValue,
                            parameter,
                            OperationFactory.CreateLiteralExpression(parameter.ExplicitDefaultConstantValue, parameter.Type, invocationSyntax),
                            inConversion: null,
                            outConversion: null,
                            isInvalid: parameter.ExplicitDefaultConstantValue.IsBad,
                            syntax: invocationSyntax,
                            type: null,
                            constantValue: default(Optional<object>)));
                    }
                    else
                    {
                        // If the invocation is semantically valid, the parameter will be a params array and an empty array will be provided.
                        // If the argument is otherwise omitted for a parameter with no default value, the invocation is not valid and a null argument will be provided.
                        arguments.Add(DeriveArgument(parameterIndex, boundArguments.Length, boundArguments, argumentNamesOpt, argumentRefKindsOpt, parameters, invocationSyntax));
                    }
                }
                else
                {
                    arguments.Add(DeriveArgument(parameterIndex, argumentIndex, boundArguments, argumentNamesOpt, argumentRefKindsOpt, parameters, invocationSyntax));
                }
            }

            return arguments.ToImmutableAndFree();
        }

        internal class Helper
        {
            internal static BinaryOperationKind DeriveBinaryOperationKind(UnaryOperationKind incrementKind)
            {
                switch (incrementKind)
                {
                    case UnaryOperationKind.OperatorMethodPostfixIncrement:
                    case UnaryOperationKind.OperatorMethodPrefixIncrement:
                        return BinaryOperationKind.OperatorMethodAdd;
                    case UnaryOperationKind.OperatorMethodPostfixDecrement:
                    case UnaryOperationKind.OperatorMethodPrefixDecrement:
                        return BinaryOperationKind.OperatorMethodSubtract;
                    case UnaryOperationKind.IntegerPostfixIncrement:
                    case UnaryOperationKind.IntegerPrefixIncrement:
                        return BinaryOperationKind.IntegerAdd;
                    case UnaryOperationKind.IntegerPostfixDecrement:
                    case UnaryOperationKind.IntegerPrefixDecrement:
                        return BinaryOperationKind.IntegerSubtract;
                    case UnaryOperationKind.UnsignedPostfixIncrement:
                    case UnaryOperationKind.UnsignedPrefixIncrement:
                        return BinaryOperationKind.UnsignedAdd;
                    case UnaryOperationKind.UnsignedPostfixDecrement:
                    case UnaryOperationKind.UnsignedPrefixDecrement:
                        return BinaryOperationKind.UnsignedSubtract;
                    case UnaryOperationKind.FloatingPostfixIncrement:
                    case UnaryOperationKind.FloatingPrefixIncrement:
                        return BinaryOperationKind.FloatingAdd;
                    case UnaryOperationKind.FloatingPostfixDecrement:
                    case UnaryOperationKind.FloatingPrefixDecrement:
                        return BinaryOperationKind.FloatingSubtract;
                    case UnaryOperationKind.DecimalPostfixIncrement:
                    case UnaryOperationKind.DecimalPrefixIncrement:
                        return BinaryOperationKind.DecimalAdd;
                    case UnaryOperationKind.DecimalPostfixDecrement:
                    case UnaryOperationKind.DecimalPrefixDecrement:
                        return BinaryOperationKind.DecimalSubtract;
                    case UnaryOperationKind.EnumPostfixIncrement:
                    case UnaryOperationKind.EnumPrefixIncrement:
                        return BinaryOperationKind.EnumAdd;
                    case UnaryOperationKind.EnumPostfixDecrement:
                    case UnaryOperationKind.EnumPrefixDecrement:
                        return BinaryOperationKind.EnumSubtract;
                    case UnaryOperationKind.PointerPostfixIncrement:
                    case UnaryOperationKind.PointerPrefixIncrement:
                        return BinaryOperationKind.PointerIntegerAdd;
                    case UnaryOperationKind.PointerPostfixDecrement:
                    case UnaryOperationKind.PointerPrefixDecrement:
                        return BinaryOperationKind.PointerIntegerSubtract;
                    case UnaryOperationKind.DynamicPostfixIncrement:
                    case UnaryOperationKind.DynamicPrefixIncrement:
                        return BinaryOperationKind.DynamicAdd;
                    case UnaryOperationKind.DynamicPostfixDecrement:
                    case UnaryOperationKind.DynamicPrefixDecrement:
                        return BinaryOperationKind.DynamicSubtract;

                    default:
                        return BinaryOperationKind.Invalid;
                }
            }

            internal static UnaryOperationKind DeriveUnaryOperationKind(UnaryOperatorKind operatorKind)
            {
                switch (operatorKind & UnaryOperatorKind.OpMask)
                {
                    case UnaryOperatorKind.PostfixIncrement:
                        switch (operatorKind & UnaryOperatorKind.TypeMask)
                        {
                            case UnaryOperatorKind.Int:
                            case UnaryOperatorKind.Long:
                            case UnaryOperatorKind.SByte:
                            case UnaryOperatorKind.Short:
                                return UnaryOperationKind.IntegerPostfixIncrement;
                            case UnaryOperatorKind.UInt:
                            case UnaryOperatorKind.ULong:
                            case UnaryOperatorKind.Byte:
                            case UnaryOperatorKind.UShort:
                            case UnaryOperatorKind.Char:
                                return UnaryOperationKind.UnsignedPostfixIncrement;
                            case UnaryOperatorKind.Float:
                            case UnaryOperatorKind.Double:
                                return UnaryOperationKind.FloatingPostfixIncrement;
                            case UnaryOperatorKind.Decimal:
                                return UnaryOperationKind.DecimalPostfixIncrement;
                            case UnaryOperatorKind.Enum:
                                return UnaryOperationKind.EnumPostfixIncrement;
                            case UnaryOperatorKind.Pointer:
                                return UnaryOperationKind.PointerPostfixIncrement;
                            case UnaryOperatorKind.Dynamic:
                                return UnaryOperationKind.DynamicPostfixIncrement;
                            case UnaryOperatorKind.UserDefined:
                                return UnaryOperationKind.OperatorMethodPostfixIncrement;
                        }

                        break;

                    case UnaryOperatorKind.PostfixDecrement:
                        switch (operatorKind & UnaryOperatorKind.TypeMask)
                        {
                            case UnaryOperatorKind.Int:
                            case UnaryOperatorKind.Long:
                            case UnaryOperatorKind.SByte:
                            case UnaryOperatorKind.Short:
                                return UnaryOperationKind.IntegerPostfixDecrement;
                            case UnaryOperatorKind.UInt:
                            case UnaryOperatorKind.ULong:
                            case UnaryOperatorKind.Byte:
                            case UnaryOperatorKind.UShort:
                            case UnaryOperatorKind.Char:
                                return UnaryOperationKind.UnsignedPostfixDecrement;
                            case UnaryOperatorKind.Float:
                            case UnaryOperatorKind.Double:
                                return UnaryOperationKind.FloatingPostfixDecrement;
                            case UnaryOperatorKind.Decimal:
                                return UnaryOperationKind.DecimalPostfixDecrement;
                            case UnaryOperatorKind.Enum:
                                return UnaryOperationKind.EnumPostfixDecrement;
                            case UnaryOperatorKind.Pointer:
                                return UnaryOperationKind.PointerPostfixIncrement;
                            case UnaryOperatorKind.Dynamic:
                                return UnaryOperationKind.DynamicPostfixDecrement;
                            case UnaryOperatorKind.UserDefined:
                                return UnaryOperationKind.OperatorMethodPostfixDecrement;
                        }

                        break;

                    case UnaryOperatorKind.PrefixIncrement:
                        switch (operatorKind & UnaryOperatorKind.TypeMask)
                        {
                            case UnaryOperatorKind.Int:
                            case UnaryOperatorKind.Long:
                            case UnaryOperatorKind.SByte:
                            case UnaryOperatorKind.Short:
                                return UnaryOperationKind.IntegerPrefixIncrement;
                            case UnaryOperatorKind.UInt:
                            case UnaryOperatorKind.ULong:
                            case UnaryOperatorKind.Byte:
                            case UnaryOperatorKind.UShort:
                            case UnaryOperatorKind.Char:
                                return UnaryOperationKind.UnsignedPrefixIncrement;
                            case UnaryOperatorKind.Float:
                            case UnaryOperatorKind.Double:
                                return UnaryOperationKind.FloatingPrefixIncrement;
                            case UnaryOperatorKind.Decimal:
                                return UnaryOperationKind.DecimalPrefixIncrement;
                            case UnaryOperatorKind.Enum:
                                return UnaryOperationKind.EnumPrefixIncrement;
                            case UnaryOperatorKind.Pointer:
                                return UnaryOperationKind.PointerPrefixIncrement;
                            case UnaryOperatorKind.Dynamic:
                                return UnaryOperationKind.DynamicPrefixIncrement;
                            case UnaryOperatorKind.UserDefined:
                                return UnaryOperationKind.OperatorMethodPrefixIncrement;
                        }

                        break;

                    case UnaryOperatorKind.PrefixDecrement:
                        switch (operatorKind & UnaryOperatorKind.TypeMask)
                        {
                            case UnaryOperatorKind.Int:
                            case UnaryOperatorKind.Long:
                            case UnaryOperatorKind.SByte:
                            case UnaryOperatorKind.Short:
                                return UnaryOperationKind.IntegerPrefixDecrement;
                            case UnaryOperatorKind.UInt:
                            case UnaryOperatorKind.ULong:
                            case UnaryOperatorKind.Byte:
                            case UnaryOperatorKind.UShort:
                            case UnaryOperatorKind.Char:
                                return UnaryOperationKind.UnsignedPrefixDecrement;
                            case UnaryOperatorKind.Float:
                            case UnaryOperatorKind.Double:
                                return UnaryOperationKind.FloatingPrefixDecrement;
                            case UnaryOperatorKind.Decimal:
                                return UnaryOperationKind.DecimalPrefixDecrement;
                            case UnaryOperatorKind.Enum:
                                return UnaryOperationKind.EnumPrefixDecrement;
                            case UnaryOperatorKind.Pointer:
                                return UnaryOperationKind.PointerPrefixIncrement;
                            case UnaryOperatorKind.Dynamic:
                                return UnaryOperationKind.DynamicPrefixDecrement;
                            case UnaryOperatorKind.UserDefined:
                                return UnaryOperationKind.OperatorMethodPrefixDecrement;
                        }

                        break;

                    case UnaryOperatorKind.UnaryPlus:
                        switch (operatorKind & UnaryOperatorKind.TypeMask)
                        {
                            case UnaryOperatorKind.Int:
                            case UnaryOperatorKind.UInt:
                            case UnaryOperatorKind.Long:
                            case UnaryOperatorKind.ULong:
                                return UnaryOperationKind.IntegerPlus;
                            case UnaryOperatorKind.Float:
                            case UnaryOperatorKind.Double:
                                return UnaryOperationKind.FloatingPlus;
                            case UnaryOperatorKind.Decimal:
                                return UnaryOperationKind.DecimalPlus;
                            case UnaryOperatorKind.Dynamic:
                                return UnaryOperationKind.DynamicPlus;
                            case UnaryOperatorKind.UserDefined:
                                return UnaryOperationKind.OperatorMethodPlus;
                        }

                        break;

                    case UnaryOperatorKind.UnaryMinus:
                        switch (operatorKind & UnaryOperatorKind.TypeMask)
                        {
                            case UnaryOperatorKind.Int:
                            case UnaryOperatorKind.UInt:
                            case UnaryOperatorKind.Long:
                            case UnaryOperatorKind.ULong:
                                return UnaryOperationKind.IntegerMinus;
                            case UnaryOperatorKind.Float:
                            case UnaryOperatorKind.Double:
                                return UnaryOperationKind.FloatingMinus;
                            case UnaryOperatorKind.Decimal:
                                return UnaryOperationKind.DecimalMinus;
                            case UnaryOperatorKind.Dynamic:
                                return UnaryOperationKind.DynamicMinus;
                            case UnaryOperatorKind.UserDefined:
                                return UnaryOperationKind.OperatorMethodMinus;
                        }

                        break;

                    case UnaryOperatorKind.LogicalNegation:
                        switch (operatorKind & UnaryOperatorKind.TypeMask)
                        {
                            case UnaryOperatorKind.Bool:
                                return UnaryOperationKind.BooleanLogicalNot;
                            case UnaryOperatorKind.Dynamic:
                                return UnaryOperationKind.DynamicLogicalNot;
                            case UnaryOperatorKind.UserDefined:
                                return UnaryOperationKind.OperatorMethodLogicalNot;
                        }

                        break;
                    case UnaryOperatorKind.BitwiseComplement:
                        switch (operatorKind & UnaryOperatorKind.TypeMask)
                        {
                            case UnaryOperatorKind.Int:
                            case UnaryOperatorKind.UInt:
                            case UnaryOperatorKind.Long:
                            case UnaryOperatorKind.ULong:
                                return UnaryOperationKind.IntegerBitwiseNegation;
                            case UnaryOperatorKind.Bool:
                                return UnaryOperationKind.BooleanBitwiseNegation;
                            case UnaryOperatorKind.Dynamic:
                                return UnaryOperationKind.DynamicBitwiseNegation;
                            case UnaryOperatorKind.UserDefined:
                                return UnaryOperationKind.OperatorMethodBitwiseNegation;
                        }

                        break;

                    case UnaryOperatorKind.True:
                        switch (operatorKind & UnaryOperatorKind.TypeMask)
                        {
                            case UnaryOperatorKind.Dynamic:
                                return UnaryOperationKind.DynamicTrue;
                            case UnaryOperatorKind.UserDefined:
                                return UnaryOperationKind.OperatorMethodTrue;
                        }

                        break;

                    case UnaryOperatorKind.False:
                        switch (operatorKind & UnaryOperatorKind.TypeMask)
                        {
                            case UnaryOperatorKind.Dynamic:
                                return UnaryOperationKind.DynamicFalse;
                            case UnaryOperatorKind.UserDefined:
                                return UnaryOperationKind.OperatorMethodFalse;
                        }

                        break;
                }

                return UnaryOperationKind.Invalid;
            }

            internal static BinaryOperationKind DeriveBinaryOperationKind(BinaryOperatorKind operatorKind)
            {
                switch (operatorKind & BinaryOperatorKind.OpMask)
                {
                    case BinaryOperatorKind.Addition:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerAdd;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedAdd;
                            case BinaryOperatorKind.Double:
                            case BinaryOperatorKind.Float:
                                return BinaryOperationKind.FloatingAdd;
                            case BinaryOperatorKind.Decimal:
                                return BinaryOperationKind.DecimalAdd;
                            case BinaryOperatorKind.EnumAndUnderlying:
                            case BinaryOperatorKind.UnderlyingAndEnum:
                                return BinaryOperationKind.EnumAdd;
                            case BinaryOperatorKind.PointerAndInt:
                            case BinaryOperatorKind.PointerAndUInt:
                            case BinaryOperatorKind.PointerAndLong:
                            case BinaryOperatorKind.PointerAndULong:
                                return BinaryOperationKind.PointerIntegerAdd;
                            case BinaryOperatorKind.IntAndPointer:
                            case BinaryOperatorKind.UIntAndPointer:
                            case BinaryOperatorKind.LongAndPointer:
                            case BinaryOperatorKind.ULongAndPointer:
                                return BinaryOperationKind.IntegerPointerAdd;
                            case BinaryOperatorKind.Dynamic:
                                return BinaryOperationKind.DynamicAdd;
                            case BinaryOperatorKind.String:
                            case BinaryOperatorKind.StringAndObject:
                            case BinaryOperatorKind.ObjectAndString:
                                return BinaryOperationKind.StringConcatenate;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodAdd;
                        }

                        break;

                    case BinaryOperatorKind.Subtraction:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerSubtract;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedSubtract;
                            case BinaryOperatorKind.Double:
                            case BinaryOperatorKind.Float:
                                return BinaryOperationKind.FloatingSubtract;
                            case BinaryOperatorKind.Decimal:
                                return BinaryOperationKind.DecimalSubtract;
                            case BinaryOperatorKind.EnumAndUnderlying:
                            case BinaryOperatorKind.UnderlyingAndEnum:
                                return BinaryOperationKind.EnumSubtract;
                            case BinaryOperatorKind.PointerAndInt:
                            case BinaryOperatorKind.PointerAndUInt:
                            case BinaryOperatorKind.PointerAndLong:
                            case BinaryOperatorKind.PointerAndULong:
                                return BinaryOperationKind.PointerIntegerSubtract;
                            case BinaryOperatorKind.Pointer:
                                return BinaryOperationKind.PointerSubtract;
                            case BinaryOperatorKind.Dynamic:
                                return BinaryOperationKind.DynamicSubtract;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodSubtract;
                        }

                        break;

                    case BinaryOperatorKind.Multiplication:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerMultiply;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedMultiply;
                            case BinaryOperatorKind.Double:
                            case BinaryOperatorKind.Float:
                                return BinaryOperationKind.FloatingMultiply;
                            case BinaryOperatorKind.Decimal:
                                return BinaryOperationKind.DecimalMultiply;
                            case BinaryOperatorKind.Dynamic:
                                return BinaryOperationKind.DynamicMultiply;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodMultiply;
                        }

                        break;

                    case BinaryOperatorKind.Division:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerDivide;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedDivide;
                            case BinaryOperatorKind.Double:
                            case BinaryOperatorKind.Float:
                                return BinaryOperationKind.FloatingDivide;
                            case BinaryOperatorKind.Decimal:
                                return BinaryOperationKind.DecimalDivide;
                            case BinaryOperatorKind.Dynamic:
                                return BinaryOperationKind.DynamicDivide;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodDivide;
                        }

                        break;

                    case BinaryOperatorKind.Remainder:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerRemainder;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedRemainder;
                            case BinaryOperatorKind.Double:
                            case BinaryOperatorKind.Float:
                                return BinaryOperationKind.FloatingRemainder;
                            case BinaryOperatorKind.Dynamic:
                                return BinaryOperationKind.DynamicRemainder;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodRemainder;
                        }

                        break;

                    case BinaryOperatorKind.LeftShift:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerLeftShift;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedLeftShift;
                            case BinaryOperatorKind.Dynamic:
                                return BinaryOperationKind.DynamicLeftShift;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodLeftShift;
                        }

                        break;

                    case BinaryOperatorKind.RightShift:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerRightShift;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedRightShift;
                            case BinaryOperatorKind.Dynamic:
                                return BinaryOperationKind.DynamicRightShift;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodRightShift;
                        }

                        break;

                    case BinaryOperatorKind.And:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerAnd;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedAnd;
                            case BinaryOperatorKind.Bool:
                                if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                                {
                                    return BinaryOperationKind.BooleanConditionalAnd;
                                }

                                return BinaryOperationKind.BooleanAnd;
                            case BinaryOperatorKind.Enum:
                                return BinaryOperationKind.EnumAnd;
                            case BinaryOperatorKind.Dynamic:
                                return BinaryOperationKind.DynamicAnd;
                            case BinaryOperatorKind.UserDefined:
                                if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                                {
                                    return BinaryOperationKind.OperatorMethodConditionalAnd;
                                }

                                return BinaryOperationKind.OperatorMethodAnd;
                        }

                        break;

                    case BinaryOperatorKind.Or:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerOr;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedOr;
                            case BinaryOperatorKind.Bool:
                                if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                                {
                                    return BinaryOperationKind.BooleanConditionalOr;
                                }

                                return BinaryOperationKind.BooleanOr;
                            case BinaryOperatorKind.Enum:
                                return BinaryOperationKind.EnumOr;
                            case BinaryOperatorKind.Dynamic:
                                return BinaryOperationKind.DynamicOr;
                            case BinaryOperatorKind.UserDefined:
                                if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                                {
                                    return BinaryOperationKind.OperatorMethodConditionalOr;
                                }

                                return BinaryOperationKind.OperatorMethodOr;
                        }

                        break;

                    case BinaryOperatorKind.Xor:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerExclusiveOr;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedExclusiveOr;
                            case BinaryOperatorKind.Bool:
                                return BinaryOperationKind.BooleanExclusiveOr;
                            case BinaryOperatorKind.Enum:
                                return BinaryOperationKind.EnumExclusiveOr;
                            case BinaryOperatorKind.Dynamic:
                                return BinaryOperationKind.DynamicExclusiveOr;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodExclusiveOr;
                        }

                        break;

                    case BinaryOperatorKind.LessThan:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerLessThan;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedLessThan;
                            case BinaryOperatorKind.Float:
                            case BinaryOperatorKind.Double:
                                return BinaryOperationKind.FloatingLessThan;
                            case BinaryOperatorKind.Decimal:
                                return BinaryOperationKind.DecimalLessThan;
                            case BinaryOperatorKind.Pointer:
                                return BinaryOperationKind.PointerLessThan;
                            case BinaryOperatorKind.Enum:
                                return BinaryOperationKind.EnumLessThan;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodLessThan;
                        }

                        break;

                    case BinaryOperatorKind.LessThanOrEqual:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerLessThanOrEqual;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedLessThanOrEqual;
                            case BinaryOperatorKind.Float:
                            case BinaryOperatorKind.Double:
                                return BinaryOperationKind.FloatingLessThanOrEqual;
                            case BinaryOperatorKind.Decimal:
                                return BinaryOperationKind.DecimalLessThanOrEqual;
                            case BinaryOperatorKind.Pointer:
                                return BinaryOperationKind.PointerLessThanOrEqual;
                            case BinaryOperatorKind.Enum:
                                return BinaryOperationKind.EnumLessThanOrEqual;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodLessThanOrEqual;
                        }

                        break;

                    case BinaryOperatorKind.Equal:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.IntegerEquals;
                            case BinaryOperatorKind.Float:
                            case BinaryOperatorKind.Double:
                                return BinaryOperationKind.FloatingEquals;
                            case BinaryOperatorKind.Decimal:
                                return BinaryOperationKind.DecimalEquals;
                            case BinaryOperatorKind.Pointer:
                                return BinaryOperationKind.PointerEquals;
                            case BinaryOperatorKind.Enum:
                                return BinaryOperationKind.EnumEquals;
                            case BinaryOperatorKind.Bool:
                                return BinaryOperationKind.BooleanEquals;
                            case BinaryOperatorKind.String:
                                return BinaryOperationKind.StringEquals;
                            case BinaryOperatorKind.Object:
                                return BinaryOperationKind.ObjectEquals;
                            case BinaryOperatorKind.Delegate:
                                return BinaryOperationKind.DelegateEquals;
                            case BinaryOperatorKind.NullableNull:
                                return BinaryOperationKind.NullableEquals;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodEquals;
                        }

                        break;

                    case BinaryOperatorKind.NotEqual:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.IntegerNotEquals;
                            case BinaryOperatorKind.Float:
                            case BinaryOperatorKind.Double:
                                return BinaryOperationKind.FloatingNotEquals;
                            case BinaryOperatorKind.Decimal:
                                return BinaryOperationKind.DecimalNotEquals;
                            case BinaryOperatorKind.Pointer:
                                return BinaryOperationKind.PointerNotEquals;
                            case BinaryOperatorKind.Enum:
                                return BinaryOperationKind.EnumNotEquals;
                            case BinaryOperatorKind.Bool:
                                return BinaryOperationKind.BooleanNotEquals;
                            case BinaryOperatorKind.String:
                                return BinaryOperationKind.StringNotEquals;
                            case BinaryOperatorKind.Object:
                                return BinaryOperationKind.ObjectNotEquals;
                            case BinaryOperatorKind.Delegate:
                                return BinaryOperationKind.DelegateNotEquals;
                            case BinaryOperatorKind.NullableNull:
                                return BinaryOperationKind.NullableNotEquals;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodNotEquals;
                        }

                        break;

                    case BinaryOperatorKind.GreaterThanOrEqual:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerGreaterThanOrEqual;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedGreaterThanOrEqual;
                            case BinaryOperatorKind.Float:
                            case BinaryOperatorKind.Double:
                                return BinaryOperationKind.FloatingGreaterThanOrEqual;
                            case BinaryOperatorKind.Decimal:
                                return BinaryOperationKind.DecimalGreaterThanOrEqual;
                            case BinaryOperatorKind.Pointer:
                                return BinaryOperationKind.PointerGreaterThanOrEqual;
                            case BinaryOperatorKind.Enum:
                                return BinaryOperationKind.EnumGreaterThanOrEqual;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodGreaterThanOrEqual;
                        }

                        break;

                    case BinaryOperatorKind.GreaterThan:
                        switch (operatorKind & BinaryOperatorKind.TypeMask)
                        {
                            case BinaryOperatorKind.Int:
                            case BinaryOperatorKind.Long:
                                return BinaryOperationKind.IntegerGreaterThan;
                            case BinaryOperatorKind.UInt:
                            case BinaryOperatorKind.ULong:
                                return BinaryOperationKind.UnsignedGreaterThan;
                            case BinaryOperatorKind.Float:
                            case BinaryOperatorKind.Double:
                                return BinaryOperationKind.FloatingGreaterThan;
                            case BinaryOperatorKind.Decimal:
                                return BinaryOperationKind.DecimalGreaterThan;
                            case BinaryOperatorKind.Pointer:
                                return BinaryOperationKind.PointerGreaterThan;
                            case BinaryOperatorKind.Enum:
                                return BinaryOperationKind.EnumGreaterThan;
                            case BinaryOperatorKind.UserDefined:
                                return BinaryOperationKind.OperatorMethodGreaterThan;
                        }

                        break;
                }

                return BinaryOperationKind.Invalid;
            }
        }
    }
}
