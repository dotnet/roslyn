using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundExpression : Semantics.IExpression
    {
        ITypeSymbol Semantics.IExpression.ResultType
        {
            get { return this.Type; }
        }

        Semantics.OperationKind Semantics.IOperation.Kind
        {
            get { return this.ExpressionKind; }
        }

        object Semantics.IExpression.ConstantValue
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

        SyntaxNode Semantics.IOperation.Syntax
        {
            get { return this.Syntax; }
        }

        protected virtual Semantics.OperationKind ExpressionKind{ get { return Semantics.OperationKind.None; } }
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

    partial class BoundCall : Semantics.IInvocation
    {
        IMethodSymbol Semantics.IInvocation.TargetMethod
        {
            get { return this.Method; }
        }

        Semantics.IExpression Semantics.IInvocation.Instance
        {
            get { return this.ReceiverOpt; }
        }

        Semantics.InvocationKind Semantics.IInvocation.InvocationClass
        {
            get
            {
                if (this.Method.IsStatic)
                {
                    return Semantics.InvocationKind.Static;
                }

                if (this.Method.IsVirtual && !this.ReceiverOpt.SuppressVirtualCalls)
                {
                    return Semantics.InvocationKind.Virtual;
                }

                return Semantics.InvocationKind.NonVirtualInstance;
            }
        }
        ImmutableArray<Semantics.IArgument> Semantics.IInvocation.Arguments
        {
            get { return DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt); }
        }

        Semantics.IArgument Semantics.IInvocation.ArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return ArgumentMatchingParameter(this.Arguments, this.ArgsToParamsOpt, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, parameter.ContainingSymbol as IMethodSymbol, parameter);
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.Invocation; }
        }

        internal static ImmutableArray<Semantics.IArgument> DeriveArguments(ImmutableArray<BoundExpression> boundArguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds)
        {
            ArrayBuilder<Semantics.IArgument> arguments = ArrayBuilder<Semantics.IArgument>.GetInstance(boundArguments.Length);
            for (int index = 0; index < boundArguments.Length; index++)
            {
                arguments[index] = DeriveArgument(index, boundArguments, argumentNames, argumentRefKinds);
            }

            return arguments.ToImmutableAndFree();
        }

        private static Semantics.IArgument DeriveArgument(int index, ImmutableArray<BoundExpression> boundArguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds)
        {
            BoundExpression argument = boundArguments[index];
            string name = !argumentNames.IsDefaultOrEmpty ? argumentNames[index] : null;
            RefKind refMode = !argumentRefKinds.IsDefaultOrEmpty ? argumentRefKinds[index] : RefKind.None;
            Semantics.ArgumentMode mode = refMode == RefKind.None ? Semantics.ArgumentMode.In : (refMode == RefKind.Out ? Semantics.ArgumentMode.Out : Semantics.ArgumentMode.Reference);
            if (name == null)
            {
                return
                    mode == Semantics.ArgumentMode.In
                    ? new SimpleArgument(argument)
                    // ZZZ Figure out which arguments match a params parameter.
                    : (Semantics.IArgument)new Argument(Semantics.ArgumentKind.Positional, mode, argument);
            }

            return new NamedArgument(mode, argument, name);
        }

        internal static Semantics.IArgument ArgumentMatchingParameter(ImmutableArray<BoundExpression> arguments, ImmutableArray<int> argumentsToParameters, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, IMethodSymbol targetMethod, IParameterSymbol parameter)
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

        class SimpleArgument : Semantics.IArgument
        {
            readonly Semantics.IExpression ArgumentValue;
            public SimpleArgument(Semantics.IExpression value)
            {
                this.ArgumentValue = value;
            }

            public Semantics.ArgumentKind ArgumentClass
            {
                get { return Semantics.ArgumentKind.Positional; }
            }

            public Semantics.ArgumentMode Mode
            {
                get { return Semantics.ArgumentMode.In; }
            }

            public Semantics.IExpression Value
            {
                get { return this.ArgumentValue; }
            }

            public Semantics.IExpression InConversion
            {
                get { return null; }
            }

            public Semantics.IExpression OutConversion
            {
                get { return null; }
            }
        }

        class Argument : Semantics.IArgument
        {
            readonly Semantics.IExpression ArgumentValue;
            readonly Semantics.ArgumentKind ArgumentKind;
            readonly Semantics.ArgumentMode ArgumentMode;
            public Argument(Semantics.ArgumentKind kind, Semantics.ArgumentMode mode, Semantics.IExpression value)
            {
                this.ArgumentValue = value;
                this.ArgumentKind = kind;
                this.ArgumentMode = mode;
            }

            public Semantics.ArgumentKind ArgumentClass
            {
                get { return this.ArgumentKind; }
            }

            public Semantics.ArgumentMode Mode
            {
                get { return this.ArgumentMode; }
            }

            public Semantics.IExpression Value
            {
                get { return this.ArgumentValue; }
            }

            public Semantics.IExpression InConversion
            {
                get { return null; }
            }

            public Semantics.IExpression OutConversion
            {
                get { return null; }
            }
        }

        class NamedArgument : Argument, Semantics.INamedArgument
        {
            readonly string ArgumentName;
            public NamedArgument(Semantics.ArgumentMode mode, Semantics.IExpression value, string name)
                : base(Semantics.ArgumentKind.Named, mode, value)
            {
                this.ArgumentName = name;
            }
            public string Name
            {
                get { return this.ArgumentName; }
            }
        }
    }

    partial class BoundLocal : Semantics.ILocalReference
    {
        ILocalSymbol Semantics.ILocalReference.Local
        {
            get { return this.LocalSymbol; }
        }

        Semantics.ReferenceKind Semantics.IReference.ReferenceClass
        {
            get { return Semantics.ReferenceKind.Local; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.LocalReference; }
        }
    }

    partial class BoundFieldAccess : Semantics.IFieldReference
    {
        Semantics.IExpression Semantics.IMemberReference.Instance
        {
            get { return this.ReceiverOpt; }
        }

        IFieldSymbol Semantics.IFieldReference.Field
        {
            get { return this.FieldSymbol; }
        }

        Semantics.ReferenceKind Semantics.IReference.ReferenceClass
        {
            get { return this.FieldSymbol.IsStatic ? Semantics.ReferenceKind.StaticField : Semantics.ReferenceKind.InstanceField; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.FieldReference; }
        }
    }

    partial class BoundPropertyAccess : Semantics.IPropertyReference
    {
        IPropertySymbol Semantics.IPropertyReference.Property
        {
            get { return this.PropertySymbol; }
        }

        Semantics.IExpression Semantics.IMemberReference.Instance
        {
            get { return this.ReceiverOpt; }
        }

        Semantics.ReferenceKind Semantics.IReference.ReferenceClass
        {
            get { return this.PropertySymbol.IsStatic ? Semantics.ReferenceKind.StaticProperty : Semantics.ReferenceKind.InstanceProperty; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.PropertyReference; }
        }
    }

    partial class BoundParameter : Semantics.IParameterReference
    {
        IParameterSymbol Semantics.IParameterReference.Parameter
        {
            get { return this.ParameterSymbol; }
        }

        Semantics.ReferenceKind Semantics.IReference.ReferenceClass
        {
            get { return Semantics.ReferenceKind.Parameter; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.ParameterReference; }
        }
    }

    partial class BoundLiteral : Semantics.ILiteral
    {
        Semantics.LiteralKind Semantics.ILiteral.LiteralClass
        {
            get { return Semantics.Expression.DeriveLiteralKind(this.Type); }
        }

        string Semantics.ILiteral.Spelling
        {
            get { return this.Syntax.ToString(); }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.Literal; }
        }
    }

    partial class BoundObjectCreationExpression : Semantics.IObjectCreation
    {
        IMethodSymbol Semantics.IObjectCreation.Constructor
        {
            get { return this.Constructor; }
        }

        ImmutableArray<Semantics.IArgument> Semantics.IObjectCreation.ConstructorArguments
        {
            get { return BoundCall.DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt); }
        }

        Semantics.IArgument Semantics.IObjectCreation.ArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return BoundCall.ArgumentMatchingParameter(this.Arguments, this.ArgsToParamsOpt, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, this.Constructor, parameter);
        }

        ImmutableArray<Semantics.IMemberInitializer> Semantics.IObjectCreation.MemberInitializers
        {
            get
            {
                BoundExpression initializer = this.InitializerExpressionOpt;
                if (initializer != null)
                {
                    // ZZZ What's the representation in bound trees?
                }

                return ImmutableArray.Create<Semantics.IMemberInitializer>();
            }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.ObjectCreation; }
        }
    }

    partial class UnboundLambda
    {
        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.UnboundLambda; }
        }
    }

    partial class BoundLambda : Semantics.ILambda
    {
        IMethodSymbol Semantics.ILambda.Signature
        {
            get { return this.Symbol; }
        }

        Semantics.IBlock Semantics.ILambda.Body
        {
            get { return this.Body; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.Lambda; }
        }
    }

    partial class BoundConversion : Semantics.IConversion
    {
        Semantics.IExpression Semantics.IConversion.Operand
        {
            get { return this.Operand; }
        }

        Semantics.ConversionKind Semantics.IConversion.Conversion
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

        bool Semantics.IConversion.IsExplicit
        {
            get { return this.ExplicitCastInCode; }
        }

        IMethodSymbol Semantics.IOperator.Operator
        {
            get { return this.SymbolOpt; }
        }

        bool Semantics.IOperator.UsesOperatorMethod
        {
            get { return this.ConversionKind == CSharp.ConversionKind.ExplicitUserDefined || this.ConversionKind == CSharp.ConversionKind.ImplicitUserDefined; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.Conversion; }
        }
    }

    partial class BoundAsOperator : Semantics.IConversion
    {
        Semantics.IExpression Semantics.IConversion.Operand
        {
            get { return this.Operand; }
        }

        Semantics.ConversionKind Semantics.IConversion.Conversion
        {
            get { return Semantics.ConversionKind.AsCast; }
        }

        bool Semantics.IConversion.IsExplicit
        {
            get { return true; }
        }

        IMethodSymbol Semantics.IOperator.Operator
        {
            get { return null; }
        }

        bool Semantics.IOperator.UsesOperatorMethod
        {
            get { return false; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.Conversion; }
        }
    }

    partial class BoundIsOperator : Semantics.IIs
    {
        Semantics.IExpression Semantics.IIs.Operand
        {
            get { return this.Operand; }
        }

        ITypeSymbol Semantics.IIs.IsType
        {
            get { return this.TargetType.Type; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.Is; }
        }
    }

    partial class BoundSizeOfOperator : Semantics.ITypeOperation
    {
        Semantics.TypeOperationKind Semantics.ITypeOperation.TypeOperationClass
        {
            get { return CodeAnalysis.Semantics.TypeOperationKind.SizeOf; }
        }

        ITypeSymbol Semantics.ITypeOperation.TypeOperand
        {
            get { return this.SourceType.Type; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.TypeOperation; }
        }
    }

    partial class BoundTypeOfOperator : Semantics.ITypeOperation
    {
        Semantics.TypeOperationKind Semantics.ITypeOperation.TypeOperationClass
        {
            get { return CodeAnalysis.Semantics.TypeOperationKind.TypeOf; }
        }

        ITypeSymbol Semantics.ITypeOperation.TypeOperand
        {
            get { return this.SourceType.Type; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.TypeOperation; }
        }
    }

    partial class BoundArrayCreation : Semantics.IArrayCreation
    {
        ITypeSymbol Semantics.IArrayCreation.ElementType
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

        ImmutableArray<Semantics.IExpression> Semantics.IArrayCreation.DimensionSizes
        {
            get { return this.Bounds.As<Semantics.IExpression>(); }
        }

        Semantics.IArrayInitializer Semantics.IArrayCreation.ElementValues
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

        Semantics.IArrayInitializer MakeInitializer(BoundArrayInitialization initializer)
        {
            ArrayBuilder<Semantics.IArrayInitializer> dimension = ArrayBuilder<Semantics.IArrayInitializer>.GetInstance(initializer.Initializers.Length);
            for (int index = 0; index < initializer.Initializers.Length; index++)
            {
                BoundExpression elementInitializer = initializer.Initializers[index];
                BoundArrayInitialization elementArray = elementInitializer as BoundArrayInitialization;
                dimension[index] = elementArray != null ? MakeInitializer(elementArray) : new ElementInitializer(elementInitializer);
            }

            return new DimensionInitializer(dimension.ToImmutableAndFree());
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.ArrayCreation; }
        }

        class ElementInitializer : Semantics.IExpressionArrayInitializer
        {
            readonly BoundExpression element;

            public ElementInitializer(BoundExpression element)
            {
                this.element = element;
            }

            public Semantics.IExpression ElementValue
            {
                get { return this.element; }
            }

            public Semantics.ArrayInitializerKind ArrayClass
            {
                get { return Semantics.ArrayInitializerKind.Expression; }
            }
        }

        class DimensionInitializer : Semantics.IDimensionArrayInitializer
        {
            readonly ImmutableArray<Semantics.IArrayInitializer> dimension;

            public DimensionInitializer(ImmutableArray<Semantics.IArrayInitializer> dimension)
            {
                this.dimension = dimension;
            }

            public ImmutableArray<Semantics.IArrayInitializer> ElementValues
            {
                get { return this.dimension; }
            }

            public Semantics.ArrayInitializerKind ArrayClass
            {
                get { return Semantics.ArrayInitializerKind.Dimension; }
            }
        }
    }

    partial class BoundArrayInitialization
    {
        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.None; }
        }
    }

    partial class BoundDefaultOperator
    {
        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.DefaultValue; }
        }
    }

    partial class BoundDup
    {
        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.None; }
        }
    }

    partial class BoundBaseReference
    {
        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.BaseClassInstance; }
        }
    }

    partial class BoundThisReference
    {
        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.Instance; }
        }
    }

    partial class BoundAssignmentOperator : Semantics.IAssignment
    {
        Semantics.IReference Semantics.IAssignment.Target
        {
            get { return this.Left as Semantics.IReference; }
        }

        Semantics.IExpression Semantics.IAssignment.Value
        {
            get { return this.Right; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.Assignment; }
        }
    }

    partial class BoundCompoundAssignmentOperator : Semantics.ICompoundAssignment
    {
        Semantics.BinaryOperatorCode Semantics.ICompoundAssignment.Operation
        {
            get { return Expression.DeriveBinaryOperatorCode(this.Operator.Kind); }
        }

        Semantics.IReference Semantics.IAssignment.Target
        {
            get { return this.Left as Semantics.IReference; }
        }

        Semantics.IExpression Semantics.IAssignment.Value
        {
            get { return this.Right; }
        }

        bool Semantics.IOperator.UsesOperatorMethod
        {
            get { return (this.Operator.Kind & BinaryOperatorKind.UserDefined) != 0; }
        }

        IMethodSymbol Semantics.IOperator.Operator
        {
            get { return this.Operator.Method; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.CompoundAssignment; }
        }
    }

    partial class BoundBadExpression
    {
        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.None; }
        }
    }

    partial class BoundNewT
    {
        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.TypeParameterObjectCreation; }
        }
    }

    partial class BoundUnaryOperator : Semantics.IUnary
    {
        Semantics.UnaryOperatorCode Semantics.IUnary.Operation
        {
            get { return Expression.DeriveUnaryOperatorCode(this.OperatorKind); }
        }

        Semantics.IExpression Semantics.IUnary.Operand
        {
            get { return this.Operand; }
        }

        bool Semantics.IOperator.UsesOperatorMethod
        {
            get { return (this.OperatorKind & UnaryOperatorKind.UserDefined) != 0; }
        }
        IMethodSymbol Semantics.IOperator.Operator
        {
            get { return this.MethodOpt; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.UnaryOperator; }
        }
    }

    partial class BoundBinaryOperator : Semantics.IBinary, Semantics.IRelational
    {
        Semantics.BinaryOperatorCode Semantics.IBinary.Operation
        {
            get { return Expression.DeriveBinaryOperatorCode(this.OperatorKind); }
        }

        Semantics.IExpression Semantics.IBinary.Left
        {
            get { return this.Left; }
        }

        Semantics.IExpression Semantics.IBinary.Right
        {
            get { return this.Right; }
        }

        Semantics.RelationalOperatorCode Semantics.IRelational.RelationalCode
        {
            get { return Expression.DeriveRelationalOperatorCode(this.OperatorKind); }
        }

        Semantics.IExpression Semantics.IRelational.Left
        {
            get { return this.Left; }
        }

        Semantics.IExpression Semantics.IRelational.Right
        {
            get { return this.Right; }
        }
   
        bool Semantics.IOperator.UsesOperatorMethod
        {
            get { return (this.OperatorKind & BinaryOperatorKind.UserDefined) != 0; }
        }

        IMethodSymbol Semantics.IOperator.Operator
        {
            get { return this.MethodOpt; }
        }

        protected override Semantics.OperationKind ExpressionKind
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
                        return Semantics.OperationKind.BinaryOperator;
                    case BinaryOperatorKind.LessThan:
                    case BinaryOperatorKind.LessThanOrEqual:
                    case BinaryOperatorKind.Equal:
                    case BinaryOperatorKind.NotEqual:
                    case BinaryOperatorKind.GreaterThan:
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return Semantics.OperationKind.RelationalOperator;
                }

                return Semantics.OperationKind.None;
            }
        }
    }

    partial class BoundConditionalOperator : Semantics.IConditionalChoice
    {
        Semantics.IExpression Semantics.IConditionalChoice.Condition
        {
            get { return this.Condition; }
        }

        Semantics.IExpression Semantics.IConditionalChoice.IfTrue
        {
            get { return this.Consequence; }
        }

        Semantics.IExpression Semantics.IConditionalChoice.IfFalse
        {
            get { return this.Alternative; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.ConditionalChoice; }
        }
    }

    partial class BoundNullCoalescingOperator : Semantics.INullCoalescing
    {
        Semantics.IExpression Semantics.INullCoalescing.Primary
        {
            get { return this.LeftOperand; }
        }

        Semantics.IExpression Semantics.INullCoalescing.Secondary
        {
            get { return this.RightOperand; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.NullCoalescing; }
        }
    }

    partial class BoundAwaitExpression : Semantics.IAwait
    {
        Semantics.IExpression Semantics.IAwait.Upon
        {
            get { return this.Expression; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.Await; }
        }
    }

    partial class BoundArrayAccess : Semantics.IArrayElementReference
    {
        Semantics.IExpression Semantics.IArrayElementReference.ArrayReference
        {
            get { return this.Expression; }
        }

        ImmutableArray<Semantics.IExpression> Semantics.IArrayElementReference.Indices
        {
            get { return this.Indices.As<Semantics.IExpression>(); }
        }

        Semantics.ReferenceKind Semantics.IReference.ReferenceClass
        {
            get { return Semantics.ReferenceKind.ArrayElement; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.ArrayElementReference; }
        }
    }

    partial class BoundPointerIndirectionOperator : Semantics.IPointerIndirectionReference
    {
        Semantics.IExpression Semantics.IPointerIndirectionReference.Pointer
        {
            get { return this.Operand; }
        }

        Semantics.ReferenceKind Semantics.IReference.ReferenceClass
        {
            get { return Semantics.ReferenceKind.PointerIndirection; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.PointerIndirectionReference; }
        }
    }

    partial class BoundAddressOfOperator : Semantics.IAddressOf
    {
        Semantics.IReference Semantics.IAddressOf.Addressed
        {
            get { return (Semantics.IReference)this.Operand; }
        }

        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.AddressOf; }
        }
    }

    partial class BoundImplicitReceiver
    {
        protected override Semantics.OperationKind ExpressionKind
        {
            get { return Semantics.OperationKind.ImplicitInstance; }
        }
    }

    class Expression
    {
        internal static Semantics.UnaryOperatorCode DeriveUnaryOperatorCode(UnaryOperatorKind operatorKind)
        {
            if ((operatorKind & UnaryOperatorKind.UserDefined) != 0)
            {
                switch (operatorKind & UnaryOperatorKind.OpMask)
                {
                    case UnaryOperatorKind.PostfixIncrement:
                        return Semantics.UnaryOperatorCode.OperatorPostfixIncrement;
                    case UnaryOperatorKind.PostfixDecrement:
                        return Semantics.UnaryOperatorCode.OperatorPostfixDecrement;
                    case UnaryOperatorKind.PrefixIncrement:
                        return Semantics.UnaryOperatorCode.OperatorPrefixIncrement;
                    case UnaryOperatorKind.PrefixDecrement:
                        return Semantics.UnaryOperatorCode.OperatorPrefixDecrement;
                    case UnaryOperatorKind.UnaryPlus:
                        return Semantics.UnaryOperatorCode.OperatorPlus;
                    case UnaryOperatorKind.UnaryMinus:
                        return Semantics.UnaryOperatorCode.OperatorMinus;
                    case UnaryOperatorKind.LogicalNegation:
                        return Semantics.UnaryOperatorCode.OperatorLogicalNot;
                    case UnaryOperatorKind.BitwiseComplement:
                        return Semantics.UnaryOperatorCode.OperatorBitwiseNegation;
                    case UnaryOperatorKind.True:
                        return Semantics.UnaryOperatorCode.OperatorTrue;
                    case UnaryOperatorKind.False:
                        return Semantics.UnaryOperatorCode.OperatorFalse;
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
                            return Semantics.UnaryOperatorCode.IntegerPostfixIncrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return Semantics.UnaryOperatorCode.UnsignedPostfixIncrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return Semantics.UnaryOperatorCode.FloatingPostfixIncrement;
                        case UnaryOperatorKind.Decimal:
                            return Semantics.UnaryOperatorCode.DecimalPostfixIncrement;
                        case UnaryOperatorKind.Enum:
                            return Semantics.UnaryOperatorCode.EnumPostfixIncrement;
                        case UnaryOperatorKind.Pointer:
                            return Semantics.UnaryOperatorCode.PointerPostfixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return Semantics.UnaryOperatorCode.DynamicPostfixIncrement;
                    }

                    break;

                case UnaryOperatorKind.PostfixDecrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return Semantics.UnaryOperatorCode.IntegerPostfixDecrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return Semantics.UnaryOperatorCode.UnsignedPostfixDecrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return Semantics.UnaryOperatorCode.FloatingPostfixDecrement;
                        case UnaryOperatorKind.Decimal:
                            return Semantics.UnaryOperatorCode.DecimalPostfixDecrement;
                        case UnaryOperatorKind.Enum:
                            return Semantics.UnaryOperatorCode.EnumPostfixDecrement;
                        case UnaryOperatorKind.Pointer:
                            return Semantics.UnaryOperatorCode.PointerPostfixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return Semantics.UnaryOperatorCode.DynamicPostfixDecrement;
                    }

                    break;

                case UnaryOperatorKind.PrefixIncrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return Semantics.UnaryOperatorCode.IntegerPrefixIncrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return Semantics.UnaryOperatorCode.UnsignedPrefixIncrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return Semantics.UnaryOperatorCode.FloatingPrefixIncrement;
                        case UnaryOperatorKind.Decimal:
                            return Semantics.UnaryOperatorCode.DecimalPrefixIncrement;
                        case UnaryOperatorKind.Enum:
                            return Semantics.UnaryOperatorCode.EnumPrefixIncrement;
                        case UnaryOperatorKind.Pointer:
                            return Semantics.UnaryOperatorCode.PointerPrefixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return Semantics.UnaryOperatorCode.DynamicPrefixIncrement;
                    }

                    break;

                case UnaryOperatorKind.PrefixDecrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return Semantics.UnaryOperatorCode.IntegerPrefixDecrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return Semantics.UnaryOperatorCode.UnsignedPrefixDecrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return Semantics.UnaryOperatorCode.FloatingPrefixDecrement;
                        case UnaryOperatorKind.Decimal:
                            return Semantics.UnaryOperatorCode.DecimalPrefixDecrement;
                        case UnaryOperatorKind.Enum:
                            return Semantics.UnaryOperatorCode.EnumPrefixDecrement;
                        case UnaryOperatorKind.Pointer:
                            return Semantics.UnaryOperatorCode.PointerPrefixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return Semantics.UnaryOperatorCode.DynamicPrefixDecrement;
                    }

                    break;

                case UnaryOperatorKind.UnaryPlus:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.ULong:
                            return Semantics.UnaryOperatorCode.IntegerPlus;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return Semantics.UnaryOperatorCode.FloatingPlus;
                        case UnaryOperatorKind.Decimal:
                            return Semantics.UnaryOperatorCode.DecimalPlus;
                        case UnaryOperatorKind.Dynamic:
                            return Semantics.UnaryOperatorCode.DynamicPlus;
                    }

                    break;

                case UnaryOperatorKind.UnaryMinus:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.ULong:
                            return Semantics.UnaryOperatorCode.IntegerMinus;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return Semantics.UnaryOperatorCode.FloatingMinus;
                        case UnaryOperatorKind.Decimal:
                            return Semantics.UnaryOperatorCode.DecimalMinus;
                        case UnaryOperatorKind.Dynamic:
                            return Semantics.UnaryOperatorCode.DynamicMinus;
                    }

                    break;

                case UnaryOperatorKind.LogicalNegation:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Bool:
                            return Semantics.UnaryOperatorCode.BooleanLogicalNot;
                        case UnaryOperatorKind.Dynamic:
                            return Semantics.UnaryOperatorCode.DynamicLogicalNot;
                    }

                    break;
                case UnaryOperatorKind.BitwiseComplement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.ULong:
                            return Semantics.UnaryOperatorCode.IntegerBitwiseNegation;
                        case UnaryOperatorKind.Bool:
                            return Semantics.UnaryOperatorCode.BooleanBitwiseNegation;
                        case UnaryOperatorKind.Dynamic:
                            return Semantics.UnaryOperatorCode.DynamicBitwiseNegation;
                    }

                    break;

                case UnaryOperatorKind.True:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Dynamic:
                            return Semantics.UnaryOperatorCode.DynamicTrue;
                    }

                    break;

                case UnaryOperatorKind.False:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Dynamic:
                            return Semantics.UnaryOperatorCode.DynamicFalse;
                    }

                    break;
            }

            return Semantics.UnaryOperatorCode.None;
        }

        internal static Semantics.BinaryOperatorCode DeriveBinaryOperatorCode(BinaryOperatorKind operatorKind)
        {
            if ((operatorKind & BinaryOperatorKind.UserDefined) != 0)
            {
                switch (operatorKind & BinaryOperatorKind.OpMask)
                {
                    case BinaryOperatorKind.Addition:
                        return Semantics.BinaryOperatorCode.OperatorAdd;
                    case BinaryOperatorKind.Subtraction:
                        return Semantics.BinaryOperatorCode.OperatorSubtract;
                    case BinaryOperatorKind.Multiplication:
                        return Semantics.BinaryOperatorCode.OperatorMultiply;
                    case BinaryOperatorKind.Division:
                        return Semantics.BinaryOperatorCode.OperatorDivide;
                    case BinaryOperatorKind.Remainder:
                        return Semantics.BinaryOperatorCode.OperatorRemainder;
                    case BinaryOperatorKind.LeftShift:
                        return Semantics.BinaryOperatorCode.OperatorLeftShift;
                    case BinaryOperatorKind.RightShift:
                        return Semantics.BinaryOperatorCode.OperatorRightShift;
                    case BinaryOperatorKind.And:
                        if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                        {
                            return Semantics.BinaryOperatorCode.OperatorConditionalAnd;
                        }

                        return Semantics.BinaryOperatorCode.OperatorAnd;
                    case BinaryOperatorKind.Or:
                        if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                        {
                            return Semantics.BinaryOperatorCode.OperatorConditionalOr;
                        }

                        return Semantics.BinaryOperatorCode.OperatorOr;
                    case BinaryOperatorKind.Xor:
                        return Semantics.BinaryOperatorCode.OperatorXor;
                }
            }

            switch (operatorKind & BinaryOperatorKind.OpMask)
            {
                case BinaryOperatorKind.Addition:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.BinaryOperatorCode.IntegerAdd;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.BinaryOperatorCode.UnsignedAdd;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return Semantics.BinaryOperatorCode.FloatingAdd;
                        case BinaryOperatorKind.Decimal:
                            return Semantics.BinaryOperatorCode.DecimalAdd;
                        case BinaryOperatorKind.EnumAndUnderlying:
                        case BinaryOperatorKind.UnderlyingAndEnum:
                            return Semantics.BinaryOperatorCode.EnumAdd;
                        case BinaryOperatorKind.PointerAndInt:
                        case BinaryOperatorKind.PointerAndUInt:
                        case BinaryOperatorKind.PointerAndLong:
                        case BinaryOperatorKind.PointerAndULong:
                            return Semantics.BinaryOperatorCode.PointerIntegerAdd;
                        case BinaryOperatorKind.IntAndPointer:
                        case BinaryOperatorKind.UIntAndPointer:
                        case BinaryOperatorKind.LongAndPointer:
                        case BinaryOperatorKind.ULongAndPointer:
                            return Semantics.BinaryOperatorCode.IntegerPointerAdd;
                        case BinaryOperatorKind.Dynamic:
                            return Semantics.BinaryOperatorCode.DynamicAdd;
                        case BinaryOperatorKind.String:
                        case BinaryOperatorKind.StringAndObject:
                        case BinaryOperatorKind.ObjectAndString:
                            return Semantics.BinaryOperatorCode.StringConcatenation;
                    }

                    break;

                case BinaryOperatorKind.Subtraction:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.BinaryOperatorCode.IntegerSubtract;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.BinaryOperatorCode.UnsignedSubtract;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return Semantics.BinaryOperatorCode.FloatingSubtract;
                        case BinaryOperatorKind.Decimal:
                            return Semantics.BinaryOperatorCode.DecimalSubtract;
                        case BinaryOperatorKind.EnumAndUnderlying:
                        case BinaryOperatorKind.UnderlyingAndEnum:
                            return Semantics.BinaryOperatorCode.EnumSubtract;
                        case BinaryOperatorKind.PointerAndInt:
                        case BinaryOperatorKind.PointerAndUInt:
                        case BinaryOperatorKind.PointerAndLong:
                        case BinaryOperatorKind.PointerAndULong:
                            return Semantics.BinaryOperatorCode.PointerIntegerSubtract;
                        case BinaryOperatorKind.Pointer:
                            return Semantics.BinaryOperatorCode.PointerSubtract;
                        case BinaryOperatorKind.Dynamic:
                            return Semantics.BinaryOperatorCode.DynamicSubtract;
                    }

                    break;

                case BinaryOperatorKind.Multiplication:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.BinaryOperatorCode.IntegerMultiply;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.BinaryOperatorCode.UnsignedMultiply;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return Semantics.BinaryOperatorCode.FloatingMultiply;
                        case BinaryOperatorKind.Decimal:
                            return Semantics.BinaryOperatorCode.DecimalMultiply;
                        case BinaryOperatorKind.Dynamic:
                            return Semantics.BinaryOperatorCode.DynamicMultiply;
                    }

                    break;

                case BinaryOperatorKind.Division:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.BinaryOperatorCode.IntegerDivide;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.BinaryOperatorCode.UnsignedDivide;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return Semantics.BinaryOperatorCode.FloatingDivide;
                        case BinaryOperatorKind.Decimal:
                            return Semantics.BinaryOperatorCode.DecimalDivide;
                        case BinaryOperatorKind.Dynamic:
                            return Semantics.BinaryOperatorCode.DynamicDivide;
                    }

                    break;

                case BinaryOperatorKind.Remainder:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.BinaryOperatorCode.IntegerRemainder;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.BinaryOperatorCode.UnsignedRemainder;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return Semantics.BinaryOperatorCode.FloatingRemainder;
                        case BinaryOperatorKind.Dynamic:
                            return Semantics.BinaryOperatorCode.DynamicRemainder;
                    }

                    break;

                case BinaryOperatorKind.LeftShift:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.BinaryOperatorCode.IntegerLeftShift;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.BinaryOperatorCode.UnsignedLeftShift;
                        case BinaryOperatorKind.Dynamic:
                            return Semantics.BinaryOperatorCode.DynamicLeftShift;
                    }

                    break;

                case BinaryOperatorKind.RightShift:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.BinaryOperatorCode.IntegerRightShift;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.BinaryOperatorCode.UnsignedRightShift;
                        case BinaryOperatorKind.Dynamic:
                            return Semantics.BinaryOperatorCode.DynamicRightShift;
                    }

                    break;

                case BinaryOperatorKind.And:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.BinaryOperatorCode.IntegerAnd;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.BinaryOperatorCode.UnsignedAnd;
                        case BinaryOperatorKind.Bool:
                            if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                            {
                                return Semantics.BinaryOperatorCode.BooleanConditionalAnd;
                            }

                            return Semantics.BinaryOperatorCode.BooleanAnd;
                        case BinaryOperatorKind.Enum:
                            return Semantics.BinaryOperatorCode.EnumAnd;
                        case BinaryOperatorKind.Dynamic:
                            return Semantics.BinaryOperatorCode.DynamicAnd;
                    }

                    break;

                case BinaryOperatorKind.Or:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.BinaryOperatorCode.IntegerOr;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.BinaryOperatorCode.UnsignedOr;
                        case BinaryOperatorKind.Bool:
                            if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                            {
                                return Semantics.BinaryOperatorCode.BooleanConditionalOr;
                            }

                            return Semantics.BinaryOperatorCode.BooleanOr;
                        case BinaryOperatorKind.Enum:
                            return Semantics.BinaryOperatorCode.EnumOr;
                        case BinaryOperatorKind.Dynamic:
                            return Semantics.BinaryOperatorCode.DynamicOr;
                    }

                    break;

                case BinaryOperatorKind.Xor:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.BinaryOperatorCode.IntegerXor;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.BinaryOperatorCode.UnsignedXor;
                        case BinaryOperatorKind.Bool:
                            return Semantics.BinaryOperatorCode.BooleanXor;
                        case BinaryOperatorKind.Enum:
                            return Semantics.BinaryOperatorCode.EnumXor;
                        case BinaryOperatorKind.Dynamic:
                            return Semantics.BinaryOperatorCode.DynamicXor;
                    }

                    break;
            }

            return Semantics.BinaryOperatorCode.None;
        }

        internal static Semantics.RelationalOperatorCode DeriveRelationalOperatorCode(BinaryOperatorKind operatorKind)
        {
            if ((operatorKind & BinaryOperatorKind.UserDefined) != 0)
            {
                switch (operatorKind & BinaryOperatorKind.OpMask)
                {
                    case BinaryOperatorKind.LessThan:
                        return Semantics.RelationalOperatorCode.OperatorLess;
                    case BinaryOperatorKind.LessThanOrEqual:
                        return Semantics.RelationalOperatorCode.OperatorLessEqual;
                    case BinaryOperatorKind.Equal:
                        return Semantics.RelationalOperatorCode.OperatorEqual;
                    case BinaryOperatorKind.NotEqual:
                        return Semantics.RelationalOperatorCode.OperatorNotEqual;
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return Semantics.RelationalOperatorCode.OperatorGreaterEqual;
                    case BinaryOperatorKind.GreaterThan:
                        return Semantics.RelationalOperatorCode.OperatorGreater;
                }
            }

            switch (operatorKind & BinaryOperatorKind.OpMask)
            {
                case BinaryOperatorKind.LessThan:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.RelationalOperatorCode.IntegerLess;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.RelationalOperatorCode.UnsignedLess;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return Semantics.RelationalOperatorCode.FloatingLess;
                        case BinaryOperatorKind.Decimal:
                            return Semantics.RelationalOperatorCode.DecimalLess;
                        case BinaryOperatorKind.Pointer:
                            return Semantics.RelationalOperatorCode.PointerLess;
                        case BinaryOperatorKind.Enum:
                            return Semantics.RelationalOperatorCode.EnumLess;
                    }

                    break;

                case BinaryOperatorKind.LessThanOrEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.RelationalOperatorCode.IntegerLessEqual;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.RelationalOperatorCode.UnsignedLessEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return Semantics.RelationalOperatorCode.FloatingLessEqual;
                        case BinaryOperatorKind.Decimal:
                            return Semantics.RelationalOperatorCode.DecimalLessEqual;
                        case BinaryOperatorKind.Pointer:
                            return Semantics.RelationalOperatorCode.PointerLessEqual;
                        case BinaryOperatorKind.Enum:
                            return Semantics.RelationalOperatorCode.EnumLessEqual;
                    }

                    break;

                case BinaryOperatorKind.Equal:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.RelationalOperatorCode.IntegerEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return Semantics.RelationalOperatorCode.FloatingEqual;
                        case BinaryOperatorKind.Decimal:
                            return Semantics.RelationalOperatorCode.DecimalEqual;
                        case BinaryOperatorKind.Pointer:
                            return Semantics.RelationalOperatorCode.PointerEqual;
                        case BinaryOperatorKind.Enum:
                            return Semantics.RelationalOperatorCode.EnumEqual;
                        case BinaryOperatorKind.Bool:
                            return Semantics.RelationalOperatorCode.BooleanEqual;
                        case BinaryOperatorKind.String:
                            return Semantics.RelationalOperatorCode.StringEqual;
                        case BinaryOperatorKind.Object:
                            return Semantics.RelationalOperatorCode.ObjectEqual;
                        case BinaryOperatorKind.Delegate:
                            return Semantics.RelationalOperatorCode.DelegateEqual;
                        case BinaryOperatorKind.NullableNull:
                            return Semantics.RelationalOperatorCode.NullableEqual;
                    }

                    break;

                case BinaryOperatorKind.NotEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.RelationalOperatorCode.IntegerNotEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return Semantics.RelationalOperatorCode.FloatingNotEqual;
                        case BinaryOperatorKind.Decimal:
                            return Semantics.RelationalOperatorCode.DecimalNotEqual;
                        case BinaryOperatorKind.Pointer:
                            return Semantics.RelationalOperatorCode.PointerNotEqual;
                        case BinaryOperatorKind.Enum:
                            return Semantics.RelationalOperatorCode.EnumNotEqual;
                        case BinaryOperatorKind.Bool:
                            return Semantics.RelationalOperatorCode.BooleanNotEqual;
                        case BinaryOperatorKind.String:
                            return Semantics.RelationalOperatorCode.StringNotEqual;
                        case BinaryOperatorKind.Object:
                            return Semantics.RelationalOperatorCode.ObjectNotEqual;
                        case BinaryOperatorKind.Delegate:
                            return Semantics.RelationalOperatorCode.DelegateNotEqual;
                        case BinaryOperatorKind.NullableNull:
                            return Semantics.RelationalOperatorCode.NullableNotEqual;
                    }

                    break;

                case BinaryOperatorKind.GreaterThanOrEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.RelationalOperatorCode.IntegerGreaterEqual;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.RelationalOperatorCode.UnsignedGreaterEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return Semantics.RelationalOperatorCode.FloatingGreaterEqual;
                        case BinaryOperatorKind.Decimal:
                            return Semantics.RelationalOperatorCode.DecimalGreaterEqual;
                        case BinaryOperatorKind.Pointer:
                            return Semantics.RelationalOperatorCode.PointerGreaterEqual;
                        case BinaryOperatorKind.Enum:
                            return Semantics.RelationalOperatorCode.EnumGreaterEqual;
                    }

                    break;

                case BinaryOperatorKind.GreaterThan:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return Semantics.RelationalOperatorCode.IntegerGreater;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return Semantics.RelationalOperatorCode.UnsignedGreater;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return Semantics.RelationalOperatorCode.FloatingGreater;
                        case BinaryOperatorKind.Decimal:
                            return Semantics.RelationalOperatorCode.DecimalGreater;
                        case BinaryOperatorKind.Pointer:
                            return Semantics.RelationalOperatorCode.PointerGreater;
                        case BinaryOperatorKind.Enum:
                            return Semantics.RelationalOperatorCode.EnumGreater;
                    }

                    break;
            }

            return Semantics.RelationalOperatorCode.None;
        }
    }
}
