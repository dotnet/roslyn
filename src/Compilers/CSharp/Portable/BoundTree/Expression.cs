// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Semantics;

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
            var list = new List<BoundNode>();
            new Collector(list).Visit(this);
            list.RemoveAt(0);
            return list.OfType<IOperation>();
        }

        IEnumerable<IOperation> IOperationSearchable.DescendantsAndSelf()
        {
            var list = new List<BoundNode>();
            new Collector(list).Visit(this);
            return list.OfType<IOperation>();
        }

        private class Collector : BoundTreeWalker
        {
            private readonly List<BoundNode> nodes;

            public Collector(List<BoundNode> nodes)
            {
                this.nodes = nodes;
            }

            public override BoundNode Visit(BoundNode node)
            {
                this.nodes.Add(node);
                return base.Visit(node);
            }
        }
    }

    partial class BoundCall : IInvocation
    {
        IMethodSymbol IInvocation.TargetMethod => this.Method;

        IExpression IInvocation.Instance => this.ReceiverOpt;
       
        InvocationKind IInvocation.InvocationKind
        {
            get
            {
                IMethodSymbol method = this.Method;

                if (method.IsStatic)
                {
                    return InvocationKind.Static;
                }

                if ((method.IsVirtual || method.IsAbstract || method.IsOverride) && !this.ReceiverOpt.SuppressVirtualCalls)
                {
                    return InvocationKind.Virtual;
                }

                return InvocationKind.NonVirtualInstance;
            }
        }

        ImmutableArray<IArgument> IInvocation.Arguments
        {
            // ToDO: This should use a ConditionalWeakTable to avoid creating a new array at each access.
            get { return DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, this.Method.ParameterCount, this.Method.Parameters[this.Method.ParameterCount - 1].IsParams); }
        }

        IArgument IInvocation.ArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return ArgumentMatchingParameter(this.Arguments, this.ArgsToParamsOpt, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, parameter.ContainingSymbol as IMethodSymbol, parameter);
        }

        protected override OperationKind ExpressionKind => OperationKind.Invocation;
        
        internal static ImmutableArray<IArgument> DeriveArguments(ImmutableArray<BoundExpression> boundArguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, int parameterCount, bool hasParamsParameter)
        {
            ArrayBuilder<IArgument> arguments = ArrayBuilder<IArgument>.GetInstance(boundArguments.Length);
            for (int index = 0; index < boundArguments.Length; index++)
            {
                arguments.Add(DeriveArgument(index, boundArguments, argumentNames, argumentRefKinds, parameterCount, hasParamsParameter));
            }

            return arguments.ToImmutableAndFree();
        }

        private static System.Runtime.CompilerServices.ConditionalWeakTable<BoundExpression, IArgument> ArgumentMappings = new System.Runtime.CompilerServices.ConditionalWeakTable<BoundExpression, IArgument>();

        private static IArgument DeriveArgument(int index, ImmutableArray<BoundExpression> boundArguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, int parameterCount, bool hasParamsParameter)
        {
            return ArgumentMappings.GetValue(
                boundArguments[index],
                (argument) =>
                {
                    string name = !argumentNames.IsDefaultOrEmpty ? argumentNames[index] : null;
                    RefKind refMode = !argumentRefKinds.IsDefaultOrEmpty ? argumentRefKinds[index] : RefKind.None;

                    ArgumentMode mode = refMode == RefKind.None ? ArgumentMode.In : (refMode == RefKind.Out ? ArgumentMode.Out : ArgumentMode.Reference);
                    if (name == null)
                    {
                        return
                            mode == ArgumentMode.In
                            ? ((index >= parameterCount - 1 && hasParamsParameter) ? (IArgument)new Argument(ArgumentKind.ParamArray, mode, argument) : new SimpleArgument(argument))
                            : (IArgument)new Argument(ArgumentKind.Positional, mode, argument);
                    }

                    return new NamedArgument(mode, argument, name);
                });
        }

        internal static IArgument ArgumentMatchingParameter(ImmutableArray<BoundExpression> arguments, ImmutableArray<int> argumentsToParameters, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, IMethodSymbol targetMethod, IParameterSymbol parameter)
        {
            int index = ArgumentIndexMatchingParameter(arguments, argumentsToParameters, parameter.ContainingSymbol as IMethodSymbol, parameter);
            if (index >= 0)
            {
                return DeriveArgument(index, arguments, argumentNames, argumentRefKinds, targetMethod.Parameters.Length, targetMethod.Parameters[targetMethod.Parameters.Length - 1].IsParams);
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
                    for (int index = 0; index < parameterIndices.Length; index++)
                    {
                        if (parameterIndices[index] == parameterIndex)
                        {
                            return index;
                        }
                    }
                }

                return parameterIndex;
            }

            return -1;
        }

        class SimpleArgument : IArgument
        {
            readonly IExpression ArgumentValue;
            public SimpleArgument(IExpression value)
            {
                this.ArgumentValue = value;
            }

            public ArgumentKind Kind => ArgumentKind.Positional;
            
            public ArgumentMode Mode => ArgumentMode.In;
           
            public IExpression Value => this.ArgumentValue;

            public IExpression InConversion => null;

            public IExpression OutConversion => null;
        }

        class Argument : IArgument
        {
            readonly IExpression ArgumentValue;
            readonly ArgumentKind ArgumentKind;
            readonly ArgumentMode ArgumentMode;
            public Argument(ArgumentKind kind, ArgumentMode mode, IExpression value)
            {
                this.ArgumentValue = value;
                this.ArgumentKind = kind;
                this.ArgumentMode = mode;
            }

            public ArgumentKind Kind => this.ArgumentKind;
           
            public ArgumentMode Mode => this.ArgumentMode;
            
            public IExpression Value => this.ArgumentValue;

            public IExpression InConversion => null;

            public IExpression OutConversion => null;
        }

        class NamedArgument : Argument, INamedArgument
        {
            readonly string ArgumentName;
            public NamedArgument(ArgumentMode mode, IExpression value, string name)
                : base(ArgumentKind.Named, mode, value)
            {
                this.ArgumentName = name;
            }

            public string Name => this.ArgumentName;
        }
    }

    partial class BoundLocal : ILocalReference
    {
        ILocalSymbol ILocalReference.Local => this.LocalSymbol;
        
        ReferenceKind IReference.ReferenceKind => ReferenceKind.Local;
       
        protected override OperationKind ExpressionKind => OperationKind.LocalReference;
    }

    partial class BoundFieldAccess : IFieldReference
    {
        IExpression IMemberReference.Instance => this.ReceiverOpt;
       
        IFieldSymbol IFieldReference.Field => this.FieldSymbol;
       
        ReferenceKind IReference.ReferenceKind => this.FieldSymbol.IsStatic ? ReferenceKind.StaticField : ReferenceKind.InstanceField;

        protected override OperationKind ExpressionKind => OperationKind.FieldReference;
    }

    partial class BoundPropertyAccess : IPropertyReference
    {
        IPropertySymbol IPropertyReference.Property => this.PropertySymbol;
       
        IExpression IMemberReference.Instance => this.ReceiverOpt;
       
        ReferenceKind IReference.ReferenceKind => this.PropertySymbol.IsStatic ? ReferenceKind.StaticProperty : ReferenceKind.InstanceProperty;

        protected override OperationKind ExpressionKind => OperationKind.PropertyReference;
    }

    partial class BoundParameter : IParameterReference
    {
        IParameterSymbol IParameterReference.Parameter => this.ParameterSymbol;

        ReferenceKind IReference.ReferenceKind => ReferenceKind.Parameter;

        protected override OperationKind ExpressionKind => OperationKind.ParameterReference;
    }

    partial class BoundLiteral : ILiteral
    {
        LiteralKind ILiteral.LiteralClass => Semantics.Expression.DeriveLiteralKind(this.Type);

        string ILiteral.Spelling => this.Syntax.ToString();

        protected override OperationKind ExpressionKind => OperationKind.Literal;
    }

    partial class BoundObjectCreationExpression : IObjectCreation
    {
        IMethodSymbol IObjectCreation.Constructor => this.Constructor;

        ImmutableArray<IArgument> IObjectCreation.ConstructorArguments => BoundCall.DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, this.Constructor.ParameterCount, this.Constructor.Parameters[this.Constructor.ParameterCount - 1].IsParams);

        IArgument IObjectCreation.ArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return BoundCall.ArgumentMatchingParameter(this.Arguments, this.ArgsToParamsOpt, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, this.Constructor, parameter);
        }

        ImmutableArray<IMemberInitializer> IObjectCreation.MemberInitializers
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

        protected override OperationKind ExpressionKind => OperationKind.ObjectCreation;
    }

    partial class UnboundLambda
    {
        protected override OperationKind ExpressionKind => OperationKind.UnboundLambda;
    }

    partial class BoundLambda : ILambda
    {
        IMethodSymbol ILambda.Signature => this.Symbol;

        IBlock ILambda.Body => this.Body;

        protected override OperationKind ExpressionKind => OperationKind.Lambda;
    }

    partial class BoundConversion : IConversion
    {
        IExpression IConversion.Operand => this.Operand;

        Semantics.ConversionKind IConversion.Conversion
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
                }

                return Semantics.ConversionKind.None;
            }
        }

        bool IConversion.IsExplicit => this.ExplicitCastInCode;

        IMethodSymbol IWithOperator.Operator => this.SymbolOpt;

        bool IWithOperator.UsesOperatorMethod => this.ConversionKind == CSharp.ConversionKind.ExplicitUserDefined || this.ConversionKind == CSharp.ConversionKind.ImplicitUserDefined;

        protected override OperationKind ExpressionKind => OperationKind.Conversion;
    }

    partial class BoundAsOperator : IConversion
    {
        IExpression IConversion.Operand => this.Operand;

        Semantics.ConversionKind IConversion.Conversion => Semantics.ConversionKind.AsCast;

        bool IConversion.IsExplicit => true;

        IMethodSymbol IWithOperator.Operator => null;

        bool IWithOperator.UsesOperatorMethod => false;

        protected override OperationKind ExpressionKind => OperationKind.Conversion;
    }

    partial class BoundIsOperator : IIs
    {
        IExpression IIs.Operand => this.Operand;

        ITypeSymbol IIs.IsType => this.TargetType.Type;

        protected override OperationKind ExpressionKind => OperationKind.Is;
    }

    partial class BoundSizeOfOperator : ITypeOperation
    {
        TypeOperationKind ITypeOperation.TypeOperationClass => TypeOperationKind.SizeOf;

        ITypeSymbol ITypeOperation.TypeOperand => this.SourceType.Type;

        protected override OperationKind ExpressionKind => OperationKind.TypeOperation;
    }

    partial class BoundTypeOfOperator : ITypeOperation
    {
        TypeOperationKind ITypeOperation.TypeOperationClass => TypeOperationKind.TypeOf;

        ITypeSymbol ITypeOperation.TypeOperand => this.SourceType.Type;

        protected override OperationKind ExpressionKind => OperationKind.TypeOperation;
    }

    partial class BoundArrayCreation : IArrayCreation
    {
        ITypeSymbol IArrayCreation.ElementType
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

        ImmutableArray<IExpression> IArrayCreation.DimensionSizes => this.Bounds.As<IExpression>();

        IArrayInitializer IArrayCreation.ElementValues
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

        protected override OperationKind ExpressionKind => OperationKind.ArrayCreation;

        class ElementInitializer : IExpressionArrayInitializer
        {
            readonly BoundExpression element;

            public ElementInitializer(BoundExpression element)
            {
                this.element = element;
            }

            public IExpression ElementValue => this.element;

            public ArrayInitializerKind ArrayClass => ArrayInitializerKind.Expression;
        }

        class DimensionInitializer : IDimensionArrayInitializer
        {
            readonly ImmutableArray<IArrayInitializer> dimension;

            public DimensionInitializer(ImmutableArray<IArrayInitializer> dimension)
            {
                this.dimension = dimension;
            }

            public ImmutableArray<IArrayInitializer> ElementValues => this.dimension;

            public ArrayInitializerKind ArrayClass => ArrayInitializerKind.Dimension;
        }
    }

    partial class BoundArrayInitialization
    {
        protected override OperationKind ExpressionKind => OperationKind.None;
    }

    partial class BoundDefaultOperator
    {
        protected override OperationKind ExpressionKind => OperationKind.DefaultValue;
    }

    partial class BoundDup
    {
        protected override OperationKind ExpressionKind => OperationKind.None;
    }

    partial class BoundBaseReference
    {
        protected override OperationKind ExpressionKind => OperationKind.BaseClassInstance;
    }

    partial class BoundThisReference
    {
        protected override OperationKind ExpressionKind => OperationKind.Instance;
    }

    partial class BoundAssignmentOperator : IAssignment
    {
        IReference IAssignment.Target => this.Left as IReference;

        IExpression IAssignment.Value => this.Right;

        protected override OperationKind ExpressionKind => OperationKind.Assignment;
    }

    partial class BoundCompoundAssignmentOperator : ICompoundAssignment
    {
        BinaryOperationKind ICompoundAssignment.BinaryKind => Expression.DeriveBinaryOperationKind(this.Operator.Kind);

        IReference IAssignment.Target => this.Left as IReference;

        IExpression IAssignment.Value => this.Right;

        bool IWithOperator.UsesOperatorMethod => (this.Operator.Kind & BinaryOperatorKind.TypeMask) == BinaryOperatorKind.UserDefined;

        IMethodSymbol IWithOperator.Operator => this.Operator.Method;

        protected override OperationKind ExpressionKind => OperationKind.CompoundAssignment;
    }

    partial class BoundIncrementOperator : IIncrement
    {
        UnaryOperationKind IIncrement.IncrementKind => Expression.DeriveUnaryOperationKind(this.OperatorKind);

        BinaryOperationKind ICompoundAssignment.BinaryKind => Expression.DeriveBinaryOperationKind(((IIncrement)this).IncrementKind);

        IReference IAssignment.Target => this.Operand as IReference;

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<BoundIncrementOperator, IExpression> IncrementValueMappings = new System.Runtime.CompilerServices.ConditionalWeakTable<BoundIncrementOperator, IExpression>();

        IExpression IAssignment.Value => IncrementValueMappings.GetValue(this, (increment) => new BoundLiteral(this.Syntax, Semantics.Expression.SynthesizeNumeric(increment.Type, 1), increment.Type));

        bool IWithOperator.UsesOperatorMethod => (this.OperatorKind & UnaryOperatorKind.TypeMask) == UnaryOperatorKind.UserDefined;

        IMethodSymbol IWithOperator.Operator => this.MethodOpt;

        protected override OperationKind ExpressionKind => OperationKind.Increment;
    }

    partial class BoundBadExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.None;
    }

    partial class BoundNewT
    {
        protected override OperationKind ExpressionKind => OperationKind.TypeParameterObjectCreation;
    }

    partial class BoundUnaryOperator : IUnary
    {
        UnaryOperationKind IUnary.UnaryKind => Expression.DeriveUnaryOperationKind(this.OperatorKind);

        IExpression IUnary.Operand => this.Operand;

        bool IWithOperator.UsesOperatorMethod => (this.OperatorKind & UnaryOperatorKind.TypeMask) == UnaryOperatorKind.UserDefined;

        IMethodSymbol IWithOperator.Operator => this.MethodOpt;

        protected override OperationKind ExpressionKind => OperationKind.UnaryOperator;
    }

    partial class BoundBinaryOperator : IBinary, IRelational
    {
        BinaryOperationKind IBinary.BinaryKind => Expression.DeriveBinaryOperationKind(this.OperatorKind);

        IExpression IBinary.Left => this.Left;

        IExpression IBinary.Right => this.Right;

        RelationalOperationKind IRelational.RelationalKind => Expression.DeriveRelationalOperationKind(this.OperatorKind);

        IExpression IRelational.Left => this.Left;

        IExpression IRelational.Right => this.Right;
   
        bool IWithOperator.UsesOperatorMethod => (this.OperatorKind & BinaryOperatorKind.TypeMask) == BinaryOperatorKind.UserDefined;

        IMethodSymbol IWithOperator.Operator => this.MethodOpt;

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
                        return OperationKind.BinaryOperator;
                    case BinaryOperatorKind.LessThan:
                    case BinaryOperatorKind.LessThanOrEqual:
                    case BinaryOperatorKind.Equal:
                    case BinaryOperatorKind.NotEqual:
                    case BinaryOperatorKind.GreaterThan:
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return OperationKind.RelationalOperator;
                }

                return OperationKind.None;
            }
        }
    }

    partial class BoundConditionalOperator : IConditionalChoice
    {
        IExpression IConditionalChoice.Condition => this.Condition;

        IExpression IConditionalChoice.IfTrue => this.Consequence;

        IExpression IConditionalChoice.IfFalse => this.Alternative;

        protected override OperationKind ExpressionKind => OperationKind.ConditionalChoice;
    }

    partial class BoundNullCoalescingOperator : INullCoalescing
    {
        IExpression INullCoalescing.Primary => this.LeftOperand;

        IExpression INullCoalescing.Secondary => this.RightOperand;

        protected override OperationKind ExpressionKind => OperationKind.NullCoalescing;
    }

    partial class BoundAwaitExpression : IAwait
    {
        IExpression IAwait.Upon => this.Expression;

        protected override OperationKind ExpressionKind => OperationKind.Await;
    }

    partial class BoundArrayAccess : IArrayElementReference
    {
        IExpression IArrayElementReference.ArrayReference => this.Expression;

        ImmutableArray<IExpression> IArrayElementReference.Indices => this.Indices.As<IExpression>();

        ReferenceKind IReference.ReferenceKind => ReferenceKind.ArrayElement;

        protected override OperationKind ExpressionKind => OperationKind.ArrayElementReference;
    }

    partial class BoundPointerIndirectionOperator : IPointerIndirectionReference
    {
        IExpression IPointerIndirectionReference.Pointer => this.Operand;

        ReferenceKind IReference.ReferenceKind => ReferenceKind.PointerIndirection;

        protected override OperationKind ExpressionKind => OperationKind.PointerIndirectionReference;
    }

    partial class BoundAddressOfOperator : IAddressOf
    {
        IReference IAddressOf.Addressed => (IReference)this.Operand;

        protected override OperationKind ExpressionKind => OperationKind.AddressOf;
    }

    partial class BoundImplicitReceiver
    {
        protected override OperationKind ExpressionKind => OperationKind.ImplicitInstance;
    }

    partial class BoundConditionalAccess : IConditionalAccess
    {
        IExpression IConditionalAccess.Access => this.AccessExpression;

        protected override OperationKind ExpressionKind => OperationKind.ConditionalAccess;
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
            }

            return BinaryOperationKind.None;
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

            return UnaryOperationKind.None;
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
                            return BinaryOperationKind.IntegerXor;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedXor;
                        case BinaryOperatorKind.Bool:
                            return BinaryOperationKind.BooleanXor;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperationKind.EnumXor;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicXor;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorXor;
                    }

                    break;
            }

            return BinaryOperationKind.None;
        }

        internal static RelationalOperationKind DeriveRelationalOperationKind(BinaryOperatorKind operatorKind)
        {
            switch (operatorKind & BinaryOperatorKind.OpMask)
            {
                case BinaryOperatorKind.LessThan:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return RelationalOperationKind.IntegerLess;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperationKind.UnsignedLess;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperationKind.FloatingLess;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperationKind.DecimalLess;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperationKind.PointerLess;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperationKind.EnumLess;
                        case BinaryOperatorKind.UserDefined:
                            return RelationalOperationKind.OperatorLess;
                    }

                    break;

                case BinaryOperatorKind.LessThanOrEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return RelationalOperationKind.IntegerLessEqual;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperationKind.UnsignedLessEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperationKind.FloatingLessEqual;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperationKind.DecimalLessEqual;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperationKind.PointerLessEqual;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperationKind.EnumLessEqual;
                        case BinaryOperatorKind.UserDefined:
                            return RelationalOperationKind.OperatorLessEqual;
                    }

                    break;

                case BinaryOperatorKind.Equal:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperationKind.IntegerEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperationKind.FloatingEqual;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperationKind.DecimalEqual;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperationKind.PointerEqual;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperationKind.EnumEqual;
                        case BinaryOperatorKind.Bool:
                            return RelationalOperationKind.BooleanEqual;
                        case BinaryOperatorKind.String:
                            return RelationalOperationKind.StringEqual;
                        case BinaryOperatorKind.Object:
                            return RelationalOperationKind.ObjectEqual;
                        case BinaryOperatorKind.Delegate:
                            return RelationalOperationKind.DelegateEqual;
                        case BinaryOperatorKind.NullableNull:
                            return RelationalOperationKind.NullableEqual;
                        case BinaryOperatorKind.UserDefined:
                            return RelationalOperationKind.OperatorEqual;
                    }

                    break;

                case BinaryOperatorKind.NotEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperationKind.IntegerNotEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperationKind.FloatingNotEqual;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperationKind.DecimalNotEqual;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperationKind.PointerNotEqual;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperationKind.EnumNotEqual;
                        case BinaryOperatorKind.Bool:
                            return RelationalOperationKind.BooleanNotEqual;
                        case BinaryOperatorKind.String:
                            return RelationalOperationKind.StringNotEqual;
                        case BinaryOperatorKind.Object:
                            return RelationalOperationKind.ObjectNotEqual;
                        case BinaryOperatorKind.Delegate:
                            return RelationalOperationKind.DelegateNotEqual;
                        case BinaryOperatorKind.NullableNull:
                            return RelationalOperationKind.NullableNotEqual;
                        case BinaryOperatorKind.UserDefined:
                            return RelationalOperationKind.OperatorNotEqual;
                    }

                    break;

                case BinaryOperatorKind.GreaterThanOrEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return RelationalOperationKind.IntegerGreaterEqual;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperationKind.UnsignedGreaterEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperationKind.FloatingGreaterEqual;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperationKind.DecimalGreaterEqual;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperationKind.PointerGreaterEqual;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperationKind.EnumGreaterEqual;
                        case BinaryOperatorKind.UserDefined:
                            return RelationalOperationKind.OperatorGreaterEqual;
                    }

                    break;

                case BinaryOperatorKind.GreaterThan:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return RelationalOperationKind.IntegerGreater;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperationKind.UnsignedGreater;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperationKind.FloatingGreater;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperationKind.DecimalGreater;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperationKind.PointerGreater;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperationKind.EnumGreater;
                        case BinaryOperatorKind.UserDefined:
                            return RelationalOperationKind.OperatorGreater;
                    }

                    break;
            }

            return RelationalOperationKind.None;
        }
    }
}
