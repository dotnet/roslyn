// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal abstract partial class BaseAddressOfExpression : Operation, IAddressOfOperation
    {
        protected BaseAddressOfExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.AddressOf, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ReferenceImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Reference != null)
                {
                    yield return Reference;
                }
            }
        }
        /// <summary>
        /// Addressed reference.
        /// </summary>
        public IOperation Reference => Operation.SetParentOperation(ReferenceImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAddressOf(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAddressOf(this, argument);
        }
    }
    /// <summary>
    /// Represents an operation that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal sealed partial class AddressOfExpression : BaseAddressOfExpression, IAddressOfOperation
    {
        public AddressOfExpression(IOperation reference, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ReferenceImpl = reference;
        }

        protected override IOperation ReferenceImpl { get; }
    }
    /// <summary>
    /// Represents an operation that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal sealed partial class LazyAddressOfExpression : BaseAddressOfExpression, IAddressOfOperation
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
    internal abstract partial class BaseNameOfExpression : Operation, INameOfOperation
    {
        protected BaseNameOfExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.NameOf, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ArgumentImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Argument != null)
                {
                    yield return Argument;
                }
            }
        }
        /// <summary>
        /// Argument to name of expression.
        /// </summary>
        public IOperation Argument => Operation.SetParentOperation(ArgumentImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNameOf(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNameOf(this, argument);
        }
    }
    /// <summary>
    /// Represents C# nameof and VB NameOf expression.
    /// </summary>
    internal sealed partial class NameOfExpression : BaseNameOfExpression, INameOfOperation
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
    internal sealed partial class LazyNameOfExpression : BaseNameOfExpression, INameOfOperation
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
    internal abstract partial class BaseThrowExpression : Operation, IThrowOperation
    {
        protected BaseThrowExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.Throw, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Exception != null)
                {
                    yield return Exception;
                }
            }
        }
        /// <summary>
        /// Expression.
        /// </summary>
        public IOperation Exception => Operation.SetParentOperation(ExpressionImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitThrow(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitThrow(this, argument);
        }
    }
    /// <summary>
    /// Represents C# throw expression.
    /// </summary>
    internal sealed partial class ThrowExpression : BaseThrowExpression, IThrowOperation
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
    internal sealed partial class LazyThrowExpression : BaseThrowExpression, IThrowOperation
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
    internal abstract partial class BaseArgument : Operation, IArgumentOperation
    {
        protected BaseArgument(ArgumentKind argumentKind, IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Argument, semanticModel, syntax, type: null, constantValue: constantValue, isImplicit: isImplicit)
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
        public abstract CommonConversion InConversion { get; }
        public abstract CommonConversion OutConversion { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value != null)
                {
                    yield return Value;
                }
            }
        }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);
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
    /// Represents the creation of an array instance.
    /// </summary>
    internal abstract partial class BaseArrayCreationExpression : Operation, IArrayCreationOperation
    {
        protected BaseArrayCreationExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ArrayCreation, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract ImmutableArray<IOperation> DimensionSizesImpl { get; }
        protected abstract IArrayInitializerOperation InitializerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var dimensionSize in DimensionSizes)
                {
                    if (dimensionSize != null)
                    {
                        yield return dimensionSize;
                    }
                }
                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        public ImmutableArray<IOperation> DimensionSizes => Operation.SetParentOperation(DimensionSizesImpl, this);
        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        public IArrayInitializerOperation Initializer => Operation.SetParentOperation(InitializerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayCreation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayCreation(this, argument);
        }
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    internal sealed partial class ArrayCreationExpression : BaseArrayCreationExpression, IArrayCreationOperation
    {
        public ArrayCreationExpression(ImmutableArray<IOperation> dimensionSizes, IArrayInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            DimensionSizesImpl = dimensionSizes;
            InitializerImpl = initializer;
        }

        protected override ImmutableArray<IOperation> DimensionSizesImpl { get; }
        protected override IArrayInitializerOperation InitializerImpl { get; }
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    internal sealed partial class LazyArrayCreationExpression : BaseArrayCreationExpression, IArrayCreationOperation
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyDimensionSizes;
        private readonly Lazy<IArrayInitializerOperation> _lazyInitializer;

        public LazyArrayCreationExpression(Lazy<ImmutableArray<IOperation>> dimensionSizes, Lazy<IArrayInitializerOperation> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyDimensionSizes = dimensionSizes;
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }

        protected override ImmutableArray<IOperation> DimensionSizesImpl => _lazyDimensionSizes.Value;

        protected override IArrayInitializerOperation InitializerImpl => _lazyInitializer.Value;
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    internal abstract partial class BaseArrayElementReferenceExpression : Operation, IArrayElementReferenceOperation
    {
        protected BaseArrayElementReferenceExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.ArrayElementReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ArrayReferenceImpl { get; }
        protected abstract ImmutableArray<IOperation> IndicesImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (ArrayReference != null)
                {
                    yield return ArrayReference;
                }

                foreach (var index in Indices)
                {
                    if (index != null)
                    {
                        yield return index;
                    }
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
            visitor.VisitArrayElementReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayElementReference(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    internal sealed partial class ArrayElementReferenceExpression : BaseArrayElementReferenceExpression, IArrayElementReferenceOperation
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
    internal sealed partial class LazyArrayElementReferenceExpression : BaseArrayElementReferenceExpression, IArrayElementReferenceOperation
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
    internal abstract partial class BaseArrayInitializer : Operation, IArrayInitializerOperation
    {
        protected BaseArrayInitializer(SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ArrayInitializer, semanticModel, syntax, type: null, constantValue: constantValue, isImplicit: isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> ElementValuesImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var elementValue in ElementValues)
                {
                    if (elementValue != null)
                    {
                        yield return elementValue;
                    }
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
    internal sealed partial class ArrayInitializer : BaseArrayInitializer, IArrayInitializerOperation
    {
        public ArrayInitializer(ImmutableArray<IOperation> elementValues, SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, constantValue, isImplicit)
        {
            ElementValuesImpl = elementValues;
        }

        protected override ImmutableArray<IOperation> ElementValuesImpl { get; }
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    internal sealed partial class LazyArrayInitializer : BaseArrayInitializer, IArrayInitializerOperation
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyElementValues;

        public LazyArrayInitializer(Lazy<ImmutableArray<IOperation>> elementValues, SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, constantValue, isImplicit)
        {
            _lazyElementValues = elementValues;
        }

        protected override ImmutableArray<IOperation> ElementValuesImpl => _lazyElementValues.Value;
    }

    /// <summary>
    /// Represents an base type of assignment expression.
    /// </summary>
    internal abstract partial class AssignmentExpression : Operation, IAssignmentOperation
    {
        protected AssignmentExpression(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public sealed override IEnumerable<IOperation> Children
        {
            get
            {
                if (Target != null)
                {
                    yield return Target;
                }
                if (Value != null)
                {
                    yield return Value;
                }
            }
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
    internal abstract partial class BaseSimpleAssignmentExpression : AssignmentExpression, ISimpleAssignmentOperation
    {
        public BaseSimpleAssignmentExpression(bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.SimpleAssignment, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsRef = isRef;
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSimpleAssignment(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSimpleAssignment(this, argument);
        }

        /// <summary>
        /// Is this a ref assignment
        /// </summary>
        public bool IsRef { get; }
    }

    /// <summary>
    /// Represents a simple assignment expression.
    /// </summary>
    internal sealed partial class SimpleAssignmentExpression : BaseSimpleAssignmentExpression, ISimpleAssignmentOperation
    {
        public SimpleAssignmentExpression(IOperation target, bool isRef, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
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
    internal sealed partial class LazySimpleAssignmentExpression : BaseSimpleAssignmentExpression, ISimpleAssignmentOperation
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;

        public LazySimpleAssignmentExpression(Lazy<IOperation> target, bool isRef, Lazy<IOperation> value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException(nameof(target));
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        protected override IOperation TargetImpl => _lazyTarget.Value;
        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a deconstruction assignment expression.
    /// </summary>
    internal abstract partial class BaseDeconstructionAssignmentExpression : AssignmentExpression, IDeconstructionAssignmentOperation
    {
        public BaseDeconstructionAssignmentExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DeconstructionAssignment, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDeconstructionAssignment(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDeconstructionAssignment(this, argument);
        }
    }

    /// <summary>
    /// Represents a deconstruction assignment expression.
    /// </summary>
    internal sealed partial class DeconstructionAssignmentExpression : BaseDeconstructionAssignmentExpression, IDeconstructionAssignmentOperation
    {
        public DeconstructionAssignmentExpression(IOperation target, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            TargetImpl = target;
            ValueImpl = value;
        }
        protected override IOperation TargetImpl { get; }
        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents a deconstruction assignment expression.
    /// </summary>
    internal sealed partial class LazyDeconstructionAssignmentExpression : BaseDeconstructionAssignmentExpression, IDeconstructionAssignmentOperation
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyDeconstructionAssignmentExpression(Lazy<IOperation> target, Lazy<IOperation> value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException(nameof(target));
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        protected override IOperation TargetImpl => _lazyTarget.Value;
        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a declaration expression in C#.
    /// Unlike a regular variable declaration, this operation represents an "expression" declaring a variable.
    /// For example,
    ///   1. "var (x, y)" is a deconstruction declaration expression with variables x and y.
    ///   2. "(var x, var y)" is a tuple expression with two declaration expressions.
    ///   3. "M(out var x);" is an invocation expression with an out "var x" declaration expression.
    /// </summary>
    internal abstract partial class BaseDeclarationExpression : Operation, IDeclarationExpressionOperation
    {
        public BaseDeclarationExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DeclarationExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Expression != null)
                {
                    yield return Expression;
                }
            }
        }
        /// <summary>
        /// Underlying expression.
        /// </summary>
        public IOperation Expression => Operation.SetParentOperation(ExpressionImpl, this);
        protected abstract IOperation ExpressionImpl { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDeclarationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDeclarationExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a declaration expression in C#.
    /// Unlike a regular variable declaration, this operation represents an "expression" declaring a variable.
    /// For example,
    ///   1. "var (x, y)" is a deconstruction declaration expression with variables x and y.
    ///   2. "(var x, var y)" is a tuple expression with two declaration expressions.
    ///   3. "M(out var x);" is an invocation expression with an out "var x" declaration expression.
    /// </summary>
    internal sealed partial class DeclarationExpression : BaseDeclarationExpression, IDeclarationExpressionOperation
    {
        public DeclarationExpression(IOperation expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
        }
        protected override IOperation ExpressionImpl { get; }
    }

    /// <summary>
    /// Represents a declaration expression in C#.
    /// Unlike a regular variable declaration, this operation represents an "expression" declaring a variable.
    /// For example,
    ///   1. "var (x, y)" is a deconstruction declaration expression with variables x and y.
    ///   2. "(var x, var y)" is a tuple expression with two declaration expressions.
    ///   3. "M(out var x);" is an invocation expression with an out "var x" declaration expression.
    /// </summary>
    internal sealed partial class LazyDeclarationExpression : BaseDeclarationExpression, IDeclarationExpressionOperation
    {
        private readonly Lazy<IOperation> _lazyExpression;

        public LazyDeclarationExpression(Lazy<IOperation> expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
        }
        protected override IOperation ExpressionImpl => _lazyExpression.Value;
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal abstract partial class BaseAwaitExpression : Operation, IAwaitOperation
    {
        protected BaseAwaitExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.Await, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation != null)
                {
                    yield return Operation;
                }
            }
        }
        /// <summary>
        /// Awaited expression.
        /// </summary>
        public IOperation Operation => CodeAnalysis.Operation.SetParentOperation(ExpressionImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAwait(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAwait(this, argument);
        }
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal sealed partial class AwaitExpression : BaseAwaitExpression, IAwaitOperation
    {
        public AwaitExpression(IOperation expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
        }

        protected override IOperation ExpressionImpl { get; }
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal sealed partial class LazyAwaitExpression : BaseAwaitExpression, IAwaitOperation
    {
        private readonly Lazy<IOperation> _lazyExpression;

        public LazyAwaitExpression(Lazy<IOperation> expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
        }

        protected override IOperation ExpressionImpl => _lazyExpression.Value;
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    internal abstract partial class BaseBinaryOperatorExpression : Operation, IBinaryOperation
    {
        protected BaseBinaryOperatorExpression(BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.BinaryOperator, semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperatorKind = operatorKind;
            IsLifted = isLifted;
            IsChecked = isChecked;
            IsCompareText = isCompareText;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        public BinaryOperatorKind OperatorKind { get; }
        protected abstract IOperation LeftOperandImpl { get; }
        protected abstract IOperation RightOperandImpl { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
        /// <summary>
        /// <code>true</code> if this is a 'lifted' binary operator.  When there is an
        /// operator that is defined to work on a value type, 'lifted' operators are
        /// created to work on the <see cref="System.Nullable{T}"/> versions of those
        /// value types.
        /// </summary>
        public bool IsLifted { get; }
        /// <summary>
        /// <code>true</code> if overflow checking is performed for the arithmetic operation.
        /// </summary>
        public bool IsChecked { get; }
        /// <summary>
        /// <code>true</code> if the comparison is text based for string or object comparison in VB.
        /// </summary>
        public bool IsCompareText { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (LeftOperand != null)
                {
                    yield return LeftOperand;
                }
                if (RightOperand != null)
                {
                    yield return RightOperand;
                }
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
            visitor.VisitBinaryOperator(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBinaryOperator(this, argument);
        }
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    internal sealed partial class BinaryOperatorExpression : BaseBinaryOperatorExpression, IBinaryOperation
    {
        public BinaryOperatorExpression(BinaryOperatorKind operatorKind, IOperation leftOperand, IOperation rightOperand, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(operatorKind, isLifted, isChecked, isCompareText, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
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
    internal sealed partial class LazyBinaryOperatorExpression : BaseBinaryOperatorExpression, IBinaryOperation
    {
        private readonly Lazy<IOperation> _lazyLeftOperand;
        private readonly Lazy<IOperation> _lazyRightOperand;

        public LazyBinaryOperatorExpression(BinaryOperatorKind operatorKind, Lazy<IOperation> leftOperand, Lazy<IOperation> rightOperand, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(operatorKind, isLifted, isChecked, isCompareText, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
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
    internal abstract partial class BaseBlockStatement : Operation, IBlockOperation
    {
        protected BaseBlockStatement(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Block, semanticModel, syntax, type, constantValue, isImplicit)
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
                foreach (var statement in Operations)
                {
                    if (statement != null)
                    {
                        yield return statement;
                    }
                }
            }
        }
        /// <summary>
        /// Statements contained within the block.
        /// </summary>
        public ImmutableArray<IOperation> Operations => Operation.SetParentOperation(StatementsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBlock(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBlock(this, argument);
        }
    }

    /// <summary>
    /// Represents a block scope.
    /// </summary>
    internal sealed partial class BlockStatement : BaseBlockStatement, IBlockOperation
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
    internal sealed partial class LazyBlockStatement : BaseBlockStatement, IBlockOperation
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
    internal sealed partial class BranchStatement : Operation, IBranchOperation
    {
        public BranchStatement(ILabelSymbol target, BranchKind branchKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.Branch, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitBranch(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBranch(this, argument);
        }
    }

    /// <summary>
    /// Represents a clause of a C# case or a VB Case.
    /// </summary>
    internal abstract partial class CaseClause : Operation, ICaseClauseOperation
    {
        protected CaseClause(CaseKind caseKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.CaseClause, semanticModel, syntax, type, constantValue, isImplicit)
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
    internal abstract partial class BaseCatchClause : Operation, ICatchClauseOperation
    {
        protected BaseCatchClause(ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.CatchClause, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExceptionType = exceptionType;
            Locals = locals;
        }
        /// <summary>
        /// Type of the exception handled by the catch clause.
        /// </summary>
        public ITypeSymbol ExceptionType { get; }
        /// <summary>
        /// Locals declared by the <see cref="ExceptionDeclarationOrExpression"/> and/or <see cref="Filter"/> clause.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Locals { get; }
        protected abstract IOperation ExceptionDeclarationOrExpressionImpl { get; }
        protected abstract IOperation FilterImpl { get; }
        protected abstract IBlockOperation HandlerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (ExceptionDeclarationOrExpression != null)
                {
                    yield return ExceptionDeclarationOrExpression;
                }
                if (Filter != null)
                {
                    yield return Filter;
                }
                if (Handler != null)
                {
                    yield return Handler;
                }
            }
        }
        /// <summary>
        /// Optional source for exception. This could be any of the following operation:
        /// 1. Declaration for the local catch variable bound to the caught exception (C# and VB) OR
        /// 2. Type expression for the caught expression type (C#) OR
        /// 3. Null, indicating no expression (C#)
        /// 4. Reference to an existing local or parameter (VB) OR
        /// 5. An error expression (VB)
        /// </summary>
        public IOperation ExceptionDeclarationOrExpression => Operation.SetParentOperation(ExceptionDeclarationOrExpressionImpl, this);
        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        public IOperation Filter => Operation.SetParentOperation(FilterImpl, this);
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        public IBlockOperation Handler => Operation.SetParentOperation(HandlerImpl, this);
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
    internal sealed partial class CatchClause : BaseCatchClause, ICatchClauseOperation
    {
        public CatchClause(IOperation exceptionDeclarationOrExpression, ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, IOperation filter, IBlockOperation handler, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(exceptionType, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExceptionDeclarationOrExpressionImpl = exceptionDeclarationOrExpression;
            FilterImpl = filter;
            HandlerImpl = handler;
        }

        protected override IBlockOperation HandlerImpl { get; }
        protected override IOperation FilterImpl { get; }
        protected override IOperation ExceptionDeclarationOrExpressionImpl { get; }
    }

    /// <summary>
    /// Represents a C# catch or VB Catch clause.
    /// </summary>
    internal sealed partial class LazyCatchClause : BaseCatchClause, ICatchClauseOperation
    {
        private readonly Lazy<IOperation> _lazyExceptionDeclarationOrExpression;
        private readonly Lazy<IOperation> _lazyFilter;
        private readonly Lazy<IBlockOperation> _lazyHandler;

        public LazyCatchClause(Lazy<IOperation> exceptionDeclarationOrExpression, ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, Lazy<IOperation> filter, Lazy<IBlockOperation> handler, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(exceptionType, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExceptionDeclarationOrExpression = exceptionDeclarationOrExpression ?? throw new System.ArgumentNullException(nameof(exceptionDeclarationOrExpression));
            _lazyFilter = filter ?? throw new System.ArgumentNullException(nameof(filter));
            _lazyHandler = handler ?? throw new System.ArgumentNullException(nameof(handler));
        }

        protected override IOperation ExceptionDeclarationOrExpressionImpl => _lazyExceptionDeclarationOrExpression.Value;
        protected override IOperation FilterImpl => _lazyFilter.Value;
        protected override IBlockOperation HandlerImpl => _lazyHandler.Value;
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    internal abstract partial class BaseCompoundAssignmentExpression : AssignmentExpression, ICompoundAssignmentOperation
    {
        protected BaseCompoundAssignmentExpression(BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.CompoundAssignment, semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperatorKind = operatorKind;
            IsLifted = isLifted;
            IsChecked = isChecked;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        public BinaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// <code>true</code> if this assignment contains a 'lifted' binary operation.
        /// </summary>
        public bool IsLifted { get; }
        /// <summary>
        /// <code>true</code> if overflow checking is performed for the arithmetic operation.
        /// </summary>
        public bool IsChecked { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCompoundAssignment(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCompoundAssignment(this, argument);
        }
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    internal sealed partial class CompoundAssignmentExpression : BaseCompoundAssignmentExpression, ICompoundAssignmentOperation
    {
        public CompoundAssignmentExpression(BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IOperation target, IOperation value, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
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
    internal sealed partial class LazyCompoundAssignmentExpression : BaseCompoundAssignmentExpression, ICompoundAssignmentOperation
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyCompoundAssignmentExpression(BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, Lazy<IOperation> target, Lazy<IOperation> value, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
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
    internal abstract partial class BaseConditionalAccessExpression : Operation, IConditionalAccessOperation
    {
        protected BaseConditionalAccessExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ConditionalAccess, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation WhenNotNullImpl { get; }
        protected abstract IOperation ExpressionImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation != null)
                {
                    yield return Operation;
                }
                if (WhenNotNull != null)
                {
                    yield return WhenNotNull;
                }
            }
        }
        /// <summary>
        /// Expresson that is conditionally accessed.
        /// </summary>
        public IOperation Operation => CodeAnalysis.Operation.SetParentOperation(ExpressionImpl, this);
        /// <summary>
        /// Expression to be evaluated if the conditional instance is non null.
        /// </summary>
        public IOperation WhenNotNull => CodeAnalysis.Operation.SetParentOperation(WhenNotNullImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalAccess(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalAccess(this, argument);
        }
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    internal sealed partial class ConditionalAccessExpression : BaseConditionalAccessExpression, IConditionalAccessOperation
    {
        public ConditionalAccessExpression(IOperation whenNotNull, IOperation expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            WhenNotNullImpl = whenNotNull;
            ExpressionImpl = expression;
        }

        protected override IOperation ExpressionImpl { get; }
        protected override IOperation WhenNotNullImpl { get; }
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    internal sealed partial class LazyConditionalAccessExpression : BaseConditionalAccessExpression, IConditionalAccessOperation
    {
        private readonly Lazy<IOperation> _lazyWhenNotNull;
        private readonly Lazy<IOperation> _lazyExpression;

        public LazyConditionalAccessExpression(Lazy<IOperation> whenNotNull, Lazy<IOperation> expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyWhenNotNull = whenNotNull ?? throw new System.ArgumentNullException(nameof(whenNotNull));
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
        }

        protected override IOperation ExpressionImpl => _lazyExpression.Value;
        protected override IOperation WhenNotNullImpl => _lazyWhenNotNull.Value;
    }

    /// <summary>
    /// Represents the value of a conditionally-accessed expression within an expression containing a conditional access.
    /// </summary>
    internal sealed partial class ConditionalAccessInstanceExpression : Operation, IConditionalAccessInstanceOperation
    {
        public ConditionalAccessInstanceExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.ConditionalAccessInstance, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitConditionalAccessInstance(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalAccessInstance(this, argument);
        }
    }

    /// <summary>
    /// Represents a conditional operation with:
    ///  1. <see cref="IConditionalOperation.Condition"/> to be tested,
    ///  2. <see cref="IConditionalOperation.WhenTrue"/> operation to be executed when <see cref="IConditionalOperation.Condition"/> is true and
    ///  3. <see cref="IConditionalOperation.WhenFalse"/> operation to be executed when the <see cref="IConditionalOperation.Condition"/> is false.
    /// For example, a C# ternary expression "a ? b : c" or a VB "If(a, b, c)" expression is represented as a conditional operation whose resulting value is the result of the expression.
    /// Additionally, a C# "if else" statement or VB "If Then Else" is also is represented as a conditional operation, but with a dropped or no result value.
    /// </summary>
    internal abstract partial class BaseConditionalOperation : Operation, IConditionalOperation
    {
        protected BaseConditionalOperation(bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Conditional, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsRef = isRef;
        }
        protected abstract IOperation ConditionImpl { get; }
        protected abstract IOperation WhenTrueImpl { get; }
        protected abstract IOperation WhenFalseImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Condition != null)
                {
                    yield return Condition;
                }
                if (WhenTrue != null)
                {
                    yield return WhenTrue;
                }
                if (WhenFalse != null)
                {
                    yield return WhenFalse;
                }
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
            visitor.VisitConditional(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditional(this, argument);
        }

        /// <summary>
        /// Is result a managed reference
        /// </summary>
        public bool IsRef { get; }
    }

    /// <summary>
    /// Represents a conditional operation with:
    ///  1. <see cref="IConditionalOperation.Condition"/> to be tested,
    ///  2. <see cref="IConditionalOperation.WhenTrue"/> operation to be executed when <see cref="IConditionalOperation.Condition"/> is true and
    ///  3. <see cref="IConditionalOperation.WhenFalse"/> operation to be executed when the <see cref="IConditionalOperation.Condition"/> is false.
    /// For example, a C# ternary expression "a ? b : c" or a VB "If(a, b, c)" expression is represented as a conditional operation whose resulting value is the result of the expression.
    /// Additionally, a C# "if else" statement or VB "If Then Else" is also is represented as a conditional operation, but with a dropped or no result value.
    /// </summary>
    internal sealed partial class ConditionalOperation : BaseConditionalOperation, IConditionalOperation
    {
        public ConditionalOperation(
            IOperation condition, IOperation whenTrue, IOperation whenFalse, bool isRef, 
            SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
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
    /// Represents a conditional operation with:
    ///  1. <see cref="IConditionalOperation.Condition"/> to be tested,
    ///  2. <see cref="IConditionalOperation.WhenTrue"/> operation to be executed when <see cref="IConditionalOperation.Condition"/> is true and
    ///  3. <see cref="IConditionalOperation.WhenFalse"/> operation to be executed when the <see cref="IConditionalOperation.Condition"/> is false.
    /// For example, a C# ternary expression "a ? b : c" or a VB "If(a, b, c)" expression is represented as a conditional operation whose resulting value is the result of the expression.
    /// Additionally, a C# "if else" statement or VB "If Then Else" is also is represented as a conditional operation, but with a dropped or no result value.
    /// </summary>
    internal sealed partial class LazyConditionalOperation : BaseConditionalOperation, IConditionalOperation
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyWhenTrue;
        private readonly Lazy<IOperation> _lazyWhenFalse;

        public LazyConditionalOperation(
            Lazy<IOperation> condition, Lazy<IOperation> whenTrue, Lazy<IOperation> whenFalse, bool isRef, 
            SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
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
    internal abstract partial class BaseConversionExpression : Operation, IConversionOperation
    {
        protected BaseConversionExpression(bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Conversion, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsTryCast = isTryCast;
            IsChecked = isChecked;
        }

        public abstract IOperation OperandImpl { get; }
        public abstract CommonConversion Conversion { get; }
        public bool IsTryCast { get; }
        public bool IsChecked { get; }
        public IMethodSymbol OperatorMethod => Conversion.MethodSymbol;
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operand != null)
                {
                    yield return Operand;
                }
            }
        }
        /// <summary>
        /// Value to be converted.
        /// </summary>
        public IOperation Operand => Operation.SetParentOperation(OperandImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConversion(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConversion(this, argument);
        }
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class DefaultValueExpression : Operation, IDefaultValueOperation
    {
        public DefaultValueExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DefaultValue, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitDefaultValue(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDefaultValue(this, argument);
        }
    }

    /// <summary>
    /// Reprsents an empty statement.
    /// </summary>
    internal sealed partial class EmptyStatement : Operation, IEmptyOperation
    {
        public EmptyStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.Empty, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitEmpty(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEmpty(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB End statement.
    /// </summary>
    internal sealed partial class EndStatement : Operation, IEndOperation
    {
        public EndStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.End, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitEnd(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEnd(this, argument);
        }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    internal abstract partial class BaseEventAssignmentOperation : Operation, IEventAssignmentOperation
    {
        protected BaseEventAssignmentOperation(bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.EventAssignment, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Adds = adds;
        }

        /// <summary>
        /// Reference to the event being bound.
        /// </summary>
        protected abstract IEventReferenceOperation EventReferenceImpl { get; }

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
                if (EventReference != null)
                {
                    yield return EventReference;
                }
                if (HandlerValue != null)
                {
                    yield return HandlerValue;
                }
            }
        }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        public IEventReferenceOperation EventReference => Operation.SetParentOperation(EventReferenceImpl, this);

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        public IOperation HandlerValue => Operation.SetParentOperation(HandlerValueImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEventAssignment(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEventAssignment(this, argument);
        }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    internal sealed partial class EventAssignmentOperation : BaseEventAssignmentOperation, IEventAssignmentOperation
    {
        public EventAssignmentOperation(IEventReferenceOperation eventReference, IOperation handlerValue, bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(adds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            EventReferenceImpl = eventReference;
            HandlerValueImpl = handlerValue;
        }

        protected override IEventReferenceOperation EventReferenceImpl { get; }
        protected override IOperation HandlerValueImpl { get; }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    internal sealed partial class LazyEventAssignmentOperation : BaseEventAssignmentOperation, IEventAssignmentOperation
    {
        private readonly Lazy<IEventReferenceOperation> _lazyEventReference;
        private readonly Lazy<IOperation> _lazyHandlerValue;

        public LazyEventAssignmentOperation(Lazy<IEventReferenceOperation> eventReference, Lazy<IOperation> handlerValue, bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(adds, semanticModel, syntax, type, constantValue, isImplicit)

        {
            _lazyEventReference = eventReference ?? throw new System.ArgumentNullException(nameof(eventReference));
            _lazyHandlerValue = handlerValue ?? throw new System.ArgumentNullException(nameof(handlerValue));
        }

        protected override IEventReferenceOperation EventReferenceImpl => _lazyEventReference.Value;

        protected override IOperation HandlerValueImpl => _lazyHandlerValue.Value;
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    internal abstract partial class BaseEventReferenceExpression : MemberReferenceExpression, IEventReferenceOperation
    {
        public BaseEventReferenceExpression(IEventSymbol @event, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(@event, OperationKind.EventReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        /// <summary>
        /// Referenced event.
        /// </summary>
        public IEventSymbol Event => (IEventSymbol)Member;

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance != null)
                {
                    yield return Instance;
                }
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEventReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEventReference(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    internal sealed partial class EventReferenceExpression : BaseEventReferenceExpression, IEventReferenceOperation
    {
        public EventReferenceExpression(IEventSymbol @event, IOperation instance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(@event, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceImpl = instance;
        }
        protected override IOperation InstanceImpl { get; }
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    internal sealed partial class LazyEventReferenceExpression : BaseEventReferenceExpression, IEventReferenceOperation
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyEventReferenceExpression(IEventSymbol @event, Lazy<IOperation> instance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(@event, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
        }
        protected override IOperation InstanceImpl => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// </summary>
    internal abstract partial class BaseExpressionStatement : Operation, IExpressionStatementOperation
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
                if (Operation != null)
                {
                    yield return Operation;
                }
            }
        }
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public IOperation Operation => CodeAnalysis.Operation.SetParentOperation(ExpressionImpl, this);
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
    internal sealed partial class ExpressionStatement : BaseExpressionStatement, IExpressionStatementOperation
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
    internal sealed partial class LazyExpressionStatement : BaseExpressionStatement, IExpressionStatementOperation
    {
        private readonly Lazy<IOperation> _lazyExpression;

        public LazyExpressionStatement(Lazy<IOperation> expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
        }

        protected override IOperation ExpressionImpl => _lazyExpression.Value;
    }

    /// <summary>
    /// Represents an initialization of a local variable.
    /// </summary>
    internal abstract partial class BaseVariableInitializer : SymbolInitializer, IVariableInitializerOperation
    {
        public BaseVariableInitializer(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.VariableInitializer, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value != null)
                {
                    yield return Value;
                }
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents an initialization of a local variable.
    /// </summary>
    internal sealed partial class VariableInitializer : BaseVariableInitializer, IVariableInitializerOperation
    {
        public VariableInitializer(IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
        }
        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents an initialization of a local variable.
    /// </summary>
    internal sealed partial class LazyVariableInitializer : BaseVariableInitializer, IVariableInitializerOperation
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyVariableInitializer(Lazy<IOperation> value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    internal abstract partial class BaseFieldInitializer : SymbolInitializer, IFieldInitializerOperation
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
                if (Value != null)
                {
                    yield return Value;
                }
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
    internal sealed partial class FieldInitializer : BaseFieldInitializer, IFieldInitializerOperation
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
    internal sealed partial class LazyFieldInitializer : BaseFieldInitializer, IFieldInitializerOperation
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
    internal abstract partial class BaseFieldReferenceExpression : MemberReferenceExpression, IFieldReferenceOperation
    {
        public BaseFieldReferenceExpression(IFieldSymbol field, bool isDeclaration, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(field, OperationKind.FieldReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsDeclaration = isDeclaration;
        }
        /// <summary>
        /// Referenced field.
        /// </summary>
        public IFieldSymbol Field => (IFieldSymbol)Member;
        public bool IsDeclaration { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance != null)
                {
                    yield return Instance;
                }
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFieldReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFieldReference(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    internal sealed partial class FieldReferenceExpression : BaseFieldReferenceExpression, IFieldReferenceOperation
    {
        public FieldReferenceExpression(IFieldSymbol field, bool isDeclaration, IOperation instance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(field, isDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceImpl = instance;
        }
        protected override IOperation InstanceImpl { get; }
    }

    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    internal sealed partial class LazyFieldReferenceExpression : BaseFieldReferenceExpression, IFieldReferenceOperation
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyFieldReferenceExpression(IFieldSymbol field, bool isDeclaration, Lazy<IOperation> instance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(field, isDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
        }
        protected override IOperation InstanceImpl => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a C# fixed statement.
    /// </summary>
    internal abstract partial class BaseFixedStatement : Operation, IFixedOperation
    {
        protected BaseFixedStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    // https://github.com/dotnet/roslyn/issues/21281
                    // base(OperationKind.Fixed, semanticModel, syntax, type, constantValue)
                    base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IVariableDeclarationGroupOperation VariablesImpl { get; }
        protected abstract IOperation BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Variables != null)
                {
                    yield return Variables;
                }
                if (Body != null)
                {
                    yield return Body;
                }
            }
        }
        /// <summary>
        /// Variables to be fixed.
        /// </summary>
        public IVariableDeclarationGroupOperation Variables => Operation.SetParentOperation(VariablesImpl, this);
        /// <summary>
        /// Body of the fixed, over which the variables are fixed.
        /// </summary>
        public IOperation Body => Operation.SetParentOperation(BodyImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFixed(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFixed(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# fixed statement.
    /// </summary>
    internal sealed partial class FixedStatement : BaseFixedStatement, IFixedOperation
    {
        public FixedStatement(IVariableDeclarationGroupOperation variables, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            VariablesImpl = variables;
            BodyImpl = body;
        }

        protected override IVariableDeclarationGroupOperation VariablesImpl { get; }
        protected override IOperation BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# fixed statement.
    /// </summary>
    internal sealed partial class LazyFixedStatement : BaseFixedStatement, IFixedOperation
    {
        private readonly Lazy<IVariableDeclarationGroupOperation> _lazyVariables;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyFixedStatement(Lazy<IVariableDeclarationGroupOperation> variables, Lazy<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyVariables = variables ?? throw new System.ArgumentNullException(nameof(variables));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override IVariableDeclarationGroupOperation VariablesImpl => _lazyVariables.Value;

        protected override IOperation BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# 'foreach' statement or a VB 'For Each' statement.
    /// </summary>
    internal abstract partial class BaseForEachLoopStatement : LoopStatement, IForEachLoopOperation
    {
        public BaseForEachLoopStatement(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(LoopKind.ForEach, locals, OperationKind.Loop, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IOperation LoopControlVariableImpl { get; }
        protected abstract IOperation CollectionImpl { get; }
        protected abstract ImmutableArray<IOperation> NextVariablesImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Collection != null)
                {
                    yield return Collection;
                }
                if (LoopControlVariable != null)
                {
                    yield return LoopControlVariable;
                }
                if (Body != null)
                {
                    yield return Body;
                }
                foreach (var expression in NextVariables)
                {
                    if (expression != null)
                    {
                        yield return expression;
                    }
                }
            }
        }
        /// <summary>
        /// Optional loop control variable in VB that refers to the operation for declaring a new local variable or reference an existing variable or an expression.
        /// This field is always null for C#.
        /// </summary>
        public IOperation LoopControlVariable => Operation.SetParentOperation(LoopControlVariableImpl, this);
        /// <summary>
        /// Collection value over which the loop iterates.
        /// </summary>
        public IOperation Collection => Operation.SetParentOperation(CollectionImpl, this);
        /// <summary>
        /// Optional list of comma separate operations to execute at loop bottom for VB.
        /// This list is always empty for C#.
        /// </summary>
        public ImmutableArray<IOperation> NextVariables => Operation.SetParentOperation(NextVariablesImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitForEachLoop(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitForEachLoop(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# 'foreach' statement or a VB 'For Each' statement.
    /// </summary>
    internal sealed partial class ForEachLoopStatement : BaseForEachLoopStatement, IForEachLoopOperation
    {
        public ForEachLoopStatement(ImmutableArray<ILocalSymbol> locals, IOperation loopControlVariable, IOperation collection, ImmutableArray<IOperation> nextVariables, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LoopControlVariableImpl = loopControlVariable;
            CollectionImpl = collection;
            NextVariablesImpl = nextVariables;
            BodyImpl = body;
        }

        protected override IOperation LoopControlVariableImpl { get; }
        protected override IOperation CollectionImpl { get; }
        protected override ImmutableArray<IOperation> NextVariablesImpl { get; }
        protected override IOperation BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# 'foreach' statement or a VB 'For Each' statement.
    /// </summary>
    internal sealed partial class LazyForEachLoopStatement : BaseForEachLoopStatement, IForEachLoopOperation
    {
        private readonly Lazy<IOperation> _lazyLoopControlVariable;
        private readonly Lazy<IOperation> _lazyCollection;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyNextVariables;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyForEachLoopStatement(ImmutableArray<ILocalSymbol> locals, Lazy<IOperation> loopControlVariable, Lazy<IOperation> collection, Lazy<ImmutableArray<IOperation>> nextVariables, Lazy<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyLoopControlVariable = loopControlVariable ?? throw new System.ArgumentNullException(nameof(loopControlVariable));
            _lazyCollection = collection ?? throw new System.ArgumentNullException(nameof(collection));
            _lazyNextVariables = nextVariables ?? throw new System.ArgumentNullException(nameof(nextVariables));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override IOperation LoopControlVariableImpl => _lazyLoopControlVariable.Value;
        protected override IOperation CollectionImpl => _lazyCollection.Value;
        protected override ImmutableArray<IOperation> NextVariablesImpl => _lazyNextVariables.Value;
        protected override IOperation BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# 'for' loop statement.
    /// </summary>
    internal abstract partial class BaseForLoopStatement : LoopStatement, IForLoopOperation
    {
        public BaseForLoopStatement(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(LoopKind.For, locals, OperationKind.Loop, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> BeforeImpl { get; }
        protected abstract IOperation ConditionImpl { get; }
        protected abstract ImmutableArray<IOperation> AtLoopBottomImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var before in Before)
                {
                    if (before != null)
                    {
                        yield return before;
                    }
                }
                if (Condition != null)
                {
                    yield return Condition;
                }
                if (Body != null)
                {
                    yield return Body;
                }
                foreach (var atLoopBottom in AtLoopBottom)
                {
                    if (atLoopBottom != null)
                    {
                        yield return atLoopBottom;
                    }
                }
            }
        }
        /// <summary>
        /// List of operations to execute before entry to the loop. This comes from the first clause of the for statement.
        /// </summary>
        public ImmutableArray<IOperation> Before => Operation.SetParentOperation(BeforeImpl, this);
        /// <summary>
        /// Condition of the loop. This comes from the second clause of the for statement.
        /// </summary>
        public IOperation Condition => Operation.SetParentOperation(ConditionImpl, this);
        /// <summary>
        /// List of operations to execute at the bottom of the loop. This comes from the third clause of the for statement.
        /// </summary>
        public ImmutableArray<IOperation> AtLoopBottom => Operation.SetParentOperation(AtLoopBottomImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitForLoop(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitForLoop(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# 'for' loop statement.
    /// </summary>
    internal sealed partial class ForLoopStatement : BaseForLoopStatement, IForLoopOperation
    {
        public ForLoopStatement(ImmutableArray<IOperation> before, IOperation condition, ImmutableArray<IOperation> atLoopBottom, ImmutableArray<ILocalSymbol> locals, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            BeforeImpl = before;
            ConditionImpl = condition;
            AtLoopBottomImpl = atLoopBottom;
            BodyImpl = body;
        }

        protected override ImmutableArray<IOperation> BeforeImpl { get; }
        protected override IOperation ConditionImpl { get; }
        protected override ImmutableArray<IOperation> AtLoopBottomImpl { get; }
        protected override IOperation BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# 'for' loop statement.
    /// </summary>
    internal sealed partial class LazyForLoopStatement : BaseForLoopStatement, IForLoopOperation
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyBefore;
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyAtLoopBottom;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyForLoopStatement(Lazy<ImmutableArray<IOperation>> before, Lazy<IOperation> condition, Lazy<ImmutableArray<IOperation>> atLoopBottom, ImmutableArray<ILocalSymbol> locals, Lazy<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyBefore = before ?? throw new System.ArgumentNullException(nameof(before));
            _lazyCondition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            _lazyAtLoopBottom = atLoopBottom ?? throw new System.ArgumentNullException(nameof(atLoopBottom));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override ImmutableArray<IOperation> BeforeImpl => _lazyBefore.Value;
        protected override IOperation ConditionImpl => _lazyCondition.Value;
        protected override ImmutableArray<IOperation> AtLoopBottomImpl => _lazyAtLoopBottom.Value;
        protected override IOperation BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a VB 'For To' loop statement.
    /// </summary>
    internal abstract partial class BaseForToLoopStatement : LoopStatement, IForToLoopOperation
    {
        public BaseForToLoopStatement(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(LoopKind.ForTo, locals, OperationKind.Loop, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation LoopControlVariableImpl { get; }
        protected abstract IOperation InitialValueImpl { get; }
        protected abstract IOperation LimitValueImpl { get; }
        protected abstract IOperation StepValueImpl { get; }
        protected abstract ImmutableArray<IOperation> NextVariablesImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (LoopControlVariable != null)
                {
                    yield return LoopControlVariable;
                }
                if (InitialValue != null)
                {
                    yield return InitialValue;
                }
                if (LimitValue != null)
                {
                    yield return LimitValue;
                }
                if (StepValue != null)
                {
                    yield return StepValue;
                }
                if (Body != null)
                {
                    yield return Body;
                }
                foreach (var expression in NextVariables)
                {
                    if (expression != null)
                    {
                        yield return expression;
                    }
                }
            }
        }
        /// <summary>
        /// Loop control variable refers to the operation for declaring a new local variable or reference an existing variable or an expression.
        /// </summary>
        public IOperation LoopControlVariable => Operation.SetParentOperation(LoopControlVariableImpl, this);

        /// <summary>
        /// Operation for setting the initial value of the loop control variable. This comes from the expression between the 'For' and 'To' keywords.
        /// </summary>
        public IOperation InitialValue => Operation.SetParentOperation(InitialValueImpl, this);

        /// <summary>
        /// Operation for the limit value of the loop control variable. This comes from the expression after the 'To' keyword.
        /// </summary>
        public IOperation LimitValue => Operation.SetParentOperation(LimitValueImpl, this);

        /// <summary>
        /// Optional operation for the step value of the loop control variable. This comes from the expression after the 'Step' keyword.
        /// </summary>
        public IOperation StepValue => Operation.SetParentOperation(StepValueImpl, this);

        /// <summary>
        /// Optional list of comma separated next variables at loop bottom.
        /// </summary>
        public ImmutableArray<IOperation> NextVariables => Operation.SetParentOperation(NextVariablesImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitForToLoop(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitForToLoop(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB 'For To' loop statement.
    /// </summary>
    internal sealed partial class ForToLoopStatement : BaseForToLoopStatement, IForToLoopOperation
    {
        public ForToLoopStatement(ImmutableArray<ILocalSymbol> locals, IOperation loopControlVariable, IOperation initialValue, IOperation limitValue, IOperation stepValue, IOperation body, ImmutableArray<IOperation> nextVariables, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LoopControlVariableImpl = loopControlVariable;
            InitialValueImpl = initialValue;
            LimitValueImpl = limitValue;
            StepValueImpl = stepValue;
            BodyImpl = body;
            NextVariablesImpl = nextVariables;
        }

        protected override IOperation LoopControlVariableImpl { get; }
        protected override IOperation InitialValueImpl { get; }
        protected override IOperation LimitValueImpl { get; }
        protected override IOperation StepValueImpl { get; }
        protected override IOperation BodyImpl { get; }
        protected override ImmutableArray<IOperation> NextVariablesImpl { get; }
    }

    /// <summary>
    /// Represents a VB 'For To' loop statement.
    /// </summary>
    internal sealed partial class LazyForToLoopStatement : BaseForToLoopStatement, IForToLoopOperation
    {
        private readonly Lazy<IOperation> _lazyLoopControlVariable;
        private readonly Lazy<IOperation> _lazyInitialValue;
        private readonly Lazy<IOperation> _lazyLimitValue;
        private readonly Lazy<IOperation> _lazyStepValue;
        private readonly Lazy<IOperation> _lazyBody;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyNextVariables;

        public LazyForToLoopStatement(ImmutableArray<ILocalSymbol> locals, Lazy<IOperation> loopControlVariable, Lazy<IOperation> initialValue, Lazy<IOperation> limitValue, Lazy<IOperation> stepValue, Lazy<IOperation> body, Lazy<ImmutableArray<IOperation>> nextVariables, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyLoopControlVariable = loopControlVariable ?? throw new System.ArgumentNullException(nameof(loopControlVariable));
            _lazyInitialValue = initialValue ?? throw new System.ArgumentNullException(nameof(initialValue));
            _lazyLimitValue = limitValue ?? throw new System.ArgumentNullException(nameof(limitValue));
            _lazyStepValue = stepValue ?? throw new System.ArgumentNullException(nameof(stepValue));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
            _lazyNextVariables = nextVariables ?? throw new System.ArgumentNullException(nameof(nextVariables));
        }

        protected override IOperation LoopControlVariableImpl => _lazyLoopControlVariable.Value;
        protected override IOperation InitialValueImpl => _lazyInitialValue.Value;
        protected override IOperation LimitValueImpl => _lazyLimitValue.Value;
        protected override IOperation StepValueImpl => _lazyStepValue.Value;
        protected override IOperation BodyImpl => _lazyBody.Value;
        protected override ImmutableArray<IOperation> NextVariablesImpl => _lazyNextVariables.Value;
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    internal abstract partial class BaseIncrementExpression : Operation, IIncrementOrDecrementOperation
    {
        public BaseIncrementExpression(bool isDecrement, bool isPostfix, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isDecrement ? OperationKind.Decrement : OperationKind.Increment, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsPostfix = isPostfix;
            IsLifted = isLifted;
            IsChecked = isChecked;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// <code>true</code> if this is a postfix expression.
        /// <code>false</code> if this is a prefix expression.
        /// </summary>
        public bool IsPostfix { get; }
        /// <summary>
        /// <code>true</code> if this is a 'lifted' increment operator.  When there is an
        /// operator that is defined to work on a value type, 'lifted' operators are
        /// created to work on the <see cref="System.Nullable{T}"/> versions of those
        /// value types.
        /// </summary>
        public bool IsLifted { get; }
        /// <summary>
        /// <code>true</code> if overflow checking is performed for the arithmetic operation.
        /// </summary>
        public bool IsChecked { get; }
        protected abstract IOperation TargetImpl { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Target != null)
                {
                    yield return Target;
                }
            }
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public IOperation Target => Operation.SetParentOperation(TargetImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIncrementOrDecrement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIncrementOrDecrement(this, argument);
        }
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    internal sealed partial class IncrementExpression : BaseIncrementExpression, IIncrementOrDecrementOperation
    {
        public IncrementExpression(bool isDecrement, bool isPostfix, bool isLifted, bool isChecked, IOperation target, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isDecrement, isPostfix, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TargetImpl = target;
        }

        protected override IOperation TargetImpl { get; }
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    internal sealed partial class LazyIncrementExpression : BaseIncrementExpression, IIncrementOrDecrementOperation
    {
        private readonly Lazy<IOperation> _lazyTarget;

        public LazyIncrementExpression(bool isDecrement, bool isPostfix, bool isLifted, bool isChecked, Lazy<IOperation> target, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isDecrement, isPostfix, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException(nameof(target));
        }

        protected override IOperation TargetImpl => _lazyTarget.Value;
    }

    /// <summary>
    /// Represents a C# this or base expression, or a VB Me, MyClass, or MyBase expression.
    /// </summary>
    internal sealed partial class InstanceReferenceExpression : Operation, IInstanceReferenceOperation
    {
        public InstanceReferenceExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.InstanceReference, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitInstanceReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInstanceReference(this, argument);
        }
    }

    /// <remarks>
    /// Represents an interpolated string expression.
    /// </remarks>
    internal abstract partial class BaseInterpolatedStringExpression : Operation, IInterpolatedStringOperation
    {
        protected BaseInterpolatedStringExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.InterpolatedString, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IInterpolatedStringContentOperation> PartsImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var part in Parts)
                {
                    if (part != null)
                    {
                        yield return part;
                    }
                }
            }
        }
        /// <summary>
        /// Constituent parts of interpolated string, each of which is an <see cref="IInterpolatedStringContentOperation"/>.
        /// </summary>
        public ImmutableArray<IInterpolatedStringContentOperation> Parts => Operation.SetParentOperation(PartsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInterpolatedString(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInterpolatedString(this, argument);
        }
    }

    /// <remarks>
    /// Represents an interpolated string expression.
    /// </remarks>
    internal sealed partial class InterpolatedStringExpression : BaseInterpolatedStringExpression, IInterpolatedStringOperation
    {
        public InterpolatedStringExpression(ImmutableArray<IInterpolatedStringContentOperation> parts, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            PartsImpl = parts;
        }

        protected override ImmutableArray<IInterpolatedStringContentOperation> PartsImpl { get; }
    }

    /// <remarks>
    /// Represents an interpolated string expression.
    /// </remarks>
    internal sealed partial class LazyInterpolatedStringExpression : BaseInterpolatedStringExpression, IInterpolatedStringOperation
    {
        private readonly Lazy<ImmutableArray<IInterpolatedStringContentOperation>> _lazyParts;

        public LazyInterpolatedStringExpression(Lazy<ImmutableArray<IInterpolatedStringContentOperation>> parts, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyParts = parts;
        }

        protected override ImmutableArray<IInterpolatedStringContentOperation> PartsImpl => _lazyParts.Value;
    }

    /// <remarks>
    /// Represents a constituent string literal part of an interpolated string expression.
    /// </remarks>
    internal abstract partial class BaseInterpolatedStringText : Operation, IInterpolatedStringTextOperation
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
                if (Text != null)
                {
                    yield return Text;
                }
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
    internal sealed partial class InterpolatedStringText : BaseInterpolatedStringText, IInterpolatedStringTextOperation
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
    internal sealed partial class LazyInterpolatedStringText : BaseInterpolatedStringText, IInterpolatedStringTextOperation
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
    internal abstract partial class BaseInterpolation : Operation, IInterpolationOperation
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
                if (Expression != null)
                {
                    yield return Expression;
                }
                if (Alignment != null)
                {
                    yield return Alignment;
                }
                if (FormatString != null)
                {
                    yield return FormatString;
                }
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
    internal sealed partial class Interpolation : BaseInterpolation, IInterpolationOperation
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
    internal sealed partial class LazyInterpolation : BaseInterpolation, IInterpolationOperation
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
    internal abstract partial class BaseInvalidOperation : Operation, IInvalidOperation
    {
        protected BaseInvalidOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.Invalid, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract ImmutableArray<IOperation> ChildrenImpl { get; }
        /// <summary>
        /// Child operations.
        /// </summary>
        public override IEnumerable<IOperation> Children => Operation.SetParentOperation(ChildrenImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalid(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvalid(this, argument);
        }
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class InvalidOperation : BaseInvalidOperation, IInvalidOperation
    {
        public InvalidOperation(ImmutableArray<IOperation> children, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            // we don't allow null children.
            Debug.Assert(children.All(o => o != null));
            ChildrenImpl = children;
        }
        protected override ImmutableArray<IOperation> ChildrenImpl { get; }
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class LazyInvalidOperation : BaseInvalidOperation, IInvalidOperation
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyChildren;

        public LazyInvalidOperation(Lazy<ImmutableArray<IOperation>> children, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            // we don't allow null children.
            Debug.Assert(children.Value.All(o => o != null));
            _lazyChildren = children;
        }
        protected override ImmutableArray<IOperation> ChildrenImpl => _lazyChildren.Value;
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal abstract partial class BaseInvocationExpression : Operation, IInvocationOperation
    {
        protected BaseInvocationExpression(IMethodSymbol targetMethod, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Invocation, semanticModel, syntax, type, constantValue, isImplicit)
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
        protected abstract ImmutableArray<IArgumentOperation> ArgumentsImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance != null)
                {
                    yield return Instance;
                }
                foreach (var argument in Arguments)
                {
                    if (argument != null)
                    {
                        yield return argument;
                    }
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
        public ImmutableArray<IArgumentOperation> Arguments => Operation.SetParentOperation(ArgumentsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvocation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvocation(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal sealed partial class InvocationExpression : BaseInvocationExpression, IInvocationOperation
    {
        public InvocationExpression(IMethodSymbol targetMethod, IOperation instance, bool isVirtual, ImmutableArray<IArgumentOperation> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceImpl = instance;
            ArgumentsImpl = arguments;
        }

        protected override IOperation InstanceImpl { get; }
        protected override ImmutableArray<IArgumentOperation> ArgumentsImpl { get; }
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal sealed partial class LazyInvocationExpression : BaseInvocationExpression, IInvocationOperation
    {
        private readonly Lazy<IOperation> _lazyInstance;
        private readonly Lazy<ImmutableArray<IArgumentOperation>> _lazyArguments;

        public LazyInvocationExpression(IMethodSymbol targetMethod, Lazy<IOperation> instance, bool isVirtual, Lazy<ImmutableArray<IArgumentOperation>> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
            _lazyArguments = arguments;
        }

        protected override IOperation InstanceImpl => _lazyInstance.Value;

        protected override ImmutableArray<IArgumentOperation> ArgumentsImpl => _lazyArguments.Value;
    }

    /// <summary>
    /// Represents a VB raise event statement.
    /// </summary>
    internal abstract partial class BaseRaiseEventStatement : Operation, IRaiseEventOperation
    {
        protected BaseRaiseEventStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.RaiseEvent, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IEventReferenceOperation EventReferenceImpl { get; }
        protected abstract ImmutableArray<IArgumentOperation> ArgumentsImpl { get; }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (EventReference != null)
                {
                    yield return EventReference;
                }
                foreach (var argument in Arguments)
                {
                    if (argument != null)
                    {
                        yield return argument;
                    }
                }
            }
        }
        /// <summary>
        /// Reference to the event to be raised.
        /// </summary>
        public IEventReferenceOperation EventReference => Operation.SetParentOperation(EventReferenceImpl, this);

        public ImmutableArray<IArgumentOperation> Arguments => Operation.SetParentOperation(ArgumentsImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitRaiseEvent(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitRaiseEvent(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB raise event statement.
    /// </summary>
    internal sealed partial class RaiseEventStatement : BaseRaiseEventStatement, IRaiseEventOperation
    {
        public RaiseEventStatement(IEventReferenceOperation eventReference, ImmutableArray<IArgumentOperation> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            EventReferenceImpl = eventReference;
            ArgumentsImpl = arguments;
        }

        protected override IEventReferenceOperation EventReferenceImpl { get; }

        protected override ImmutableArray<IArgumentOperation> ArgumentsImpl { get; }
    }

    /// <summary>
    /// Represents a VB raise event statement.
    /// </summary>
    internal sealed partial class LazyRaiseEventStatement : BaseRaiseEventStatement, IRaiseEventOperation
    {
        private readonly Lazy<IEventReferenceOperation> _lazyEventReference;
        private readonly Lazy<ImmutableArray<IArgumentOperation>> _lazyArguments;

        public LazyRaiseEventStatement(Lazy<IEventReferenceOperation> eventReference, Lazy<ImmutableArray<IArgumentOperation>> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyEventReference = eventReference;
            _lazyArguments = arguments;
        }

        protected override IEventReferenceOperation EventReferenceImpl => _lazyEventReference.Value;

        protected override ImmutableArray<IArgumentOperation> ArgumentsImpl => _lazyArguments.Value;
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal abstract partial class BaseIsTypeExpression : Operation, IIsTypeOperation
    {
        protected BaseIsTypeExpression(ITypeSymbol typeOperand, bool isNegated, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.IsType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TypeOperand = typeOperand;
            IsNegated = isNegated;
        }

        protected abstract IOperation ValueOperandImpl { get; }
        /// <summary>
        /// Type for which to test.
        /// </summary>
        public ITypeSymbol TypeOperand { get; }
        /// <summary>
        /// Flag indicating if this is an "is not" type expression.
        /// True for VB "TypeOf ... IsNot ..." expression.
        /// False, otherwise.
        /// </summary>
        public bool IsNegated { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (ValueOperand != null)
                {
                    yield return ValueOperand;
                }
            }
        }
        /// <summary>
        /// Value to test.
        /// </summary>
        public IOperation ValueOperand => Operation.SetParentOperation(ValueOperandImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIsType(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIsType(this, argument);
        }
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal sealed partial class IsTypeExpression : BaseIsTypeExpression, IIsTypeOperation
    {
        public IsTypeExpression(IOperation valueOperand, ITypeSymbol typeOperand, bool isNegated, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(typeOperand, isNegated, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueOperandImpl = valueOperand;
        }

        protected override IOperation ValueOperandImpl { get; }
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal sealed partial class LazyIsTypeExpression : BaseIsTypeExpression, IIsTypeOperation
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyIsTypeExpression(Lazy<IOperation> operand, ITypeSymbol isType, bool isNotTypeExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(isType, isNotTypeExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException(nameof(operand));
        }

        protected override IOperation ValueOperandImpl => _lazyOperand.Value;
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal abstract partial class BaseLabeledStatement : Operation, ILabeledOperation
    {
        protected BaseLabeledStatement(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Labeled, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Label = label;
        }
        /// <summary>
        ///  Label that can be the target of branches.
        /// </summary>
        public ILabelSymbol Label { get; }
        protected abstract IOperation StatementImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation != null)
                {
                    yield return Operation;
                }
            }
        }
        /// <summary>
        /// Statement that has been labeled.
        /// </summary>
        public IOperation Operation => CodeAnalysis.Operation.SetParentOperation(StatementImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLabeled(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLabeled(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal sealed partial class LabeledStatement : BaseLabeledStatement, ILabeledOperation
    {
        public LabeledStatement(ILabelSymbol label, IOperation labeledStatement, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            StatementImpl = labeledStatement;
        }

        protected override IOperation StatementImpl { get; }
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal sealed partial class LazyLabeledStatement : BaseLabeledStatement, ILabeledOperation
    {
        private readonly Lazy<IOperation> _lazyStatement;

        public LazyLabeledStatement(ILabelSymbol label, Lazy<IOperation> statement, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyStatement = statement ?? throw new System.ArgumentNullException(nameof(statement));
        }

        protected override IOperation StatementImpl => _lazyStatement.Value;
    }

    internal abstract partial class BaseAnonymousFunctionExpression : Operation, IAnonymousFunctionOperation
    {
        protected BaseAnonymousFunctionExpression(IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.AnonymousFunction, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Symbol = symbol;
        }
        public IMethodSymbol Symbol { get; }
        protected abstract IBlockOperation BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Body != null)
                {
                    yield return Body;
                }
            }
        }
        public IBlockOperation Body => Operation.SetParentOperation(BodyImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAnonymousFunction(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAnonymousFunction(this, argument);
        }
    }

    internal sealed partial class AnonymousFunctionExpression : BaseAnonymousFunctionExpression, IAnonymousFunctionOperation
    {
        public AnonymousFunctionExpression(IMethodSymbol symbol, IBlockOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            BodyImpl = body;
        }

        protected override IBlockOperation BodyImpl { get; }
    }

    internal sealed partial class LazyAnonymousFunctionExpression : BaseAnonymousFunctionExpression, IAnonymousFunctionOperation
    {
        private readonly Lazy<IBlockOperation> _lazyBody;

        public LazyAnonymousFunctionExpression(IMethodSymbol symbol, Lazy<IBlockOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override IBlockOperation BodyImpl => _lazyBody.Value;
    }

    internal abstract partial class BaseDelegateCreationExpression : Operation, IDelegateCreationOperation
    {
        public BaseDelegateCreationExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DelegateCreation, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation TargetImpl { get; }
        public IOperation Target => Operation.SetParentOperation(TargetImpl, this);

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Target != null)
                {
                    yield return Target;
                }
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDelegateCreation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDelegateCreation(this, argument);
        }
    }

    internal partial class DelegateCreationExpression : BaseDelegateCreationExpression
    {
        public DelegateCreationExpression(IOperation target, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            TargetImpl = target;
        }

        protected override IOperation TargetImpl { get; }
    }

    internal partial class LazyDelegateCreationExpression : BaseDelegateCreationExpression
    {
        private readonly Lazy<IOperation> _lazyTarget;
        public LazyDelegateCreationExpression(Lazy<IOperation> lazyTarget, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyTarget = lazyTarget;
        }

        protected override IOperation TargetImpl => _lazyTarget.Value;
    }

    /// <summary>
    /// Represents a dynamic access to a member of a class, struct, or module.
    /// </summary>
    internal abstract partial class BaseDynamicMemberReferenceExpression : Operation, IDynamicMemberReferenceOperation
    {
        protected BaseDynamicMemberReferenceExpression(string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DynamicMemberReference, semanticModel, syntax, type, constantValue, isImplicit)
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
                if (Instance != null)
                {
                    yield return Instance;
                }
            }
        }
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        public IOperation Instance => Operation.SetParentOperation(InstanceImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicMemberReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicMemberReference(this, argument);
        }

    }

    /// <summary>
    /// Represents a dynamic access to a member of a class, struct, or module.
    /// </summary>
    internal sealed partial class DynamicMemberReferenceExpression : BaseDynamicMemberReferenceExpression, IDynamicMemberReferenceOperation
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
    internal sealed partial class LazyDynamicMemberReferenceExpression : BaseDynamicMemberReferenceExpression, IDynamicMemberReferenceOperation
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
    internal sealed partial class LiteralExpression : Operation, ILiteralOperation
    {
        public LiteralExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.Literal, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitLiteral(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLiteral(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a declared local variable.
    /// </summary>
    internal sealed partial class LocalReferenceExpression : Operation, ILocalReferenceOperation
    {
        public LocalReferenceExpression(ILocalSymbol local, bool isDeclaration, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.LocalReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Local = local;
            IsDeclaration = isDeclaration;
        }
        /// <summary>
        /// Referenced local variable.
        /// </summary>
        public ILocalSymbol Local { get; }
        public bool IsDeclaration { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLocalReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLocalReference(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    internal abstract partial class BaseLockStatement : Operation, ILockOperation
    {
        protected BaseLockStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Lock, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        protected abstract IOperation BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (LockedValue != null)
                {
                    yield return LockedValue;
                }
                if (Body != null)
                {
                    yield return Body;
                }
            }
        }
        /// <summary>
        /// Expression producing a value to be locked.
        /// </summary>
        public IOperation LockedValue => CodeAnalysis.Operation.SetParentOperation(ExpressionImpl, this);
        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        public IOperation Body => CodeAnalysis.Operation.SetParentOperation(BodyImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLock(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLock(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    internal sealed partial class LockStatement : BaseLockStatement, ILockOperation
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
    internal sealed partial class LazyLockStatement : BaseLockStatement, ILockOperation
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
    internal abstract partial class LoopStatement : Operation, ILoopOperation
    {
        protected LoopStatement(LoopKind loopKind, ImmutableArray<ILocalSymbol> locals, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LoopKind = loopKind;
            Locals = locals;
        }
        protected abstract IOperation BodyImpl { get; }
        /// <summary>
        /// Kind of the loop.
        /// </summary>
        public LoopKind LoopKind { get; }
        /// <summary>
        /// Declarations local to the loop.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public IOperation Body => Operation.SetParentOperation(BodyImpl, this);
    }

    /// <summary>
    /// Represents a reference to a member of a class, struct, or interface.
    /// </summary>
    internal abstract partial class MemberReferenceExpression : Operation, IMemberReferenceOperation
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
    internal abstract partial class BaseMethodReferenceExpression : MemberReferenceExpression, IMethodReferenceOperation
    {
        public BaseMethodReferenceExpression(IMethodSymbol method, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(method, OperationKind.MethodReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsVirtual = isVirtual;
        }
        /// <summary>
        /// Referenced method.
        /// </summary>
        public IMethodSymbol Method => (IMethodSymbol)Member;

        /// <summary>
        /// Indicates whether the reference uses virtual semantics.
        /// </summary>
        public bool IsVirtual { get; }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance != null)
                {
                    yield return Instance;
                }
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitMethodReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitMethodReference(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a method other than as the target of an invocation.
    /// </summary>
    internal sealed partial class MethodReferenceExpression : BaseMethodReferenceExpression, IMethodReferenceOperation
    {
        public MethodReferenceExpression(IMethodSymbol method, bool isVirtual, IOperation instance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(method, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
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
    internal sealed partial class LazyMethodReferenceExpression : BaseMethodReferenceExpression, IMethodReferenceOperation
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyMethodReferenceExpression(IMethodSymbol method, bool isVirtual, Lazy<IOperation> instance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(method, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
        }
        protected override IOperation InstanceImpl => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal abstract partial class BaseCoalesceExpression : Operation, ICoalesceOperation
    {
        protected BaseCoalesceExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Coalesce, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        protected abstract IOperation WhenNullImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value != null)
                {
                    yield return Value;
                }
                if (WhenNull != null)
                {
                    yield return WhenNull;
                }
            }
        }
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        public IOperation Value => Operation.SetParentOperation(ExpressionImpl, this);
        /// <summary>
        /// Value to be evaluated if <see cref="Value"/> evaluates to null/Nothing.
        /// </summary>
        public IOperation WhenNull => Operation.SetParentOperation(WhenNullImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCoalesce(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCoalesce(this, argument);
        }
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal sealed partial class CoalesceExpression : BaseCoalesceExpression, ICoalesceOperation
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
    internal sealed partial class LazyCoalesceExpression : BaseCoalesceExpression, ICoalesceOperation
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
    internal abstract partial class BaseObjectCreationExpression : Operation, IObjectCreationOperation
    {
        protected BaseObjectCreationExpression(IMethodSymbol constructor, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ObjectCreation, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Constructor = constructor;
        }
        /// <summary>
        /// Constructor to be invoked on the created instance.
        /// </summary>
        public IMethodSymbol Constructor { get; }
        protected abstract IObjectOrCollectionInitializerOperation InitializerImpl { get; }
        protected abstract ImmutableArray<IArgumentOperation> ArgumentsImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var argument in Arguments)
                {
                    if (argument != null)
                    {
                        yield return argument;
                    }
                }
                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        public IObjectOrCollectionInitializerOperation Initializer => Operation.SetParentOperation(InitializerImpl, this);
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays.
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public ImmutableArray<IArgumentOperation> Arguments => Operation.SetParentOperation(ArgumentsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitObjectCreation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitObjectCreation(this, argument);
        }
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    internal sealed partial class ObjectCreationExpression : BaseObjectCreationExpression, IObjectCreationOperation
    {
        public ObjectCreationExpression(IMethodSymbol constructor, IObjectOrCollectionInitializerOperation initializer, ImmutableArray<IArgumentOperation> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(constructor, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializerImpl = initializer;
            ArgumentsImpl = arguments;
        }

        protected override IObjectOrCollectionInitializerOperation InitializerImpl { get; }
        protected override ImmutableArray<IArgumentOperation> ArgumentsImpl { get; }
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    internal sealed partial class LazyObjectCreationExpression : BaseObjectCreationExpression, IObjectCreationOperation
    {
        private readonly Lazy<IObjectOrCollectionInitializerOperation> _lazyInitializer;
        private readonly Lazy<ImmutableArray<IArgumentOperation>> _lazyArguments;

        public LazyObjectCreationExpression(IMethodSymbol constructor, Lazy<IObjectOrCollectionInitializerOperation> initializer, Lazy<ImmutableArray<IArgumentOperation>> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(constructor, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInitializer = initializer;
            _lazyArguments = arguments;
        }

        protected override IObjectOrCollectionInitializerOperation InitializerImpl => _lazyInitializer.Value;

        protected override ImmutableArray<IArgumentOperation> ArgumentsImpl => _lazyArguments.Value;

    }

    /// <summary>
    /// Represents a C# or VB new/New anonymous object creation expression.
    /// </summary>
    internal abstract partial class BaseAnonymousObjectCreationExpression : Operation, IAnonymousObjectCreationOperation
    {
        protected BaseAnonymousObjectCreationExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.AnonymousObjectCreation, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> InitializersImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var initializer in Initializers)
                {
                    if (initializer != null)
                    {
                        yield return initializer;
                    }
                }
            }
        }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public ImmutableArray<IOperation> Initializers => Operation.SetParentOperation(InitializersImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAnonymousObjectCreation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAnonymousObjectCreation(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB new/New anonymous object creation expression.
    /// </summary>
    internal sealed partial class AnonymousObjectCreationExpression : BaseAnonymousObjectCreationExpression, IAnonymousObjectCreationOperation
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
    internal sealed partial class LazyAnonymousObjectCreationExpression : BaseAnonymousObjectCreationExpression, IAnonymousObjectCreationOperation
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
    internal sealed partial class OmittedArgumentExpression : Operation, IOmittedArgumentOperation
    {
        public OmittedArgumentExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.OmittedArgument, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitOmittedArgument(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitOmittedArgument(this, argument);
        }
    }

    /// <summary>
    /// Represents an initialization of a parameter at the point of declaration.
    /// </summary>
    internal abstract partial class BaseParameterInitializer : SymbolInitializer, IParameterInitializerOperation
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
                if (Value != null)
                {
                    yield return Value;
                }
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
    internal sealed partial class ParameterInitializer : BaseParameterInitializer, IParameterInitializerOperation
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
    internal sealed partial class LazyParameterInitializer : BaseParameterInitializer, IParameterInitializerOperation
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
    internal sealed partial class ParameterReferenceExpression : Operation, IParameterReferenceOperation
    {
        public ParameterReferenceExpression(IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.ParameterReference, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitParameterReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParameterReference(this, argument);
        }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    internal abstract partial class BaseParenthesizedExpression : Operation, IParenthesizedOperation
    {
        protected BaseParenthesizedExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Parenthesized, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation OperandImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operand != null)
                {
                    yield return Operand;
                }
            }
        }
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        public IOperation Operand => Operation.SetParentOperation(OperandImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParenthesized(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParenthesized(this, argument);
        }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    internal sealed partial class ParenthesizedExpression : BaseParenthesizedExpression, IParenthesizedOperation
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
    internal sealed partial class LazyParenthesizedExpression : BaseParenthesizedExpression, IParenthesizedOperation
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
    internal sealed partial class PlaceholderExpression : Operation, IPlaceholderOperation
    {
        public PlaceholderExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            // https://github.com/dotnet/roslyn/issues/21294
            // base(OperationKind.Placeholder, semanticModel, syntax, type, constantValue, isImplicit)
            base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitPlaceholder(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPlaceholder(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    internal abstract partial class BasePointerIndirectionReferenceExpression : Operation, IPointerIndirectionReferenceOperation
    {
        protected BasePointerIndirectionReferenceExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            // API is internal for V1
            // https://github.com/dotnet/roslyn/issues/21295
            base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation PointerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Pointer != null)
                {
                    yield return Pointer;
                }
            }
        }
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        public IOperation Pointer => Operation.SetParentOperation(PointerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPointerIndirectionReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPointerIndirectionReference(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    internal sealed partial class PointerIndirectionReferenceExpression : BasePointerIndirectionReferenceExpression, IPointerIndirectionReferenceOperation
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
    internal sealed partial class LazyPointerIndirectionReferenceExpression : BasePointerIndirectionReferenceExpression, IPointerIndirectionReferenceOperation
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
    internal abstract partial class BasePropertyInitializer : SymbolInitializer, IPropertyInitializerOperation
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
                if (Value != null)
                {
                    yield return Value;
                }
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
    internal sealed partial class PropertyInitializer : BasePropertyInitializer, IPropertyInitializerOperation
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
    internal sealed partial class LazyPropertyInitializer : BasePropertyInitializer, IPropertyInitializerOperation
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
    internal abstract partial class BasePropertyReferenceExpression : MemberReferenceExpression, IPropertyReferenceOperation
    {
        protected BasePropertyReferenceExpression(IPropertySymbol property, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(property, OperationKind.PropertyReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        /// <summary>
        /// Referenced property.
        /// </summary>
        public IPropertySymbol Property => (IPropertySymbol)Member;
        protected abstract ImmutableArray<IArgumentOperation> ArgumentsImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance != null)
                {
                    yield return Instance;
                }
                foreach (var argument in Arguments)
                {
                    if (argument != null)
                    {
                        yield return argument;
                    }
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
        public ImmutableArray<IArgumentOperation> Arguments => Operation.SetParentOperation(ArgumentsImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyReference(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    internal sealed partial class PropertyReferenceExpression : BasePropertyReferenceExpression, IPropertyReferenceOperation
    {
        public PropertyReferenceExpression(IPropertySymbol property, IOperation instance, ImmutableArray<IArgumentOperation> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(property, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InstanceImpl = instance;
            ArgumentsImpl = arguments;
        }
        protected override IOperation InstanceImpl { get; }
        protected override ImmutableArray<IArgumentOperation> ArgumentsImpl { get; }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyReference(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    internal sealed partial class LazyPropertyReferenceExpression : BasePropertyReferenceExpression, IPropertyReferenceOperation
    {
        private readonly Lazy<IOperation> _lazyInstance;
        private readonly Lazy<ImmutableArray<IArgumentOperation>> _lazyArguments;

        public LazyPropertyReferenceExpression(IPropertySymbol property, Lazy<IOperation> instance, Lazy<ImmutableArray<IArgumentOperation>> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(property, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
            _lazyArguments = arguments ?? throw new System.ArgumentNullException(nameof(arguments));
        }
        protected override IOperation InstanceImpl => _lazyInstance.Value;

        protected override ImmutableArray<IArgumentOperation> ArgumentsImpl => _lazyArguments.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyReference(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyReference(this, argument);
        }
    }

    /// <summary>
    /// Represents Case x To y in VB.
    /// </summary>
    internal abstract partial class BaseRangeCaseClause : CaseClause, IRangeCaseClauseOperation
    {
        public BaseRangeCaseClause(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(CaseKind.Range, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation MinimumValueImpl { get; }
        protected abstract IOperation MaximumValueImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (MinimumValue != null)
                {
                    yield return MinimumValue;
                }
                if (MaximumValue != null)
                {
                    yield return MaximumValue;
                }
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
    internal sealed partial class RangeCaseClause : BaseRangeCaseClause, IRangeCaseClauseOperation
    {
        public RangeCaseClause(IOperation minimumValue, IOperation maximumValue, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
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
    internal sealed partial class LazyRangeCaseClause : BaseRangeCaseClause, IRangeCaseClauseOperation
    {
        private readonly Lazy<IOperation> _lazyMinimumValue;
        private readonly Lazy<IOperation> _lazyMaximumValue;

        public LazyRangeCaseClause(Lazy<IOperation> minimumValue, Lazy<IOperation> maximumValue, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
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
    internal abstract partial class BaseRelationalCaseClause : CaseClause, IRelationalCaseClauseOperation
    {
        public BaseRelationalCaseClause(BinaryOperatorKind relation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(CaseKind.Relational, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Relation = relation;
        }

        protected abstract IOperation ValueImpl { get; }
        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        public BinaryOperatorKind Relation { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value != null)
                {
                    yield return Value;
                }
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
    internal sealed partial class RelationalCaseClause : BaseRelationalCaseClause, IRelationalCaseClauseOperation
    {
        public RelationalCaseClause(IOperation value, BinaryOperatorKind relation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(relation, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
        }

        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents Case Is op x in VB.
    /// </summary>
    internal sealed partial class LazyRelationalCaseClause : BaseRelationalCaseClause, IRelationalCaseClauseOperation
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyRelationalCaseClause(Lazy<IOperation> value, BinaryOperatorKind relation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(relation, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }

        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    internal abstract partial class BaseReturnStatement : Operation, IReturnOperation
    {
        protected BaseReturnStatement(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Debug.Assert(kind == OperationKind.Return
                      || kind == OperationKind.YieldReturn
                      || kind == OperationKind.YieldBreak);
        }

        protected abstract IOperation ReturnedValueImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (ReturnedValue != null)
                {
                    yield return ReturnedValue;
                }
            }
        }
        /// <summary>
        /// Value to be returned.
        /// </summary>
        public IOperation ReturnedValue => Operation.SetParentOperation(ReturnedValueImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitReturn(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitReturn(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    internal sealed partial class ReturnStatement : BaseReturnStatement, IReturnOperation
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
    internal sealed partial class LazyReturnStatement : BaseReturnStatement, IReturnOperation
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
    internal abstract partial class BaseSingleValueCaseClause : CaseClause, ISingleValueCaseClauseOperation
    {
        public BaseSingleValueCaseClause(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(CaseKind.SingleValue, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ValueImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value != null)
                {
                    yield return Value;
                }
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
    internal sealed partial class SingleValueCaseClause : BaseSingleValueCaseClause, ISingleValueCaseClauseOperation
    {
        public SingleValueCaseClause(IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
        }

        protected override IOperation ValueImpl { get; }
    }

    /// <summary>
    /// Represents case x in C# or Case x in VB.
    /// </summary>
    internal sealed partial class LazySingleValueCaseClause : BaseSingleValueCaseClause, ISingleValueCaseClauseOperation
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazySingleValueCaseClause(Lazy<IOperation> value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }

        protected override IOperation ValueImpl => _lazyValue.Value;
    }

    /// <summary>
    /// Represents default case in C# or Case Else in VB.
    /// </summary>
    internal sealed partial class DefaultCaseClause : CaseClause, IDefaultCaseClauseOperation
    {
        public DefaultCaseClause(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(CaseKind.Default, semanticModel, syntax, type, constantValue, isImplicit)
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
    internal sealed partial class SizeOfExpression : Operation, ISizeOfOperation
    {
        public SizeOfExpression(ITypeSymbol typeOperand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.SizeOf, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TypeOperand = typeOperand;
        }
        /// <summary>
        /// Type operand.
        /// </summary>
        public ITypeSymbol TypeOperand { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSizeOf(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSizeOf(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB Stop statement.
    /// </summary>
    internal sealed partial class StopStatement : Operation, IStopOperation
    {
        public StopStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.Stop, semanticModel, syntax, type, constantValue, isImplicit)
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
            visitor.VisitStop(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitStop(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    internal abstract partial class BaseSwitchCase : Operation, ISwitchCaseOperation
    {
        protected BaseSwitchCase(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.SwitchCase, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<ICaseClauseOperation> ClausesImpl { get; }
        protected abstract ImmutableArray<IOperation> BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var clause in Clauses)
                {
                    if (clause != null)
                    {
                        yield return clause;
                    }
                }
                foreach (var body in Body)
                {
                    if (body != null)
                    {
                        yield return body;
                    }
                }
            }
        }
        /// <summary>
        /// Clauses of the case. For C# there is one clause per case, but for VB there can be multiple.
        /// </summary>
        public ImmutableArray<ICaseClauseOperation> Clauses => Operation.SetParentOperation(ClausesImpl, this);
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
    internal sealed partial class SwitchCase : BaseSwitchCase, ISwitchCaseOperation
    {
        public SwitchCase(ImmutableArray<ICaseClauseOperation> clauses, ImmutableArray<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ClausesImpl = clauses;
            BodyImpl = body;
        }

        protected override ImmutableArray<ICaseClauseOperation> ClausesImpl { get; }
        protected override ImmutableArray<IOperation> BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    internal sealed partial class LazySwitchCase : BaseSwitchCase, ISwitchCaseOperation
    {
        private readonly Lazy<ImmutableArray<ICaseClauseOperation>> _lazyClauses;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyBody;

        public LazySwitchCase(Lazy<ImmutableArray<ICaseClauseOperation>> clauses, Lazy<ImmutableArray<IOperation>> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyClauses = clauses;
            _lazyBody = body;
        }

        protected override ImmutableArray<ICaseClauseOperation> ClausesImpl => _lazyClauses.Value;

        protected override ImmutableArray<IOperation> BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal abstract partial class BaseSwitchStatement : Operation, ISwitchOperation
    {
        protected BaseSwitchStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Switch, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ValueImpl { get; }
        protected abstract ImmutableArray<ISwitchCaseOperation> CasesImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value != null)
                {
                    yield return Value;
                }
                foreach (var @case in Cases)
                {
                    if (@case != null)
                    {
                        yield return @case;
                    }
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
        public ImmutableArray<ISwitchCaseOperation> Cases => Operation.SetParentOperation(CasesImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSwitch(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSwitch(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal sealed partial class SwitchStatement : BaseSwitchStatement, ISwitchOperation
    {
        public SwitchStatement(IOperation value, ImmutableArray<ISwitchCaseOperation> cases, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
            CasesImpl = cases;
        }

        protected override IOperation ValueImpl { get; }
        protected override ImmutableArray<ISwitchCaseOperation> CasesImpl { get; }
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal sealed partial class LazySwitchStatement : BaseSwitchStatement, ISwitchOperation
    {
        private readonly Lazy<IOperation> _lazyValue;
        private readonly Lazy<ImmutableArray<ISwitchCaseOperation>> _lazyCases;

        public LazySwitchStatement(Lazy<IOperation> value, Lazy<ImmutableArray<ISwitchCaseOperation>> cases, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
            _lazyCases = cases;
        }

        protected override IOperation ValueImpl => _lazyValue.Value;

        protected override ImmutableArray<ISwitchCaseOperation> CasesImpl => _lazyCases.Value;
    }

    /// <summary>
    /// Represents an initializer for a field, property, or parameter.
    /// </summary>
    internal abstract partial class SymbolInitializer : Operation, ISymbolInitializerOperation
    {
        protected SymbolInitializer(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IOperation ValueImpl { get; }
        public IOperation Value => Operation.SetParentOperation(ValueImpl, this);
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal abstract partial class BaseTryStatement : Operation, ITryOperation
    {
        protected BaseTryStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.Try, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IBlockOperation BodyImpl { get; }
        protected abstract ImmutableArray<ICatchClauseOperation> CatchesImpl { get; }
        protected abstract IBlockOperation FinallyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Body != null)
                {
                    yield return Body;
                }
                foreach (var @catch in Catches)
                {
                    if (@catch != null)
                    {
                        yield return @catch;
                    }
                }
                if (Finally != null)
                {
                    yield return Finally;
                }
            }
        }
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        public IBlockOperation Body => Operation.SetParentOperation(BodyImpl, this);
        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        public ImmutableArray<ICatchClauseOperation> Catches => Operation.SetParentOperation(CatchesImpl, this);
        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        public IBlockOperation Finally => Operation.SetParentOperation(FinallyImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTry(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTry(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal sealed partial class TryStatement : BaseTryStatement, ITryOperation
    {
        public TryStatement(IBlockOperation body, ImmutableArray<ICatchClauseOperation> catches, IBlockOperation finallyHandler, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            BodyImpl = body;
            CatchesImpl = catches;
            FinallyImpl = finallyHandler;
        }

        protected override IBlockOperation BodyImpl { get; }
        protected override ImmutableArray<ICatchClauseOperation> CatchesImpl { get; }
        protected override IBlockOperation FinallyImpl { get; }
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal sealed partial class LazyTryStatement : BaseTryStatement, ITryOperation
    {
        private readonly Lazy<IBlockOperation> _lazyBody;
        private readonly Lazy<ImmutableArray<ICatchClauseOperation>> _lazyCatches;
        private readonly Lazy<IBlockOperation> _lazyFinallyHandler;

        public LazyTryStatement(Lazy<IBlockOperation> body, Lazy<ImmutableArray<ICatchClauseOperation>> catches, Lazy<IBlockOperation> finallyHandler, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
            _lazyCatches = catches;
            _lazyFinallyHandler = finallyHandler ?? throw new System.ArgumentNullException(nameof(finallyHandler));
        }

        protected override IBlockOperation BodyImpl => _lazyBody.Value;

        protected override ImmutableArray<ICatchClauseOperation> CatchesImpl => _lazyCatches.Value;

        protected override IBlockOperation FinallyImpl => _lazyFinallyHandler.Value;
    }

    /// <summary>
    /// Represents a tuple expression.
    /// </summary>
    internal abstract partial class BaseTupleExpression : Operation, ITupleOperation
    {
        protected BaseTupleExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Tuple, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> ElementsImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var element in Elements)
                {
                    if (element != null)
                    {
                        yield return element;
                    }
                }
            }
        }
        /// <summary>
        /// Elements for tuple expression.
        /// </summary>
        public ImmutableArray<IOperation> Elements => Operation.SetParentOperation(ElementsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTuple(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTuple(this, argument);
        }
    }

    /// <summary>
    /// Represents a tuple expression.
    /// </summary>
    internal sealed partial class TupleExpression : BaseTupleExpression, ITupleOperation
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
    internal sealed partial class LazyTupleExpression : BaseTupleExpression, ITupleOperation
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
    internal sealed partial class TypeOfExpression : Operation, ITypeOfOperation
    {
        public TypeOfExpression(ITypeSymbol typeOperand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.TypeOf, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TypeOperand = typeOperand;
        }
        /// <summary>
        /// Type operand.
        /// </summary>
        public ITypeSymbol TypeOperand { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield break;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTypeOf(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTypeOf(this, argument);
        }
    }

    /// <summary>
    /// Represents a type parameter object creation expression, i.e. new T(), where T is a type parameter with new constraint.
    /// </summary>
    internal abstract partial class BaseTypeParameterObjectCreationExpression : Operation, ITypeParameterObjectCreationOperation
    {
        public BaseTypeParameterObjectCreationExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.TypeParameterObjectCreation, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IObjectOrCollectionInitializerOperation InitializerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        public IObjectOrCollectionInitializerOperation Initializer => Operation.SetParentOperation(InitializerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTypeParameterObjectCreation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTypeParameterObjectCreation(this, argument);
        }
    }

    /// <summary>
    /// Represents a type parameter object creation expression, i.e. new T(), where T is a type parameter with new constraint.
    /// </summary>
    internal sealed partial class TypeParameterObjectCreationExpression : BaseTypeParameterObjectCreationExpression, ITypeParameterObjectCreationOperation
    {
        public TypeParameterObjectCreationExpression(IObjectOrCollectionInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializerImpl = initializer;
        }
        protected override IObjectOrCollectionInitializerOperation InitializerImpl { get; }
    }

    /// <summary>
    /// Represents a type parameter object creation expression, i.e. new T(), where T is a type parameter with new constraint.
    /// </summary>
    internal sealed partial class LazyTypeParameterObjectCreationExpression : BaseTypeParameterObjectCreationExpression, ITypeParameterObjectCreationOperation
    {
        private readonly Lazy<IObjectOrCollectionInitializerOperation> _lazyInitializer;
        public LazyTypeParameterObjectCreationExpression(Lazy<IObjectOrCollectionInitializerOperation> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }
        protected override IObjectOrCollectionInitializerOperation InitializerImpl => _lazyInitializer.Value;
    }

    /// <remarks>
    /// Represents a dynamically bound new/New expression.
    /// </remarks>
    internal abstract partial class HasDynamicArgumentsExpression : Operation
    {
        protected HasDynamicArgumentsExpression(OperationKind operationKind, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(operationKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArgumentNames = argumentNames;
            ArgumentRefKinds = argumentRefKinds;
        }

        protected abstract ImmutableArray<IOperation> ArgumentsImpl { get; }

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
    }

    /// <remarks>
    /// Represents a dynamically bound new/New expression.
    /// </remarks>
    internal abstract partial class BaseDynamicObjectCreationExpression : HasDynamicArgumentsExpression, IDynamicObjectCreationOperation
    {
        public BaseDynamicObjectCreationExpression(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DynamicObjectCreation, argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IObjectOrCollectionInitializerOperation InitializerImpl { get; }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var argument in Arguments)
                {
                    if (argument != null)
                    {
                        yield return argument;
                    }
                }
                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        public IObjectOrCollectionInitializerOperation Initializer => Operation.SetParentOperation(InitializerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicObjectCreation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicObjectCreation(this, argument);
        }
    }

    /// <remarks>
    /// Represents a dynamically bound new/New expression.
    /// </remarks>
    internal sealed partial class DynamicObjectCreationExpression : BaseDynamicObjectCreationExpression, IDynamicObjectCreationOperation
    {
        public DynamicObjectCreationExpression(ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, IObjectOrCollectionInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArgumentsImpl = arguments;
            InitializerImpl = initializer;
        }
        protected override ImmutableArray<IOperation> ArgumentsImpl { get; }
        protected override IObjectOrCollectionInitializerOperation InitializerImpl { get; }
    }

    /// <remarks>
    /// Represents a dynamically bound new/New expression.
    /// </remarks>
    internal sealed partial class LazyDynamicObjectCreationExpression : BaseDynamicObjectCreationExpression, IDynamicObjectCreationOperation
    {
        private readonly Lazy<IObjectOrCollectionInitializerOperation> _lazyInitializer;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyArguments;
        public LazyDynamicObjectCreationExpression(Lazy<ImmutableArray<IOperation>> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, Lazy<IObjectOrCollectionInitializerOperation> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyArguments = arguments ?? throw new System.ArgumentNullException(nameof(arguments));
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }
        protected override ImmutableArray<IOperation> ArgumentsImpl => _lazyArguments.Value;
        protected override IObjectOrCollectionInitializerOperation InitializerImpl => _lazyInitializer.Value;
    }

    /// <remarks>
    /// Represents a dynamically bound invocation expression in C# and late bound invocation in VB.
    /// </remarks>
    internal abstract partial class BaseDynamicInvocationExpression : HasDynamicArgumentsExpression, IDynamicInvocationOperation
    {
        public BaseDynamicInvocationExpression(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DynamicInvocation, argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IOperation ExpressionImpl { get; }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation != null)
                {
                    yield return Operation;
                }
                foreach (var argument in Arguments)
                {
                    if (argument != null)
                    {
                        yield return argument;
                    }
                }
            }
        }
        /// <summary>
        /// Dynamically or late bound expression.
        /// </summary>
        public IOperation Operation => CodeAnalysis.Operation.SetParentOperation(ExpressionImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicInvocation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicInvocation(this, argument);
        }
    }

    /// <remarks>
    /// Represents a dynamically bound invocation expression in C# and late bound invocation in VB.
    /// </remarks>
    internal sealed partial class DynamicInvocationExpression : BaseDynamicInvocationExpression, IDynamicInvocationOperation
    {
        public DynamicInvocationExpression(IOperation expression, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
            ArgumentsImpl = arguments;
        }
        protected override IOperation ExpressionImpl { get; }
        protected override ImmutableArray<IOperation> ArgumentsImpl { get; }
    }

    /// <remarks>
    /// Represents a dynamically bound invocation expression in C# and late bound invocation in VB.
    /// </remarks>
    internal sealed partial class LazyDynamicInvocationExpression : BaseDynamicInvocationExpression, IDynamicInvocationOperation
    {
        private readonly Lazy<IOperation> _lazyExpression;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyArguments;
        public LazyDynamicInvocationExpression(Lazy<IOperation> expression, Lazy<ImmutableArray<IOperation>> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
            _lazyArguments = arguments ?? throw new System.ArgumentNullException(nameof(arguments));
        }
        protected override IOperation ExpressionImpl => _lazyExpression.Value;
        protected override ImmutableArray<IOperation> ArgumentsImpl => _lazyArguments.Value;
    }

    /// <remarks>
    /// Represents a dynamic indexer expression in C#.
    /// </remarks>
    internal abstract partial class BaseDynamicIndexerAccessExpression : HasDynamicArgumentsExpression, IDynamicIndexerAccessOperation
    {
        public BaseDynamicIndexerAccessExpression(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DynamicIndexerAccess, argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IOperation ExpressionImpl { get; }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation != null)
                {
                    yield return Operation;
                }
                foreach (var argument in Arguments)
                {
                    if (argument != null)
                    {
                        yield return argument;
                    }
                }
            }
        }
        /// <summary>
        /// Dynamically indexed expression.
        /// </summary>
        public IOperation Operation => CodeAnalysis.Operation.SetParentOperation(ExpressionImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicIndexerAccess(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicIndexerAccess(this, argument);
        }
    }

    /// <remarks>
    /// Represents a dynamic indexer expression in C#.
    /// </remarks>
    internal sealed partial class DynamicIndexerAccessExpression : BaseDynamicIndexerAccessExpression, IDynamicIndexerAccessOperation
    {
        public DynamicIndexerAccessExpression(IOperation expression, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
            ArgumentsImpl = arguments;
        }
        protected override IOperation ExpressionImpl { get; }
        protected override ImmutableArray<IOperation> ArgumentsImpl { get; }
    }

    /// <remarks>
    /// Represents a dynamic indexer expression in C#.
    /// </remarks>
    internal sealed partial class LazyDynamicIndexerAccessExpression : BaseDynamicIndexerAccessExpression, IDynamicIndexerAccessOperation
    {
        private readonly Lazy<IOperation> _lazyExpression;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyArguments;
        public LazyDynamicIndexerAccessExpression(Lazy<IOperation> expression, Lazy<ImmutableArray<IOperation>> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
            _lazyArguments = arguments ?? throw new System.ArgumentNullException(nameof(arguments));
        }
        protected override IOperation ExpressionImpl => _lazyExpression.Value;
        protected override ImmutableArray<IOperation> ArgumentsImpl => _lazyArguments.Value;
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    internal abstract partial class BaseUnaryOperatorExpression : Operation, IUnaryOperation
    {
        protected BaseUnaryOperatorExpression(UnaryOperatorKind unaryOperationKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.UnaryOperator, semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperatorKind = unaryOperationKind;
            IsLifted = isLifted;
            IsChecked = isChecked;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// Kind of unary operation.
        /// </summary>
        public UnaryOperatorKind OperatorKind { get; }
        protected abstract IOperation OperandImpl { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
        /// <summary>
        /// <code>true</code> if this is a 'lifted' binary operator.  When there is an
        /// operator that is defined to work on a value type, 'lifted' operators are
        /// created to work on the <see cref="System.Nullable{T}"/> versions of those
        /// value types.
        /// </summary>
        public bool IsLifted { get; }
        /// <summary>
        /// <code>true</code> if overflow checking is performed for the arithmetic operation.
        /// </summary>
        public bool IsChecked { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operand != null)
                {
                    yield return Operand;
                }
            }
        }
        /// <summary>
        /// Single operand.
        /// </summary>
        public IOperation Operand => Operation.SetParentOperation(OperandImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitUnaryOperator(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitUnaryOperator(this, argument);
        }
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    internal sealed partial class UnaryOperatorExpression : BaseUnaryOperatorExpression, IUnaryOperation
    {
        public UnaryOperatorExpression(UnaryOperatorKind unaryOperationKind, IOperation operand, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(unaryOperationKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperandImpl = operand;
        }

        protected override IOperation OperandImpl { get; }
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    internal sealed partial class LazyUnaryOperatorExpression : BaseUnaryOperatorExpression, IUnaryOperation
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyUnaryOperatorExpression(UnaryOperatorKind unaryOperationKind, Lazy<IOperation> operand, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(unaryOperationKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException(nameof(operand));
        }

        protected override IOperation OperandImpl => _lazyOperand.Value;
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal abstract partial class BaseUsingStatement : Operation, IUsingOperation
    {
        protected BaseUsingStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.Using, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ResourcesImpl { get; }
        protected abstract IOperation BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Resources != null)
                {
                    yield return Resources;
                }
                if (Body != null)
                {
                    yield return Body;
                }
            }
        }

        /// <summary>
        /// Declaration introduced or resource held by the using.
        /// </summary>
        public IOperation Resources => Operation.SetParentOperation(ResourcesImpl, this);

        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        public IOperation Body => Operation.SetParentOperation(BodyImpl, this);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitUsing(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitUsing(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal sealed partial class UsingStatement : BaseUsingStatement, IUsingOperation
    {
        public UsingStatement(IOperation resources, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ResourcesImpl = resources;
            BodyImpl = body;
        }

        protected override IOperation ResourcesImpl { get; }
        protected override IOperation BodyImpl { get; }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal sealed partial class LazyUsingStatement : BaseUsingStatement, IUsingOperation
    {
        private readonly Lazy<IOperation> _lazyResources;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyUsingStatement(Lazy<IOperation> resources, Lazy<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyResources = resources ?? throw new System.ArgumentNullException(nameof(resources));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override IOperation ResourcesImpl => _lazyResources.Value;
        protected override IOperation BodyImpl => _lazyBody.Value;
    }

    internal abstract partial class BaseVariableDeclarator : Operation, IVariableDeclaratorOperation
    {
        protected BaseVariableDeclarator(ILocalSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.VariableDeclarator, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Symbol = symbol;
        }

        public ILocalSymbol Symbol { get; }

        protected abstract IVariableInitializerOperation InitializerImpl { get; }

        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        public IVariableInitializerOperation Initializer => Operation.SetParentOperation(InitializerImpl, this);

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableDeclarator(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableDeclarator(this, argument);
        }
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    internal sealed partial class VariableDeclarator : BaseVariableDeclarator
    {
        public VariableDeclarator(ILocalSymbol symbol, IVariableInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializerImpl = initializer;
        }

        protected override IVariableInitializerOperation InitializerImpl { get; }
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    internal sealed partial class LazyVariableDeclarator : BaseVariableDeclarator
    {
        private readonly Lazy<IVariableInitializerOperation> _lazyInitializer;

        public LazyVariableDeclarator(ILocalSymbol symbol, Lazy<IVariableInitializerOperation> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }

        protected override IVariableInitializerOperation InitializerImpl => _lazyInitializer.Value;
    }

    internal abstract partial class BaseVariableDeclaration : Operation, IVariableDeclarationOperation
    {
        protected BaseVariableDeclaration(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.VariableDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        public ImmutableArray<IVariableDeclaratorOperation> Declarators => Operation.SetParentOperation(DeclarationsImpl, this);
        protected abstract ImmutableArray<IVariableDeclaratorOperation> DeclarationsImpl { get; }

        protected abstract IVariableInitializerOperation InitializerImpl { get; }

        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        public IVariableInitializerOperation Initializer => Operation.SetParentOperation(InitializerImpl, this);

        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var declaration in Declarators)
                {
                    yield return declaration;
                }

                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableDeclaration(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableDeclaration(this, argument);
        }
    }

    internal partial class VariableDeclaration : BaseVariableDeclaration
    {
        public VariableDeclaration(ImmutableArray<IVariableDeclaratorOperation> declarations, IVariableInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            DeclarationsImpl = declarations;
            InitializerImpl = initializer;
        }

        protected override ImmutableArray<IVariableDeclaratorOperation> DeclarationsImpl { get; }

        protected override IVariableInitializerOperation InitializerImpl { get; }
    }

    internal partial class LazyVariableDeclaration : BaseVariableDeclaration
    {
        private readonly Lazy<ImmutableArray<IVariableDeclaratorOperation>> _lazyDeclarations;
        private readonly Lazy<IVariableInitializerOperation> _lazyInitializer;

        public LazyVariableDeclaration(Lazy<ImmutableArray<IVariableDeclaratorOperation>> declarations, Lazy<IVariableInitializerOperation> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyDeclarations = declarations;
            _lazyInitializer = initializer;
        }

        protected override ImmutableArray<IVariableDeclaratorOperation> DeclarationsImpl => _lazyDeclarations.Value;

        protected override IVariableInitializerOperation InitializerImpl => _lazyInitializer.Value;
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal abstract partial class BaseVariableDeclarationGroupOperation : Operation, IVariableDeclarationGroupOperation
    {
        protected BaseVariableDeclarationGroupOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.VariableDeclarationGroup, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IVariableDeclarationOperation> DeclarationsImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var declaration in Declarations)
                {
                    if (declaration != null)
                    {
                        yield return declaration;
                    }
                }
            }
        }
        /// <summary>
        /// Variables declared by the statement.
        /// </summary>
        public ImmutableArray<IVariableDeclarationOperation> Declarations => Operation.SetParentOperation(DeclarationsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableDeclarationGroup(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableDeclarationGroup(this, argument);
        }
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal sealed partial class VariableDeclarationGroupOperation : BaseVariableDeclarationGroupOperation, IVariableDeclarationGroupOperation
    {
        public VariableDeclarationGroupOperation(ImmutableArray<IVariableDeclarationOperation> declarations, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            DeclarationsImpl = declarations;
        }

        protected override ImmutableArray<IVariableDeclarationOperation> DeclarationsImpl { get; }
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal sealed partial class LazyVariableDeclarationGroupOperation : BaseVariableDeclarationGroupOperation, IVariableDeclarationGroupOperation
    {
        private readonly Lazy<ImmutableArray<IVariableDeclarationOperation>> _lazyDeclarations;

        public LazyVariableDeclarationGroupOperation(Lazy<ImmutableArray<IVariableDeclarationOperation>> declarations, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyDeclarations = declarations;
        }

        protected override ImmutableArray<IVariableDeclarationOperation> DeclarationsImpl => _lazyDeclarations.Value;
    }

    /// <summary>
    /// Represents a while or do while loop.
    /// <para>
    /// Current usage:
    ///  (1) C# 'while' and 'do while' loop statements.
    ///  (2) VB 'While', 'Do While' and 'Do Until' loop statements.
    /// </para>
    /// </summary>
    internal abstract partial class BaseWhileLoopStatement : LoopStatement, IWhileLoopOperation
    {
        public BaseWhileLoopStatement(ImmutableArray<ILocalSymbol> locals, bool conditionIsTop, bool conditionIsUntil, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(LoopKind.While, locals, OperationKind.Loop, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ConditionIsTop = conditionIsTop;
            ConditionIsUntil = conditionIsUntil;
        }
        protected abstract IOperation ConditionImpl { get; }
        protected abstract IOperation IgnoredConditionImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (ConditionIsTop)
                {
                    if (Condition != null)
                    {
                        yield return Condition;
                    }
                }
                if (Body != null)
                {
                    yield return Body;
                }
                if (!ConditionIsTop)
                {
                    if (Condition != null)
                    {
                        yield return Condition;
                    }
                }
                if (IgnoredCondition != null)
                {
                    yield return IgnoredCondition;
                }
            }
        }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public IOperation Condition => Operation.SetParentOperation(ConditionImpl, this);
        /// <summary>
        /// True if the <see cref="Condition"/> is evaluated at start of each loop iteration.
        /// False if it is evaluated at the end of each loop iteration.
        /// </summary>

        public bool ConditionIsTop { get; }

        /// <summary>
        /// True if the loop has 'Until' loop semantics and the loop is executed while <see cref="Condition"/> is false.
        /// </summary>

        public bool ConditionIsUntil { get; }
        /// <summary>
        /// Additional conditional supplied for loop in error cases, which is ignored by the compiler.
        /// For example, for VB 'Do While' or 'Do Until' loop with syntax errors where both the top and bottom conditions are provided.
        /// The top condition is preferred and exposed as <see cref="Condition"/> and the bottom condition is ignored and exposed by this property.
        /// This property should be null for all non-error cases.
        /// </summary>
        public IOperation IgnoredCondition => Operation.SetParentOperation(IgnoredConditionImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitWhileLoop(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitWhileLoop(this, argument);
        }
    }

    /// <summary>
    /// Represents a while or do while loop.
    /// <para>
    /// Current usage:
    ///  (1) C# 'while' and 'do while' loop statements.
    ///  (2) VB 'While', 'Do While' and 'Do Until' loop statements.
    /// </para>
    /// </summary>
    internal sealed partial class WhileLoopStatement : BaseWhileLoopStatement, IWhileLoopOperation
    {
        public WhileLoopStatement(IOperation condition, IOperation body, IOperation ignoredCondition, ImmutableArray<ILocalSymbol> locals, bool conditionIsTop, bool conditionIsUntil, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, conditionIsTop, conditionIsUntil, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ConditionImpl = condition;
            BodyImpl = body;
            IgnoredConditionImpl = ignoredCondition;
        }
        protected override IOperation ConditionImpl { get; }
        protected override IOperation BodyImpl { get; }
        protected override IOperation IgnoredConditionImpl { get; }
    }

    /// <summary>
    /// Represents a while or do while loop.
    /// <para>
    /// Current usage:
    ///  (1) C# 'while' and 'do while' loop statements.
    ///  (2) VB 'While', 'Do While' and 'Do Until' loop statements.
    /// </para>
    /// </summary>
    internal sealed partial class LazyWhileLoopStatement : BaseWhileLoopStatement, IWhileLoopOperation
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyBody;
        private readonly Lazy<IOperation> _lazyIgnoredCondition;

        public LazyWhileLoopStatement(Lazy<IOperation> condition, Lazy<IOperation> body, Lazy<IOperation> ignoredCondition, ImmutableArray<ILocalSymbol> locals, bool conditionIsTop, bool conditionIsUntil, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, conditionIsTop, conditionIsUntil, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyCondition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
            _lazyIgnoredCondition = ignoredCondition ?? throw new System.ArgumentNullException(nameof(ignoredCondition));
        }
        protected override IOperation ConditionImpl => _lazyCondition.Value;
        protected override IOperation BodyImpl => _lazyBody.Value;
        protected override IOperation IgnoredConditionImpl => _lazyIgnoredCondition.Value;
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    internal abstract partial class BaseWithStatement : Operation, IWithOperation
    {
        protected BaseWithStatement(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            // https://github.com/dotnet/roslyn/issues/22005
            // base(OperationKind.With, semanticModel, syntax, type, constantValue, isImplicit)
            base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation BodyImpl { get; }
        protected abstract IOperation ValueImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value != null)
                {
                    yield return Value;
                }
                if (Body != null)
                {
                    yield return Body;
                }
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
            visitor.VisitWith(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitWith(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    internal sealed partial class WithStatement : BaseWithStatement, IWithOperation
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
    internal sealed partial class LazyWithStatement : BaseWithStatement, IWithOperation
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
    internal abstract partial class BaseLocalFunctionStatement : Operation, ILocalFunctionOperation
    {
        protected BaseLocalFunctionStatement(IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.LocalFunction, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Symbol = symbol;
        }
        /// <summary>
        /// Local function symbol.
        /// </summary>
        public IMethodSymbol Symbol { get; }
        protected abstract IBlockOperation BodyImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Body != null)
                {
                    yield return Body;
                }
            }
        }
        /// <summary>
        /// Body of the local function.
        /// </summary>
        public IBlockOperation Body => Operation.SetParentOperation(BodyImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLocalFunction(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLocalFunction(this, argument);
        }
    }

    /// <summary>
    /// Represents a local function statement.
    /// </summary>
    internal sealed partial class LocalFunctionStatement : BaseLocalFunctionStatement, ILocalFunctionOperation
    {
        public LocalFunctionStatement(IMethodSymbol symbol, IBlockOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            BodyImpl = body;
        }

        protected override IBlockOperation BodyImpl { get; }
    }

    /// <summary>
    /// Represents a local function statement.
    /// </summary>
    internal sealed partial class LazyLocalFunctionStatement : BaseLocalFunctionStatement, ILocalFunctionOperation
    {
        private readonly Lazy<IBlockOperation> _lazyBody;

        public LazyLocalFunctionStatement(IMethodSymbol symbol, Lazy<IBlockOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        protected override IBlockOperation BodyImpl => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# constant pattern.
    /// </summary>
    internal abstract partial class BaseConstantPattern : Operation, IConstantPatternOperation
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
                if (Value != null)
                {
                    yield return Value;
                }
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
    internal sealed partial class ConstantPattern : BaseConstantPattern, IConstantPatternOperation
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
    internal sealed partial class LazyConstantPattern : BaseConstantPattern, IConstantPatternOperation
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
    internal sealed partial class DeclarationPattern : Operation, IDeclarationPatternOperation
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
    internal abstract partial class BasePatternCaseClause : CaseClause, IPatternCaseClauseOperation
    {
        protected BasePatternCaseClause(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(CaseKind.Pattern, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Label = label;
        }
        /// <summary>
        /// Label associated with the case clause.
        /// </summary>
        public ILabelSymbol Label { get; }
        protected abstract IPatternOperation PatternImpl { get; }
        protected abstract IOperation GuardExpressionImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Pattern != null)
                {
                    yield return Pattern;
                }
                if (Guard != null)
                {
                    yield return Guard;
                }
            }
        }
        /// <summary>
        /// Pattern associated with case clause.
        /// </summary>
        public IPatternOperation Pattern => Operation.SetParentOperation(PatternImpl, this);
        /// <summary>
        /// Guard expression associated with the pattern case clause.
        /// </summary>
        public IOperation Guard => Operation.SetParentOperation(GuardExpressionImpl, this);
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
    internal sealed partial class PatternCaseClause : BasePatternCaseClause, IPatternCaseClauseOperation
    {
        public PatternCaseClause(ILabelSymbol label, IPatternOperation pattern, IOperation guardExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            PatternImpl = pattern;
            GuardExpressionImpl = guardExpression;
        }

        protected override IPatternOperation PatternImpl { get; }
        protected override IOperation GuardExpressionImpl { get; }
    }

    /// <summary>
    /// Represents a C# pattern case clause.
    /// </summary>
    internal sealed partial class LazyPatternCaseClause : BasePatternCaseClause, IPatternCaseClauseOperation
    {
        private readonly Lazy<IPatternOperation> _lazyPattern;
        private readonly Lazy<IOperation> _lazyGuardExpression;

        public LazyPatternCaseClause(ILabelSymbol label, Lazy<IPatternOperation> lazyPattern, Lazy<IOperation> lazyGuardExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyPattern = lazyPattern ?? throw new System.ArgumentNullException(nameof(lazyPattern));
            _lazyGuardExpression = lazyGuardExpression ?? throw new System.ArgumentNullException(nameof(lazyGuardExpression));
        }

        protected override IPatternOperation PatternImpl => _lazyPattern.Value;

        protected override IOperation GuardExpressionImpl => _lazyGuardExpression.Value;
    }

    /// <summary>
    /// Represents a C# is pattern expression. For example, "x is int i".
    /// </summary>
    internal abstract partial class BaseIsPatternExpression : Operation, IIsPatternOperation
    {
        protected BaseIsPatternExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.IsPattern, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation ExpressionImpl { get; }
        protected abstract IPatternOperation PatternImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value != null)
                {
                    yield return Value;
                }
                if (Pattern != null)
                {
                    yield return Pattern;
                }
            }
        }
        /// <summary>
        /// Expression.
        /// </summary>
        public IOperation Value => CodeAnalysis.Operation.SetParentOperation(ExpressionImpl, this);
        /// <summary>
        /// Pattern.
        /// </summary>
        public IPatternOperation Pattern => CodeAnalysis.Operation.SetParentOperation(PatternImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIsPattern(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIsPattern(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# is pattern expression. For example, "x is int i".
    /// </summary>
    internal sealed partial class IsPatternExpression : BaseIsPatternExpression, IIsPatternOperation
    {
        public IsPatternExpression(IOperation expression, IPatternOperation pattern, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
            PatternImpl = pattern;
        }

        protected override IOperation ExpressionImpl { get; }
        protected override IPatternOperation PatternImpl { get; }
    }

    /// <summary>
    /// Represents a C# is pattern expression. For example, "x is int i".
    /// </summary>
    internal sealed partial class LazyIsPatternExpression : BaseIsPatternExpression, IIsPatternOperation
    {
        private readonly Lazy<IOperation> _lazyExpression;
        private readonly Lazy<IPatternOperation> _lazyPattern;

        public LazyIsPatternExpression(Lazy<IOperation> lazyExpression, Lazy<IPatternOperation> lazyPattern, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = lazyExpression ?? throw new System.ArgumentNullException(nameof(lazyExpression));
            _lazyPattern = lazyPattern ?? throw new System.ArgumentNullException(nameof(lazyPattern));
        }

        protected override IOperation ExpressionImpl => _lazyExpression.Value;

        protected override IPatternOperation PatternImpl => _lazyPattern.Value;
    }

    /// <summary>
    /// Represents a C# or VB object or collection initializer expression.
    /// </summary>
    internal abstract partial class BaseObjectOrCollectionInitializerExpression : Operation, IObjectOrCollectionInitializerOperation
    {
        protected BaseObjectOrCollectionInitializerExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.ObjectOrCollectionInitializer, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> InitializersImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var initializer in Initializers)
                {
                    if (initializer != null)
                    {
                        yield return initializer;
                    }
                }
            }
        }
        /// <summary>
        /// Object member or collection initializers.
        /// </summary>
        public ImmutableArray<IOperation> Initializers => Operation.SetParentOperation(InitializersImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitObjectOrCollectionInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitObjectOrCollectionInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB object or collection initializer expression.
    /// </summary>
    internal sealed partial class ObjectOrCollectionInitializerExpression : BaseObjectOrCollectionInitializerExpression, IObjectOrCollectionInitializerOperation
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
    internal sealed partial class LazyObjectOrCollectionInitializerExpression : BaseObjectOrCollectionInitializerExpression, IObjectOrCollectionInitializerOperation
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
    internal abstract partial class BaseMemberInitializerExpression : Operation, IMemberInitializerOperation
    {
        protected BaseMemberInitializerExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.MemberInitializer, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation InitializedMemberImpl { get; }
        protected abstract IObjectOrCollectionInitializerOperation InitializerImpl { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (InitializedMember != null)
                {
                    yield return InitializedMember;
                }
                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }
        /// <summary>
        /// Initialized member.
        /// </summary>
        public IOperation InitializedMember => Operation.SetParentOperation(InitializedMemberImpl, this);

        /// <summary>
        /// Member initializer.
        /// </summary>
        public IObjectOrCollectionInitializerOperation Initializer => Operation.SetParentOperation(InitializerImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitMemberInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitMemberInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB member initializer expression within an object initializer expression.
    /// </summary>
    internal sealed partial class MemberInitializerExpression : BaseMemberInitializerExpression, IMemberInitializerOperation
    {
        public MemberInitializerExpression(IOperation initializedMember, IObjectOrCollectionInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializedMemberImpl = initializedMember;
            InitializerImpl = initializer;
        }

        protected override IOperation InitializedMemberImpl { get; }
        protected override IObjectOrCollectionInitializerOperation InitializerImpl { get; }
    }

    /// <summary>
    /// Represents a C# or VB member initializer expression within an object initializer expression.
    /// </summary>
    internal sealed partial class LazyMemberInitializerExpression : BaseMemberInitializerExpression, IMemberInitializerOperation
    {
        private readonly Lazy<IOperation> _lazyInitializedMember;
        private readonly Lazy<IObjectOrCollectionInitializerOperation> _lazyInitializer;

        public LazyMemberInitializerExpression(Lazy<IOperation> initializedMember, Lazy<IObjectOrCollectionInitializerOperation> initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyInitializedMember = initializedMember ?? throw new System.ArgumentNullException(nameof(initializedMember));
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }

        protected override IOperation InitializedMemberImpl => _lazyInitializedMember.Value;

        protected override IObjectOrCollectionInitializerOperation InitializerImpl => _lazyInitializer.Value;
    }

    /// <summary>
    /// Represents a C# nested collection element initializer expression within a collection initializer.
    /// </summary>
    internal abstract partial class BaseCollectionElementInitializerExpression : Operation, ICollectionElementInitializerOperation
    {
        protected BaseCollectionElementInitializerExpression(IMethodSymbol addMethod, bool isDynamic, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.CollectionElementInitializer, semanticModel, syntax, type, constantValue, isImplicit)
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
                    if (argument != null)
                    {
                        yield return argument;
                    }
                }
            }
        }
        /// <summary>
        /// Arguments passed to add method invocation.
        /// </summary>
        public ImmutableArray<IOperation> Arguments => Operation.SetParentOperation(ArgumentsImpl, this);
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCollectionElementInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCollectionElementInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# nested collection element initializer expression within a collection initializer.
    /// </summary>
    internal sealed partial class CollectionElementInitializerExpression : BaseCollectionElementInitializerExpression, ICollectionElementInitializerOperation
    {
        public CollectionElementInitializerExpression(IMethodSymbol addMethod, bool isDynamic, ImmutableArray<IOperation> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(addMethod, isDynamic, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArgumentsImpl = arguments;
        }

        protected override ImmutableArray<IOperation> ArgumentsImpl { get; }
    }

    /// <summary>
    /// Represents a C# nested collection element initializer expression within a collection initializer.
    /// </summary>
    internal sealed partial class LazyCollectionElementInitializerExpression : BaseCollectionElementInitializerExpression, ICollectionElementInitializerOperation
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyArguments;

        public LazyCollectionElementInitializerExpression(IMethodSymbol addMethod, bool isDynamic, Lazy<ImmutableArray<IOperation>> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(addMethod, isDynamic, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyArguments = arguments ?? throw new System.ArgumentNullException(nameof(arguments));
        }

        protected override ImmutableArray<IOperation> ArgumentsImpl => _lazyArguments.Value;
    }

    /// <summary>
    /// Represents an unrolled/lowered query expression in C# and VB.
    /// For example, for the query expression "from x in set where x.Name != null select x.Name", the Operation tree has the following shape:
    ///   ITranslatedQueryExpression
    ///     IInvocationExpression ('Select' invocation for "select x.Name")
    ///       IInvocationExpression ('Where' invocation for "where x.Name != null")
    ///         IInvocationExpression ('From' invocation for "from x in set")
    /// </summary>
    internal abstract partial class BaseTranslatedQueryExpression : Operation, ITranslatedQueryOperation
    {
        protected BaseTranslatedQueryExpression(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
                    base(OperationKind.TranslatedQuery, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        protected abstract IOperation ExpressionImpl { get; }
        /// <summary>
        /// Underlying unrolled expression.
        /// </summary>
        public IOperation Operation => CodeAnalysis.Operation.SetParentOperation(ExpressionImpl, this);
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation != null)
                {
                    yield return Operation;
                }
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTranslatedQuery(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTranslatedQuery(this, argument);
        }
    }

    /// <summary>
    /// Represents an unrolled/lowered query expression in C# and VB.
    /// For example, for the query expression "from x in set where x.Name != null select x.Name", the Operation tree has the following shape:
    ///   ITranslatedQueryExpression
    ///     IInvocationExpression ('Select' invocation for "select x.Name")
    ///       IInvocationExpression ('Where' invocation for "where x.Name != null")
    ///         IInvocationExpression ('From' invocation for "from x in set")
    /// </summary>
    internal sealed partial class TranslatedQueryExpression : BaseTranslatedQueryExpression, ITranslatedQueryOperation
    {
        public TranslatedQueryExpression(IOperation expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExpressionImpl = expression;
        }
        protected override IOperation ExpressionImpl { get; }
    }

    /// <summary>
    /// Represents an unrolled/lowered query expression in C# and VB.
    /// For example, for the query expression "from x in set where x.Name != null select x.Name", the Operation tree has the following shape:
    ///   ITranslatedQueryExpression
    ///     IInvocationExpression ('Select' invocation for "select x.Name")
    ///       IInvocationExpression ('Where' invocation for "where x.Name != null")
    ///         IInvocationExpression ('From' invocation for "from x in set")
    /// </summary>
    internal sealed partial class LazyTranslatedQueryExpression : BaseTranslatedQueryExpression, ITranslatedQueryOperation
    {
        private readonly Lazy<IOperation> _lazyExpression;

        public LazyTranslatedQueryExpression(Lazy<IOperation> expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
        }
        protected override IOperation ExpressionImpl => _lazyExpression.Value;
    }
}
