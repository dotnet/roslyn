using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundExpression : IExpression
    {
        ITypeSymbol IExpression.ResultType
        {
            get { return this.Type; }
        }

        OperationKind IOperation.Kind
        {
            get { return this.ExpressionKind; }
        }

        object IExpression.ConstantValue
        {
            get
            {
                ConstantValue value = this.ConstantValue;
                if (value == null)
                {
                    return null;
                }

                return value.Value;
            }
        }

        SyntaxNode IOperation.Syntax
        {
            get { return this.Syntax; }
        }

        protected virtual OperationKind ExpressionKind{ get { return OperationKind.None; } }
        // protected abstract Unified.ExpressionKind ExpressionKind { get; }
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
        IMethodSymbol IInvocation.TargetMethod
        {
            get { return this.Method; }
        }

        IExpression IInvocation.Instance
        {
            get { return this.ReceiverOpt; }
        }

        InvocationKind IInvocation.InvocationClass
        {
            get
            {
                if (this.Method.IsStatic)
                {
                    return InvocationKind.Static;
                }

                if (this.Method.IsVirtual && !this.ReceiverOpt.SuppressVirtualCalls)
                {
                    return InvocationKind.Virtual;
                }

                return InvocationKind.NonVirtualInstance;
            }
        }
        ImmutableArray<IArgument> IInvocation.Arguments
        {
            get { return DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt); }
        }

        IArgument IInvocation.ArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return ArgumentMatchingParameter(this.Arguments, this.ArgsToParamsOpt, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, parameter.ContainingSymbol as IMethodSymbol, parameter);
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.Invocation; }
        }

        internal static ImmutableArray<IArgument> DeriveArguments(ImmutableArray<BoundExpression> boundArguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds)
        {
            ArrayBuilder<IArgument> arguments = ArrayBuilder<IArgument>.GetInstance(boundArguments.Length);
            for (int index = 0; index < boundArguments.Length; index++)
            {
                arguments[index] = DeriveArgument(index, boundArguments, argumentNames, argumentRefKinds);
            }

            return arguments.ToImmutableAndFree();
        }

        private static System.Runtime.CompilerServices.ConditionalWeakTable<BoundExpression, IArgument> ArgumentMappings = new System.Runtime.CompilerServices.ConditionalWeakTable<BoundExpression, IArgument>();

        private static IArgument DeriveArgument(int index, ImmutableArray<BoundExpression> boundArguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds)
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
                            ? new SimpleArgument(argument)
                            // ZZZ Figure out which arguments match a params parameter.
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
                return DeriveArgument(index, arguments, argumentNames, argumentRefKinds);
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

            public ArgumentKind ArgumentClass
            {
                get { return ArgumentKind.Positional; }
            }

            public ArgumentMode Mode
            {
                get { return ArgumentMode.In; }
            }

            public IExpression Value
            {
                get { return this.ArgumentValue; }
            }

            public IExpression InConversion
            {
                get { return null; }
            }

            public IExpression OutConversion
            {
                get { return null; }
            }
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

            public ArgumentKind ArgumentClass
            {
                get { return this.ArgumentKind; }
            }

            public ArgumentMode Mode
            {
                get { return this.ArgumentMode; }
            }

            public IExpression Value
            {
                get { return this.ArgumentValue; }
            }

            public IExpression InConversion
            {
                get { return null; }
            }

            public IExpression OutConversion
            {
                get { return null; }
            }
        }

        class NamedArgument : Argument, INamedArgument
        {
            readonly string ArgumentName;
            public NamedArgument(ArgumentMode mode, IExpression value, string name)
                : base(ArgumentKind.Named, mode, value)
            {
                this.ArgumentName = name;
            }
            public string Name
            {
                get { return this.ArgumentName; }
            }
        }
    }

    partial class BoundLocal : ILocalReference
    {
        ILocalSymbol ILocalReference.Local
        {
            get { return this.LocalSymbol; }
        }

        ReferenceKind IReference.ReferenceClass
        {
            get { return ReferenceKind.Local; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.LocalReference; }
        }
    }

    partial class BoundFieldAccess : IFieldReference
    {
        IExpression IMemberReference.Instance
        {
            get { return this.ReceiverOpt; }
        }

        IFieldSymbol IFieldReference.Field
        {
            get { return this.FieldSymbol; }
        }

        ReferenceKind IReference.ReferenceClass
        {
            get { return this.FieldSymbol.IsStatic ? ReferenceKind.StaticField : ReferenceKind.InstanceField; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.FieldReference; }
        }
    }

    partial class BoundPropertyAccess : IPropertyReference
    {
        IPropertySymbol IPropertyReference.Property
        {
            get { return this.PropertySymbol; }
        }

        IExpression IMemberReference.Instance
        {
            get { return this.ReceiverOpt; }
        }

        ReferenceKind IReference.ReferenceClass
        {
            get { return this.PropertySymbol.IsStatic ? ReferenceKind.StaticProperty : ReferenceKind.InstanceProperty; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.PropertyReference; }
        }
    }

    partial class BoundParameter : IParameterReference
    {
        IParameterSymbol IParameterReference.Parameter
        {
            get { return this.ParameterSymbol; }
        }

        ReferenceKind IReference.ReferenceClass
        {
            get { return ReferenceKind.Parameter; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.ParameterReference; }
        }
    }

    partial class BoundLiteral : ILiteral
    {
        LiteralKind ILiteral.LiteralClass
        {
            get { return Semantics.Expression.DeriveLiteralKind(this.Type); }
        }

        string ILiteral.Spelling
        {
            get { return this.Syntax.ToString(); }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.Literal; }
        }
    }

    partial class BoundObjectCreationExpression : IObjectCreation
    {
        IMethodSymbol IObjectCreation.Constructor
        {
            get { return this.Constructor; }
        }

        ImmutableArray<IArgument> IObjectCreation.ConstructorArguments
        {
            get { return BoundCall.DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt); }
        }

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

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.ObjectCreation; }
        }
    }

    partial class UnboundLambda
    {
        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.UnboundLambda; }
        }
    }

    partial class BoundLambda : ILambda
    {
        IMethodSymbol ILambda.Signature
        {
            get { return this.Symbol; }
        }

        IBlock ILambda.Body
        {
            get { return this.Body; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.Lambda; }
        }
    }

    partial class BoundConversion : IConversion
    {
        IExpression IConversion.Operand
        {
            get { return this.Operand; }
        }

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

        bool IConversion.IsExplicit
        {
            get { return this.ExplicitCastInCode; }
        }

        IMethodSymbol IOperator.Operator
        {
            get { return this.SymbolOpt; }
        }

        bool IOperator.UsesOperatorMethod
        {
            get { return this.ConversionKind == CSharp.ConversionKind.ExplicitUserDefined || this.ConversionKind == CSharp.ConversionKind.ImplicitUserDefined; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.Conversion; }
        }
    }

    partial class BoundAsOperator : IConversion
    {
        IExpression IConversion.Operand
        {
            get { return this.Operand; }
        }

        Semantics.ConversionKind IConversion.Conversion
        {
            get { return Semantics.ConversionKind.AsCast; }
        }

        bool IConversion.IsExplicit
        {
            get { return true; }
        }

        IMethodSymbol IOperator.Operator
        {
            get { return null; }
        }

        bool IOperator.UsesOperatorMethod
        {
            get { return false; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.Conversion; }
        }
    }

    partial class BoundIsOperator : IIs
    {
        IExpression IIs.Operand
        {
            get { return this.Operand; }
        }

        ITypeSymbol IIs.IsType
        {
            get { return this.TargetType.Type; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.Is; }
        }
    }

    partial class BoundSizeOfOperator : ITypeOperation
    {
        TypeOperationKind ITypeOperation.TypeOperationClass
        {
            get { return TypeOperationKind.SizeOf; }
        }

        ITypeSymbol ITypeOperation.TypeOperand
        {
            get { return this.SourceType.Type; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.TypeOperation; }
        }
    }

    partial class BoundTypeOfOperator : ITypeOperation
    {
        TypeOperationKind ITypeOperation.TypeOperationClass
        {
            get { return TypeOperationKind.TypeOf; }
        }

        ITypeSymbol ITypeOperation.TypeOperand
        {
            get { return this.SourceType.Type; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.TypeOperation; }
        }
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

        ImmutableArray<IExpression> IArrayCreation.DimensionSizes
        {
            get { return this.Bounds.As<IExpression>(); }
        }

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

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.ArrayCreation; }
        }

        class ElementInitializer : IExpressionArrayInitializer
        {
            readonly BoundExpression element;

            public ElementInitializer(BoundExpression element)
            {
                this.element = element;
            }

            public IExpression ElementValue
            {
                get { return this.element; }
            }

            public ArrayInitializerKind ArrayClass
            {
                get { return ArrayInitializerKind.Expression; }
            }
        }

        class DimensionInitializer : IDimensionArrayInitializer
        {
            readonly ImmutableArray<IArrayInitializer> dimension;

            public DimensionInitializer(ImmutableArray<IArrayInitializer> dimension)
            {
                this.dimension = dimension;
            }

            public ImmutableArray<IArrayInitializer> ElementValues
            {
                get { return this.dimension; }
            }

            public ArrayInitializerKind ArrayClass
            {
                get { return ArrayInitializerKind.Dimension; }
            }
        }
    }

    partial class BoundArrayInitialization
    {
        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.None; }
        }
    }

    partial class BoundDefaultOperator
    {
        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.DefaultValue; }
        }
    }

    partial class BoundDup
    {
        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.None; }
        }
    }

    partial class BoundBaseReference
    {
        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.BaseClassInstance; }
        }
    }

    partial class BoundThisReference
    {
        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.Instance; }
        }
    }

    partial class BoundAssignmentOperator : IAssignment
    {
        IReference IAssignment.Target
        {
            get { return this.Left as IReference; }
        }

        IExpression IAssignment.Value
        {
            get { return this.Right; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.Assignment; }
        }
    }

    partial class BoundCompoundAssignmentOperator : ICompoundAssignment
    {
        BinaryOperatorCode ICompoundAssignment.Operation
        {
            get { return Expression.DeriveBinaryOperatorCode(this.Operator.Kind); }
        }

        IReference IAssignment.Target
        {
            get { return this.Left as IReference; }
        }

        IExpression IAssignment.Value
        {
            get { return this.Right; }
        }

        bool IOperator.UsesOperatorMethod
        {
            get { return (this.Operator.Kind & BinaryOperatorKind.UserDefined) != 0; }
        }

        IMethodSymbol IOperator.Operator
        {
            get { return this.Operator.Method; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.CompoundAssignment; }
        }
    }

    partial class BoundBadExpression
    {
        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.None; }
        }
    }

    partial class BoundNewT
    {
        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.TypeParameterObjectCreation; }
        }
    }

    partial class BoundUnaryOperator : IUnary
    {
        UnaryOperatorCode IUnary.Operation
        {
            get { return Expression.DeriveUnaryOperatorCode(this.OperatorKind); }
        }

        IExpression IUnary.Operand
        {
            get { return this.Operand; }
        }

        bool IOperator.UsesOperatorMethod
        {
            get { return (this.OperatorKind & UnaryOperatorKind.UserDefined) != 0; }
        }
        IMethodSymbol IOperator.Operator
        {
            get { return this.MethodOpt; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.UnaryOperator; }
        }
    }

    partial class BoundBinaryOperator : IBinary, IRelational
    {
        BinaryOperatorCode IBinary.Operation
        {
            get { return Expression.DeriveBinaryOperatorCode(this.OperatorKind); }
        }

        IExpression IBinary.Left
        {
            get { return this.Left; }
        }

        IExpression IBinary.Right
        {
            get { return this.Right; }
        }

        RelationalOperatorCode IRelational.RelationalCode
        {
            get { return Expression.DeriveRelationalOperatorCode(this.OperatorKind); }
        }

        IExpression IRelational.Left
        {
            get { return this.Left; }
        }

        IExpression IRelational.Right
        {
            get { return this.Right; }
        }
   
        bool IOperator.UsesOperatorMethod
        {
            get { return (this.OperatorKind & BinaryOperatorKind.UserDefined) != 0; }
        }

        IMethodSymbol IOperator.Operator
        {
            get { return this.MethodOpt; }
        }

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
        IExpression IConditionalChoice.Condition
        {
            get { return this.Condition; }
        }

        IExpression IConditionalChoice.IfTrue
        {
            get { return this.Consequence; }
        }

        IExpression IConditionalChoice.IfFalse
        {
            get { return this.Alternative; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.ConditionalChoice; }
        }
    }

    partial class BoundNullCoalescingOperator : INullCoalescing
    {
        IExpression INullCoalescing.Primary
        {
            get { return this.LeftOperand; }
        }

        IExpression INullCoalescing.Secondary
        {
            get { return this.RightOperand; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.NullCoalescing; }
        }
    }

    partial class BoundAwaitExpression : IAwait
    {
        IExpression IAwait.Upon
        {
            get { return this.Expression; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.Await; }
        }
    }

    partial class BoundArrayAccess : IArrayElementReference
    {
        IExpression IArrayElementReference.ArrayReference
        {
            get { return this.Expression; }
        }

        ImmutableArray<IExpression> IArrayElementReference.Indices
        {
            get { return this.Indices.As<IExpression>(); }
        }

        ReferenceKind IReference.ReferenceClass
        {
            get { return ReferenceKind.ArrayElement; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.ArrayElementReference; }
        }
    }

    partial class BoundPointerIndirectionOperator : IPointerIndirectionReference
    {
        IExpression IPointerIndirectionReference.Pointer
        {
            get { return this.Operand; }
        }

        ReferenceKind IReference.ReferenceClass
        {
            get { return ReferenceKind.PointerIndirection; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.PointerIndirectionReference; }
        }
    }

    partial class BoundAddressOfOperator : IAddressOf
    {
        IReference IAddressOf.Addressed
        {
            get { return (IReference)this.Operand; }
        }

        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.AddressOf; }
        }
    }

    partial class BoundImplicitReceiver
    {
        protected override OperationKind ExpressionKind
        {
            get { return OperationKind.ImplicitInstance; }
        }
    }

    class Expression
    {
        internal static UnaryOperatorCode DeriveUnaryOperatorCode(UnaryOperatorKind operatorKind)
        {
            if ((operatorKind & UnaryOperatorKind.UserDefined) != 0)
            {
                switch (operatorKind & UnaryOperatorKind.OpMask)
                {
                    case UnaryOperatorKind.PostfixIncrement:
                        return UnaryOperatorCode.OperatorPostfixIncrement;
                    case UnaryOperatorKind.PostfixDecrement:
                        return UnaryOperatorCode.OperatorPostfixDecrement;
                    case UnaryOperatorKind.PrefixIncrement:
                        return UnaryOperatorCode.OperatorPrefixIncrement;
                    case UnaryOperatorKind.PrefixDecrement:
                        return UnaryOperatorCode.OperatorPrefixDecrement;
                    case UnaryOperatorKind.UnaryPlus:
                        return UnaryOperatorCode.OperatorPlus;
                    case UnaryOperatorKind.UnaryMinus:
                        return UnaryOperatorCode.OperatorMinus;
                    case UnaryOperatorKind.LogicalNegation:
                        return UnaryOperatorCode.OperatorLogicalNot;
                    case UnaryOperatorKind.BitwiseComplement:
                        return UnaryOperatorCode.OperatorBitwiseNegation;
                    case UnaryOperatorKind.True:
                        return UnaryOperatorCode.OperatorTrue;
                    case UnaryOperatorKind.False:
                        return UnaryOperatorCode.OperatorFalse;
                }
            }

            switch (operatorKind & UnaryOperatorKind.OpMask)
            {
                case UnaryOperatorKind.PostfixIncrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return UnaryOperatorCode.IntegerPostfixIncrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return UnaryOperatorCode.UnsignedPostfixIncrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperatorCode.FloatingPostfixIncrement;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperatorCode.DecimalPostfixIncrement;
                        case UnaryOperatorKind.Enum:
                            return UnaryOperatorCode.EnumPostfixIncrement;
                        case UnaryOperatorKind.Pointer:
                            return UnaryOperatorCode.PointerPostfixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperatorCode.DynamicPostfixIncrement;
                    }

                    break;

                case UnaryOperatorKind.PostfixDecrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return UnaryOperatorCode.IntegerPostfixDecrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return UnaryOperatorCode.UnsignedPostfixDecrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperatorCode.FloatingPostfixDecrement;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperatorCode.DecimalPostfixDecrement;
                        case UnaryOperatorKind.Enum:
                            return UnaryOperatorCode.EnumPostfixDecrement;
                        case UnaryOperatorKind.Pointer:
                            return UnaryOperatorCode.PointerPostfixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperatorCode.DynamicPostfixDecrement;
                    }

                    break;

                case UnaryOperatorKind.PrefixIncrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return UnaryOperatorCode.IntegerPrefixIncrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return UnaryOperatorCode.UnsignedPrefixIncrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperatorCode.FloatingPrefixIncrement;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperatorCode.DecimalPrefixIncrement;
                        case UnaryOperatorKind.Enum:
                            return UnaryOperatorCode.EnumPrefixIncrement;
                        case UnaryOperatorKind.Pointer:
                            return UnaryOperatorCode.PointerPrefixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperatorCode.DynamicPrefixIncrement;
                    }

                    break;

                case UnaryOperatorKind.PrefixDecrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return UnaryOperatorCode.IntegerPrefixDecrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return UnaryOperatorCode.UnsignedPrefixDecrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperatorCode.FloatingPrefixDecrement;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperatorCode.DecimalPrefixDecrement;
                        case UnaryOperatorKind.Enum:
                            return UnaryOperatorCode.EnumPrefixDecrement;
                        case UnaryOperatorKind.Pointer:
                            return UnaryOperatorCode.PointerPrefixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperatorCode.DynamicPrefixDecrement;
                    }

                    break;

                case UnaryOperatorKind.UnaryPlus:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.ULong:
                            return UnaryOperatorCode.IntegerPlus;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperatorCode.FloatingPlus;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperatorCode.DecimalPlus;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperatorCode.DynamicPlus;
                    }

                    break;

                case UnaryOperatorKind.UnaryMinus:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.ULong:
                            return UnaryOperatorCode.IntegerMinus;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperatorCode.FloatingMinus;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperatorCode.DecimalMinus;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperatorCode.DynamicMinus;
                    }

                    break;

                case UnaryOperatorKind.LogicalNegation:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Bool:
                            return UnaryOperatorCode.BooleanLogicalNot;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperatorCode.DynamicLogicalNot;
                    }

                    break;
                case UnaryOperatorKind.BitwiseComplement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.ULong:
                            return UnaryOperatorCode.IntegerBitwiseNegation;
                        case UnaryOperatorKind.Bool:
                            return UnaryOperatorCode.BooleanBitwiseNegation;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperatorCode.DynamicBitwiseNegation;
                    }

                    break;

                case UnaryOperatorKind.True:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperatorCode.DynamicTrue;
                    }

                    break;

                case UnaryOperatorKind.False:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperatorCode.DynamicFalse;
                    }

                    break;
            }

            return UnaryOperatorCode.None;
        }

        internal static BinaryOperatorCode DeriveBinaryOperatorCode(BinaryOperatorKind operatorKind)
        {
            if ((operatorKind & BinaryOperatorKind.UserDefined) != 0)
            {
                switch (operatorKind & BinaryOperatorKind.OpMask)
                {
                    case BinaryOperatorKind.Addition:
                        return BinaryOperatorCode.OperatorAdd;
                    case BinaryOperatorKind.Subtraction:
                        return BinaryOperatorCode.OperatorSubtract;
                    case BinaryOperatorKind.Multiplication:
                        return BinaryOperatorCode.OperatorMultiply;
                    case BinaryOperatorKind.Division:
                        return BinaryOperatorCode.OperatorDivide;
                    case BinaryOperatorKind.Remainder:
                        return BinaryOperatorCode.OperatorRemainder;
                    case BinaryOperatorKind.LeftShift:
                        return BinaryOperatorCode.OperatorLeftShift;
                    case BinaryOperatorKind.RightShift:
                        return BinaryOperatorCode.OperatorRightShift;
                    case BinaryOperatorKind.And:
                        if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                        {
                            return BinaryOperatorCode.OperatorConditionalAnd;
                        }

                        return BinaryOperatorCode.OperatorAnd;
                    case BinaryOperatorKind.Or:
                        if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                        {
                            return BinaryOperatorCode.OperatorConditionalOr;
                        }

                        return BinaryOperatorCode.OperatorOr;
                    case BinaryOperatorKind.Xor:
                        return BinaryOperatorCode.OperatorXor;
                }
            }

            switch (operatorKind & BinaryOperatorKind.OpMask)
            {
                case BinaryOperatorKind.Addition:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperatorCode.IntegerAdd;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperatorCode.UnsignedAdd;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return BinaryOperatorCode.FloatingAdd;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperatorCode.DecimalAdd;
                        case BinaryOperatorKind.EnumAndUnderlying:
                        case BinaryOperatorKind.UnderlyingAndEnum:
                            return BinaryOperatorCode.EnumAdd;
                        case BinaryOperatorKind.PointerAndInt:
                        case BinaryOperatorKind.PointerAndUInt:
                        case BinaryOperatorKind.PointerAndLong:
                        case BinaryOperatorKind.PointerAndULong:
                            return BinaryOperatorCode.PointerIntegerAdd;
                        case BinaryOperatorKind.IntAndPointer:
                        case BinaryOperatorKind.UIntAndPointer:
                        case BinaryOperatorKind.LongAndPointer:
                        case BinaryOperatorKind.ULongAndPointer:
                            return BinaryOperatorCode.IntegerPointerAdd;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperatorCode.DynamicAdd;
                        case BinaryOperatorKind.String:
                        case BinaryOperatorKind.StringAndObject:
                        case BinaryOperatorKind.ObjectAndString:
                            return BinaryOperatorCode.StringConcatenation;
                    }

                    break;

                case BinaryOperatorKind.Subtraction:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperatorCode.IntegerSubtract;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperatorCode.UnsignedSubtract;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return BinaryOperatorCode.FloatingSubtract;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperatorCode.DecimalSubtract;
                        case BinaryOperatorKind.EnumAndUnderlying:
                        case BinaryOperatorKind.UnderlyingAndEnum:
                            return BinaryOperatorCode.EnumSubtract;
                        case BinaryOperatorKind.PointerAndInt:
                        case BinaryOperatorKind.PointerAndUInt:
                        case BinaryOperatorKind.PointerAndLong:
                        case BinaryOperatorKind.PointerAndULong:
                            return BinaryOperatorCode.PointerIntegerSubtract;
                        case BinaryOperatorKind.Pointer:
                            return BinaryOperatorCode.PointerSubtract;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperatorCode.DynamicSubtract;
                    }

                    break;

                case BinaryOperatorKind.Multiplication:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperatorCode.IntegerMultiply;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperatorCode.UnsignedMultiply;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return BinaryOperatorCode.FloatingMultiply;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperatorCode.DecimalMultiply;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperatorCode.DynamicMultiply;
                    }

                    break;

                case BinaryOperatorKind.Division:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperatorCode.IntegerDivide;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperatorCode.UnsignedDivide;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return BinaryOperatorCode.FloatingDivide;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperatorCode.DecimalDivide;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperatorCode.DynamicDivide;
                    }

                    break;

                case BinaryOperatorKind.Remainder:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperatorCode.IntegerRemainder;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperatorCode.UnsignedRemainder;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return BinaryOperatorCode.FloatingRemainder;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperatorCode.DynamicRemainder;
                    }

                    break;

                case BinaryOperatorKind.LeftShift:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperatorCode.IntegerLeftShift;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperatorCode.UnsignedLeftShift;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperatorCode.DynamicLeftShift;
                    }

                    break;

                case BinaryOperatorKind.RightShift:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperatorCode.IntegerRightShift;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperatorCode.UnsignedRightShift;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperatorCode.DynamicRightShift;
                    }

                    break;

                case BinaryOperatorKind.And:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperatorCode.IntegerAnd;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperatorCode.UnsignedAnd;
                        case BinaryOperatorKind.Bool:
                            if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                            {
                                return BinaryOperatorCode.BooleanConditionalAnd;
                            }

                            return BinaryOperatorCode.BooleanAnd;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperatorCode.EnumAnd;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperatorCode.DynamicAnd;
                    }

                    break;

                case BinaryOperatorKind.Or:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperatorCode.IntegerOr;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperatorCode.UnsignedOr;
                        case BinaryOperatorKind.Bool:
                            if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                            {
                                return BinaryOperatorCode.BooleanConditionalOr;
                            }

                            return BinaryOperatorCode.BooleanOr;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperatorCode.EnumOr;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperatorCode.DynamicOr;
                    }

                    break;

                case BinaryOperatorKind.Xor:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperatorCode.IntegerXor;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperatorCode.UnsignedXor;
                        case BinaryOperatorKind.Bool:
                            return BinaryOperatorCode.BooleanXor;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperatorCode.EnumXor;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperatorCode.DynamicXor;
                    }

                    break;
            }

            return BinaryOperatorCode.None;
        }

        internal static RelationalOperatorCode DeriveRelationalOperatorCode(BinaryOperatorKind operatorKind)
        {
            if ((operatorKind & BinaryOperatorKind.UserDefined) != 0)
            {
                switch (operatorKind & BinaryOperatorKind.OpMask)
                {
                    case BinaryOperatorKind.LessThan:
                        return RelationalOperatorCode.OperatorLess;
                    case BinaryOperatorKind.LessThanOrEqual:
                        return RelationalOperatorCode.OperatorLessEqual;
                    case BinaryOperatorKind.Equal:
                        return RelationalOperatorCode.OperatorEqual;
                    case BinaryOperatorKind.NotEqual:
                        return RelationalOperatorCode.OperatorNotEqual;
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return RelationalOperatorCode.OperatorGreaterEqual;
                    case BinaryOperatorKind.GreaterThan:
                        return RelationalOperatorCode.OperatorGreater;
                }
            }

            switch (operatorKind & BinaryOperatorKind.OpMask)
            {
                case BinaryOperatorKind.LessThan:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return RelationalOperatorCode.IntegerLess;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperatorCode.UnsignedLess;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperatorCode.FloatingLess;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperatorCode.DecimalLess;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperatorCode.PointerLess;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperatorCode.EnumLess;
                    }

                    break;

                case BinaryOperatorKind.LessThanOrEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return RelationalOperatorCode.IntegerLessEqual;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperatorCode.UnsignedLessEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperatorCode.FloatingLessEqual;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperatorCode.DecimalLessEqual;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperatorCode.PointerLessEqual;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperatorCode.EnumLessEqual;
                    }

                    break;

                case BinaryOperatorKind.Equal:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperatorCode.IntegerEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperatorCode.FloatingEqual;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperatorCode.DecimalEqual;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperatorCode.PointerEqual;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperatorCode.EnumEqual;
                        case BinaryOperatorKind.Bool:
                            return RelationalOperatorCode.BooleanEqual;
                        case BinaryOperatorKind.String:
                            return RelationalOperatorCode.StringEqual;
                        case BinaryOperatorKind.Object:
                            return RelationalOperatorCode.ObjectEqual;
                        case BinaryOperatorKind.Delegate:
                            return RelationalOperatorCode.DelegateEqual;
                        case BinaryOperatorKind.NullableNull:
                            return RelationalOperatorCode.NullableEqual;
                    }

                    break;

                case BinaryOperatorKind.NotEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperatorCode.IntegerNotEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperatorCode.FloatingNotEqual;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperatorCode.DecimalNotEqual;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperatorCode.PointerNotEqual;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperatorCode.EnumNotEqual;
                        case BinaryOperatorKind.Bool:
                            return RelationalOperatorCode.BooleanNotEqual;
                        case BinaryOperatorKind.String:
                            return RelationalOperatorCode.StringNotEqual;
                        case BinaryOperatorKind.Object:
                            return RelationalOperatorCode.ObjectNotEqual;
                        case BinaryOperatorKind.Delegate:
                            return RelationalOperatorCode.DelegateNotEqual;
                        case BinaryOperatorKind.NullableNull:
                            return RelationalOperatorCode.NullableNotEqual;
                    }

                    break;

                case BinaryOperatorKind.GreaterThanOrEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return RelationalOperatorCode.IntegerGreaterEqual;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperatorCode.UnsignedGreaterEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperatorCode.FloatingGreaterEqual;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperatorCode.DecimalGreaterEqual;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperatorCode.PointerGreaterEqual;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperatorCode.EnumGreaterEqual;
                    }

                    break;

                case BinaryOperatorKind.GreaterThan:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return RelationalOperatorCode.IntegerGreater;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return RelationalOperatorCode.UnsignedGreater;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return RelationalOperatorCode.FloatingGreater;
                        case BinaryOperatorKind.Decimal:
                            return RelationalOperatorCode.DecimalGreater;
                        case BinaryOperatorKind.Pointer:
                            return RelationalOperatorCode.PointerGreater;
                        case BinaryOperatorKind.Enum:
                            return RelationalOperatorCode.EnumGreater;
                    }

                    break;
            }

            return RelationalOperatorCode.None;
        }
    }
}
