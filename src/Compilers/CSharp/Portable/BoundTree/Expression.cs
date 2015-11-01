// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundExpression : IExpression
    {
        ITypeSymbol IExpression.ResultType => this.Type;

        OperationKind IOperation.Kind => this.ExpressionKind;
       
        object IExpression.ConstantValue => this.ConstantValue?.Value;

        SyntaxNode IOperation.Syntax => this.Syntax;
        
        protected virtual OperationKind ExpressionKind => OperationKind.None;
        // protected abstract OperationKind ExpressionKind { get; }
    }

    internal abstract partial class BoundNode : IOperationSearchable
    {
        IEnumerable<IOperation> IOperationSearchable.Descendants()
        {
            var list = new List<IOperation>();
            new Collector(list).Visit(this);
            list.RemoveAt(0);
            return list;
        }

        IEnumerable<IOperation> IOperationSearchable.DescendantsAndSelf()
        {
            var list = new List<IOperation>();
            new Collector(list).Visit(this);
            return list;
        }

        private class Collector : BoundTreeWalkerWithStackGuard
        {
            private readonly List<IOperation> nodes;

            public Collector(List<IOperation> nodes)
            {
                this.nodes = nodes;
            }

            public override BoundNode Visit(BoundNode node)
            {
                IOperation operation = node as IOperation;
                if (operation != null)
                {
                    this.nodes.Add(operation);
                }

                return base.Visit(node);
            }
        }
    }

    partial class BoundCall : IInvocationExpression
    {
        IMethodSymbol IInvocationExpression.TargetMethod => this.Method;

        IExpression IInvocationExpression.Instance => this.ReceiverOpt;

        bool IInvocationExpression.IsVirtual => (this.Method.IsVirtual || this.Method.IsAbstract || this.Method.IsOverride) && !this.ReceiverOpt.SuppressVirtualCalls;

        ImmutableArray<IArgument> IInvocationExpression.ArgumentsInSourceOrder
        {
            get
            {
                ArrayBuilder<IArgument> sourceOrderArguments = ArrayBuilder<IArgument>.GetInstance(this.Arguments.Length);
                for (int argumentIndex = 0; argumentIndex < this.Arguments.Length; argumentIndex++)
                {
                    sourceOrderArguments.Add(DeriveArgument(this.ArgsToParamsOpt.IsDefault ? argumentIndex : this.ArgsToParamsOpt[argumentIndex], argumentIndex, this.Arguments, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, this.Method.Parameters));
                }

                return sourceOrderArguments.ToImmutableAndFree();
            }
        }
        
        ImmutableArray<IArgument> IInvocationExpression.ArgumentsInParameterOrder => DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgsToParamsOpt, this.ArgumentRefKindsOpt, this.Method.Parameters);

        IArgument IInvocationExpression.ArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return ArgumentMatchingParameter(this.Arguments, this.ArgsToParamsOpt, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, parameter.ContainingSymbol as Symbols.MethodSymbol, parameter);
        }

        protected override OperationKind ExpressionKind => OperationKind.InvocationExpression;
        
        internal static ImmutableArray<IArgument> DeriveArguments(ImmutableArray<BoundExpression> boundArguments, ImmutableArray<string> argumentNames, ImmutableArray<int> argumentsToParameters, ImmutableArray<RefKind> argumentRefKinds, ImmutableArray<Symbols.ParameterSymbol> parameters)
        {
            ArrayBuilder<IArgument> arguments = ArrayBuilder<IArgument>.GetInstance(boundArguments.Length);
            for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                int argumentIndex = -1;
                if (argumentsToParameters.IsDefault)
                {
                    argumentIndex = parameterIndex;
                }
                else
                {
                    argumentIndex = argumentsToParameters.IndexOf(parameterIndex);
                }

                if (argumentIndex == -1)
                {
                    // No argument has been supplied for the parameter.
                    Symbols.ParameterSymbol parameter = parameters[parameterIndex];
                    if (parameter.HasExplicitDefaultValue)
                    {
                        arguments.Add(new Argument(ArgumentKind.DefaultValue, parameter, new Literal(parameter.ExplicitDefaultConstantValue, parameter.Type, null)));
                    }
                    else
                    {
                        arguments.Add(null);
                    }
                }
                else
                {
                    arguments.Add(DeriveArgument(parameterIndex, argumentIndex, boundArguments, argumentNames, argumentRefKinds, parameters));
                }
            }

            return arguments.ToImmutableAndFree();
        }

        private static System.Runtime.CompilerServices.ConditionalWeakTable<BoundExpression, IArgument> ArgumentMappings = new System.Runtime.CompilerServices.ConditionalWeakTable<BoundExpression, IArgument>();

        private static IArgument DeriveArgument(int parameterIndex, int argumentIndex, ImmutableArray<BoundExpression> boundArguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, ImmutableArray<Symbols.ParameterSymbol> parameters)
        {
            if (argumentIndex >= boundArguments.Length)
            {
                // Check for an omitted argument that becomes an empty params array.
                if (parameters.Length > 0)
                {
                    Symbols.ParameterSymbol lastParameter = parameters[parameters.Length - 1];
                    if (lastParameter.IsParams)
                    {
                        return new Argument(ArgumentKind.ParamArray, lastParameter, CreateParamArray(lastParameter, boundArguments, argumentIndex));
                    }
                }

                // There is no supplied argument and there is no params parameter. Any action is suspect at this point.
                return null;
            }

            return ArgumentMappings.GetValue(
                boundArguments[argumentIndex],
                (argument) =>
                {
                    string name = !argumentNames.IsDefaultOrEmpty ? argumentNames[argumentIndex] : null;

                    if (name == null)
                    {
                        RefKind refMode = !argumentRefKinds.IsDefaultOrEmpty ? argumentRefKinds[argumentIndex] : RefKind.None;
                        return
                            refMode == RefKind.None
                            ? ((argumentIndex >= parameters.Length - 1 && parameters.Length > 0 && parameters[parameters.Length - 1].IsParams)
                                ? (IArgument)new Argument(ArgumentKind.ParamArray, parameters[parameters.Length - 1], CreateParamArray(parameters[parameters.Length - 1], boundArguments, argumentIndex))
                                : new SimpleArgument(parameters[parameterIndex], argument))
                            : (IArgument)new Argument(ArgumentKind.Positional, parameters[parameterIndex], argument);
                    }

                    return new Argument(ArgumentKind.Named, parameters[parameterIndex], argument);
                });
        }
        
        private static IExpression CreateParamArray(IParameterSymbol parameter, ImmutableArray<BoundExpression> boundArguments, int firstArgumentElementIndex)
        {
            if (parameter.Type.TypeKind == TypeKind.Array)
            {
                IArrayTypeSymbol arrayType = (IArrayTypeSymbol)parameter.Type;
                ArrayBuilder<IExpression> paramArrayArguments = ArrayBuilder<IExpression>.GetInstance(boundArguments.Length - firstArgumentElementIndex);
                for (int index = firstArgumentElementIndex; index < boundArguments.Length; index++)
                {
                    paramArrayArguments.Add(boundArguments[index]);
                }

                return new ArrayCreation(arrayType, paramArrayArguments.ToImmutableAndFree(), boundArguments.Length > 0 ? boundArguments[0].Syntax : null);
            }

            return null;
        }

        internal static IArgument ArgumentMatchingParameter(ImmutableArray<BoundExpression> arguments, ImmutableArray<int> argumentsToParameters, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, Symbols.MethodSymbol targetMethod, IParameterSymbol parameter)
        {
            int argumentIndex = ArgumentIndexMatchingParameter(arguments, argumentsToParameters, targetMethod, parameter);
            if (argumentIndex >= 0)
            {
                return DeriveArgument(parameter.Ordinal, argumentIndex, arguments, argumentNames, argumentRefKinds, targetMethod.Parameters);
            }

            return null;
        }

        static int ArgumentIndexMatchingParameter(ImmutableArray<BoundExpression> arguments, ImmutableArray<int> argumentsToParameters, IMethodSymbol targetMethod, IParameterSymbol parameter)
        {
            if (parameter.ContainingSymbol == targetMethod)
            {
                int parameterIndex = parameter.Ordinal;
                ImmutableArray<int> parameterIndices = argumentsToParameters;
                if (!parameterIndices.IsDefaultOrEmpty)
                {
                    return parameterIndices.IndexOf(parameterIndex);
                }

                return parameterIndex;
            }

            return -1;
        }

        class SimpleArgument : IArgument
        {
            public SimpleArgument(IParameterSymbol parameter, IExpression value)
            {
                this.Value = value;
                this.Parameter = parameter;
            }

            public ArgumentKind Kind => ArgumentKind.Positional;

            public IParameterSymbol Parameter { get; }

            public IExpression Value { get; }

            public IExpression InConversion => null;

            public IExpression OutConversion => null;
        }

        class Argument : IArgument
        {
            public Argument(ArgumentKind kind, IParameterSymbol parameter, IExpression value)
            {
                this.Value = value;
                this.Kind = kind;
                this.Parameter = parameter;
            }

            public ArgumentKind Kind { get; }

            public IParameterSymbol Parameter { get; }
            
            public IExpression Value { get; }

            public IExpression InConversion => null;

            public IExpression OutConversion => null;
        }
    }

    partial class BoundLocal : ILocalReferenceExpression
    {
        ILocalSymbol ILocalReferenceExpression.Local => this.LocalSymbol;
        
        protected override OperationKind ExpressionKind => OperationKind.LocalReferenceExpression;
    }

    partial class BoundFieldAccess : IFieldReferenceExpression
    {
        IExpression IMemberReferenceExpression.Instance => this.ReceiverOpt;
       
        IFieldSymbol IFieldReferenceExpression.Field => this.FieldSymbol;

        protected override OperationKind ExpressionKind => OperationKind.FieldReferenceExpression;
    }

    partial class BoundPropertyAccess : IPropertyReferenceExpression
    {
        IPropertySymbol IPropertyReferenceExpression.Property => this.PropertySymbol;
       
        IExpression IMemberReferenceExpression.Instance => this.ReceiverOpt;
       
        protected override OperationKind ExpressionKind => OperationKind.PropertyReferenceExpression;
    }

    partial class BoundParameter : IParameterReferenceExpression
    {
        IParameterSymbol IParameterReferenceExpression.Parameter => this.ParameterSymbol;

        protected override OperationKind ExpressionKind => OperationKind.ParameterReferenceExpression;
    }

    partial class BoundLiteral : ILiteralExpression
    {
        string ILiteralExpression.Spelling => this.Syntax.ToString();

        protected override OperationKind ExpressionKind => OperationKind.LiteralExpression;
    }

    partial class BoundObjectCreationExpression : IObjectCreationExpression
    {
        IMethodSymbol IObjectCreationExpression.Constructor => this.Constructor;

        ImmutableArray<IArgument> IObjectCreationExpression.ConstructorArguments => BoundCall.DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgsToParamsOpt, this.ArgumentRefKindsOpt, this.Constructor.Parameters);

        IArgument IObjectCreationExpression.ArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return BoundCall.ArgumentMatchingParameter(this.Arguments, this.ArgsToParamsOpt, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, this.Constructor, parameter);
        }

        ImmutableArray<IMemberInitializer> IObjectCreationExpression.MemberInitializers
        {
            get
            {
                BoundExpression initializer = this.InitializerExpressionOpt;
                if (initializer != null)
                {
                    // ZZZ What's the representation in bound trees?
                }

                return ImmutableArray.Create<IMemberInitializer>();
            }
        }

        protected override OperationKind ExpressionKind => OperationKind.ObjectCreationExpression;
    }

    partial class UnboundLambda
    {
        protected override OperationKind ExpressionKind => OperationKind.UnboundLambdaExpression;
    }

    partial class BoundLambda : ILambdaExpression
    {
        IMethodSymbol ILambdaExpression.Signature => this.Symbol;

        IBlockStatement ILambdaExpression.Body => this.Body;

        protected override OperationKind ExpressionKind => OperationKind.LambdaExpression;
    }

    partial class BoundConversion : IConversionExpression
    {
        IExpression IConversionExpression.Operand => this.Operand;

        Semantics.ConversionKind IConversionExpression.Conversion
        {
            get
            {
                switch (this.ConversionKind)
                {
                    case CSharp.ConversionKind.ExplicitUserDefined:
                    case CSharp.ConversionKind.ImplicitUserDefined:
                        return Semantics.ConversionKind.Operator;

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
                    case CSharp.ConversionKind.ExplicitNullable:
                    case CSharp.ConversionKind.ImplicitNullable:
                    case CSharp.ConversionKind.ExplicitNumeric:
                    case CSharp.ConversionKind.ImplicitNumeric:
                    case CSharp.ConversionKind.ImplicitConstant:
                    case CSharp.ConversionKind.IntegerToPointer:
                    case CSharp.ConversionKind.IntPtr:
                    case CSharp.ConversionKind.NullLiteral:
                    case CSharp.ConversionKind.NullToPointer:
                    case CSharp.ConversionKind.PointerToInteger:
                    case CSharp.ConversionKind.PointerToPointer:
                    case CSharp.ConversionKind.PointerToVoid:
                        return Semantics.ConversionKind.CSharp;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.ConversionKind);
                }
            }
        }

        bool IConversionExpression.IsExplicit => this.ExplicitCastInCode;

        IMethodSymbol IHasOperatorExpression.Operator => this.SymbolOpt;

        bool IHasOperatorExpression.UsesOperatorMethod => this.ConversionKind == CSharp.ConversionKind.ExplicitUserDefined || this.ConversionKind == CSharp.ConversionKind.ImplicitUserDefined;

        protected override OperationKind ExpressionKind => OperationKind.ConversionExpression;
    }

    partial class BoundAsOperator : IConversionExpression
    {
        IExpression IConversionExpression.Operand => this.Operand;

        Semantics.ConversionKind IConversionExpression.Conversion => Semantics.ConversionKind.AsCast;

        bool IConversionExpression.IsExplicit => true;

        IMethodSymbol IHasOperatorExpression.Operator => null;

        bool IHasOperatorExpression.UsesOperatorMethod => false;

        protected override OperationKind ExpressionKind => OperationKind.ConversionExpression;
    }

    partial class BoundIsOperator : IIsExpression
    {
        IExpression IIsExpression.Operand => this.Operand;

        ITypeSymbol IIsExpression.IsType => this.TargetType.Type;

        protected override OperationKind ExpressionKind => OperationKind.IsExpression;
    }

    partial class BoundSizeOfOperator : ITypeOperationExpression
    {
        TypeOperationKind ITypeOperationExpression.TypeOperationClass => TypeOperationKind.SizeOf;

        ITypeSymbol ITypeOperationExpression.TypeOperand => this.SourceType.Type;

        protected override OperationKind ExpressionKind => OperationKind.TypeOperationExpression;
    }

    partial class BoundTypeOfOperator : ITypeOperationExpression
    {
        TypeOperationKind ITypeOperationExpression.TypeOperationClass => TypeOperationKind.TypeOf;

        ITypeSymbol ITypeOperationExpression.TypeOperand => this.SourceType.Type;

        protected override OperationKind ExpressionKind => OperationKind.TypeOperationExpression;
    }

    partial class BoundArrayCreation : IArrayCreationExpression
    {
        ITypeSymbol IArrayCreationExpression.ElementType
        {
            get
            {
                IArrayTypeSymbol arrayType = this.Type as IArrayTypeSymbol;
                if (arrayType != null)
                {
                    return arrayType.ElementType;
                }

                return null;
            }
        }

        ImmutableArray<IExpression> IArrayCreationExpression.DimensionSizes => this.Bounds.As<IExpression>();

        IArrayInitializer IArrayCreationExpression.ElementValues
        {
            get
            {
                BoundArrayInitialization initializer = this.InitializerOpt;
                if (initializer != null)
                {
                    return MakeInitializer(initializer);
                }

                return null; 
            }
        }

        private static System.Runtime.CompilerServices.ConditionalWeakTable<BoundArrayInitialization, IArrayInitializer> ArrayInitializerMappings = new System.Runtime.CompilerServices.ConditionalWeakTable<BoundArrayInitialization, IArrayInitializer>();

        IArrayInitializer MakeInitializer(BoundArrayInitialization initializer)
        {
            return ArrayInitializerMappings.GetValue(
                initializer,
                (arrayInitializer) =>
                {
                    ArrayBuilder<IArrayInitializer> dimension = ArrayBuilder<IArrayInitializer>.GetInstance(arrayInitializer.Initializers.Length);
                    for (int index = 0; index < arrayInitializer.Initializers.Length; index++)
                    {
                        BoundExpression elementInitializer = arrayInitializer.Initializers[index];
                        BoundArrayInitialization elementArray = elementInitializer as BoundArrayInitialization;
                        dimension[index] = elementArray != null ? MakeInitializer(elementArray) : new ElementInitializer(elementInitializer);
                    }

                    return new DimensionInitializer(dimension.ToImmutableAndFree());
                });
        }

        protected override OperationKind ExpressionKind => OperationKind.ArrayCreationExpression;

        class ElementInitializer : IExpressionArrayInitializer
        {
            private readonly BoundExpression _element;

            public ElementInitializer(BoundExpression element)
            {
                _element = element;
            }

            public IExpression ElementValue => _element;

            public ArrayInitializerKind ArrayClass => ArrayInitializerKind.Expression;
        }

        class DimensionInitializer : IDimensionArrayInitializer
        {
            private readonly ImmutableArray<IArrayInitializer> _dimension;

            public DimensionInitializer(ImmutableArray<IArrayInitializer> dimension)
            {
                _dimension = dimension;
            }

            public ImmutableArray<IArrayInitializer> ElementValues => _dimension;

            public ArrayInitializerKind ArrayClass => ArrayInitializerKind.Dimension;
        }
    }

    partial class BoundArrayInitialization
    {
        protected override OperationKind ExpressionKind => OperationKind.None;
    }

    partial class BoundDefaultOperator
    {
        protected override OperationKind ExpressionKind => OperationKind.DefaultValueExpression;
    }

    partial class BoundDup
    {
        protected override OperationKind ExpressionKind => OperationKind.None;
    }

    partial class BoundBaseReference : IInstanceReferenceExpression
    {
        bool IInstanceReferenceExpression.IsExplicit => true;

        IParameterSymbol IParameterReferenceExpression.Parameter => (IParameterSymbol)this.ExpressionSymbol;
        
        protected override OperationKind ExpressionKind => OperationKind.BaseClassInstanceReferenceExpression;
    }

    partial class BoundThisReference : IInstanceReferenceExpression
    {
        bool IInstanceReferenceExpression.IsExplicit => true;

        IParameterSymbol IParameterReferenceExpression.Parameter => (IParameterSymbol)this.ExpressionSymbol;

        protected override OperationKind ExpressionKind => OperationKind.InstanceReferenceExpression;
    }

    partial class BoundAssignmentOperator : IAssignmentExpression
    {
        IReferenceExpression IAssignmentExpression.Target => this.Left as IReferenceExpression;

        IExpression IAssignmentExpression.Value => this.Right;

        protected override OperationKind ExpressionKind => OperationKind.AssignmentExpression;
    }

    partial class BoundCompoundAssignmentOperator : ICompoundAssignmentExpression
    {
        BinaryOperationKind ICompoundAssignmentExpression.BinaryKind => Expression.DeriveBinaryOperationKind(this.Operator.Kind);

        IReferenceExpression IAssignmentExpression.Target => this.Left as IReferenceExpression;

        IExpression IAssignmentExpression.Value => this.Right;

        bool IHasOperatorExpression.UsesOperatorMethod => (this.Operator.Kind & BinaryOperatorKind.TypeMask) == BinaryOperatorKind.UserDefined;

        IMethodSymbol IHasOperatorExpression.Operator => this.Operator.Method;

        protected override OperationKind ExpressionKind => OperationKind.CompoundAssignmentExpression;
    }

    partial class BoundIncrementOperator : IIncrementExpression
    {
        UnaryOperationKind IIncrementExpression.IncrementKind => Expression.DeriveUnaryOperationKind(this.OperatorKind);

        BinaryOperationKind ICompoundAssignmentExpression.BinaryKind => Expression.DeriveBinaryOperationKind(((IIncrementExpression)this).IncrementKind);

        IReferenceExpression IAssignmentExpression.Target => this.Operand as IReferenceExpression;

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<BoundIncrementOperator, IExpression> IncrementValueMappings = new System.Runtime.CompilerServices.ConditionalWeakTable<BoundIncrementOperator, IExpression>();

        IExpression IAssignmentExpression.Value => IncrementValueMappings.GetValue(this, (increment) => new BoundLiteral(this.Syntax, Semantics.Expression.SynthesizeNumeric(increment.Type, 1), increment.Type));

        bool IHasOperatorExpression.UsesOperatorMethod => (this.OperatorKind & UnaryOperatorKind.TypeMask) == UnaryOperatorKind.UserDefined;

        IMethodSymbol IHasOperatorExpression.Operator => this.MethodOpt;

        protected override OperationKind ExpressionKind => OperationKind.IncrementExpression;
    }

    partial class BoundBadExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.None;
    }

    partial class BoundNewT
    {
        protected override OperationKind ExpressionKind => OperationKind.TypeParameterObjectCreationExpression;
    }

    partial class BoundUnaryOperator : IUnaryOperatorExpression
    {
        UnaryOperationKind IUnaryOperatorExpression.UnaryKind => Expression.DeriveUnaryOperationKind(this.OperatorKind);

        IExpression IUnaryOperatorExpression.Operand => this.Operand;

        bool IHasOperatorExpression.UsesOperatorMethod => (this.OperatorKind & UnaryOperatorKind.TypeMask) == UnaryOperatorKind.UserDefined;

        IMethodSymbol IHasOperatorExpression.Operator => this.MethodOpt;

        protected override OperationKind ExpressionKind => OperationKind.UnaryOperatorExpression;
    }

    partial class BoundBinaryOperator : IBinaryOperatorExpression
    {
        BinaryOperationKind IBinaryOperatorExpression.BinaryKind => Expression.DeriveBinaryOperationKind(this.OperatorKind);

        IExpression IBinaryOperatorExpression.Left => this.Left;

        IExpression IBinaryOperatorExpression.Right => this.Right;
   
        bool IHasOperatorExpression.UsesOperatorMethod => (this.OperatorKind & BinaryOperatorKind.TypeMask) == BinaryOperatorKind.UserDefined;

        IMethodSymbol IHasOperatorExpression.Operator => this.MethodOpt;

        protected override OperationKind ExpressionKind
        {
            get
            {
                switch (this.OperatorKind & BinaryOperatorKind.OpMask)
                {
                    case BinaryOperatorKind.Addition:
                    case BinaryOperatorKind.Subtraction:
                    case BinaryOperatorKind.Multiplication:
                    case BinaryOperatorKind.Division:
                    case BinaryOperatorKind.Remainder:
                    case BinaryOperatorKind.LeftShift:
                    case BinaryOperatorKind.RightShift:
                    case BinaryOperatorKind.And:
                    case BinaryOperatorKind.Or:
                    case BinaryOperatorKind.Xor:
                    case BinaryOperatorKind.LessThan:
                    case BinaryOperatorKind.LessThanOrEqual:
                    case BinaryOperatorKind.Equal:
                    case BinaryOperatorKind.NotEqual:
                    case BinaryOperatorKind.GreaterThan:
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return OperationKind.BinaryOperatorExpression;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.OperatorKind & BinaryOperatorKind.OpMask);
                }
            }
        }
    }

    partial class BoundConditionalOperator : IConditionalChoiceExpression
    {
        IExpression IConditionalChoiceExpression.Condition => this.Condition;

        IExpression IConditionalChoiceExpression.IfTrue => this.Consequence;

        IExpression IConditionalChoiceExpression.IfFalse => this.Alternative;

        protected override OperationKind ExpressionKind => OperationKind.ConditionalChoiceExpression;
    }

    partial class BoundNullCoalescingOperator : INullCoalescingExpression
    {
        IExpression INullCoalescingExpression.Primary => this.LeftOperand;

        IExpression INullCoalescingExpression.Secondary => this.RightOperand;

        protected override OperationKind ExpressionKind => OperationKind.NullCoalescingExpression;
    }

    partial class BoundAwaitExpression : IAwaitExpression
    {
        IExpression IAwaitExpression.Upon => this.Expression;

        protected override OperationKind ExpressionKind => OperationKind.AwaitExpression;
    }

    partial class BoundArrayAccess : IArrayElementReferenceExpression
    {
        IExpression IArrayElementReferenceExpression.ArrayReference => this.Expression;

        ImmutableArray<IExpression> IArrayElementReferenceExpression.Indices => this.Indices.As<IExpression>();
        
        protected override OperationKind ExpressionKind => OperationKind.ArrayElementReferenceExpression;
    }

    partial class BoundPointerIndirectionOperator : IPointerIndirectionReferenceExpression
    {
        IExpression IPointerIndirectionReferenceExpression.Pointer => this.Operand;
        
        protected override OperationKind ExpressionKind => OperationKind.PointerIndirectionReferenceExpression;
    }

    partial class BoundAddressOfOperator : IAddressOfExpression
    {
        IReferenceExpression IAddressOfExpression.Addressed => (IReferenceExpression)this.Operand;

        protected override OperationKind ExpressionKind => OperationKind.AddressOfExpression;
    }

    partial class BoundImplicitReceiver : IInstanceReferenceExpression
    {
        bool IInstanceReferenceExpression.IsExplicit => false;

        IParameterSymbol IParameterReferenceExpression.Parameter => (IParameterSymbol)this.ExpressionSymbol;

        protected override OperationKind ExpressionKind => OperationKind.InstanceReferenceExpression;
    }

    partial class BoundConditionalAccess : IConditionalAccessExpression
    {
        IExpression IConditionalAccessExpression.Access => this.AccessExpression;

        protected override OperationKind ExpressionKind => OperationKind.ConditionalAccessExpression;
    }

    class Expression
    {
        internal static BinaryOperationKind DeriveBinaryOperationKind(UnaryOperationKind incrementKind)
        {
            switch (incrementKind)
            {
                case UnaryOperationKind.OperatorPostfixIncrement:
                case UnaryOperationKind.OperatorPrefixIncrement:
                    return BinaryOperationKind.OperatorAdd;
                case UnaryOperationKind.OperatorPostfixDecrement:
                case UnaryOperationKind.OperatorPrefixDecrement:
                    return BinaryOperationKind.OperatorSubtract;
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
                    throw ExceptionUtilities.UnexpectedValue(incrementKind);
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
                            return UnaryOperationKind.OperatorPostfixIncrement;
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
                            return UnaryOperationKind.OperatorPostfixDecrement;
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
                            return UnaryOperationKind.OperatorPrefixIncrement;
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
                            return UnaryOperationKind.OperatorPrefixDecrement;
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
                            return UnaryOperationKind.OperatorPlus;
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
                            return UnaryOperationKind.OperatorMinus;
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
                            return UnaryOperationKind.OperatorLogicalNot;
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
                            return UnaryOperationKind.OperatorBitwiseNegation;
                    }

                    break;

                case UnaryOperatorKind.True:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicTrue;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorTrue;
                    }

                    break;

                case UnaryOperatorKind.False:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicFalse;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorFalse;
                    }

                    break;
            }

            throw ExceptionUtilities.UnexpectedValue(operatorKind & UnaryOperatorKind.TypeMask);
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
                            return BinaryOperationKind.StringConcatenation;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorAdd;
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
                            return BinaryOperationKind.OperatorSubtract;
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
                            return BinaryOperationKind.OperatorMultiply;
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
                            return BinaryOperationKind.OperatorDivide;
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
                            return BinaryOperationKind.OperatorRemainder;
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
                            return BinaryOperationKind.OperatorLeftShift;
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
                            return BinaryOperationKind.OperatorRightShift;
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
                                return BinaryOperationKind.OperatorConditionalAnd;
                            }

                            return BinaryOperationKind.OperatorAnd;
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
                                return BinaryOperationKind.OperatorConditionalOr;
                            }

                            return BinaryOperationKind.OperatorOr;
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
                            return BinaryOperationKind.OperatorExclusiveOr;
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
                            return BinaryOperationKind.OperatorLessThan;
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
                            return BinaryOperationKind.OperatorLessThanOrEqual;
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
                            return BinaryOperationKind.OperatorEquals;
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
                            return BinaryOperationKind.OperatorNotEquals;
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
                            return BinaryOperationKind.OperatorGreaterThanOrEqual;
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
                            return BinaryOperationKind.OperatorGreaterThan;
                    }

                    break;
            }

            throw ExceptionUtilities.UnexpectedValue(operatorKind & BinaryOperatorKind.TypeMask);
        }
    }
}
