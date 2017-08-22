// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an expression that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal abstract partial class BaseAddressOfExpression : Operation, IAddressOfExpression
    {
        protected BaseAddressOfExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.AddressOfExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ReferenceImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Reference;
            }
        }
        /// <summary>
        /// Addressed reference.
        /// </summary>
        public IOperation Reference => Operation.SetParentOperation(ReferenceImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAddressOfExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAddressOfExpression(this, argument);
        }
    }
    /// <summary>
    /// Represents an expression that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal sealed partial class AddressOfExpression : BaseAddressOfExpression, IAddressOfExpression
    {
        public AddressOfExpression(IOperation reference, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ReferenceImpl = reference;
        }

        protected override IOperation ReferenceImpl { get; }
    }
    /// <summary>
    /// Represents an expression that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal sealed partial class LazyAddressOfExpression : BaseAddressOfExpression, IAddressOfExpression
    {
        private readonly Lazy<IOperation> _lazyReference;

        public LazyAddressOfExpression(Lazy<IOperation> reference, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyReference = reference ?? throw new System.ArgumentNullException(nameof(reference));
        }

        protected override IOperation ReferenceImpl => _lazyReference.Value;
    }

    /// <summary>
    /// Represents C# nameof and VB NameOf expression.
    /// </summary>
    internal abstract partial class BaseNameOfExpression : Operation, INameOfExpression
    {
        protected BaseNameOfExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.NameOfExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ArgumentImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Argument;
            }
        }
        /// <summary>
        /// Argument to name of expression.
        /// </summary>
        public IOperation Argument => Operation.SetParentOperation(ArgumentImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNameOfExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNameOfExpression(this, argument);
        }
    }
    /// <summary>
    /// Represents C# nameof and VB NameOf expression.
    /// </summary>
    internal sealed partial class NameOfExpression : BaseNameOfExpression, INameOfExpression
    {
        public NameOfExpression(IOperation argument, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArgumentImpl = argument;
        }

        protected override IOperation ArgumentImpl { get; }
    }
    /// <summary>
    /// Represents C# nameof and VB NameOf expression.
    /// </summary>
    internal sealed partial class LazyNameOfExpression : BaseNameOfExpression, INameOfExpression
    {
        private readonly Lazy<IOperation> _lazyArgument;

        public LazyNameOfExpression(Lazy<IOperation> argument, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyArgument = argument ?? throw new System.ArgumentNullException(nameof(argument));
        }

        protected override IOperation ArgumentImpl => _lazyArgument.Value;
    }

    /// <summary>
    /// Represents C# throw expression.
    /// </summary>
    internal abstract partial class BaseThrowExpression : Operation, IThrowExpression
    {
        protected BaseThrowExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.ThrowExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Expression;
            }
        }
        /// <summary>
        /// Expression.
        /// </summary>
        public IOperation Expression => Operation.SetParentOperation(ExpressionImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitThrowExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitThrowExpression(this, argument);
        }
    }
    /// <summary>
    /// Represents C# throw expression.
    /// </summary>
    internal sealed partial class ThrowExpression : BaseThrowExpression, IThrowExpression
    {
        public ThrowExpression(IOperation expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
        }

        protected override IOperation ExpressionImpl { get; }
    }
    /// <summary>
    /// Represents C# throw expression.
    /// </summary>
    internal sealed partial class LazyThrowExpression : BaseThrowExpression, IThrowExpression
    {
        private readonly Lazy<IOperation> _lazyExpression;

        public LazyThrowExpression(Lazy<IOperation> expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
        }

        protected override IOperation ExpressionImpl => _lazyExpression.Value;
    }

    /// <summary>
    /// Represents an argument in a method invocation.
    /// </summary>
    internal abstract partial class BaseArgument : Operation, IArgument
    {
        protected BaseArgument(ArgumentKind argumentKind, IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Argument, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArgumentKind = argumentKind;
            Parameter = parameter;
        }
        /// <summary>
        /// Kind of argument.
        /// </summary>
        public ArgumentKind ArgumentKind { get; }
        /// <summary>
        /// Parameter the argument matches.
        /// </summary>
        public IParameterSymbol Parameter { get; }
        protected abstract IOperation ValueImpl { get; }
        protected abstract IOperation InConversionImpl { get; }
        protected abstract IOperation OutConversionImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
                yield return InConversion;
                yield return OutConversion;
            }
        }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);
        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        public IOperation InConversion => Operation.SetParentOperation(InConversionImpl, this);
        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        public IOperation OutConversion => Operation.SetParentOperation(OutConversionImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArgument(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArgument(this, argument);
        }
    }

    /// <summary>
    /// Represents an argument in a method invocation.
    /// </summary>
    internal sealed partial class Argument : BaseArgument, IArgument
    {
        public Argument(ArgumentKind argumentKind, IParameterSymbol parameter, IOperation value, IOperation inConversion, IOperation outConversion, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentKind, parameter, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
            InConversionImpl = inConversion;
            OutConversionImpl = outConversion;
        }

        protected override IOperation ValueImpl { get; }
        protected override IOperation InConversionImpl { get; }
        protected override IOperation OutConversionImpl { get; }
    }

    /// <summary>
    /// Represents an argument in a method invocation.
    /// </summary>
    internal sealed partial class LazyArgument : BaseArgument, IArgument
    {
        private readonly Lazy<IOperation> _lazyValue;
        private readonly Lazy<IOperation> _lazyInConversion;
        private readonly Lazy<IOperation> _lazyOutConversion;

        public LazyArgument(ArgumentKind argumentKind, IParameterSymbol parameter, Lazy<IOperation> value, Lazy<IOperation> inConversion, Lazy<IOperation> outConversion, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(argumentKind, parameter, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
            _lazyInConversion = inConversion ?? throw new System.ArgumentNullException(nameof(inConversion));
            _lazyOutConversion = outConversion ?? throw new System.ArgumentNullException(nameof(outConversion));
        }

        protected override IOperation ValueImpl => _lazyValue.Value;

        protected override IOperation InConversionImpl => _lazyInConversion.Value;

        protected override IOperation OutConversionImpl => _lazyOutConversion.Value;
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    internal abstract partial class BaseArrayCreationExpression : Operation, IArrayCreationExpression
    {
        protected BaseArrayCreationExpression(ITypeSymbol elementType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ArrayCreationExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ElementType = elementType;
        }
        /// <summary>
        /// Element type of the created array instance.
        /// </summary>
        public ITypeSymbol ElementType { get; }
        protected abstract ImmutableArray<IOperation> DimensionSizesImpl { get; }
        protected abstract IArrayInitializer InitializerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var dimensionSize in DimensionSizes)
                {
                    yield return dimensionSize;
                }
                yield return Initializer;
            }
        }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        public ImmutableArray<IOperation> DimensionSizes => Operation.SetParentOperation(DimensionSizesImpl, this);
        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        public IArrayInitializer Initializer => Operation.SetParentOperation(InitializerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayCreationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayCreationExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    internal sealed partial class ArrayCreationExpression : BaseArrayCreationExpression, IArrayCreationExpression
    {
        public ArrayCreationExpression(ITypeSymbol elementType, ImmutableArray<IOperation> dimensionSizes, IArrayInitializer initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(elementType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            DimensionSizesImpl = dimensionSizes;
            InitializerImpl = initializer;
        }

        protected override ImmutableArray<IOperation> DimensionSizesImpl { get; }
        protected override IArrayInitializer InitializerImpl { get; }
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    internal sealed partial class LazyArrayCreationExpression : BaseArrayCreationExpression, IArrayCreationExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyDimensionSizes;
        private readonly Lazy<IArrayInitializer> _lazyInitializer;

        public LazyArrayCreationExpression(ITypeSymbol elementType, Lazy<ImmutableArray<IOperation>> dimensionSizes, Lazy<IArrayInitializer> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(elementType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyDimensionSizes = dimensionSizes;
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }

        protected override ImmutableArray<IOperation> DimensionSizesImpl => _lazyDimensionSizes.Value;

        protected override IArrayInitializer InitializerImpl => _lazyInitializer.Value;
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    internal abstract partial class BaseArrayElementReferenceExpression : Operation, IArrayElementReferenceExpression
    {
        protected BaseArrayElementReferenceExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ArrayElementReferenceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ArrayReferenceImpl { get; }
        protected abstract ImmutableArray<IOperation> IndicesImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return ArrayReference;
                foreach (var index in Indices)
                {
                    yield return index;
                }
            }
        }
        /// <summary>
        /// Array to be indexed.
        /// </summary>
        public IOperation ArrayReference => Operation.SetParentOperation(ArrayReferenceImpl, this);
        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        public ImmutableArray<IOperation> Indices => Operation.SetParentOperation(IndicesImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayElementReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayElementReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    internal sealed partial class ArrayElementReferenceExpression : BaseArrayElementReferenceExpression, IArrayElementReferenceExpression
    {
        public ArrayElementReferenceExpression(IOperation arrayReference, ImmutableArray<IOperation> indices, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArrayReferenceImpl = arrayReference;
            IndicesImpl = indices;
        }

        protected override IOperation ArrayReferenceImpl { get; }
        protected override ImmutableArray<IOperation> IndicesImpl { get; }
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    internal sealed partial class LazyArrayElementReferenceExpression : BaseArrayElementReferenceExpression, IArrayElementReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyArrayReference;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyIndices;

        public LazyArrayElementReferenceExpression(Lazy<IOperation> arrayReference, Lazy<ImmutableArray<IOperation>> indices, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyArrayReference = arrayReference ?? throw new System.ArgumentNullException(nameof(arrayReference));
            _lazyIndices = indices;
        }

        protected override IOperation ArrayReferenceImpl => _lazyArrayReference.Value;

        protected override ImmutableArray<IOperation> IndicesImpl => _lazyIndices.Value;
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    internal abstract partial class BaseArrayInitializer : Operation, IArrayInitializer
    {
        protected BaseArrayInitializer(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ArrayInitializer, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> ElementValuesImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var elementValue in ElementValues)
                {
                    yield return elementValue;
                }
            }
        }
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        public ImmutableArray<IOperation> ElementValues => Operation.SetParentOperation(ElementValuesImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    internal sealed partial class ArrayInitializer : BaseArrayInitializer, IArrayInitializer
    {
        public ArrayInitializer(ImmutableArray<IOperation> elementValues, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ElementValuesImpl = elementValues;
        }

        protected override ImmutableArray<IOperation> ElementValuesImpl { get; }
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    internal sealed partial class LazyArrayInitializer : BaseArrayInitializer, IArrayInitializer
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyElementValues;

        public LazyArrayInitializer(Lazy<ImmutableArray<IOperation>> elementValues, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyElementValues = elementValues;
        }

        protected override ImmutableArray<IOperation> ElementValuesImpl => _lazyElementValues.Value;
    }

    /// <summary>
    /// Represents an base type of assignment expression.
    /// </summary>
    internal abstract partial class AssignmentExpression : Operation, IAssignmentExpression
    {
        protected AssignmentExpression(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IOperation TargetImpl { get; }
        protected abstract IOperation ValueImpl { get; }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public IOperation Target => Operation.SetParentOperation(TargetImpl, this);
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);
    }

    /// <summary>
    /// Represents a simple assignment expression.
    /// </summary>
    internal abstract partial class BaseSimpleAssignmentExpression : AssignmentExpression, ISimpleAssignmentExpression
    {
        public BaseSimpleAssignmentExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.SimpleAssignmentExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Target;
                yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSimpleAssignmentExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSimpleAssignmentExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a simple assignment expression.
    /// </summary>
    internal sealed partial class SimpleAssignmentExpression : BaseSimpleAssignmentExpression, ISimpleAssignmentExpression
    {
        public SimpleAssignmentExpression(IOperation target, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            TargetImpl = target;
            ValueImpl = value;
        }
        protected override IOperation TargetImpl { get; }
        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents a simple assignment expression.
    /// </summary>
    internal sealed partial class LazySimpleAssignmentExpression : BaseSimpleAssignmentExpression, ISimpleAssignmentExpression
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;

        public LazySimpleAssignmentExpression(Lazy<IOperation> target, Lazy<IOperation> value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException(nameof(target));
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        protected override IOperation TargetImpl => _lazyTarget.Value;
        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal abstract partial class BaseAwaitExpression : Operation, IAwaitExpression
    {
        protected BaseAwaitExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.AwaitExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation AwaitedValueImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return AwaitedValue;
            }
        }
        /// <summary>
        /// Value to be awaited.
        /// </summary>
        public IOperation AwaitedValue => Operation.SetParentOperation(AwaitedValueImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAwaitExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAwaitExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal sealed partial class AwaitExpression : BaseAwaitExpression, IAwaitExpression
    {
        public AwaitExpression(IOperation awaitedValue, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            AwaitedValueImpl = awaitedValue;
        }

        protected override IOperation AwaitedValueImpl { get; }
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal sealed partial class LazyAwaitExpression : BaseAwaitExpression, IAwaitExpression
    {
        private readonly Lazy<IOperation> _lazyAwaitedValue;

        public LazyAwaitExpression(Lazy<IOperation> awaitedValue, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyAwaitedValue = awaitedValue ?? throw new System.ArgumentNullException(nameof(awaitedValue));
        }

        protected override IOperation AwaitedValueImpl => _lazyAwaitedValue.Value;
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    internal abstract partial class BaseBinaryOperatorExpression : Operation, IHasOperatorMethodExpression, IBinaryOperatorExpression
    {
        protected BaseBinaryOperatorExpression(BinaryOperationKind binaryOperationKind, bool isLifted, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.BinaryOperatorExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            BinaryOperationKind = binaryOperationKind;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod;
            IsLifted = isLifted;
        }
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        public BinaryOperationKind BinaryOperationKind { get; }
        protected abstract IOperation LeftOperandImpl { get; }
        protected abstract IOperation RightOperandImpl { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }

        public bool IsLifted { get; }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return LeftOperand;
                yield return RightOperand;
            }
        }
        /// <summary>
        /// Left operand.
        /// </summary>
        public IOperation LeftOperand => Operation.SetParentOperation(LeftOperandImpl, this);
        /// <summary>
        /// Right operand.
        /// </summary>
        public IOperation RightOperand => Operation.SetParentOperation(RightOperandImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBinaryOperatorExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBinaryOperatorExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    internal sealed partial class BinaryOperatorExpression : BaseBinaryOperatorExpression, IHasOperatorMethodExpression, IBinaryOperatorExpression
    {
        public BinaryOperatorExpression(BinaryOperationKind binaryOperationKind, IOperation leftOperand, IOperation rightOperand, bool isLifted, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(binaryOperationKind, isLifted, usesOperatorMethod, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LeftOperandImpl = leftOperand;
            RightOperandImpl = rightOperand;
        }

        protected override IOperation LeftOperandImpl { get; }
        protected override IOperation RightOperandImpl { get; }
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    internal sealed partial class LazyBinaryOperatorExpression : BaseBinaryOperatorExpression, IHasOperatorMethodExpression, IBinaryOperatorExpression
    {
        private readonly Lazy<IOperation> _lazyLeftOperand;
        private readonly Lazy<IOperation> _lazyRightOperand;

        public LazyBinaryOperatorExpression(BinaryOperationKind binaryOperationKind, Lazy<IOperation> leftOperand, Lazy<IOperation> rightOperand, bool isLifted, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : 
            base(binaryOperationKind, isLifted, usesOperatorMethod, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyLeftOperand = leftOperand ?? throw new System.ArgumentNullException(nameof(leftOperand));
            _lazyRightOperand = rightOperand ?? throw new System.ArgumentNullException(nameof(rightOperand));
        }

        protected override IOperation LeftOperandImpl => _lazyLeftOperand.Value;

        protected override IOperation RightOperandImpl => _lazyRightOperand.Value;
    }

    /// <summary>
    /// Represents a block scope.
    /// </summary>
    internal abstract partial class BaseBlockStatement : Operation, IBlockStatement
    {
        protected BaseBlockStatement(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.BlockStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Locals = locals;
        }

        protected abstract ImmutableArray<IOperation> StatementsImpl { get; }
        /// <summary>
        /// Local declarations contained within the block.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var statement in Statements)
                {
                    yield return statement;
                }
            }
        }
        /// <summary>
        /// Statements contained within the block.
        /// </summary>
        public ImmutableArray<IOperation> Statements => Operation.SetParentOperation(StatementsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBlockStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBlockStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a block scope.
    /// </summary>
    internal sealed partial class BlockStatement : BaseBlockStatement, IBlockStatement
    {
        public BlockStatement(ImmutableArray<IOperation> statements, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            StatementsImpl = statements;
        }

        protected override ImmutableArray<IOperation> StatementsImpl { get; }
    }

    /// <summary>
    /// Represents a block scope.
    /// </summary>
    internal sealed partial class LazyBlockStatement : BaseBlockStatement, IBlockStatement
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyStatements;

        public LazyBlockStatement(Lazy<ImmutableArray<IOperation>> statements, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyStatements = statements;
        }

        protected override ImmutableArray<IOperation> StatementsImpl => _lazyStatements.Value;
    }

    /// <summary>
    /// Represents a C# goto, break, or continue statement, or a VB GoTo, Exit ***, or Continue *** statement
    /// </summary>
    internal sealed partial class BranchStatement : Operation, IBranchStatement
    {
        public BranchStatement(ILabelSymbol target, BranchKind branchKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.BranchStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Target = target;
            BranchKind = branchKind;
        }
        /// <summary>
        /// Label that is the target of the branch.
        /// </summary>
        public ILabelSymbol Target { get; }
        /// <summary>
        /// Kind of the branch.
        /// </summary>
        public BranchKind BranchKind { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBranchStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBranchStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a clause of a C# case or a VB Case.
    /// </summary>
    internal abstract partial class CaseClause : Operation, ICaseClause
    {
        protected CaseClause(CaseKind caseKind, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            CaseKind = caseKind;
        }
        /// <summary>
        /// Kind of the clause.
        /// </summary>
        public CaseKind CaseKind { get; }
    }

    /// <summary>
    /// Represents a C# catch or VB Catch clause.
    /// </summary>
    internal abstract partial class BaseCatchClause : Operation, ICatchClause
    {
        protected BaseCatchClause(ITypeSymbol caughtType, ILocalSymbol exceptionLocal, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.CatchClause, semanticModel, syntax, type, constantValue, isImplicit)
        {
            CaughtType = caughtType;
            ExceptionLocal = exceptionLocal;
        }

        protected abstract IBlockStatement HandlerImpl { get; }
        /// <summary>
        /// Type of exception to be handled.
        /// </summary>
        public ITypeSymbol CaughtType { get; }
        protected abstract IOperation FilterImpl { get; }
        /// <summary>
        /// Symbol for the local catch variable bound to the caught exception.
        /// </summary>
        public ILocalSymbol ExceptionLocal { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Filter;
                yield return Handler;
            }
        }
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        public IBlockStatement Handler => Operation.SetParentOperation(HandlerImpl, this);
        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        public IOperation Filter => Operation.SetParentOperation(FilterImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCatchClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCatchClause(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# catch or VB Catch clause.
    /// </summary>
    internal sealed partial class CatchClause : BaseCatchClause, ICatchClause
    {
        public CatchClause(IBlockStatement handler, ITypeSymbol caughtType, IOperation filter, ILocalSymbol exceptionLocal, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(caughtType, exceptionLocal, semanticModel, syntax, type, constantValue, isImplicit)
        {
            HandlerImpl = handler;
            FilterImpl = filter;
        }

        protected override IBlockStatement HandlerImpl { get; }
        protected override IOperation FilterImpl { get; }
    }

    /// <summary>
    /// Represents a C# catch or VB Catch clause.
    /// </summary>
    internal sealed partial class LazyCatchClause : BaseCatchClause, ICatchClause
    {
        private readonly Lazy<IBlockStatement> _lazyHandler;
        private readonly Lazy<IOperation> _lazyFilter;

        public LazyCatchClause(Lazy<IBlockStatement> handler, ITypeSymbol caughtType, Lazy<IOperation> filter, ILocalSymbol exceptionLocal, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(caughtType, exceptionLocal, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyHandler = handler ?? throw new System.ArgumentNullException(nameof(handler));
            _lazyFilter = filter ?? throw new System.ArgumentNullException(nameof(filter));
        }

        protected override IBlockStatement HandlerImpl => _lazyHandler.Value;

        protected override IOperation FilterImpl => _lazyFilter.Value;
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    internal abstract partial class BaseCompoundAssignmentExpression : AssignmentExpression, IHasOperatorMethodExpression, ICompoundAssignmentExpression
    {
        protected BaseCompoundAssignmentExpression(BinaryOperationKind binaryOperationKind, bool isLifted, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.CompoundAssignmentExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            BinaryOperationKind = binaryOperationKind;
            IsLifted = isLifted;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        public BinaryOperationKind BinaryOperationKind { get; }
        /// <summary>
        /// <code>true</code> if this assignment contains a 'lifted' binary operation.
        /// </summary>
        public bool IsLifted { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Target;
                yield return Value;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCompoundAssignmentExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCompoundAssignmentExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    internal sealed partial class CompoundAssignmentExpression : BaseCompoundAssignmentExpression, IHasOperatorMethodExpression, ICompoundAssignmentExpression
    {
        public CompoundAssignmentExpression(BinaryOperationKind binaryOperationKind, bool isLifted, IOperation target, IOperation value, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(binaryOperationKind, isLifted, usesOperatorMethod, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TargetImpl = target;
            ValueImpl = value;
        }
        protected override IOperation TargetImpl { get; }
        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    internal sealed partial class LazyCompoundAssignmentExpression : BaseCompoundAssignmentExpression, IHasOperatorMethodExpression, ICompoundAssignmentExpression
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyCompoundAssignmentExpression(BinaryOperationKind binaryOperationKind, bool isLifted, Lazy<IOperation> target, Lazy<IOperation> value, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(binaryOperationKind, isLifted, usesOperatorMethod, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException(nameof(target));
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        protected override IOperation TargetImpl => _lazyTarget.Value;
        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    internal abstract partial class BaseConditionalAccessExpression : Operation, IConditionalAccessExpression
    {
        protected BaseConditionalAccessExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ConditionalAccessExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ConditionalValueImpl { get; }
        protected abstract IOperation ConditionalInstanceImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return ConditionalInstance;
                yield return ConditionalValue;
            }
        }
        /// <summary>
        /// Expression to be evaluated if the conditional instance is non null.
        /// </summary>
        public IOperation ConditionalValue => Operation.SetParentOperation(ConditionalValueImpl, this);
        /// <summary>
        /// Expresson that is conditionally accessed.
        /// </summary>
        public IOperation ConditionalInstance => Operation.SetParentOperation(ConditionalInstanceImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalAccessExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalAccessExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    internal sealed partial class ConditionalAccessExpression : BaseConditionalAccessExpression, IConditionalAccessExpression
    {
        public ConditionalAccessExpression(IOperation conditionalValue, IOperation conditionalInstance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ConditionalValueImpl = conditionalValue;
            ConditionalInstanceImpl = conditionalInstance;
        }

        protected override IOperation ConditionalValueImpl { get; }
        protected override IOperation ConditionalInstanceImpl { get; }
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    internal sealed partial class LazyConditionalAccessExpression : BaseConditionalAccessExpression, IConditionalAccessExpression
    {
        private readonly Lazy<IOperation> _lazyConditionalValue;
        private readonly Lazy<IOperation> _lazyConditionalInstance;

        public LazyConditionalAccessExpression(Lazy<IOperation> conditionalValue, Lazy<IOperation> conditionalInstance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyConditionalValue = conditionalValue ?? throw new System.ArgumentNullException(nameof(conditionalValue));
            _lazyConditionalInstance = conditionalInstance ?? throw new System.ArgumentNullException(nameof(conditionalInstance));
        }

        protected override IOperation ConditionalValueImpl => _lazyConditionalValue.Value;

        protected override IOperation ConditionalInstanceImpl => _lazyConditionalInstance.Value;
    }

    /// <summary>
    /// Represents the value of a conditionally-accessed expression within an expression containing a conditional access.
    /// </summary>
    internal sealed partial class ConditionalAccessInstanceExpression : Operation, IConditionalAccessInstanceExpression
    {
        public ConditionalAccessInstanceExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.ConditionalAccessInstanceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalAccessInstanceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalAccessInstanceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    internal abstract partial class BaseConditionalExpression : Operation, IConditionalExpression
    {
        protected BaseConditionalExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ConditionalExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ConditionImpl { get; }
        protected abstract IOperation WhenTrueImpl { get; }
        protected abstract IOperation WhenFalseImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Condition;
                yield return WhenTrue;
                yield return WhenFalse;
            }
        }
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        public IOperation Condition => Operation.SetParentOperation(ConditionImpl, this);
        /// <summary>
        /// Value evaluated if the Condition is true.
        /// </summary>
        public IOperation WhenTrue => Operation.SetParentOperation(WhenTrueImpl, this);
        /// <summary>
        /// Value evaluated if the Condition is false.
        /// </summary>
        public IOperation WhenFalse => Operation.SetParentOperation(WhenFalseImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    internal sealed partial class ConditionalExpression : BaseConditionalExpression, IConditionalExpression
    {
        public ConditionalExpression(IOperation condition, IOperation whenTrue, IOperation whenFalse, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ConditionImpl = condition;
            WhenTrueImpl = whenTrue;
            WhenFalseImpl = whenFalse;
        }

        protected override IOperation ConditionImpl { get; }
        protected override IOperation WhenTrueImpl { get; }
        protected override IOperation WhenFalseImpl { get; }
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    internal sealed partial class LazyConditionalExpression : BaseConditionalExpression, IConditionalExpression
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyWhenTrue;
        private readonly Lazy<IOperation> _lazyWhenFalse;

        public LazyConditionalExpression(Lazy<IOperation> condition, Lazy<IOperation> whenTrue, Lazy<IOperation> whenFalse, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyCondition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            _lazyWhenTrue = whenTrue ?? throw new System.ArgumentNullException(nameof(whenTrue));
            _lazyWhenFalse = whenFalse ?? throw new System.ArgumentNullException(nameof(whenFalse));
        }

        protected override IOperation ConditionImpl => _lazyCondition.Value;

        protected override IOperation WhenTrueImpl => _lazyWhenTrue.Value;

        protected override IOperation WhenFalseImpl => _lazyWhenFalse.Value;
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    internal abstract partial class BaseConversionExpression : Operation, IHasOperatorMethodExpression, IConversionExpression
    {
        protected BaseConversionExpression(bool isExplicitInCode, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ConversionExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsExplicitInCode = isExplicitInCode;
            IsTryCast = isTryCast;
            IsChecked = isChecked;
        }

        public abstract IOperation OperandImpl { get; }
        public abstract CommonConversion Conversion { get; }
        public bool IsExplicitInCode { get; }
        public bool IsTryCast { get; }
        public bool IsChecked { get; }
        public bool UsesOperatorMethod => Conversion.IsUserDefined;
        public IMethodSymbol OperatorMethod => Conversion.MethodSymbol;
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Operand;
            }
        }
        /// <summary>
        /// Value to be converted.
        /// </summary>
        public IOperation Operand => Operation.SetParentOperation(OperandImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConversionExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConversionExpression(this, argument);
        }
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class DefaultValueExpression : Operation, IDefaultValueExpression
    {
        public DefaultValueExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DefaultValueExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDefaultValueExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDefaultValueExpression(this, argument);
        }
    }

    /// <summary>
    /// Reprsents an empty statement.
    /// </summary>
    internal sealed partial class EmptyStatement : Operation, IEmptyStatement
    {
        public EmptyStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.EmptyStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEmptyStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEmptyStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB End statement.
    /// </summary>
    internal sealed partial class EndStatement : Operation, IEndStatement
    {
        public EndStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.EndStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEndStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEndStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    internal abstract partial class BaseEventAssignmentExpression : Operation, IEventAssignmentExpression
    {
        protected BaseEventAssignmentExpression(bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.EventAssignmentExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Adds = adds;
        }

        /// <summary>
        /// Reference to the event being bound.
        /// </summary>
        protected abstract IEventReferenceExpression EventReferenceImpl { get; }

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        protected abstract IOperation HandlerValueImpl { get; }

        /// <summary>
        /// True for adding a binding, false for removing one.
        /// </summary>
        public bool Adds { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return EventReference;
                yield return HandlerValue;
            }
        }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        public IEventReferenceExpression EventReference => Operation.SetParentOperation(EventReferenceImpl, this);

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        public IOperation HandlerValue => Operation.SetParentOperation(HandlerValueImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEventAssignmentExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEventAssignmentExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    internal sealed partial class EventAssignmentExpression : BaseEventAssignmentExpression, IEventAssignmentExpression
    {
        public EventAssignmentExpression(IEventReferenceExpression eventReference, IOperation handlerValue, bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(adds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            EventReferenceImpl = eventReference;
            HandlerValueImpl = handlerValue;
        }

        protected override IEventReferenceExpression EventReferenceImpl { get; }
        protected override IOperation HandlerValueImpl { get; }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    internal sealed partial class LazyEventAssignmentExpression : BaseEventAssignmentExpression, IEventAssignmentExpression
    {
        private readonly Lazy<IEventReferenceExpression> _lazyEventReference;
        private readonly Lazy<IOperation> _lazyHandlerValue;
        
        public LazyEventAssignmentExpression(Lazy<IEventReferenceExpression> eventReference, Lazy<IOperation> handlerValue, bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(adds, semanticModel, syntax, type, constantValue, isImplicit)

        {
            _lazyEventReference = eventReference ?? throw new System.ArgumentNullException(nameof(eventReference));
            _lazyHandlerValue = handlerValue ?? throw new System.ArgumentNullException(nameof(handlerValue));
        }
        
        protected override IEventReferenceExpression EventReferenceImpl => _lazyEventReference.Value;

        protected override IOperation HandlerValueImpl => _lazyHandlerValue.Value;
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    internal abstract partial class BaseEventReferenceExpression : MemberReferenceExpression, IEventReferenceExpression
    {
        public BaseEventReferenceExpression(IEventSymbol @event, ISymbol member, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(member, OperationKind.EventReferenceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Event = @event;
        }
        /// <summary>
        /// Referenced event.
        /// </summary>
        public IEventSymbol Event { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Instance;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEventReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEventReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    internal sealed partial class EventReferenceExpression : BaseEventReferenceExpression, IEventReferenceExpression
    {
        public EventReferenceExpression(IEventSymbol @event, IOperation instance, ISymbol member, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(@event, member, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceImpl = instance;
        }
        protected override IOperation InstanceImpl { get; }
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    internal sealed partial class LazyEventReferenceExpression : BaseEventReferenceExpression, IEventReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyEventReferenceExpression(IEventSymbol @event, Lazy<IOperation> instance, ISymbol member, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(@event, member, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
        }
        protected override IOperation InstanceImpl => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// </summary>
    internal abstract partial class BaseExpressionStatement : Operation, IExpressionStatement
    {
        protected BaseExpressionStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ExpressionStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Expression;
            }
        }
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public IOperation Expression => Operation.SetParentOperation(ExpressionImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitExpressionStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// </summary>
    internal sealed partial class ExpressionStatement : BaseExpressionStatement, IExpressionStatement
    {
        public ExpressionStatement(IOperation expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
        }

        protected override IOperation ExpressionImpl { get; }
    }

    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// </summary>
    internal sealed partial class LazyExpressionStatement : BaseExpressionStatement, IExpressionStatement
    {
        private readonly Lazy<IOperation> _lazyExpression;

        public LazyExpressionStatement(Lazy<IOperation> expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
        }

        protected override IOperation ExpressionImpl => _lazyExpression.Value;
    }

    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    internal abstract partial class BaseFieldInitializer : SymbolInitializer, IFieldInitializer
    {
        public BaseFieldInitializer(ImmutableArray<IFieldSymbol> initializedFields, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializedFields = initializedFields;
        }
        /// <summary>
        /// Initialized fields. There can be multiple fields for Visual Basic fields declared with As New.
        /// </summary>
        public ImmutableArray<IFieldSymbol> InitializedFields { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFieldInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFieldInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    internal sealed partial class FieldInitializer : BaseFieldInitializer, IFieldInitializer
    {
        public FieldInitializer(ImmutableArray<IFieldSymbol> initializedFields, IOperation value, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(initializedFields, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
        }
        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    internal sealed partial class LazyFieldInitializer : BaseFieldInitializer, IFieldInitializer
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyFieldInitializer(ImmutableArray<IFieldSymbol> initializedFields, Lazy<IOperation> value, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(initializedFields, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    internal abstract partial class BaseFieldReferenceExpression : MemberReferenceExpression, IFieldReferenceExpression
    {
        public BaseFieldReferenceExpression(IFieldSymbol field, ISymbol member, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(member, OperationKind.FieldReferenceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Field = field;
        }
        /// <summary>
        /// Referenced field.
        /// </summary>
        public IFieldSymbol Field { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Instance;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFieldReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFieldReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    internal sealed partial class FieldReferenceExpression : BaseFieldReferenceExpression, IFieldReferenceExpression
    {
        public FieldReferenceExpression(IFieldSymbol field, IOperation instance, ISymbol member, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(field, member, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceImpl = instance;
        }
        protected override IOperation InstanceImpl { get; }
    }

    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    internal sealed partial class LazyFieldReferenceExpression : BaseFieldReferenceExpression, IFieldReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyFieldReferenceExpression(IFieldSymbol field, Lazy<IOperation> instance, ISymbol member, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(field, member, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
        }
        protected override IOperation InstanceImpl => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a C# fixed statement.
    /// </summary>
    internal abstract partial class BaseFixedStatement : Operation, IFixedStatement
    {
        protected BaseFixedStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.FixedStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IVariableDeclarationStatement VariablesImpl { get; }
        protected abstract IOperation BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Variables;
                yield return Body;
            }
        }
        /// <summary>
        /// Variables to be fixed.
        /// </summary>
        public IVariableDeclarationStatement Variables => Operation.SetParentOperation(VariablesImpl, this);
        /// <summary>
        /// Body of the fixed, over which the variables are fixed.
        /// </summary>
        public IOperation Body => Operation.SetParentOperation(BodyImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFixedStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFixedStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# fixed statement.
    /// </summary>
    internal sealed partial class FixedStatement : BaseFixedStatement, IFixedStatement
    {
        public FixedStatement(IVariableDeclarationStatement variables, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            VariablesImpl = variables;
            BodyImpl = body;
        }

        protected override IVariableDeclarationStatement VariablesImpl { get; }
        protected override IOperation BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# fixed statement.
    /// </summary>
    internal sealed partial class LazyFixedStatement : BaseFixedStatement, IFixedStatement
    {
        private readonly Lazy<IVariableDeclarationStatement> _lazyVariables;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyFixedStatement(Lazy<IVariableDeclarationStatement> variables, Lazy<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyVariables = variables ?? throw new System.ArgumentNullException(nameof(variables));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override IVariableDeclarationStatement VariablesImpl => _lazyVariables.Value;

        protected override IOperation BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# foreach statement or a VB For Each statement.
    /// </summary>
    internal abstract partial class BaseForEachLoopStatement : LoopStatement, IForEachLoopStatement
    {
        public BaseForEachLoopStatement(ILocalSymbol iterationVariable, LoopKind loopKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(loopKind, OperationKind.LoopStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IterationVariable = iterationVariable;
        }
        /// <summary>
        /// Iteration variable of the loop.
        /// </summary>
        public ILocalSymbol IterationVariable { get; }
        protected abstract IOperation CollectionImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Collection;
                yield return Body;
            }
        }
        /// <summary>
        /// Collection value over which the loop iterates.
        /// </summary>
        public IOperation Collection => Operation.SetParentOperation(CollectionImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitForEachLoopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitForEachLoopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# foreach statement or a VB For Each statement.
    /// </summary>
    internal sealed partial class ForEachLoopStatement : BaseForEachLoopStatement, IForEachLoopStatement
    {
        public ForEachLoopStatement(ILocalSymbol iterationVariable, IOperation collection, LoopKind loopKind, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(iterationVariable, loopKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            CollectionImpl = collection;
            BodyImpl = body;
        }

        protected override IOperation CollectionImpl { get; }
        protected override IOperation BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# foreach statement or a VB For Each statement.
    /// </summary>
    internal sealed partial class LazyForEachLoopStatement : BaseForEachLoopStatement, IForEachLoopStatement
    {
        private readonly Lazy<IOperation> _lazyCollection;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyForEachLoopStatement(ILocalSymbol iterationVariable, Lazy<IOperation> collection, LoopKind loopKind, Lazy<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(iterationVariable, loopKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyCollection = collection ?? throw new System.ArgumentNullException(nameof(collection));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override IOperation CollectionImpl => _lazyCollection.Value;
        protected override IOperation BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# for statement or a VB For statement.
    /// </summary>
    internal abstract partial class BaseForLoopStatement : ForWhileUntilLoopStatement, IForLoopStatement
    {
        public BaseForLoopStatement(ImmutableArray<ILocalSymbol> locals, LoopKind loopKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(loopKind, OperationKind.LoopStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Locals = locals;
        }

        protected abstract ImmutableArray<IOperation> BeforeImpl { get; }
        protected abstract ImmutableArray<IOperation> AtLoopBottomImpl { get; }
        /// <summary>
        /// Declarations local to the loop.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var before in Before)
                {
                    yield return before;
                }
                yield return Condition;
                yield return Body;
                foreach (var atLoopBottom in AtLoopBottom)
                {
                    yield return atLoopBottom;
                }
            }
        }
        /// <summary>
        /// Statements to execute before entry to the loop. For C# these come from the first clause of the for statement. For VB these initialize the index variable of the For statement.
        /// </summary>
        public ImmutableArray<IOperation> Before => Operation.SetParentOperation(BeforeImpl, this);
        /// <summary>
        /// Statements to execute at the bottom of the loop. For C# these come from the third clause of the for statement. For VB these increment the index variable of the For statement.
        /// </summary>
        public ImmutableArray<IOperation> AtLoopBottom => Operation.SetParentOperation(AtLoopBottomImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitForLoopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitForLoopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# for statement or a VB For statement.
    /// </summary>
    internal sealed partial class ForLoopStatement : BaseForLoopStatement, IForLoopStatement
    {
        public ForLoopStatement(ImmutableArray<IOperation> before, ImmutableArray<IOperation> atLoopBottom, ImmutableArray<ILocalSymbol> locals, IOperation condition, LoopKind loopKind, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, loopKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            BeforeImpl = before;
            AtLoopBottomImpl = atLoopBottom;
            ConditionImpl = condition;
            BodyImpl = body;
        }

        protected override ImmutableArray<IOperation> BeforeImpl { get; }
        protected override ImmutableArray<IOperation> AtLoopBottomImpl { get; }
        protected override IOperation ConditionImpl { get; }
        protected override IOperation BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# for statement or a VB For statement.
    /// </summary>
    internal sealed partial class LazyForLoopStatement : BaseForLoopStatement, IForLoopStatement
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyBefore;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyAtLoopBottom;
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyForLoopStatement(Lazy<ImmutableArray<IOperation>> before, Lazy<ImmutableArray<IOperation>> atLoopBottom, ImmutableArray<ILocalSymbol> locals, Lazy<IOperation> condition, LoopKind loopKind, Lazy<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, loopKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyBefore = before;
            _lazyAtLoopBottom = atLoopBottom;
            _lazyCondition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override ImmutableArray<IOperation> BeforeImpl => _lazyBefore.Value;

        protected override ImmutableArray<IOperation> AtLoopBottomImpl => _lazyAtLoopBottom.Value;

        protected override IOperation ConditionImpl => _lazyCondition.Value;

        protected override IOperation BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# while, for, or do statement, or a VB While, For, or Do statement.
    /// </summary>
    internal abstract partial class ForWhileUntilLoopStatement : LoopStatement, IForWhileUntilLoopStatement
    {
        protected ForWhileUntilLoopStatement(LoopKind loopKind, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(loopKind, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IOperation ConditionImpl { get; }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public IOperation Condition => Operation.SetParentOperation(ConditionImpl, this);
    }

    /// <summary>
    /// Represents an if statement in C# or an If statement in VB.
    /// </summary>
    internal abstract partial class BaseIfStatement : Operation, IIfStatement
    {
        protected BaseIfStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.IfStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ConditionImpl { get; }
        protected abstract IOperation IfTrueStatementImpl { get; }
        protected abstract IOperation IfFalseStatementImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Condition;
                yield return IfTrueStatement;
                yield return IfFalseStatement;
            }
        }
        /// <summary>
        /// Condition of the if statement. For C# there is naturally one clause per if, but for VB If statements with multiple clauses are rewritten to have only one.
        /// </summary>
        public IOperation Condition => Operation.SetParentOperation(ConditionImpl, this);
        /// <summary>
        /// Statement executed if the condition is true.
        /// </summary>
        public IOperation IfTrueStatement => Operation.SetParentOperation(IfTrueStatementImpl, this);
        /// <summary>
        /// Statement executed if the condition is false.
        /// </summary>
        public IOperation IfFalseStatement => Operation.SetParentOperation(IfFalseStatementImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIfStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIfStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents an if statement in C# or an If statement in VB.
    /// </summary>
    internal sealed partial class IfStatement : BaseIfStatement, IIfStatement
    {
        public IfStatement(IOperation condition, IOperation ifTrueStatement, IOperation ifFalseStatement, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ConditionImpl = condition;
            IfTrueStatementImpl = ifTrueStatement;
            IfFalseStatementImpl = ifFalseStatement;
        }

        protected override IOperation ConditionImpl { get; }
        protected override IOperation IfTrueStatementImpl { get; }
        protected override IOperation IfFalseStatementImpl { get; }
    }

    /// <summary>
    /// Represents an if statement in C# or an If statement in VB.
    /// </summary>
    internal sealed partial class LazyIfStatement : BaseIfStatement, IIfStatement
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyIfTrueStatement;
        private readonly Lazy<IOperation> _lazyIfFalseStatement;

        public LazyIfStatement(Lazy<IOperation> condition, Lazy<IOperation> ifTrueStatement, Lazy<IOperation> ifFalseStatement, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyCondition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            _lazyIfTrueStatement = ifTrueStatement ?? throw new System.ArgumentNullException(nameof(ifTrueStatement));
            _lazyIfFalseStatement = ifFalseStatement ?? throw new System.ArgumentNullException(nameof(ifFalseStatement));
        }

        protected override IOperation ConditionImpl => _lazyCondition.Value;

        protected override IOperation IfTrueStatementImpl => _lazyIfTrueStatement.Value;

        protected override IOperation IfFalseStatementImpl => _lazyIfFalseStatement.Value;
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    internal abstract partial class BaseIncrementExpression : Operation, IIncrementExpression
    {
        public BaseIncrementExpression(UnaryOperationKind incrementOperationKind, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.IncrementExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IncrementOperationKind = incrementOperationKind;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// Kind of increment.
        /// </summary>
        public UnaryOperationKind IncrementOperationKind { get; }
        protected abstract IOperation TargetImpl { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Target;
            }
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public IOperation Target => Operation.SetParentOperation(TargetImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIncrementExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIncrementExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    internal sealed partial class IncrementExpression : BaseIncrementExpression, IIncrementExpression
    {
        public IncrementExpression(UnaryOperationKind incrementOperationKind, IOperation target, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(incrementOperationKind, usesOperatorMethod, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TargetImpl = target;
        }

        protected override IOperation TargetImpl { get; }
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    internal sealed partial class LazyIncrementExpression : BaseIncrementExpression, IIncrementExpression
    {
        private readonly Lazy<IOperation> _lazyTarget;

        public LazyIncrementExpression(UnaryOperationKind incrementOperationKind, Lazy<IOperation> target, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(incrementOperationKind, usesOperatorMethod, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException(nameof(target));
        }

        protected override IOperation TargetImpl => _lazyTarget.Value;
    }

    /// <summary>
    /// Represents a C# this or base expression, or a VB Me, MyClass, or MyBase expression.
    /// </summary>
    internal sealed partial class InstanceReferenceExpression : Operation, IInstanceReferenceExpression
    {
        public InstanceReferenceExpression(InstanceReferenceKind instanceReferenceKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.InstanceReferenceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceReferenceKind = instanceReferenceKind;
        }
        ///
        /// <summary>
        /// Kind of instance reference.
        /// </summary>
        public InstanceReferenceKind InstanceReferenceKind { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInstanceReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInstanceReferenceExpression(this, argument);
        }
    }

    /// <remarks>
    /// Represents an interpolated string expression.
    /// </remarks>
    internal abstract partial class BaseInterpolatedStringExpression : Operation, IInterpolatedStringExpression
    {
        protected BaseInterpolatedStringExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.InterpolatedStringExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IInterpolatedStringContent> PartsImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var part in Parts)
                {
                    yield return part;
                }
            }
        }
        /// <summary>
        /// Constituent parts of interpolated string, each of which is an <see cref="IInterpolatedStringContent"/>.
        /// </summary>
        public ImmutableArray<IInterpolatedStringContent> Parts => Operation.SetParentOperation(PartsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInterpolatedStringExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInterpolatedStringExpression(this, argument);
        }
    }

    /// <remarks>
    /// Represents an interpolated string expression.
    /// </remarks>
    internal sealed partial class InterpolatedStringExpression : BaseInterpolatedStringExpression, IInterpolatedStringExpression
    {
        public InterpolatedStringExpression(ImmutableArray<IInterpolatedStringContent> parts, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            PartsImpl = parts;
        }

        protected override ImmutableArray<IInterpolatedStringContent> PartsImpl { get; }
    }

    /// <remarks>
    /// Represents an interpolated string expression.
    /// </remarks>
    internal sealed partial class LazyInterpolatedStringExpression : BaseInterpolatedStringExpression, IInterpolatedStringExpression
    {
        private readonly Lazy<ImmutableArray<IInterpolatedStringContent>> _lazyParts;

        public LazyInterpolatedStringExpression(Lazy<ImmutableArray<IInterpolatedStringContent>> parts, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyParts = parts;
        }

        protected override ImmutableArray<IInterpolatedStringContent> PartsImpl => _lazyParts.Value;
    }

    /// <remarks>
    /// Represents a constituent string literal part of an interpolated string expression.
    /// </remarks>
    internal abstract partial class BaseInterpolatedStringText : Operation, IInterpolatedStringText
    {
        protected BaseInterpolatedStringText(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.InterpolatedStringText, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation TextImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Text;
            }
        }
        /// <summary>
        /// Text content.
        /// </summary>
        public IOperation Text => Operation.SetParentOperation(TextImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInterpolatedStringText(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInterpolatedStringText(this, argument);
        }
    }

    /// <remarks>
    /// Represents a constituent string literal part of an interpolated string expression.
    /// </remarks>
    internal sealed partial class InterpolatedStringText : BaseInterpolatedStringText, IInterpolatedStringText
    {
        public InterpolatedStringText(IOperation text, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            TextImpl = text;
        }

        protected override IOperation TextImpl { get; }
    }

    /// <remarks>
    /// Represents a constituent string literal part of an interpolated string expression.
    /// </remarks>
    internal sealed partial class LazyInterpolatedStringText : BaseInterpolatedStringText, IInterpolatedStringText
    {
        private readonly Lazy<IOperation> _lazyText;

        public LazyInterpolatedStringText(Lazy<IOperation> text, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyText = text;
        }

        protected override IOperation TextImpl => _lazyText.Value;
    }

    /// <remarks>
    /// Represents a constituent interpolation part of an interpolated string expression.
    /// </remarks>
    internal abstract partial class BaseInterpolation : Operation, IInterpolation
    {
        protected BaseInterpolation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Interpolation, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        protected abstract IOperation AlignmentImpl { get; }
        protected abstract IOperation FormatStringImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Expression;
                yield return Alignment;
                yield return FormatString;
            }
        }
        /// <summary>
        /// Expression of the interpolation.
        /// </summary>
        public IOperation Expression => Operation.SetParentOperation(ExpressionImpl, this);
        /// <summary>
        /// Optional alignment of the interpolation.
        /// </summary>
        public IOperation Alignment => Operation.SetParentOperation(AlignmentImpl, this);
        /// <summary>
        /// Optional format string of the interpolation.
        /// </summary>
        public IOperation FormatString => Operation.SetParentOperation(FormatStringImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInterpolation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInterpolation(this, argument);
        }
    }

    /// <remarks>
    /// Represents a constituent interpolation part of an interpolated string expression.
    /// </remarks>
    internal sealed partial class Interpolation : BaseInterpolation, IInterpolation
    {
        public Interpolation(IOperation expression, IOperation alignment, IOperation formatString, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
            AlignmentImpl = alignment;
            FormatStringImpl = formatString;
        }

        protected override IOperation ExpressionImpl { get; }
        protected override IOperation AlignmentImpl { get; }
        protected override IOperation FormatStringImpl { get; }
    }

    /// <remarks>
    /// Represents a constituent interpolation part of an interpolated string expression.
    /// </remarks>
    internal sealed partial class LazyInterpolation : BaseInterpolation, IInterpolation
    {
        private readonly Lazy<IOperation> _lazyExpression;
        private readonly Lazy<IOperation> _lazyAlignment;
        private readonly Lazy<IOperation> _lazyFormatString;

        public LazyInterpolation(Lazy<IOperation> expression, Lazy<IOperation> alignment, Lazy<IOperation> formatString, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = expression;
            _lazyAlignment = alignment;
            _lazyFormatString = formatString;
        }

        protected override IOperation ExpressionImpl => _lazyExpression.Value;

        protected override IOperation AlignmentImpl => _lazyAlignment.Value;

        protected override IOperation FormatStringImpl => _lazyFormatString.Value;
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal abstract partial class BaseInvalidExpression : Operation, IInvalidExpression
    {
        protected BaseInvalidExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.InvalidExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract ImmutableArray<IOperation> ChildrenImpl { get; }
        /// <summary>
        /// Child operations.
        /// </summary>
        public override IEnumerable<IOperation> Children => Operation.SetParentOperation(ChildrenImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalidExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvalidExpression(this, argument);
        }
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class InvalidExpression : BaseInvalidExpression, IInvalidExpression
    {
        public InvalidExpression(ImmutableArray<IOperation> children, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ChildrenImpl = children;
        }
        protected override ImmutableArray<IOperation> ChildrenImpl { get; }
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class LazyInvalidExpression : BaseInvalidExpression, IInvalidExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyChildren;

        public LazyInvalidExpression(Lazy<ImmutableArray<IOperation>> children, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyChildren = children;
        }
        protected override ImmutableArray<IOperation> ChildrenImpl => _lazyChildren.Value;
    }

    /// <summary>
    /// Represents a syntactically or semantically invalid C# or VB statement.
    /// </summary>
    internal abstract partial class BaseInvalidStatement : Operation, IInvalidStatement
    {
        protected BaseInvalidStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.InvalidStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract ImmutableArray<IOperation> ChildrenImpl { get; }
        /// <summary>
        /// Child operations.
        /// </summary>
        public override IEnumerable<IOperation> Children => Operation.SetParentOperation(ChildrenImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalidStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvalidStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a syntactically or semantically invalid C# or VB statement.
    /// </summary>
    internal sealed partial class InvalidStatement : BaseInvalidStatement, IInvalidStatement
    {
        public InvalidStatement(ImmutableArray<IOperation> children, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ChildrenImpl = children;
        }
        protected override ImmutableArray<IOperation> ChildrenImpl { get; }
    }

    /// <summary>
    /// Represents a syntactically or semantically invalid C# or VB statement.
    /// </summary>
    internal sealed partial class LazyInvalidStatement : BaseInvalidStatement, IInvalidStatement
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyChildren;

        public LazyInvalidStatement(Lazy<ImmutableArray<IOperation>> children, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyChildren = children;
        }
        protected override ImmutableArray<IOperation> ChildrenImpl => _lazyChildren.Value;
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal abstract partial class BaseInvocationExpression : Operation, IHasArgumentsExpression, IInvocationExpression
    {
        protected BaseInvocationExpression(IMethodSymbol targetMethod, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.InvocationExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TargetMethod = targetMethod;
            IsVirtual = isVirtual;
        }
        /// <summary>
        /// Method to be invoked.
        /// </summary>
        public IMethodSymbol TargetMethod { get; }
        protected abstract IOperation InstanceImpl { get; }
        /// <summary>
        /// True if the invocation uses a virtual mechanism, and false otherwise.
        /// </summary>
        public bool IsVirtual { get; }
        protected abstract ImmutableArray<IArgument> ArgumentsInEvaluationOrderImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Instance;
                foreach (var argumentsInEvaluationOrder in ArgumentsInEvaluationOrder)
                {
                    yield return argumentsInEvaluationOrder;
                }
            }
        }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        public IOperation Instance => Operation.SetParentOperation(InstanceImpl, this);
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays.
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public ImmutableArray<IArgument> ArgumentsInEvaluationOrder => Operation.SetParentOperation(ArgumentsInEvaluationOrderImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvocationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvocationExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal sealed partial class InvocationExpression : BaseInvocationExpression, IHasArgumentsExpression, IInvocationExpression
    {
        public InvocationExpression(IMethodSymbol targetMethod, IOperation instance, bool isVirtual, ImmutableArray<IArgument> argumentsInEvaluationOrder, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceImpl = instance;
            ArgumentsInEvaluationOrderImpl = argumentsInEvaluationOrder;
        }

        protected override IOperation InstanceImpl { get; }
        protected override ImmutableArray<IArgument> ArgumentsInEvaluationOrderImpl { get; }
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal sealed partial class LazyInvocationExpression : BaseInvocationExpression, IHasArgumentsExpression, IInvocationExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;
        private readonly Lazy<ImmutableArray<IArgument>> _lazyArgumentsInEvaluationOrder;

        public LazyInvocationExpression(IMethodSymbol targetMethod, Lazy<IOperation> instance, bool isVirtual, Lazy<ImmutableArray<IArgument>> argumentsInEvaluationOrder, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
            _lazyArgumentsInEvaluationOrder = argumentsInEvaluationOrder;
        }

        protected override IOperation InstanceImpl => _lazyInstance.Value;

        protected override ImmutableArray<IArgument> ArgumentsInEvaluationOrderImpl => _lazyArgumentsInEvaluationOrder.Value;
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal abstract partial class BaseIsTypeExpression : Operation, IIsTypeExpression
    {
        protected BaseIsTypeExpression(ITypeSymbol isType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.IsTypeExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsType = isType;
        }

        protected abstract IOperation OperandImpl { get; }
        /// <summary>
        /// Type for which to test.
        /// </summary>
        public ITypeSymbol IsType { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Operand;
            }
        }
        /// <summary>
        /// Value to test.
        /// </summary>
        public IOperation Operand => Operation.SetParentOperation(OperandImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIsTypeExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIsTypeExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal sealed partial class IsTypeExpression : BaseIsTypeExpression, IIsTypeExpression
    {
        public IsTypeExpression(IOperation operand, ITypeSymbol isType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperandImpl = operand;
        }

        protected override IOperation OperandImpl { get; }
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal sealed partial class LazyIsTypeExpression : BaseIsTypeExpression, IIsTypeExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyIsTypeExpression(Lazy<IOperation> operand, ITypeSymbol isType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(isType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException(nameof(operand));
        }

        protected override IOperation OperandImpl => _lazyOperand.Value;
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal abstract partial class BaseLabelStatement : Operation, ILabelStatement
    {
        protected BaseLabelStatement(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.LabelStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Label = label;
        }
        /// <summary>
        ///  Label that can be the target of branches.
        /// </summary>
        public ILabelSymbol Label { get; }
        protected abstract IOperation LabeledStatementImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return LabeledStatement;
            }
        }
        /// <summary>
        /// Statement that has been labeled.
        /// </summary>
        public IOperation LabeledStatement => Operation.SetParentOperation(LabeledStatementImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLabelStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLabelStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal sealed partial class LabelStatement : BaseLabelStatement, ILabelStatement
    {
        public LabelStatement(ILabelSymbol label, IOperation labeledStatement, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LabeledStatementImpl = labeledStatement;
        }

        protected override IOperation LabeledStatementImpl { get; }
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal sealed partial class LazyLabelStatement : BaseLabelStatement, ILabelStatement
    {
        private readonly Lazy<IOperation> _lazyLabeledStatement;

        public LazyLabelStatement(ILabelSymbol label, Lazy<IOperation> labeledStatement, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyLabeledStatement = labeledStatement ?? throw new System.ArgumentNullException(nameof(labeledStatement));
        }

        protected override IOperation LabeledStatementImpl => _lazyLabeledStatement.Value;
    }

    internal abstract partial class BaseAnonymousFunctionExpression : Operation, IAnonymousFunctionExpression
    {
        protected BaseAnonymousFunctionExpression(IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.AnonymousFunctionExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Symbol = symbol;
        }
        public IMethodSymbol Symbol { get; }
        protected abstract IBlockStatement BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Body;
            }
        }
        public IBlockStatement Body => Operation.SetParentOperation(BodyImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAnonymousFunctionExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAnonymousFunctionExpression(this, argument);
        }
    }

    internal sealed partial class AnonymousFunctionExpression : BaseAnonymousFunctionExpression, IAnonymousFunctionExpression
    {
        public AnonymousFunctionExpression(IMethodSymbol symbol, IBlockStatement body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            BodyImpl = body;
        }

        protected override IBlockStatement BodyImpl { get; }
    }

    internal sealed partial class LazyAnonymousFunctionExpression : BaseAnonymousFunctionExpression, IAnonymousFunctionExpression
    {
        private readonly Lazy<IBlockStatement> _lazyBody;

        public LazyAnonymousFunctionExpression(IMethodSymbol symbol, Lazy<IBlockStatement> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override IBlockStatement BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a dynamic access to a member of a class, struct, or module.
    /// </summary>
    internal abstract partial class BaseDynamicMemberReferenceExpression : Operation, IDynamicMemberReferenceExpression
    {
        protected BaseDynamicMemberReferenceExpression(string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DynamicMemberReferenceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            MemberName = memberName;
            TypeArguments = typeArguments;
            ContainingType = containingType;
        }

        protected abstract IOperation InstanceImpl { get; }
        /// <summary>
        /// Name of the member.
        /// </summary>
        public string MemberName { get; }
        /// <summary>
        /// Type arguments.
        /// </summary>
        public ImmutableArray<ITypeSymbol> TypeArguments { get; }
        /// <summary>
        /// The containing type of this expression. In C#, this will always be null.
        /// </summary>
        public ITypeSymbol ContainingType { get; }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Instance;
            }
        }
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        public IOperation Instance => Operation.SetParentOperation(InstanceImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicMemberReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicMemberReferenceExpression(this, argument);
        }

    }

    /// <summary>
    /// Represents a dynamic access to a member of a class, struct, or module.
    /// </summary>
    internal sealed partial class DynamicMemberReferenceExpression : BaseDynamicMemberReferenceExpression, IDynamicMemberReferenceExpression
    {
        public DynamicMemberReferenceExpression(IOperation instance, string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceImpl = instance;
        }

        protected override IOperation InstanceImpl { get; }
    }

    /// <summary>
    /// Represents a dynamic access to a member of a class, struct, or module.
    /// </summary>
    internal sealed partial class LazyDynamicMemberReferenceExpression : BaseDynamicMemberReferenceExpression, IDynamicMemberReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyDynamicMemberReferenceExpression(Lazy<IOperation> lazyInstance, string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = lazyInstance;
        }

        protected override IOperation InstanceImpl => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a textual literal numeric, string, etc. expression.
    /// </summary>
    internal sealed partial class LiteralExpression : Operation, ILiteralExpression
    {
        public LiteralExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.LiteralExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLiteralExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLiteralExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a declared local variable.
    /// </summary>
    internal sealed partial class LocalReferenceExpression : Operation, ILocalReferenceExpression
    {
        public LocalReferenceExpression(ILocalSymbol local, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.LocalReferenceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Local = local;
        }
        /// <summary>
        /// Referenced local variable.
        /// </summary>
        public ILocalSymbol Local { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLocalReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLocalReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    internal abstract partial class BaseLockStatement : Operation, ILockStatement
    {
        protected BaseLockStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.LockStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        protected abstract IOperation BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Expression;
                yield return Body;
            }
        }
        /// <summary>
        /// Expression producing a value to be locked.
        /// </summary>
        public IOperation Expression => Operation.SetParentOperation(ExpressionImpl, this);
        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        public IOperation Body => Operation.SetParentOperation(BodyImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLockStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLockStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    internal sealed partial class LockStatement : BaseLockStatement, ILockStatement
    {
        public LockStatement(IOperation expression, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
            BodyImpl = body;
        }

        protected override IOperation ExpressionImpl { get; }
        protected override IOperation BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    internal sealed partial class LazyLockStatement : BaseLockStatement, ILockStatement
    {
        private readonly Lazy<IOperation> _lazyExpression;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyLockStatement(Lazy<IOperation> lockedObject, Lazy<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = lockedObject ?? throw new System.ArgumentNullException(nameof(lockedObject));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override IOperation ExpressionImpl => _lazyExpression.Value;

        protected override IOperation BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# while, for, foreach, or do statement, or a VB While, For, For Each, or Do statement.
    /// </summary>
    internal abstract partial class LoopStatement : Operation, ILoopStatement
    {
        protected LoopStatement(LoopKind loopKind, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LoopKind = loopKind;
        }
        protected abstract IOperation BodyImpl { get; }
        /// <summary>
        /// Kind of the loop.
        /// </summary>
        public LoopKind LoopKind { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public IOperation Body => Operation.SetParentOperation(BodyImpl, this);
    }

    /// <summary>
    /// Represents a reference to a member of a class, struct, or interface.
    /// </summary>
    internal abstract partial class MemberReferenceExpression : Operation, IMemberReferenceExpression
    {
        protected MemberReferenceExpression(ISymbol member, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Member = member;
        }
        protected abstract IOperation InstanceImpl { get; }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public IOperation Instance => Operation.SetParentOperation(InstanceImpl, this);

        /// <summary>
        /// Referenced member.
        /// </summary>
        public ISymbol Member { get; }
    }

    /// <summary>
    /// Represents a reference to a method other than as the target of an invocation.
    /// </summary>
    internal abstract partial class BaseMethodBindingExpression : MemberReferenceExpression, IMethodBindingExpression
    {
        public BaseMethodBindingExpression(IMethodSymbol method, bool isVirtual, ISymbol member, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(member, OperationKind.MethodBindingExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Method = method;
            IsVirtual = isVirtual;
        }
        /// <summary>
        /// Referenced method.
        /// </summary>
        public IMethodSymbol Method { get; }

        /// <summary>
        /// Indicates whether the reference uses virtual semantics.
        /// </summary>
        public bool IsVirtual { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Instance;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitMethodBindingExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitMethodBindingExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a method other than as the target of an invocation.
    /// </summary>
    internal sealed partial class MethodBindingExpression : BaseMethodBindingExpression, IMethodBindingExpression
    {
        public MethodBindingExpression(IMethodSymbol method, bool isVirtual, IOperation instance, ISymbol member, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(method, isVirtual, member, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceImpl = instance;
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        protected override IOperation InstanceImpl { get; }
    }

    /// <summary>
    /// Represents a reference to a method other than as the target of an invocation.
    /// </summary>
    internal sealed partial class LazyMethodBindingExpression : BaseMethodBindingExpression, IMethodBindingExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyMethodBindingExpression(IMethodSymbol method, bool isVirtual, Lazy<IOperation> instance, ISymbol member, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(method, isVirtual, member, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
        }
        protected override IOperation InstanceImpl => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal abstract partial class BaseCoalesceExpression : Operation, ICoalesceExpression
    {
        protected BaseCoalesceExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.CoalesceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        protected abstract IOperation WhenNullImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Expression;
                yield return WhenNull;
            }
        }
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        public IOperation Expression => Operation.SetParentOperation(ExpressionImpl, this);
        /// <summary>
        /// Value to be evaluated if <see cref="Expression"/> evaluates to null/Nothing.
        /// </summary>
        public IOperation WhenNull => Operation.SetParentOperation(WhenNullImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCoalesceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCoalesceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal sealed partial class CoalesceExpression : BaseCoalesceExpression, ICoalesceExpression
    {
        public CoalesceExpression(IOperation expression, IOperation whenNull, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
            WhenNullImpl = whenNull;
        }

        protected override IOperation ExpressionImpl { get; }
        protected override IOperation WhenNullImpl { get; }
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal sealed partial class LazyCoalesceExpression : BaseCoalesceExpression, ICoalesceExpression
    {
        private readonly Lazy<IOperation> _lazyExpression;
        private readonly Lazy<IOperation> _lazyWhenNull;

        public LazyCoalesceExpression(Lazy<IOperation> expression, Lazy<IOperation> whenNull, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
            _lazyWhenNull = whenNull ?? throw new System.ArgumentNullException(nameof(whenNull));
        }

        protected override IOperation ExpressionImpl => _lazyExpression.Value;

        protected override IOperation WhenNullImpl => _lazyWhenNull.Value;
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    internal abstract partial class BaseObjectCreationExpression : Operation, IHasArgumentsExpression, IObjectCreationExpression
    {
        protected BaseObjectCreationExpression(IMethodSymbol constructor, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ObjectCreationExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Constructor = constructor;
        }
        /// <summary>
        /// Constructor to be invoked on the created instance.
        /// </summary>
        public IMethodSymbol Constructor { get; }
        protected abstract IObjectOrCollectionInitializerExpression InitializerImpl { get; }
        protected abstract ImmutableArray<IArgument> ArgumentsInEvaluationOrderImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var argumentsInEvaluationOrder in ArgumentsInEvaluationOrder)
                {
                    yield return argumentsInEvaluationOrder;
                }
                yield return Initializer;
            }
        }
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        public IObjectOrCollectionInitializerExpression Initializer => Operation.SetParentOperation(InitializerImpl, this);
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public ImmutableArray<IArgument> ArgumentsInEvaluationOrder => Operation.SetParentOperation(ArgumentsInEvaluationOrderImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitObjectCreationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitObjectCreationExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    internal sealed partial class ObjectCreationExpression : BaseObjectCreationExpression, IHasArgumentsExpression, IObjectCreationExpression
    {
        public ObjectCreationExpression(IMethodSymbol constructor, IObjectOrCollectionInitializerExpression initializer, ImmutableArray<IArgument> argumentsInEvaluationOrder, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(constructor, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializerImpl = initializer;
            ArgumentsInEvaluationOrderImpl = argumentsInEvaluationOrder;
        }

        protected override IObjectOrCollectionInitializerExpression InitializerImpl { get; }
        protected override ImmutableArray<IArgument> ArgumentsInEvaluationOrderImpl { get; }
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    internal sealed partial class LazyObjectCreationExpression : BaseObjectCreationExpression, IHasArgumentsExpression, IObjectCreationExpression
    {
        private readonly Lazy<IObjectOrCollectionInitializerExpression> _lazyInitializer;
        private readonly Lazy<ImmutableArray<IArgument>> _lazyArgumentsInEvaluationOrder;

        public LazyObjectCreationExpression(IMethodSymbol constructor, Lazy<IObjectOrCollectionInitializerExpression> initializer, Lazy<ImmutableArray<IArgument>> argumentsInEvaluationOrder, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(constructor, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInitializer = initializer;
            _lazyArgumentsInEvaluationOrder = argumentsInEvaluationOrder;
        }

        protected override IObjectOrCollectionInitializerExpression InitializerImpl => _lazyInitializer.Value;

        protected override ImmutableArray<IArgument> ArgumentsInEvaluationOrderImpl => _lazyArgumentsInEvaluationOrder.Value;

    }

    /// <summary>
    /// Represents a C# or VB new/New anonymous object creation expression.
    /// </summary>
    internal abstract partial class BaseAnonymousObjectCreationExpression : Operation, IAnonymousObjectCreationExpression
    {
        protected BaseAnonymousObjectCreationExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.AnonymousObjectCreationExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> InitializersImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var initializer in Initializers)
                {
                    yield return initializer;
                }
            }
        }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public ImmutableArray<IOperation> Initializers => Operation.SetParentOperation(InitializersImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAnonymousObjectCreationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAnonymousObjectCreationExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB new/New anonymous object creation expression.
    /// </summary>
    internal sealed partial class AnonymousObjectCreationExpression : BaseAnonymousObjectCreationExpression, IAnonymousObjectCreationExpression
    {
        public AnonymousObjectCreationExpression(ImmutableArray<IOperation> initializers, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializersImpl = initializers;
        }

        protected override ImmutableArray<IOperation> InitializersImpl { get; }
    }

    /// <summary>
    /// Represents a C# or VB new/New anonymous object creation expression.
    /// </summary>
    internal sealed partial class LazyAnonymousObjectCreationExpression : BaseAnonymousObjectCreationExpression, IAnonymousObjectCreationExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyInitializers;

        public LazyAnonymousObjectCreationExpression(Lazy<ImmutableArray<IOperation>> initializers, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInitializers = initializers;
        }

        protected override ImmutableArray<IOperation> InitializersImpl => _lazyInitializers.Value;
    }

    /// <summary>
    /// Represents an argument value that has been omitted in an invocation.
    /// </summary>
    internal sealed partial class OmittedArgumentExpression : Operation, IOmittedArgumentExpression
    {
        public OmittedArgumentExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.OmittedArgumentExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitOmittedArgumentExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitOmittedArgumentExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an initialization of a parameter at the point of declaration.
    /// </summary>
    internal abstract partial class BaseParameterInitializer : SymbolInitializer, IParameterInitializer
    {
        public BaseParameterInitializer(IParameterSymbol parameter, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Parameter = parameter;
        }
        /// <summary>
        /// Initialized parameter.
        /// </summary>
        public IParameterSymbol Parameter { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParameterInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParameterInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents an initialization of a parameter at the point of declaration.
    /// </summary>
    internal sealed partial class ParameterInitializer : BaseParameterInitializer, IParameterInitializer
    {
        public ParameterInitializer(IParameterSymbol parameter, IOperation value, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(parameter, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
        }
        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents an initialization of a parameter at the point of declaration.
    /// </summary>
    internal sealed partial class LazyParameterInitializer : BaseParameterInitializer, IParameterInitializer
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyParameterInitializer(IParameterSymbol parameter, Lazy<IOperation> value, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(parameter, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a reference to a parameter.
    /// </summary>
    internal sealed partial class ParameterReferenceExpression : Operation, IParameterReferenceExpression
    {
        public ParameterReferenceExpression(IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.ParameterReferenceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Parameter = parameter;
        }
        /// <summary>
        /// Referenced parameter.
        /// </summary>
        public IParameterSymbol Parameter { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParameterReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParameterReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    internal abstract partial class BaseParenthesizedExpression : Operation, IParenthesizedExpression
    {
        protected BaseParenthesizedExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ParenthesizedExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation OperandImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Operand;
            }
        }
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        public IOperation Operand => Operation.SetParentOperation(OperandImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParenthesizedExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParenthesizedExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    internal sealed partial class ParenthesizedExpression : BaseParenthesizedExpression, IParenthesizedExpression
    {
        public ParenthesizedExpression(IOperation operand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperandImpl = operand;
        }

        protected override IOperation OperandImpl { get; }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    internal sealed partial class LazyParenthesizedExpression : BaseParenthesizedExpression, IParenthesizedExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyParenthesizedExpression(Lazy<IOperation> operand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException(nameof(operand));
        }

        protected override IOperation OperandImpl => _lazyOperand.Value;
    }

    /// <summary>
    /// Represents a general placeholder when no more specific kind of placeholder is available.
    /// A placeholder is an expression whose meaning is inferred from context.
    /// </summary>
    internal sealed partial class PlaceholderExpression : Operation, IPlaceholderExpression
    {
        public PlaceholderExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.PlaceholderExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPlaceholderExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPlaceholderExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    internal abstract partial class BasePointerIndirectionReferenceExpression : Operation, IPointerIndirectionReferenceExpression
    {
        protected BasePointerIndirectionReferenceExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.PointerIndirectionReferenceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation PointerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Pointer;
            }
        }
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        public IOperation Pointer => Operation.SetParentOperation(PointerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPointerIndirectionReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPointerIndirectionReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    internal sealed partial class PointerIndirectionReferenceExpression : BasePointerIndirectionReferenceExpression, IPointerIndirectionReferenceExpression
    {
        public PointerIndirectionReferenceExpression(IOperation pointer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            PointerImpl = pointer;
        }

        protected override IOperation PointerImpl { get; }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    internal sealed partial class LazyPointerIndirectionReferenceExpression : BasePointerIndirectionReferenceExpression, IPointerIndirectionReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyPointer;

        public LazyPointerIndirectionReferenceExpression(Lazy<IOperation> pointer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyPointer = pointer ?? throw new System.ArgumentNullException(nameof(pointer));
        }

        protected override IOperation PointerImpl => _lazyPointer.Value;
    }

    /// <summary>
    /// Represents an initialization of a property.
    /// </summary>
    internal abstract partial class BasePropertyInitializer : SymbolInitializer, IPropertyInitializer
    {
        public BasePropertyInitializer(IPropertySymbol initializedProperty, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializedProperty = initializedProperty;
        }
        /// <summary>
        /// Set method used to initialize the property.
        /// </summary>
        public IPropertySymbol InitializedProperty { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents an initialization of a property.
    /// </summary>
    internal sealed partial class PropertyInitializer : BasePropertyInitializer, IPropertyInitializer
    {
        public PropertyInitializer(IPropertySymbol initializedProperty, IOperation value, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(initializedProperty, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
        }
        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents an initialization of a property.
    /// </summary>
    internal sealed partial class LazyPropertyInitializer : BasePropertyInitializer, IPropertyInitializer
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyPropertyInitializer(IPropertySymbol initializedProperty, Lazy<IOperation> value, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(initializedProperty, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    internal abstract partial class BasePropertyReferenceExpression : MemberReferenceExpression, IPropertyReferenceExpression, IHasArgumentsExpression
    {
        protected BasePropertyReferenceExpression(IPropertySymbol property, ISymbol member, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(member, OperationKind.PropertyReferenceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Property = property;
        }
        /// <summary>
        /// Referenced property.
        /// </summary>
        public IPropertySymbol Property { get; }
        protected abstract ImmutableArray<IArgument> ArgumentsInEvaluationOrderImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Instance;
                foreach (var argumentsInEvaluationOrder in ArgumentsInEvaluationOrder)
                {
                    yield return argumentsInEvaluationOrder;
                }
            }
        }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public ImmutableArray<IArgument> ArgumentsInEvaluationOrder => Operation.SetParentOperation(ArgumentsInEvaluationOrderImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    internal sealed partial class PropertyReferenceExpression : BasePropertyReferenceExpression, IPropertyReferenceExpression, IHasArgumentsExpression
    {
        public PropertyReferenceExpression(IPropertySymbol property, IOperation instance, ISymbol member, ImmutableArray<IArgument> argumentsInEvaluationOrder, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(property, member, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceImpl = instance;
            ArgumentsInEvaluationOrderImpl = argumentsInEvaluationOrder;
        }
        protected override IOperation InstanceImpl { get; }
        protected override ImmutableArray<IArgument> ArgumentsInEvaluationOrderImpl { get; }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    internal sealed partial class LazyPropertyReferenceExpression : BasePropertyReferenceExpression, IPropertyReferenceExpression, IHasArgumentsExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;
        private readonly Lazy<ImmutableArray<IArgument>> _lazyArgumentsInEvaluationOrder;

        public LazyPropertyReferenceExpression(IPropertySymbol property, Lazy<IOperation> instance, ISymbol member, Lazy<ImmutableArray<IArgument>> argumentsInEvaluationOrder, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(property, member, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
            _lazyArgumentsInEvaluationOrder = argumentsInEvaluationOrder ?? throw new System.ArgumentNullException(nameof(argumentsInEvaluationOrder));
        }
        protected override IOperation InstanceImpl => _lazyInstance.Value;

        protected override ImmutableArray<IArgument> ArgumentsInEvaluationOrderImpl => _lazyArgumentsInEvaluationOrder.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents Case x To y in VB.
    /// </summary>
    internal abstract partial class BaseRangeCaseClause : CaseClause, IRangeCaseClause
    {
        public BaseRangeCaseClause(CaseKind caseKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(caseKind, OperationKind.RangeCaseClause, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation MinimumValueImpl { get; }
        protected abstract IOperation MaximumValueImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return MinimumValue;
                yield return MaximumValue;
            }
        }
        /// <summary>
        /// Minimum value of the case range.
        /// </summary>
        public IOperation MinimumValue => Operation.SetParentOperation(MinimumValueImpl, this);
        /// <summary>
        /// Maximum value of the case range.
        /// </summary>
        public IOperation MaximumValue => Operation.SetParentOperation(MaximumValueImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitRangeCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitRangeCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents Case x To y in VB.
    /// </summary>
    internal sealed partial class RangeCaseClause : BaseRangeCaseClause, IRangeCaseClause
    {
        public RangeCaseClause(IOperation minimumValue, IOperation maximumValue, CaseKind caseKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(caseKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            MinimumValueImpl = minimumValue;
            MaximumValueImpl = maximumValue;
        }

        protected override IOperation MinimumValueImpl { get; }
        protected override IOperation MaximumValueImpl { get; }
    }

    /// <summary>
    /// Represents Case x To y in VB.
    /// </summary>
    internal sealed partial class LazyRangeCaseClause : BaseRangeCaseClause, IRangeCaseClause
    {
        private readonly Lazy<IOperation> _lazyMinimumValue;
        private readonly Lazy<IOperation> _lazyMaximumValue;

        public LazyRangeCaseClause(Lazy<IOperation> minimumValue, Lazy<IOperation> maximumValue, CaseKind caseKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(caseKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyMinimumValue = minimumValue ?? throw new System.ArgumentNullException(nameof(minimumValue));
            _lazyMaximumValue = maximumValue ?? throw new System.ArgumentNullException(nameof(maximumValue));
        }

        protected override IOperation MinimumValueImpl => _lazyMinimumValue.Value;

        protected override IOperation MaximumValueImpl => _lazyMaximumValue.Value;
    }

    /// <summary>
    /// Represents Case Is op x in VB.
    /// </summary>
    internal abstract partial class BaseRelationalCaseClause : CaseClause, IRelationalCaseClause
    {
        public BaseRelationalCaseClause(BinaryOperationKind relation, CaseKind caseKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(caseKind, OperationKind.RelationalCaseClause, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Relation = relation;
        }

        protected abstract IOperation ValueImpl { get; }
        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        public BinaryOperationKind Relation { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
            }
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitRelationalCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitRelationalCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents Case Is op x in VB.
    /// </summary>
    internal sealed partial class RelationalCaseClause : BaseRelationalCaseClause, IRelationalCaseClause
    {
        public RelationalCaseClause(IOperation value, BinaryOperationKind relation, CaseKind caseKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(relation, caseKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
        }

        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents Case Is op x in VB.
    /// </summary>
    internal sealed partial class LazyRelationalCaseClause : BaseRelationalCaseClause, IRelationalCaseClause
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyRelationalCaseClause(Lazy<IOperation> value, BinaryOperationKind relation, CaseKind caseKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(relation, caseKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }

        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    internal abstract partial class BaseReturnStatement : Operation, IReturnStatement
    {
        protected BaseReturnStatement(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Debug.Assert(kind == OperationKind.ReturnStatement
                      || kind == OperationKind.YieldReturnStatement
                      || kind == OperationKind.YieldBreakStatement);
        }

        protected abstract IOperation ReturnedValueImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return ReturnedValue;
            }
        }
        /// <summary>
        /// Value to be returned.
        /// </summary>
        public IOperation ReturnedValue => Operation.SetParentOperation(ReturnedValueImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            if (Kind == OperationKind.YieldBreakStatement)
            {
                visitor.VisitYieldBreakStatement(this);
            }
            else
            {
                visitor.VisitReturnStatement(this);
            }
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            if (Kind == OperationKind.YieldBreakStatement)
            {
                return visitor.VisitYieldBreakStatement(this, argument);
            }
            else
            {
                return visitor.VisitReturnStatement(this, argument);
            }
        }
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    internal sealed partial class ReturnStatement : BaseReturnStatement, IReturnStatement
    {
        public ReturnStatement(OperationKind kind, IOperation returnedValue, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ReturnedValueImpl = returnedValue;
        }

        protected override IOperation ReturnedValueImpl { get; }
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    internal sealed partial class LazyReturnStatement : BaseReturnStatement, IReturnStatement
    {
        private readonly Lazy<IOperation> _lazyReturnedValue;

        public LazyReturnStatement(OperationKind kind, Lazy<IOperation> returnedValue, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyReturnedValue = returnedValue ?? throw new System.ArgumentNullException(nameof(returnedValue));
        }

        protected override IOperation ReturnedValueImpl => _lazyReturnedValue.Value;
    }

    /// <summary>
    /// Represents case x in C# or Case x in VB.
    /// </summary>
    internal abstract partial class BaseSingleValueCaseClause : CaseClause, ISingleValueCaseClause
    {
        public BaseSingleValueCaseClause(BinaryOperationKind equality, CaseKind caseKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(caseKind, OperationKind.SingleValueCaseClause, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Equality = equality;
        }

        protected abstract IOperation ValueImpl { get; }
        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        public BinaryOperationKind Equality { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
            }
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSingleValueCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSingleValueCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents case x in C# or Case x in VB.
    /// </summary>
    internal sealed partial class SingleValueCaseClause : BaseSingleValueCaseClause, ISingleValueCaseClause
    {
        public SingleValueCaseClause(IOperation value, BinaryOperationKind equality, CaseKind caseKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(equality, caseKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
        }

        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents case x in C# or Case x in VB.
    /// </summary>
    internal sealed partial class LazySingleValueCaseClause : BaseSingleValueCaseClause, ISingleValueCaseClause
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazySingleValueCaseClause(Lazy<IOperation> value, BinaryOperationKind equality, CaseKind caseKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(equality, caseKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }

        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents default case in C# or Case Else in VB.
    /// </summary>
    internal sealed partial class DefaultCaseClause : CaseClause, IDefaultCaseClause
    {
        public DefaultCaseClause(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(CaseKind.Default, OperationKind.DefaultCaseClause, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDefaultCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDefaultCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents a SizeOf expression.
    /// </summary>
    internal sealed partial class SizeOfExpression : TypeOperationExpression, ISizeOfExpression
    {
        public SizeOfExpression(ITypeSymbol typeOperand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(typeOperand, OperationKind.SizeOfExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSizeOfExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSizeOfExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB Stop statement.
    /// </summary>
    internal sealed partial class StopStatement : Operation, IStopStatement
    {
        public StopStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.StopStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitStopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitStopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    internal abstract partial class BaseSwitchCase : Operation, ISwitchCase
    {
        protected BaseSwitchCase(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.SwitchCase, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<ICaseClause> ClausesImpl { get; }
        protected abstract ImmutableArray<IOperation> BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var clause in Clauses)
                {
                    yield return clause;
                }
                foreach (var body in Body)
                {
                    yield return body;
                }
            }
        }
        /// <summary>
        /// Clauses of the case. For C# there is one clause per case, but for VB there can be multiple.
        /// </summary>
        public ImmutableArray<ICaseClause> Clauses => Operation.SetParentOperation(ClausesImpl, this);
        /// <summary>
        /// Statements of the case.
        /// </summary>
        public ImmutableArray<IOperation> Body => Operation.SetParentOperation(BodyImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSwitchCase(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSwitchCase(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    internal sealed partial class SwitchCase : BaseSwitchCase, ISwitchCase
    {
        public SwitchCase(ImmutableArray<ICaseClause> clauses, ImmutableArray<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ClausesImpl = clauses;
            BodyImpl = body;
        }

        protected override ImmutableArray<ICaseClause> ClausesImpl { get; }
        protected override ImmutableArray<IOperation> BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    internal sealed partial class LazySwitchCase : BaseSwitchCase, ISwitchCase
    {
        private readonly Lazy<ImmutableArray<ICaseClause>> _lazyClauses;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyBody;

        public LazySwitchCase(Lazy<ImmutableArray<ICaseClause>> clauses, Lazy<ImmutableArray<IOperation>> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyClauses = clauses;
            _lazyBody = body;
        }

        protected override ImmutableArray<ICaseClause> ClausesImpl => _lazyClauses.Value;

        protected override ImmutableArray<IOperation> BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal abstract partial class BaseSwitchStatement : Operation, ISwitchStatement
    {
        protected BaseSwitchStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.SwitchStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ValueImpl { get; }
        protected abstract ImmutableArray<ISwitchCase> CasesImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
                foreach (var @case in Cases)
                {
                    yield return @case;
                }
            }
        }
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);
        /// <summary>
        /// Cases of the switch.
        /// </summary>
        public ImmutableArray<ISwitchCase> Cases => Operation.SetParentOperation(CasesImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSwitchStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSwitchStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal sealed partial class SwitchStatement : BaseSwitchStatement, ISwitchStatement
    {
        public SwitchStatement(IOperation value, ImmutableArray<ISwitchCase> cases, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
            CasesImpl = cases;
        }

        protected override IOperation ValueImpl { get; }
        protected override ImmutableArray<ISwitchCase> CasesImpl { get; }
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal sealed partial class LazySwitchStatement : BaseSwitchStatement, ISwitchStatement
    {
        private readonly Lazy<IOperation> _lazyValue;
        private readonly Lazy<ImmutableArray<ISwitchCase>> _lazyCases;

        public LazySwitchStatement(Lazy<IOperation> value, Lazy<ImmutableArray<ISwitchCase>> cases, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
            _lazyCases = cases;
        }

        protected override IOperation ValueImpl => _lazyValue.Value;

        protected override ImmutableArray<ISwitchCase> CasesImpl => _lazyCases.Value;
    }

    /// <summary>
    /// Represents an initializer for a field, property, or parameter.
    /// </summary>
    internal abstract partial class SymbolInitializer : Operation, ISymbolInitializer
    {
        protected SymbolInitializer(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IOperation ValueImpl { get; }
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    internal abstract partial class BaseSyntheticLocalReferenceExpression : Operation, ISyntheticLocalReferenceExpression
    {
        protected BaseSyntheticLocalReferenceExpression(SyntheticLocalKind syntheticLocalKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.SyntheticLocalReferenceExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            SyntheticLocalKind = syntheticLocalKind;
        }
        /// <summary>
        /// Kind of the synthetic local.
        /// </summary>
        public SyntheticLocalKind SyntheticLocalKind { get; }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSyntheticLocalReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSyntheticLocalReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    internal sealed partial class SyntheticLocalReferenceExpression : BaseSyntheticLocalReferenceExpression, ISyntheticLocalReferenceExpression
    {
        public SyntheticLocalReferenceExpression(SyntheticLocalKind syntheticLocalKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(syntheticLocalKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    internal sealed partial class LazySyntheticLocalReferenceExpression : BaseSyntheticLocalReferenceExpression, ISyntheticLocalReferenceExpression
    {
        public LazySyntheticLocalReferenceExpression(SyntheticLocalKind syntheticLocalKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(syntheticLocalKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal abstract partial class BaseTryStatement : Operation, ITryStatement
    {
        protected BaseTryStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.TryStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IBlockStatement BodyImpl { get; }
        protected abstract ImmutableArray<ICatchClause> CatchesImpl { get; }
        protected abstract IBlockStatement FinallyHandlerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Body;
                foreach (var catche in Catches)
                {
                    yield return catche;
                }
                yield return FinallyHandler;
            }
        }
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        public IBlockStatement Body => Operation.SetParentOperation(BodyImpl, this);
        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        public ImmutableArray<ICatchClause> Catches => Operation.SetParentOperation(CatchesImpl, this);
        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        public IBlockStatement FinallyHandler => Operation.SetParentOperation(FinallyHandlerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTryStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTryStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal sealed partial class TryStatement : BaseTryStatement, ITryStatement
    {
        public TryStatement(IBlockStatement body, ImmutableArray<ICatchClause> catches, IBlockStatement finallyHandler, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            BodyImpl = body;
            CatchesImpl = catches;
            FinallyHandlerImpl = finallyHandler;
        }

        protected override IBlockStatement BodyImpl { get; }
        protected override ImmutableArray<ICatchClause> CatchesImpl { get; }
        protected override IBlockStatement FinallyHandlerImpl { get; }
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal sealed partial class LazyTryStatement : BaseTryStatement, ITryStatement
    {
        private readonly Lazy<IBlockStatement> _lazyBody;
        private readonly Lazy<ImmutableArray<ICatchClause>> _lazyCatches;
        private readonly Lazy<IBlockStatement> _lazyFinallyHandler;

        public LazyTryStatement(Lazy<IBlockStatement> body, Lazy<ImmutableArray<ICatchClause>> catches, Lazy<IBlockStatement> finallyHandler, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
            _lazyCatches = catches;
            _lazyFinallyHandler = finallyHandler ?? throw new System.ArgumentNullException(nameof(finallyHandler));
        }

        protected override IBlockStatement BodyImpl => _lazyBody.Value;

        protected override ImmutableArray<ICatchClause> CatchesImpl => _lazyCatches.Value;

        protected override IBlockStatement FinallyHandlerImpl => _lazyFinallyHandler.Value;
    }

    /// <summary>
    /// Represents a tuple expression.
    /// </summary>
    internal abstract partial class BaseTupleExpression : Operation, ITupleExpression
    {
        protected BaseTupleExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.TupleExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> ElementsImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var element in Elements)
                {
                    yield return element;
                }
            }
        }
        /// <summary>
        /// Elements for tuple expression.
        /// </summary>
        public ImmutableArray<IOperation> Elements => Operation.SetParentOperation(ElementsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTupleExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTupleExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a tuple expression.
    /// </summary>
    internal sealed partial class TupleExpression : BaseTupleExpression, ITupleExpression
    {
        public TupleExpression(ImmutableArray<IOperation> elements, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ElementsImpl = elements;
        }

        protected override ImmutableArray<IOperation> ElementsImpl { get; }
    }

    /// <summary>
    /// Represents a tuple expression.
    /// </summary>
    internal sealed partial class LazyTupleExpression : BaseTupleExpression, ITupleExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyElements;

        public LazyTupleExpression(Lazy<ImmutableArray<IOperation>> elements, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyElements = elements;
        }

        protected override ImmutableArray<IOperation> ElementsImpl => _lazyElements.Value;
    }

    /// <summary>
    /// Represents a TypeOf expression.
    /// </summary>
    internal sealed partial class TypeOfExpression : TypeOperationExpression, ITypeOfExpression
    {
        public TypeOfExpression(ITypeSymbol typeOperand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(typeOperand, OperationKind.TypeOfExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTypeOfExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTypeOfExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an expression operating on a type.
    /// </summary>
    internal abstract partial class TypeOperationExpression : Operation, ITypeOperationExpression
    {
        protected TypeOperationExpression(ITypeSymbol typeOperand, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TypeOperand = typeOperand;
        }
        /// <summary>
        /// Type operand.
        /// </summary>
        public ITypeSymbol TypeOperand { get; }
    }

    /// <summary>
    /// Represents a type parameter object creation expression, i.e. new T(), where T is a type parameter with new constraint.
    /// </summary>
    internal abstract partial class BaseTypeParameterObjectCreationExpression : Operation, ITypeParameterObjectCreationExpression
    {
        public BaseTypeParameterObjectCreationExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.TypeParameterObjectCreationExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IObjectOrCollectionInitializerExpression InitializerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Initializer;
            }
        }
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        public IObjectOrCollectionInitializerExpression Initializer => Operation.SetParentOperation(InitializerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTypeParameterObjectCreationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTypeParameterObjectCreationExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a type parameter object creation expression, i.e. new T(), where T is a type parameter with new constraint.
    /// </summary>
    internal sealed partial class TypeParameterObjectCreationExpression : BaseTypeParameterObjectCreationExpression, ITypeParameterObjectCreationExpression
    {
        public TypeParameterObjectCreationExpression(IObjectOrCollectionInitializerExpression initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializerImpl = initializer;
        }
        protected override IObjectOrCollectionInitializerExpression InitializerImpl { get; }
    }

    /// <summary>
    /// Represents a type parameter object creation expression, i.e. new T(), where T is a type parameter with new constraint.
    /// </summary>
    internal sealed partial class LazyTypeParameterObjectCreationExpression : BaseTypeParameterObjectCreationExpression, ITypeParameterObjectCreationExpression
    {
        private readonly Lazy<IObjectOrCollectionInitializerExpression> _lazyInitializer;
        public LazyTypeParameterObjectCreationExpression(Lazy<IObjectOrCollectionInitializerExpression> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }
        protected override IObjectOrCollectionInitializerExpression InitializerImpl => _lazyInitializer.Value;
    }

    /// <remarks>
    /// Represents a dynamically bound new/New expression.
    /// </remarks>
    internal abstract partial class BaseDynamicObjectCreationExpression : Operation, IHasDynamicArgumentsExpression, IDynamicObjectCreationExpression
    {
        public BaseDynamicObjectCreationExpression(string name, ImmutableArray<ISymbol> applicableSymbols, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.TypeParameterObjectCreationExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Name = name;
            ApplicableSymbols = applicableSymbols;
            ArgumentNames = argumentNames;
            ArgumentRefKinds = argumentRefKinds;
        }
        protected abstract ImmutableArray<IOperation> ArgumentsImpl { get; }
        protected abstract IObjectOrCollectionInitializerExpression InitializerImpl { get; }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var argument in Arguments)
                {
                    yield return argument;
                }
                yield return Initializer;
            }
        }
        /// <summary>
        /// Name of the dynamically invoked member.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// List of applicable symbols that are dynamically bound to the <see cref="Name"/>.
        /// </summary>
        public ImmutableArray<ISymbol> ApplicableSymbols { get; }
        /// <summary>
        /// Optional argument names for named arguments.
        /// </summary>
        public ImmutableArray<string> ArgumentNames { get; }
        /// <summary>
        /// Optional argument ref kinds.
        /// </summary>
        public ImmutableArray<RefKind> ArgumentRefKinds { get; }
        /// <summary>
        /// Dynamically bound arguments, excluding the instance argument.
        /// </summary>
        public ImmutableArray<IOperation> Arguments => Operation.SetParentOperation(ArgumentsImpl, this);
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        public IObjectOrCollectionInitializerExpression Initializer => Operation.SetParentOperation(InitializerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicObjectCreationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicObjectCreationExpression(this, argument);
        }
    }

    /// <remarks>
    /// Represents a dynamically bound new/New expression.
    /// </remarks>
    internal sealed partial class DynamicObjectCreationExpression : BaseDynamicObjectCreationExpression, IHasDynamicArgumentsExpression, IDynamicObjectCreationExpression
    {
        public DynamicObjectCreationExpression(string name, ImmutableArray<ISymbol> applicableSymbols, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, IObjectOrCollectionInitializerExpression initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(name, applicableSymbols, argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArgumentsImpl = arguments;
            InitializerImpl = initializer;
        }
        protected override ImmutableArray<IOperation> ArgumentsImpl { get; }
        protected override IObjectOrCollectionInitializerExpression InitializerImpl { get; }
    }

    /// <remarks>
    /// Represents a dynamically bound new/New expression.
    /// </remarks>
    internal sealed partial class LazyDynamicObjectCreationExpression : BaseDynamicObjectCreationExpression, IHasDynamicArgumentsExpression, IDynamicObjectCreationExpression
    {
        private readonly Lazy<IObjectOrCollectionInitializerExpression> _lazyInitializer;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyArguments;
        public LazyDynamicObjectCreationExpression(string name, ImmutableArray<ISymbol> applicableSymbols, Lazy<ImmutableArray<IOperation>> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, Lazy<IObjectOrCollectionInitializerExpression> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(name, applicableSymbols, argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyArguments = arguments ?? throw new System.ArgumentNullException(nameof(arguments));
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }
        protected override ImmutableArray<IOperation> ArgumentsImpl => _lazyArguments.Value;
        protected override IObjectOrCollectionInitializerExpression InitializerImpl => _lazyInitializer.Value;
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    internal abstract partial class BaseUnaryOperatorExpression : Operation, IHasOperatorMethodExpression, IUnaryOperatorExpression
    {
        protected BaseUnaryOperatorExpression(UnaryOperationKind unaryOperationKind, bool isLifted, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.UnaryOperatorExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            UnaryOperationKind = unaryOperationKind;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod;
            IsLifted = isLifted;
        }
        /// <summary>
        /// Kind of unary operation.
        /// </summary>
        public UnaryOperationKind UnaryOperationKind { get; }
        protected abstract IOperation OperandImpl { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }

        public bool IsLifted { get; }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Operand;
            }
        }
        /// <summary>
        /// Single operand.
        /// </summary>
        public IOperation Operand => Operation.SetParentOperation(OperandImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitUnaryOperatorExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitUnaryOperatorExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    internal sealed partial class UnaryOperatorExpression : BaseUnaryOperatorExpression, IHasOperatorMethodExpression, IUnaryOperatorExpression
    {
        public UnaryOperatorExpression(UnaryOperationKind unaryOperationKind, IOperation operand, bool isLifted, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(unaryOperationKind, isLifted, usesOperatorMethod, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperandImpl = operand;
        }

        protected override IOperation OperandImpl { get; }
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    internal sealed partial class LazyUnaryOperatorExpression : BaseUnaryOperatorExpression, IHasOperatorMethodExpression, IUnaryOperatorExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyUnaryOperatorExpression(UnaryOperationKind unaryOperationKind, Lazy<IOperation> operand, bool isLifted, bool usesOperatorMethod, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : 
            base(unaryOperationKind, isLifted, usesOperatorMethod, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException(nameof(operand));
        }

        protected override IOperation OperandImpl => _lazyOperand.Value;
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal abstract partial class BaseUsingStatement : Operation, IUsingStatement
    {
        protected BaseUsingStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.UsingStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation BodyImpl { get; }
        protected abstract IVariableDeclarationStatement DeclarationImpl { get; }
        protected abstract IOperation ValueImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Declaration;
                yield return Value;
                yield return Body;
            }
        }
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        public IOperation Body => Operation.SetParentOperation(BodyImpl, this);

        /// <summary>
        /// Declaration introduced by the using statement. Null if the using statement does not declare any variables.
        /// </summary>
        public IVariableDeclarationStatement Declaration => Operation.SetParentOperation(DeclarationImpl, this);

        /// <summary>
        /// Resource held by the using. Can be null if Declaration is not null.
        /// </summary>
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitUsingStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitUsingStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal sealed partial class UsingStatement : BaseUsingStatement, IUsingStatement
    {
        public UsingStatement(IOperation body, IVariableDeclarationStatement declaration, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            BodyImpl = body;
            DeclarationImpl = declaration;
            ValueImpl = value;
        }

        protected override IOperation BodyImpl { get; }
        protected override IVariableDeclarationStatement DeclarationImpl { get; }
        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal sealed partial class LazyUsingStatement : BaseUsingStatement, IUsingStatement
    {
        private readonly Lazy<IOperation> _lazyBody;
        private readonly Lazy<IVariableDeclarationStatement> _lazyDeclaration;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyUsingStatement(Lazy<IOperation> body, Lazy<IVariableDeclarationStatement> declaration, Lazy<IOperation> value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
            _lazyDeclaration = declaration ?? throw new System.ArgumentNullException(nameof(declaration));
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }

        protected override IOperation BodyImpl => _lazyBody.Value;

        protected override IVariableDeclarationStatement DeclarationImpl => _lazyDeclaration.Value;

        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    internal abstract partial class BaseVariableDeclaration : Operation, IVariableDeclaration
    {
        protected BaseVariableDeclaration(ImmutableArray<ILocalSymbol> variables, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.VariableDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Variables = variables;
        }
        /// <summary>
        /// Symbols declared by the declaration. In VB, it's possible to declare multiple variables with the
        /// same initializer. In C#, this will always have a single symbol.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Variables { get; }
        protected abstract IOperation InitializerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Initializer;
            }
        }

        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        public IOperation Initializer => Operation.SetParentOperation(InitializerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableDeclaration(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableDeclaration(this, argument);
        }
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    internal sealed partial class VariableDeclaration : BaseVariableDeclaration, IVariableDeclaration
    {
        public VariableDeclaration(ImmutableArray<ILocalSymbol> variables, IOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(variables, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializerImpl = initializer;
        }

        protected override IOperation InitializerImpl { get; }
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    internal sealed partial class LazyVariableDeclaration : BaseVariableDeclaration, IVariableDeclaration
    {
        private readonly Lazy<IOperation> _lazyInitializer;

        public LazyVariableDeclaration(ImmutableArray<ILocalSymbol> variables, Lazy<IOperation> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(variables, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }

        protected override IOperation InitializerImpl => _lazyInitializer.Value;
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal abstract partial class BaseVariableDeclarationStatement : Operation, IVariableDeclarationStatement
    {
        protected BaseVariableDeclarationStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.VariableDeclarationStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IVariableDeclaration> DeclarationsImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var declaration in Declarations)
                {
                    yield return declaration;
                }
            }
        }
        /// <summary>
        /// Variables declared by the statement.
        /// </summary>
        public ImmutableArray<IVariableDeclaration> Declarations => Operation.SetParentOperation(DeclarationsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableDeclarationStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableDeclarationStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal sealed partial class VariableDeclarationStatement : BaseVariableDeclarationStatement, IVariableDeclarationStatement
    {
        public VariableDeclarationStatement(ImmutableArray<IVariableDeclaration> declarations, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            DeclarationsImpl = declarations;
        }

        protected override ImmutableArray<IVariableDeclaration> DeclarationsImpl { get; }
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal sealed partial class LazyVariableDeclarationStatement : BaseVariableDeclarationStatement, IVariableDeclarationStatement
    {
        private readonly Lazy<ImmutableArray<IVariableDeclaration>> _lazyDeclarations;

        public LazyVariableDeclarationStatement(Lazy<ImmutableArray<IVariableDeclaration>> declarations, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyDeclarations = declarations;
        }

        protected override ImmutableArray<IVariableDeclaration> DeclarationsImpl => _lazyDeclarations.Value;
    }

    /// <summary>
    /// Represents a C# while or do statement, or a VB While or Do statement.
    /// </summary>
    internal abstract partial class BaseWhileUntilLoopStatement : ForWhileUntilLoopStatement, IWhileUntilLoopStatement
    {
        public BaseWhileUntilLoopStatement(bool isTopTest, bool isWhile, LoopKind loopKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(loopKind, OperationKind.LoopStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsTopTest = isTopTest;
            IsWhile = isWhile;
        }
        /// <summary>
        /// True if the loop test executes at the top of the loop; false if the loop test executes at the bottom of the loop.
        /// </summary>
        public bool IsTopTest { get; }
        /// <summary>
        /// True if the loop is a while loop; false if the loop is an until loop.
        /// </summary>
        public bool IsWhile { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Condition;
                yield return Body;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitWhileUntilLoopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitWhileUntilLoopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# while or do statement, or a VB While or Do statement.
    /// </summary>
    internal sealed partial class WhileUntilLoopStatement : BaseWhileUntilLoopStatement, IWhileUntilLoopStatement
    {
        public WhileUntilLoopStatement(bool isTopTest, bool isWhile, IOperation condition, LoopKind loopKind, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isTopTest, isWhile, loopKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ConditionImpl = condition;
            BodyImpl = body;
        }
        protected override IOperation ConditionImpl { get; }
        protected override IOperation BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# while or do statement, or a VB While or Do statement.
    /// </summary>
    internal sealed partial class LazyWhileUntilLoopStatement : BaseWhileUntilLoopStatement, IWhileUntilLoopStatement
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyWhileUntilLoopStatement(bool isTopTest, bool isWhile, Lazy<IOperation> condition, LoopKind loopKind, Lazy<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isTopTest, isWhile, loopKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyCondition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }
        protected override IOperation ConditionImpl => _lazyCondition.Value;
        protected override IOperation BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    internal abstract partial class BaseWithStatement : Operation, IWithStatement
    {
        protected BaseWithStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.WithStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation BodyImpl { get; }
        protected abstract IOperation ValueImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
                yield return Body;
            }
        }
        /// <summary>
        /// Body of the with.
        /// </summary>
        public IOperation Body => Operation.SetParentOperation(BodyImpl, this);
        /// <summary>
        /// Value to whose members leading-dot-qualified references within the with body bind.
        /// </summary>
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitWithStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitWithStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    internal sealed partial class WithStatement : BaseWithStatement, IWithStatement
    {
        public WithStatement(IOperation body, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            BodyImpl = body;
            ValueImpl = value;
        }

        protected override IOperation BodyImpl { get; }
        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    internal sealed partial class LazyWithStatement : BaseWithStatement, IWithStatement
    {
        private readonly Lazy<IOperation> _lazyBody;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyWithStatement(Lazy<IOperation> body, Lazy<IOperation> value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }

        protected override IOperation BodyImpl => _lazyBody.Value;

        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a local function statement.
    /// </summary>
    internal abstract partial class BaseLocalFunctionStatement : Operation, ILocalFunctionStatement
    {
        protected BaseLocalFunctionStatement(IMethodSymbol localFunctionSymbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.LocalFunctionStatement, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LocalFunctionSymbol = localFunctionSymbol;
        }
        /// <summary>
        /// Local function symbol.
        /// </summary>
        public IMethodSymbol LocalFunctionSymbol { get; }
        protected abstract IBlockStatement BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Body;
            }
        }
        /// <summary>
        /// Body of the local function.
        /// </summary>
        public IBlockStatement Body => Operation.SetParentOperation(BodyImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLocalFunctionStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLocalFunctionStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a local function statement.
    /// </summary>
    internal sealed partial class LocalFunctionStatement : BaseLocalFunctionStatement, ILocalFunctionStatement
    {
        public LocalFunctionStatement(IMethodSymbol localFunctionSymbol, IBlockStatement body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(localFunctionSymbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            BodyImpl = body;
        }

        protected override IBlockStatement BodyImpl { get; }
    }

    /// <summary>
    /// Represents a local function statement.
    /// </summary>
    internal sealed partial class LazyLocalFunctionStatement : BaseLocalFunctionStatement, ILocalFunctionStatement
    {
        private readonly Lazy<IBlockStatement> _lazyBody;

        public LazyLocalFunctionStatement(IMethodSymbol localFunctionSymbol, Lazy<IBlockStatement> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue,bool isImplicit)
            : base(localFunctionSymbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override IBlockStatement BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# constant pattern.
    /// </summary>
    internal abstract partial class BaseConstantPattern : Operation, IConstantPattern
    {
        protected BaseConstantPattern(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ConstantPattern, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ValueImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
            }
        }
        /// <summary>
        /// Constant value of the pattern.
        /// </summary>
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConstantPattern(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConstantPattern(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# constant pattern.
    /// </summary>
    internal sealed partial class ConstantPattern : BaseConstantPattern, IConstantPattern
    {
        public ConstantPattern(IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
        }

        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents a C# constant pattern.
    /// </summary>
    internal sealed partial class LazyConstantPattern : BaseConstantPattern, IConstantPattern
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyConstantPattern(Lazy<IOperation> value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }

        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a C# declaration pattern.
    /// </summary>
    internal sealed partial class DeclarationPattern : Operation, IDeclarationPattern
    {
        public DeclarationPattern(ISymbol declaredSymbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.DeclarationPattern, semanticModel, syntax, type, constantValue, isImplicit)
        {
            DeclaredSymbol = declaredSymbol;
        }
        /// <summary>
        /// Symbol declared by the pattern.
        /// </summary>
        public ISymbol DeclaredSymbol { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDeclarationPattern(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDeclarationPattern(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# pattern case clause.
    /// </summary>
    internal abstract partial class BasePatternCaseClause : CaseClause, IPatternCaseClause
    {
        protected BasePatternCaseClause(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(CaseKind.Pattern, OperationKind.PatternCaseClause, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Label = label;
        }
        /// <summary>
        /// Label associated with the case clause.
        /// </summary>
        public ILabelSymbol Label { get; }
        protected abstract IPattern PatternImpl { get; }
        protected abstract IOperation GuardExpressionImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Pattern;
                yield return GuardExpression;
            }
        }
        /// <summary>
        /// Pattern associated with case clause.
        /// </summary>
        public IPattern Pattern => Operation.SetParentOperation(PatternImpl, this);
        /// <summary>
        /// Guard expression associated with the pattern case clause.
        /// </summary>
        public IOperation GuardExpression => Operation.SetParentOperation(GuardExpressionImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPatternCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPatternCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# pattern case clause.
    /// </summary>
    internal sealed partial class PatternCaseClause : BasePatternCaseClause, IPatternCaseClause
    {
        public PatternCaseClause(ILabelSymbol label, IPattern pattern, IOperation guardExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            PatternImpl = pattern;
            GuardExpressionImpl = guardExpression;
        }

        protected override IPattern PatternImpl { get; }
        protected override IOperation GuardExpressionImpl { get; }
    }

    /// <summary>
    /// Represents a C# pattern case clause.
    /// </summary>
    internal sealed partial class LazyPatternCaseClause : BasePatternCaseClause, IPatternCaseClause
    {
        private readonly Lazy<IPattern> _lazyPattern;
        private readonly Lazy<IOperation> _lazyGuardExpression;

        public LazyPatternCaseClause(ILabelSymbol label, Lazy<IPattern> lazyPattern, Lazy<IOperation> lazyGuardExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyPattern = lazyPattern ?? throw new System.ArgumentNullException(nameof(lazyPattern));
            _lazyGuardExpression = lazyGuardExpression ?? throw new System.ArgumentNullException(nameof(lazyGuardExpression));
        }

        protected override IPattern PatternImpl => _lazyPattern.Value;

        protected override IOperation GuardExpressionImpl => _lazyGuardExpression.Value;
    }

    /// <summary>
    /// Represents a C# is pattern expression. For example, "x is int i".
    /// </summary>
    internal abstract partial class BaseIsPatternExpression : Operation, IIsPatternExpression
    {
        protected BaseIsPatternExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.IsPatternExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        protected abstract IPattern PatternImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Expression;
                yield return Pattern;
            }
        }
        /// <summary>
        /// Expression.
        /// </summary>
        public IOperation Expression => Operation.SetParentOperation(ExpressionImpl, this);
        /// <summary>
        /// Pattern.
        /// </summary>
        public IPattern Pattern => Operation.SetParentOperation(PatternImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIsPatternExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIsPatternExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# is pattern expression. For example, "x is int i".
    /// </summary>
    internal sealed partial class IsPatternExpression : BaseIsPatternExpression, IIsPatternExpression
    {
        public IsPatternExpression(IOperation expression, IPattern pattern, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
            PatternImpl = pattern;
        }

        protected override IOperation ExpressionImpl { get; }
        protected override IPattern PatternImpl { get; }
    }

    /// <summary>
    /// Represents a C# is pattern expression. For example, "x is int i".
    /// </summary>
    internal sealed partial class LazyIsPatternExpression : BaseIsPatternExpression, IIsPatternExpression
    {
        private readonly Lazy<IOperation> _lazyExpression;
        private readonly Lazy<IPattern> _lazyPattern;

        public LazyIsPatternExpression(Lazy<IOperation> lazyExpression, Lazy<IPattern> lazyPattern, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = lazyExpression ?? throw new System.ArgumentNullException(nameof(lazyExpression));
            _lazyPattern = lazyPattern ?? throw new System.ArgumentNullException(nameof(lazyPattern));
        }

        protected override IOperation ExpressionImpl => _lazyExpression.Value;

        protected override IPattern PatternImpl => _lazyPattern.Value;
    }

    /// <summary>
    /// Represents a C# or VB object or collection initializer expression.
    /// </summary>
    internal abstract partial class BaseObjectOrCollectionInitializerExpression : Operation, IObjectOrCollectionInitializerExpression
    {
        protected BaseObjectOrCollectionInitializerExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ObjectOrCollectionInitializerExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> InitializersImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var initializer in Initializers)
                {
                    yield return initializer;
                }
            }
        }
        /// <summary>
        /// Object member or collection initializers.
        /// </summary>
        public ImmutableArray<IOperation> Initializers => Operation.SetParentOperation(InitializersImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitObjectOrCollectionInitializerExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitObjectOrCollectionInitializerExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB object or collection initializer expression.
    /// </summary>
    internal sealed partial class ObjectOrCollectionInitializerExpression : BaseObjectOrCollectionInitializerExpression, IObjectOrCollectionInitializerExpression
    {
        public ObjectOrCollectionInitializerExpression(ImmutableArray<IOperation> initializers, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializersImpl = initializers;
        }

        protected override ImmutableArray<IOperation> InitializersImpl { get; }
    }

    /// <summary>
    /// Represents a C# or VB object or collection initializer expression.
    /// </summary>
    internal sealed partial class LazyObjectOrCollectionInitializerExpression : BaseObjectOrCollectionInitializerExpression, IObjectOrCollectionInitializerExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyInitializers;

        public LazyObjectOrCollectionInitializerExpression(Lazy<ImmutableArray<IOperation>> initializers, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInitializers = initializers ?? throw new System.ArgumentNullException(nameof(initializers));
        }

        protected override ImmutableArray<IOperation> InitializersImpl => _lazyInitializers.Value;
    }

    /// <summary>
    /// Represents a C# or VB member initializer expression within an object initializer expression.
    /// </summary>
    internal abstract partial class BaseMemberInitializerExpression : Operation, IMemberInitializerExpression
    {
        protected BaseMemberInitializerExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.MemberInitializerExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IMemberReferenceExpression InitializedMemberImpl { get; }
        protected abstract IObjectOrCollectionInitializerExpression InitializerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return InitializedMember;
                yield return Initializer;
            }
        }
        /// <summary>
        /// Initialized member.
        /// </summary>
        public IMemberReferenceExpression InitializedMember => Operation.SetParentOperation(InitializedMemberImpl, this);

        /// <summary>
        /// Member initializer.
        /// </summary>
        public IObjectOrCollectionInitializerExpression Initializer => Operation.SetParentOperation(InitializerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitMemberInitializerExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitMemberInitializerExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB member initializer expression within an object initializer expression.
    /// </summary>
    internal sealed partial class MemberInitializerExpression : BaseMemberInitializerExpression, IMemberInitializerExpression
    {
        public MemberInitializerExpression(IMemberReferenceExpression initializedMember, IObjectOrCollectionInitializerExpression initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializedMemberImpl = initializedMember;
            InitializerImpl = initializer;
        }

        protected override IMemberReferenceExpression InitializedMemberImpl { get; }
        protected override IObjectOrCollectionInitializerExpression InitializerImpl { get; }
    }

    /// <summary>
    /// Represents a C# or VB member initializer expression within an object initializer expression.
    /// </summary>
    internal sealed partial class LazyMemberInitializerExpression : BaseMemberInitializerExpression, IMemberInitializerExpression
    {
        private readonly Lazy<IMemberReferenceExpression> _lazyInitializedMember;
        private readonly Lazy<IObjectOrCollectionInitializerExpression> _lazyInitializer;

        public LazyMemberInitializerExpression(Lazy<IMemberReferenceExpression> initializedMember, Lazy<IObjectOrCollectionInitializerExpression> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInitializedMember = initializedMember ?? throw new System.ArgumentNullException(nameof(initializedMember));
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }

        protected override IMemberReferenceExpression InitializedMemberImpl => _lazyInitializedMember.Value;

        protected override IObjectOrCollectionInitializerExpression InitializerImpl => _lazyInitializer.Value;
    }

    /// <summary>
    /// Represents a C# nested collection element initializer expression within a collection initializer.
    /// </summary>
    internal abstract partial class BaseCollectionElementInitializerExpression : Operation, ICollectionElementInitializerExpression
    {
        protected BaseCollectionElementInitializerExpression(IMethodSymbol addMethod, bool isDynamic, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.CollectionElementInitializerExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            AddMethod = addMethod;
            IsDynamic = isDynamic;
        }
        /// <summary>
        /// Add method invoked on collection. Might be null for dynamic invocation.
        /// </summary>
        public IMethodSymbol AddMethod { get; }
        protected abstract ImmutableArray<IOperation> ArgumentsImpl { get; }
        /// <summary>
        /// Flag indicating if this is a dynamic invocation.
        /// </summary>
        public bool IsDynamic { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var argument in Arguments)
                {
                    yield return argument;
                }
            }
        }
        /// <summary>
        /// Arguments passed to add method invocation.
        /// </summary>
        public ImmutableArray<IOperation> Arguments => Operation.SetParentOperation(ArgumentsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCollectionElementInitializerExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCollectionElementInitializerExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# nested collection element initializer expression within a collection initializer.
    /// </summary>
    internal sealed partial class CollectionElementInitializerExpression : BaseCollectionElementInitializerExpression, ICollectionElementInitializerExpression
    {
        public CollectionElementInitializerExpression(IMethodSymbol addMethod, ImmutableArray<IOperation> arguments, bool isDynamic, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(addMethod, isDynamic, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArgumentsImpl = arguments;
        }

        protected override ImmutableArray<IOperation> ArgumentsImpl { get; }
    }

    /// <summary>
    /// Represents a C# nested collection element initializer expression within a collection initializer.
    /// </summary>
    internal sealed partial class LazyCollectionElementInitializerExpression : BaseCollectionElementInitializerExpression, ICollectionElementInitializerExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyArguments;

        public LazyCollectionElementInitializerExpression(IMethodSymbol addMethod, Lazy<ImmutableArray<IOperation>> arguments, bool isDynamic, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(addMethod, isDynamic, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyArguments = arguments ?? throw new System.ArgumentNullException(nameof(arguments));
        }

        protected override ImmutableArray<IOperation> ArgumentsImpl => _lazyArguments.Value;
    }
}
