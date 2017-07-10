// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an expression that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal abstract partial class BaseAddressOfExpression : Operation, IAddressOfExpression
    {
        protected BaseAddressOfExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.AddressOfExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Addressed reference.
        /// </summary>
        public abstract IOperation Reference { get; }
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
        public AddressOfExpression(IOperation reference, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Reference = reference;
        }
        /// <summary>
        /// Addressed reference.
        /// </summary>
        public override IOperation Reference { get; }
    }
    /// <summary>
    /// Represents an expression that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal sealed partial class LazyAddressOfExpression : BaseAddressOfExpression, IAddressOfExpression
    {
        private readonly Lazy<IOperation> _lazyReference;

        public LazyAddressOfExpression(Lazy<IOperation> reference, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyReference = reference ?? throw new System.ArgumentNullException(nameof(reference));
        }
        /// <summary>
        /// Addressed reference.
        /// </summary>
        public override IOperation Reference => _lazyReference.Value;
    }

    /// <summary>
    /// Represents an argument in a method invocation.
    /// </summary>
    internal abstract partial class BaseArgument : Operation, IArgument
    {
        protected BaseArgument(ArgumentKind argumentKind, IParameterSymbol parameter, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.Argument, isInvalid, syntax, type, constantValue)
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
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        public abstract IOperation Value { get; }
        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        public abstract IOperation InConversion { get; }
        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        public abstract IOperation OutConversion { get; }
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
        public Argument(ArgumentKind argumentKind, IParameterSymbol parameter, IOperation value, IOperation inConversion, IOperation outConversion, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(argumentKind, parameter, isInvalid, syntax, type, constantValue)
        {
            Value = value;
            InConversion = inConversion;
            OutConversion = outConversion;
        }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        public override IOperation Value { get; }
        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        public override IOperation InConversion { get; }
        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        public override IOperation OutConversion { get; }
    }

    /// <summary>
    /// Represents an argument in a method invocation.
    /// </summary>
    internal sealed partial class LazyArgument : BaseArgument, IArgument
    {
        private readonly Lazy<IOperation> _lazyValue;
        private readonly Lazy<IOperation> _lazyInConversion;
        private readonly Lazy<IOperation> _lazyOutConversion;

        public LazyArgument(ArgumentKind argumentKind, IParameterSymbol parameter, Lazy<IOperation> value, Lazy<IOperation> inConversion, Lazy<IOperation> outConversion, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(argumentKind, parameter, isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
            _lazyInConversion = inConversion ?? throw new System.ArgumentNullException(nameof(inConversion));
            _lazyOutConversion = outConversion ?? throw new System.ArgumentNullException(nameof(outConversion));
        }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;

        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        public override IOperation InConversion => _lazyInConversion.Value;

        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        public override IOperation OutConversion => _lazyOutConversion.Value;
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    internal abstract partial class BaseArrayCreationExpression : Operation, IArrayCreationExpression
    {
        protected BaseArrayCreationExpression(ITypeSymbol elementType, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ArrayCreationExpression, isInvalid, syntax, type, constantValue)
        {
            ElementType = elementType;
        }
        /// <summary>
        /// Element type of the created array instance.
        /// </summary>
        public ITypeSymbol ElementType { get; }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        public abstract ImmutableArray<IOperation> DimensionSizes { get; }
        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        public abstract IArrayInitializer Initializer { get; }
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
        public ArrayCreationExpression(ITypeSymbol elementType, ImmutableArray<IOperation> dimensionSizes, IArrayInitializer initializer, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(elementType, isInvalid, syntax, type, constantValue)
        {
            DimensionSizes = dimensionSizes;
            Initializer = initializer;
        }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        public override ImmutableArray<IOperation> DimensionSizes { get; }
        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        public override IArrayInitializer Initializer { get; }
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    internal sealed partial class LazyArrayCreationExpression : BaseArrayCreationExpression, IArrayCreationExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyDimensionSizes;
        private readonly Lazy<IArrayInitializer> _lazyInitializer;

        public LazyArrayCreationExpression(ITypeSymbol elementType, Lazy<ImmutableArray<IOperation>> dimensionSizes, Lazy<IArrayInitializer> initializer, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(elementType, isInvalid, syntax, type, constantValue)
        {
            _lazyDimensionSizes = dimensionSizes;
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        public override ImmutableArray<IOperation> DimensionSizes => _lazyDimensionSizes.Value;

        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        public override IArrayInitializer Initializer => _lazyInitializer.Value;
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    internal abstract partial class BaseArrayElementReferenceExpression : Operation, IArrayElementReferenceExpression
    {
        protected BaseArrayElementReferenceExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ArrayElementReferenceExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Array to be indexed.
        /// </summary>
        public abstract IOperation ArrayReference { get; }
        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        public abstract ImmutableArray<IOperation> Indices { get; }
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
        public ArrayElementReferenceExpression(IOperation arrayReference, ImmutableArray<IOperation> indices, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            ArrayReference = arrayReference;
            Indices = indices;
        }
        /// <summary>
        /// Array to be indexed.
        /// </summary>
        public override IOperation ArrayReference { get; }
        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        public override ImmutableArray<IOperation> Indices { get; }
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    internal sealed partial class LazyArrayElementReferenceExpression : BaseArrayElementReferenceExpression, IArrayElementReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyArrayReference;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyIndices;

        public LazyArrayElementReferenceExpression(Lazy<IOperation> arrayReference, Lazy<ImmutableArray<IOperation>> indices, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyArrayReference = arrayReference ?? throw new System.ArgumentNullException(nameof(arrayReference));
            _lazyIndices = indices;
        }
        /// <summary>
        /// Array to be indexed.
        /// </summary>
        public override IOperation ArrayReference => _lazyArrayReference.Value;

        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        public override ImmutableArray<IOperation> Indices => _lazyIndices.Value;
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    internal abstract partial class BaseArrayInitializer : Operation, IArrayInitializer
    {
        protected BaseArrayInitializer(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ArrayInitializer, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        public abstract ImmutableArray<IOperation> ElementValues { get; }
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
        public ArrayInitializer(ImmutableArray<IOperation> elementValues, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            ElementValues = elementValues;
        }
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        public override ImmutableArray<IOperation> ElementValues { get; }
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    internal sealed partial class LazyArrayInitializer : BaseArrayInitializer, IArrayInitializer
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyElementValues;

        public LazyArrayInitializer(Lazy<ImmutableArray<IOperation>> elementValues, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyElementValues = elementValues;
        }
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        public override ImmutableArray<IOperation> ElementValues => _lazyElementValues.Value;
    }

    /// <summary>
    /// Represents an base type of assignment expression.
    /// </summary>
    internal abstract partial class AssignmentExpression : Operation, IAssignmentExpression
    {
        protected AssignmentExpression(OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public abstract IOperation Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public abstract IOperation Value { get; }
    }

    /// <summary>
    /// Represents a simple assignment expression.
    /// </summary>
    internal abstract partial class BaseSimpleAssignmentExpression : AssignmentExpression, ISimpleAssignmentExpression
    {
        public BaseSimpleAssignmentExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.SimpleAssignmentExpression, isInvalid, syntax, type, constantValue)
        {
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
        public SimpleAssignmentExpression(IOperation target, IOperation value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Target = target;
            Value = value;
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents a simple assignment expression.
    /// </summary>
    internal sealed partial class LazySimpleAssignmentExpression : BaseSimpleAssignmentExpression, ISimpleAssignmentExpression
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;

        public LazySimpleAssignmentExpression(Lazy<IOperation> target, Lazy<IOperation> value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException(nameof(target));
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target => _lazyTarget.Value;

        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal abstract partial class BaseAwaitExpression : Operation, IAwaitExpression
    {
        protected BaseAwaitExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.AwaitExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be awaited.
        /// </summary>
        public abstract IOperation AwaitedValue { get; }
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
        public AwaitExpression(IOperation awaitedValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            AwaitedValue = awaitedValue;
        }
        /// <summary>
        /// Value to be awaited.
        /// </summary>
        public override IOperation AwaitedValue { get; }
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal sealed partial class LazyAwaitExpression : BaseAwaitExpression, IAwaitExpression
    {
        private readonly Lazy<IOperation> _lazyAwaitedValue;

        public LazyAwaitExpression(Lazy<IOperation> awaitedValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyAwaitedValue = awaitedValue ?? throw new System.ArgumentNullException(nameof(awaitedValue));
        }
        /// <summary>
        /// Value to be awaited.
        /// </summary>
        public override IOperation AwaitedValue => _lazyAwaitedValue.Value;
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    internal abstract partial class BaseBinaryOperatorExpression : Operation, IHasOperatorMethodExpression, IBinaryOperatorExpression
    {
        protected BaseBinaryOperatorExpression(BinaryOperationKind binaryOperationKind, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.BinaryOperatorExpression, isInvalid, syntax, type, constantValue)
        {
            BinaryOperationKind = binaryOperationKind;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        public BinaryOperationKind BinaryOperationKind { get; }
        /// <summary>
        /// Left operand.
        /// </summary>
        public abstract IOperation LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        public abstract IOperation RightOperand { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
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
        public BinaryOperatorExpression(BinaryOperationKind binaryOperationKind, IOperation leftOperand, IOperation rightOperand, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(binaryOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            LeftOperand = leftOperand;
            RightOperand = rightOperand;
        }
        /// <summary>
        /// Left operand.
        /// </summary>
        public override IOperation LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        public override IOperation RightOperand { get; }
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    internal sealed partial class LazyBinaryOperatorExpression : BaseBinaryOperatorExpression, IHasOperatorMethodExpression, IBinaryOperatorExpression
    {
        private readonly Lazy<IOperation> _lazyLeftOperand;
        private readonly Lazy<IOperation> _lazyRightOperand;

        public LazyBinaryOperatorExpression(BinaryOperationKind binaryOperationKind, Lazy<IOperation> leftOperand, Lazy<IOperation> rightOperand, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(binaryOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            _lazyLeftOperand = leftOperand ?? throw new System.ArgumentNullException(nameof(leftOperand));
            _lazyRightOperand = rightOperand ?? throw new System.ArgumentNullException(nameof(rightOperand));
        }
        /// <summary>
        /// Left operand.
        /// </summary>
        public override IOperation LeftOperand => _lazyLeftOperand.Value;

        /// <summary>
        /// Right operand.
        /// </summary>
        public override IOperation RightOperand => _lazyRightOperand.Value;
    }

    /// <summary>
    /// Represents a block scope.
    /// </summary>
    internal abstract partial class BaseBlockStatement : Operation, IBlockStatement
    {
        protected BaseBlockStatement(ImmutableArray<ILocalSymbol> locals, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.BlockStatement, isInvalid, syntax, type, constantValue)
        {
            Locals = locals;
        }
        /// <summary>
        /// Statements contained within the block.
        /// </summary>
        public abstract ImmutableArray<IOperation> Statements { get; }
        /// <summary>
        /// Local declarations contained within the block.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Locals { get; }
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
        public BlockStatement(ImmutableArray<IOperation> statements, ImmutableArray<ILocalSymbol> locals, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(locals, isInvalid, syntax, type, constantValue)
        {
            Statements = statements;
        }
        /// <summary>
        /// Statements contained within the block.
        /// </summary>
        public override ImmutableArray<IOperation> Statements { get; }
    }

    /// <summary>
    /// Represents a block scope.
    /// </summary>
    internal sealed partial class LazyBlockStatement : BaseBlockStatement, IBlockStatement
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyStatements;

        public LazyBlockStatement(Lazy<ImmutableArray<IOperation>> statements, ImmutableArray<ILocalSymbol> locals, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(locals, isInvalid, syntax, type, constantValue)
        {
            _lazyStatements = statements;
        }
        /// <summary>
        /// Statements contained within the block.
        /// </summary>
        public override ImmutableArray<IOperation> Statements => _lazyStatements.Value;
    }

    /// <summary>
    /// Represents a C# goto, break, or continue statement, or a VB GoTo, Exit ***, or Continue *** statement
    /// </summary>
    internal sealed partial class BranchStatement : Operation, IBranchStatement
    {
        public BranchStatement(ILabelSymbol target, BranchKind branchKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.BranchStatement, isInvalid, syntax, type, constantValue)
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
        protected CaseClause(CaseKind caseKind, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
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
        protected BaseCatchClause(ITypeSymbol caughtType, ILocalSymbol exceptionLocal, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.CatchClause, isInvalid, syntax, type, constantValue)
        {
            CaughtType = caughtType;
            ExceptionLocal = exceptionLocal;
        }
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        public abstract IBlockStatement Handler { get; }
        /// <summary>
        /// Type of exception to be handled.
        /// </summary>
        public ITypeSymbol CaughtType { get; }
        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        public abstract IOperation Filter { get; }
        /// <summary>
        /// Symbol for the local catch variable bound to the caught exception.
        /// </summary>
        public ILocalSymbol ExceptionLocal { get; }
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
        public CatchClause(IBlockStatement handler, ITypeSymbol caughtType, IOperation filter, ILocalSymbol exceptionLocal, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(caughtType, exceptionLocal, isInvalid, syntax, type, constantValue)
        {
            Handler = handler;
            Filter = filter;
        }
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        public override IBlockStatement Handler { get; }
        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        public override IOperation Filter { get; }
    }

    /// <summary>
    /// Represents a C# catch or VB Catch clause.
    /// </summary>
    internal sealed partial class LazyCatchClause : BaseCatchClause, ICatchClause
    {
        private readonly Lazy<IBlockStatement> _lazyHandler;
        private readonly Lazy<IOperation> _lazyFilter;

        public LazyCatchClause(Lazy<IBlockStatement> handler, ITypeSymbol caughtType, Lazy<IOperation> filter, ILocalSymbol exceptionLocal, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(caughtType, exceptionLocal, isInvalid, syntax, type, constantValue)
        {
            _lazyHandler = handler ?? throw new System.ArgumentNullException(nameof(handler));
            _lazyFilter = filter ?? throw new System.ArgumentNullException(nameof(filter));
        }
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        public override IBlockStatement Handler => _lazyHandler.Value;

        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        public override IOperation Filter => _lazyFilter.Value;
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    internal abstract partial class BaseCompoundAssignmentExpression : AssignmentExpression, IHasOperatorMethodExpression, ICompoundAssignmentExpression
    {
        protected BaseCompoundAssignmentExpression(BinaryOperationKind binaryOperationKind, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.CompoundAssignmentExpression, isInvalid, syntax, type, constantValue)
        {
            BinaryOperationKind = binaryOperationKind;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        public BinaryOperationKind BinaryOperationKind { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }

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
        public CompoundAssignmentExpression(BinaryOperationKind binaryOperationKind, IOperation target, IOperation value, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(binaryOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            Target = target;
            Value = value;
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    internal sealed partial class LazyCompoundAssignmentExpression : BaseCompoundAssignmentExpression, IHasOperatorMethodExpression, ICompoundAssignmentExpression
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyCompoundAssignmentExpression(BinaryOperationKind binaryOperationKind, Lazy<IOperation> target, Lazy<IOperation> value, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(binaryOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException(nameof(target));
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target => _lazyTarget.Value;

        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    internal abstract partial class BaseConditionalAccessExpression : Operation, IConditionalAccessExpression
    {
        protected BaseConditionalAccessExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ConditionalAccessExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Expression to be evaluated if the conditional instance is non null.
        /// </summary>
        public abstract IOperation ConditionalValue { get; }
        /// <summary>
        /// Expresson that is conditionally accessed.
        /// </summary>
        public abstract IOperation ConditionalInstance { get; }
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
        public ConditionalAccessExpression(IOperation conditionalValue, IOperation conditionalInstance, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            ConditionalValue = conditionalValue;
            ConditionalInstance = conditionalInstance;
        }
        /// <summary>
        /// Expression to be evaluated if the conditional instance is non null.
        /// </summary>
        public override IOperation ConditionalValue { get; }
        /// <summary>
        /// Expresson that is conditionally accessed.
        /// </summary>
        public override IOperation ConditionalInstance { get; }
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    internal sealed partial class LazyConditionalAccessExpression : BaseConditionalAccessExpression, IConditionalAccessExpression
    {
        private readonly Lazy<IOperation> _lazyConditionalValue;
        private readonly Lazy<IOperation> _lazyConditionalInstance;

        public LazyConditionalAccessExpression(Lazy<IOperation> conditionalValue, Lazy<IOperation> conditionalInstance, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyConditionalValue = conditionalValue ?? throw new System.ArgumentNullException(nameof(conditionalValue));
            _lazyConditionalInstance = conditionalInstance ?? throw new System.ArgumentNullException(nameof(conditionalInstance));
        }
        /// <summary>
        /// Expression to be evaluated if the conditional instance is non null.
        /// </summary>
        public override IOperation ConditionalValue => _lazyConditionalValue.Value;

        /// <summary>
        /// Expresson that is conditionally accessed.
        /// </summary>
        public override IOperation ConditionalInstance => _lazyConditionalInstance.Value;
    }

    /// <summary>
    /// Represents the value of a conditionally-accessed expression within an expression containing a conditional access.
    /// </summary>
    internal sealed partial class ConditionalAccessInstanceExpression : Operation, IConditionalAccessInstanceExpression
    {
        public ConditionalAccessInstanceExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.ConditionalAccessInstanceExpression, isInvalid, syntax, type, constantValue)
        {
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
    internal abstract partial class BaseConditionalChoiceExpression : Operation, IConditionalChoiceExpression
    {
        protected BaseConditionalChoiceExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ConditionalChoiceExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        public abstract IOperation Condition { get; }
        /// <summary>
        /// Value evaluated if the Condition is true.
        /// </summary>
        public abstract IOperation IfTrueValue { get; }
        /// <summary>
        /// Value evaluated if the Condition is false.
        /// </summary>
        public abstract IOperation IfFalseValue { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalChoiceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalChoiceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    internal sealed partial class ConditionalChoiceExpression : BaseConditionalChoiceExpression, IConditionalChoiceExpression
    {
        public ConditionalChoiceExpression(IOperation condition, IOperation ifTrueValue, IOperation ifFalseValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Condition = condition;
            IfTrueValue = ifTrueValue;
            IfFalseValue = ifFalseValue;
        }
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        public override IOperation Condition { get; }
        /// <summary>
        /// Value evaluated if the Condition is true.
        /// </summary>
        public override IOperation IfTrueValue { get; }
        /// <summary>
        /// Value evaluated if the Condition is false.
        /// </summary>
        public override IOperation IfFalseValue { get; }
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    internal sealed partial class LazyConditionalChoiceExpression : BaseConditionalChoiceExpression, IConditionalChoiceExpression
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyIfTrueValue;
        private readonly Lazy<IOperation> _lazyIfFalseValue;

        public LazyConditionalChoiceExpression(Lazy<IOperation> condition, Lazy<IOperation> ifTrueValue, Lazy<IOperation> ifFalseValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyCondition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            _lazyIfTrueValue = ifTrueValue ?? throw new System.ArgumentNullException(nameof(ifTrueValue));
            _lazyIfFalseValue = ifFalseValue ?? throw new System.ArgumentNullException(nameof(ifFalseValue));
        }
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        public override IOperation Condition => _lazyCondition.Value;

        /// <summary>
        /// Value evaluated if the Condition is true.
        /// </summary>
        public override IOperation IfTrueValue => _lazyIfTrueValue.Value;

        /// <summary>
        /// Value evaluated if the Condition is false.
        /// </summary>
        public override IOperation IfFalseValue => _lazyIfFalseValue.Value;
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    internal abstract partial class BaseConversionExpression : Operation, IHasOperatorMethodExpression, IConversionExpression
    {
        protected BaseConversionExpression(ConversionKind conversionKind, bool isExplicit, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ConversionExpression, isInvalid, syntax, type, constantValue)
        {
            ConversionKind = conversionKind;
            IsExplicit = isExplicit;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// Value to be converted.
        /// </summary>
        public abstract IOperation Operand { get; }
        /// <summary>
        /// Kind of conversion.
        /// </summary>
        public ConversionKind ConversionKind { get; }
        /// <summary>
        /// True if and only if the conversion is indicated explicity by a cast operation in the source code.
        /// </summary>
        public bool IsExplicit { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConversionExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConversionExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    internal sealed partial class ConversionExpression : BaseConversionExpression, IHasOperatorMethodExpression, IConversionExpression
    {
        public ConversionExpression(IOperation operand, ConversionKind conversionKind, bool isExplicit, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(conversionKind, isExplicit, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            Operand = operand;
        }
        /// <summary>
        /// Value to be converted.
        /// </summary>
        public override IOperation Operand { get; }
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    internal sealed partial class LazyConversionExpression : BaseConversionExpression, IHasOperatorMethodExpression, IConversionExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyConversionExpression(Lazy<IOperation> operand, ConversionKind conversionKind, bool isExplicit, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(conversionKind, isExplicit, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException(nameof(operand));
        }
        /// <summary>
        /// Value to be converted.
        /// </summary>
        public override IOperation Operand => _lazyOperand.Value;
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class DefaultValueExpression : Operation, IDefaultValueExpression
    {
        public DefaultValueExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.DefaultValueExpression, isInvalid, syntax, type, constantValue)
        {
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
        public EmptyStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.EmptyStatement, isInvalid, syntax, type, constantValue)
        {
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
    /// Represents a VB End statemnt.
    /// </summary>
    internal sealed partial class EndStatement : Operation, IEndStatement
    {
        public EndStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.EndStatement, isInvalid, syntax, type, constantValue)
        {
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
        protected BaseEventAssignmentExpression(IEventSymbol @event, bool adds, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.EventAssignmentExpression, isInvalid, syntax, type, constantValue)
        {
            Event = @event;
            Adds = adds;
        }
        /// <summary>
        /// Event being bound.
        /// </summary>
        public IEventSymbol Event { get; }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        public abstract IOperation EventInstance { get; }

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        public abstract IOperation HandlerValue { get; }

        /// <summary>
        /// True for adding a binding, false for removing one.
        /// </summary>
        public bool Adds { get; }
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
        public EventAssignmentExpression(IEventSymbol @event, IOperation eventInstance, IOperation handlerValue, bool adds, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(@event, adds, isInvalid, syntax, type, constantValue)
        {
            EventInstance = eventInstance;
            HandlerValue = handlerValue;
        }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        public override IOperation EventInstance { get; }

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        public override IOperation HandlerValue { get; }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    internal sealed partial class LazyEventAssignmentExpression : BaseEventAssignmentExpression, IEventAssignmentExpression
    {
        private readonly Lazy<IOperation> _lazyEventInstance;
        private readonly Lazy<IOperation> _lazyHandlerValue;

        public LazyEventAssignmentExpression(IEventSymbol @event, Lazy<IOperation> eventInstance, Lazy<IOperation> handlerValue, bool adds, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(@event, adds, isInvalid, syntax, type, constantValue)
        {
            _lazyEventInstance = eventInstance ?? throw new System.ArgumentNullException(nameof(eventInstance));
            _lazyHandlerValue = handlerValue ?? throw new System.ArgumentNullException(nameof(handlerValue));
        }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        public override IOperation EventInstance => _lazyEventInstance.Value;

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        public override IOperation HandlerValue => _lazyHandlerValue.Value;
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    internal abstract partial class BaseEventReferenceExpression : MemberReferenceExpression, IEventReferenceExpression
    {
        public BaseEventReferenceExpression(IEventSymbol @event, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(member, OperationKind.EventReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Event = @event;
        }
        /// <summary>
        /// Referenced event.
        /// </summary>
        public IEventSymbol Event { get; }

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
        public EventReferenceExpression(IEventSymbol @event, IOperation instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(@event, member, isInvalid, syntax, type, constantValue)
        {
            Instance = instance;
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance { get; }
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    internal sealed partial class LazyEventReferenceExpression : BaseEventReferenceExpression, IEventReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyEventReferenceExpression(IEventSymbol @event, Lazy<IOperation> instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(@event, member, isInvalid, syntax, type, constantValue)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// </summary>
    internal abstract partial class BaseExpressionStatement : Operation, IExpressionStatement
    {
        protected BaseExpressionStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ExpressionStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public abstract IOperation Expression { get; }
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
        public ExpressionStatement(IOperation expression, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Expression = expression;
        }
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public override IOperation Expression { get; }
    }

    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// </summary>
    internal sealed partial class LazyExpressionStatement : BaseExpressionStatement, IExpressionStatement
    {
        private readonly Lazy<IOperation> _lazyExpression;

        public LazyExpressionStatement(Lazy<IOperation> expression, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException(nameof(expression));
        }
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public override IOperation Expression => _lazyExpression.Value;
    }

    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    internal abstract partial class BaseFieldInitializer : SymbolInitializer, IFieldInitializer
    {
        public BaseFieldInitializer(ImmutableArray<IFieldSymbol> initializedFields, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            InitializedFields = initializedFields;
        }
        /// <summary>
        /// Initialized fields. There can be multiple fields for Visual Basic fields declared with As New.
        /// </summary>
        public ImmutableArray<IFieldSymbol> InitializedFields { get; }

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
        public FieldInitializer(ImmutableArray<IFieldSymbol> initializedFields, IOperation value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(initializedFields, kind, isInvalid, syntax, type, constantValue)
        {
            Value = value;
        }
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    internal sealed partial class LazyFieldInitializer : BaseFieldInitializer, IFieldInitializer
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyFieldInitializer(ImmutableArray<IFieldSymbol> initializedFields, Lazy<IOperation> value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(initializedFields, kind, isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    internal abstract partial class BaseFieldReferenceExpression : MemberReferenceExpression, IFieldReferenceExpression
    {
        public BaseFieldReferenceExpression(IFieldSymbol field, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(member, OperationKind.FieldReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Field = field;
        }
        /// <summary>
        /// Referenced field.
        /// </summary>
        public IFieldSymbol Field { get; }

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
        public FieldReferenceExpression(IFieldSymbol field, IOperation instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(field, member, isInvalid, syntax, type, constantValue)
        {
            Instance = instance;
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance { get; }
    }

    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    internal sealed partial class LazyFieldReferenceExpression : BaseFieldReferenceExpression, IFieldReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyFieldReferenceExpression(IFieldSymbol field, Lazy<IOperation> instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(field, member, isInvalid, syntax, type, constantValue)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a C# fixed statement.
    /// </summary>
    internal abstract partial class BaseFixedStatement : Operation, IFixedStatement
    {
        protected BaseFixedStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.FixedStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Variables to be fixed.
        /// </summary>
        public abstract IVariableDeclarationStatement Variables { get; }
        /// <summary>
        /// Body of the fixed, over which the variables are fixed.
        /// </summary>
        public abstract IOperation Body { get; }
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
        public FixedStatement(IVariableDeclarationStatement variables, IOperation body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Variables = variables;
            Body = body;
        }
        /// <summary>
        /// Variables to be fixed.
        /// </summary>
        public override IVariableDeclarationStatement Variables { get; }
        /// <summary>
        /// Body of the fixed, over which the variables are fixed.
        /// </summary>
        public override IOperation Body { get; }
    }

    /// <summary>
    /// Represents a C# fixed statement.
    /// </summary>
    internal sealed partial class LazyFixedStatement : BaseFixedStatement, IFixedStatement
    {
        private readonly Lazy<IVariableDeclarationStatement> _lazyVariables;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyFixedStatement(Lazy<IVariableDeclarationStatement> variables, Lazy<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyVariables = variables ?? throw new System.ArgumentNullException(nameof(variables));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }
        /// <summary>
        /// Variables to be fixed.
        /// </summary>
        public override IVariableDeclarationStatement Variables => _lazyVariables.Value;

        /// <summary>
        /// Body of the fixed, over which the variables are fixed.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# foreach statement or a VB For Each statement.
    /// </summary>
    internal abstract partial class BaseForEachLoopStatement : LoopStatement, IForEachLoopStatement
    {
        public BaseForEachLoopStatement(ILocalSymbol iterationVariable, LoopKind loopKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(loopKind, OperationKind.LoopStatement, isInvalid, syntax, type, constantValue)
        {
            IterationVariable = iterationVariable;
        }
        /// <summary>
        /// Iteration variable of the loop.
        /// </summary>
        public ILocalSymbol IterationVariable { get; }
        /// <summary>
        /// Collection value over which the loop iterates.
        /// </summary>
        public abstract IOperation Collection { get; }

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
        public ForEachLoopStatement(ILocalSymbol iterationVariable, IOperation collection, LoopKind loopKind, IOperation body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(iterationVariable, loopKind, isInvalid, syntax, type, constantValue)
        {
            Collection = collection;
            Body = body;
        }
        /// <summary>
        /// Collection value over which the loop iterates.
        /// </summary>
        public override IOperation Collection { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body { get; }
    }

    /// <summary>
    /// Represents a C# foreach statement or a VB For Each statement.
    /// </summary>
    internal sealed partial class LazyForEachLoopStatement : BaseForEachLoopStatement, IForEachLoopStatement
    {
        private readonly Lazy<IOperation> _lazyCollection;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyForEachLoopStatement(ILocalSymbol iterationVariable, Lazy<IOperation> collection, LoopKind loopKind, Lazy<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(iterationVariable, loopKind, isInvalid, syntax, type, constantValue)
        {
            _lazyCollection = collection ?? throw new System.ArgumentNullException(nameof(collection));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }
        /// <summary>
        /// Collection value over which the loop iterates.
        /// </summary>
        public override IOperation Collection => _lazyCollection.Value;
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# for statement or a VB For statement.
    /// </summary>
    internal abstract partial class BaseForLoopStatement : ForWhileUntilLoopStatement, IForLoopStatement
    {
        public BaseForLoopStatement(ImmutableArray<ILocalSymbol> locals, LoopKind loopKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(loopKind, OperationKind.LoopStatement, isInvalid, syntax, type, constantValue)
        {
            Locals = locals;
        }
        /// <summary>
        /// Statements to execute before entry to the loop. For C# these come from the first clause of the for statement. For VB these initialize the index variable of the For statement.
        /// </summary>
        public abstract ImmutableArray<IOperation> Before { get; }
        /// <summary>
        /// Statements to execute at the bottom of the loop. For C# these come from the third clause of the for statement. For VB these increment the index variable of the For statement.
        /// </summary>
        public abstract ImmutableArray<IOperation> AtLoopBottom { get; }
        /// <summary>
        /// Declarations local to the loop.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Locals { get; }

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
        public ForLoopStatement(ImmutableArray<IOperation> before, ImmutableArray<IOperation> atLoopBottom, ImmutableArray<ILocalSymbol> locals, IOperation condition, LoopKind loopKind, IOperation body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(locals, loopKind, isInvalid, syntax, type, constantValue)
        {
            Before = before;
            AtLoopBottom = atLoopBottom;
            Condition = condition;
            Body = body;
        }
        /// <summary>
        /// Statements to execute before entry to the loop. For C# these come from the first clause of the for statement. For VB these initialize the index variable of the For statement.
        /// </summary>
        public override ImmutableArray<IOperation> Before { get; }
        /// <summary>
        /// Statements to execute at the bottom of the loop. For C# these come from the third clause of the for statement. For VB these increment the index variable of the For statement.
        /// </summary>
        public override ImmutableArray<IOperation> AtLoopBottom { get; }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public override IOperation Condition { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body { get; }
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

        public LazyForLoopStatement(Lazy<ImmutableArray<IOperation>> before, Lazy<ImmutableArray<IOperation>> atLoopBottom, ImmutableArray<ILocalSymbol> locals, Lazy<IOperation> condition, LoopKind loopKind, Lazy<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(locals, loopKind, isInvalid, syntax, type, constantValue)
        {
            _lazyBefore = before;
            _lazyAtLoopBottom = atLoopBottom;
            _lazyCondition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }
        /// <summary>
        /// Statements to execute before entry to the loop. For C# these come from the first clause of the for statement. For VB these initialize the index variable of the For statement.
        /// </summary>
        public override ImmutableArray<IOperation> Before => _lazyBefore.Value;

        /// <summary>
        /// Statements to execute at the bottom of the loop. For C# these come from the third clause of the for statement. For VB these increment the index variable of the For statement.
        /// </summary>
        public override ImmutableArray<IOperation> AtLoopBottom => _lazyAtLoopBottom.Value;

        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public override IOperation Condition => _lazyCondition.Value;

        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# while, for, or do statement, or a VB While, For, or Do statement.
    /// </summary>
    internal abstract partial class ForWhileUntilLoopStatement : LoopStatement, IForWhileUntilLoopStatement
    {
        protected ForWhileUntilLoopStatement(LoopKind loopKind, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(loopKind, kind, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public abstract IOperation Condition { get; }
    }

    /// <summary>
    /// Represents an if statement in C# or an If statement in VB.
    /// </summary>
    internal abstract partial class BaseIfStatement : Operation, IIfStatement
    {
        protected BaseIfStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.IfStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Condition of the if statement. For C# there is naturally one clause per if, but for VB If statements with multiple clauses are rewritten to have only one.
        /// </summary>
        public abstract IOperation Condition { get; }
        /// <summary>
        /// Statement executed if the condition is true.
        /// </summary>
        public abstract IOperation IfTrueStatement { get; }
        /// <summary>
        /// Statement executed if the condition is false.
        /// </summary>
        public abstract IOperation IfFalseStatement { get; }
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
        public IfStatement(IOperation condition, IOperation ifTrueStatement, IOperation ifFalseStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Condition = condition;
            IfTrueStatement = ifTrueStatement;
            IfFalseStatement = ifFalseStatement;
        }
        /// <summary>
        /// Condition of the if statement. For C# there is naturally one clause per if, but for VB If statements with multiple clauses are rewritten to have only one.
        /// </summary>
        public override IOperation Condition { get; }
        /// <summary>
        /// Statement executed if the condition is true.
        /// </summary>
        public override IOperation IfTrueStatement { get; }
        /// <summary>
        /// Statement executed if the condition is false.
        /// </summary>
        public override IOperation IfFalseStatement { get; }
    }

    /// <summary>
    /// Represents an if statement in C# or an If statement in VB.
    /// </summary>
    internal sealed partial class LazyIfStatement : BaseIfStatement, IIfStatement
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyIfTrueStatement;
        private readonly Lazy<IOperation> _lazyIfFalseStatement;

        public LazyIfStatement(Lazy<IOperation> condition, Lazy<IOperation> ifTrueStatement, Lazy<IOperation> ifFalseStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyCondition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            _lazyIfTrueStatement = ifTrueStatement ?? throw new System.ArgumentNullException(nameof(ifTrueStatement));
            _lazyIfFalseStatement = ifFalseStatement ?? throw new System.ArgumentNullException(nameof(ifFalseStatement));
        }
        /// <summary>
        /// Condition of the if statement. For C# there is naturally one clause per if, but for VB If statements with multiple clauses are rewritten to have only one.
        /// </summary>
        public override IOperation Condition => _lazyCondition.Value;

        /// <summary>
        /// Statement executed if the condition is true.
        /// </summary>
        public override IOperation IfTrueStatement => _lazyIfTrueStatement.Value;

        /// <summary>
        /// Statement executed if the condition is false.
        /// </summary>
        public override IOperation IfFalseStatement => _lazyIfFalseStatement.Value;
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    internal abstract partial class BaseIncrementExpression : Operation, IIncrementExpression
    {
        public BaseIncrementExpression(UnaryOperationKind incrementOperationKind, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.IncrementExpression, isInvalid, syntax, type, constantValue)
        {
            IncrementOperationKind = incrementOperationKind;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// Kind of increment.
        /// </summary>
        public UnaryOperationKind IncrementOperationKind { get; }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public abstract IOperation Target { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }

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
        public IncrementExpression(UnaryOperationKind incrementOperationKind, IOperation target, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(incrementOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            Target = target;
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target { get; }
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    internal sealed partial class LazyIncrementExpression : BaseIncrementExpression, IIncrementExpression
    {
        private readonly Lazy<IOperation> _lazyTarget;

        public LazyIncrementExpression(UnaryOperationKind incrementOperationKind, Lazy<IOperation> target, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(incrementOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException(nameof(target));
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target => _lazyTarget.Value;
    }

    /// <summary>
    /// Represents a C# this or base expression, or a VB Me, MyClass, or MyBase expression.
    /// </summary>
    internal sealed partial class InstanceReferenceExpression : Operation, IInstanceReferenceExpression
    {
        public InstanceReferenceExpression(InstanceReferenceKind instanceReferenceKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.InstanceReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            InstanceReferenceKind = instanceReferenceKind;
        }
        ///
        /// <summary>
        /// Kind of instance reference.
        /// </summary>
        public InstanceReferenceKind InstanceReferenceKind { get; }
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
        protected BaseInterpolatedStringExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.InterpolatedStringExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Constituent parts of interpolated string, each of which is an <see cref="IInterpolatedStringContent"/>.
        /// </summary>
        public abstract ImmutableArray<IInterpolatedStringContent> Parts { get; }
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
        public InterpolatedStringExpression(ImmutableArray<IInterpolatedStringContent> parts, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Parts = parts;
        }
        /// <summary>
        /// Constituent parts of interpolated string, each of which is an <see cref="IInterpolatedStringContent"/>.
        /// </summary>
        public override ImmutableArray<IInterpolatedStringContent> Parts { get; }
    }

    /// <remarks>
    /// Represents an interpolated string expression.
    /// </remarks>
    internal sealed partial class LazyInterpolatedStringExpression : BaseInterpolatedStringExpression, IInterpolatedStringExpression
    {
        private readonly Lazy<ImmutableArray<IInterpolatedStringContent>> _lazyParts;

        public LazyInterpolatedStringExpression(Lazy<ImmutableArray<IInterpolatedStringContent>> parts, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyParts = parts;
        }
        /// <summary>
        /// Constituent parts of interpolated string, each of which is an <see cref="IInterpolatedStringContent"/>.
        /// </summary>
        public override ImmutableArray<IInterpolatedStringContent> Parts => _lazyParts.Value;
    }

    /// <remarks>
    /// Represents a constituent string literal part of an interpolated string expression.
    /// </remarks>
    internal abstract partial class BaseInterpolatedStringText : Operation, IInterpolatedStringText
    {
        protected BaseInterpolatedStringText(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.InterpolatedStringText, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Text content.
        /// </summary>
        public abstract IOperation Text { get; }
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
        public InterpolatedStringText(IOperation text, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Text = text;
        }
        /// <summary>
        /// Text content.
        /// </summary>
        public override IOperation Text { get; }
    }

    /// <remarks>
    /// Represents a constituent string literal part of an interpolated string expression.
    /// </remarks>
    internal sealed partial class LazyInterpolatedStringText : BaseInterpolatedStringText, IInterpolatedStringText
    {
        private readonly Lazy<IOperation> _lazyText;

        public LazyInterpolatedStringText(Lazy<IOperation> text, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyText = text;
        }
        /// <summary>
        /// Text content.
        /// </summary>
        public override IOperation Text => _lazyText.Value;
    }

    /// <remarks>
    /// Represents a constituent interpolation part of an interpolated string expression.
    /// </remarks>
    internal abstract partial class BaseInterpolation : Operation, IInterpolation
    {
        protected BaseInterpolation(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.Interpolation, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Expression of the interpolation.
        /// </summary>
        public abstract IOperation Expression { get; }
        /// <summary>
        /// Optional alignment of the interpolation.
        /// </summary>
        public abstract IOperation Alignment { get; }
        /// <summary>
        /// Optional format string of the interpolation.
        /// </summary>
        public abstract IOperation FormatString { get; }
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
        public Interpolation(IOperation expression, IOperation alignment, IOperation formatString, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Expression = expression;
            Alignment = alignment;
            FormatString = formatString;
        }
        /// <summary>
        /// Expression of the interpolation.
        /// </summary>
        public override IOperation Expression { get; }
        /// <summary>
        /// Optional alignment of the interpolation.
        /// </summary>
        public override IOperation Alignment { get; }
        /// <summary>
        /// Optional format string of the interpolation.
        /// </summary>
        public override IOperation FormatString { get; }
    }

    /// <remarks>
    /// Represents a constituent interpolation part of an interpolated string expression.
    /// </remarks>
    internal sealed partial class LazyInterpolation : BaseInterpolation, IInterpolation
    {
        private readonly Lazy<IOperation> _lazyExpression;
        private readonly Lazy<IOperation> _lazyAlignment;
        private readonly Lazy<IOperation> _lazyFormatString;

        public LazyInterpolation(Lazy<IOperation> expression, Lazy<IOperation> alignment, Lazy<IOperation> formatString, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            _lazyExpression = expression;
            _lazyAlignment = alignment;
            _lazyFormatString = formatString;
        }
        /// <summary>
        /// Expression of the interpolation.
        /// </summary>
        public override IOperation Expression => _lazyExpression.Value;
        /// <summary>
        /// Optional alignment of the interpolation.
        /// </summary>
        public override IOperation Alignment => _lazyAlignment.Value;
        /// <summary>
        /// Optional format string of the interpolation.
        /// </summary>
        public override IOperation FormatString => _lazyFormatString.Value;
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal abstract partial class BaseInvalidExpression : Operation, IInvalidExpression
    {
        protected BaseInvalidExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.InvalidExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Child operations.
        /// </summary>
        public abstract ImmutableArray<IOperation> Children { get; }
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
        public InvalidExpression(ImmutableArray<IOperation> children, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Children = children;
        }
        /// <summary>
        /// Child operations.
        /// </summary>
        public override ImmutableArray<IOperation> Children { get; }
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class LazyInvalidExpression : BaseInvalidExpression, IInvalidExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyChildren;

        public LazyInvalidExpression(Lazy<ImmutableArray<IOperation>> children, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyChildren = children;
        }
        /// <summary>
        /// Child operations.
        /// </summary>
        public override ImmutableArray<IOperation> Children => _lazyChildren.Value;
    }

    /// <summary>
    /// Represents a syntactically or semantically invalid C# or VB statement.
    /// </summary>
    internal abstract partial class BaseInvalidStatement : Operation, IInvalidStatement
    {
        protected BaseInvalidStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.InvalidStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Child operations.
        /// </summary>
        public abstract ImmutableArray<IOperation> Children { get; }
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
        public InvalidStatement(ImmutableArray<IOperation> children, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Children = children;
        }
        /// <summary>
        /// Child operations.
        /// </summary>
        public override ImmutableArray<IOperation> Children { get; }
    }

    /// <summary>
    /// Represents a syntactically or semantically invalid C# or VB statement.
    /// </summary>
    internal sealed partial class LazyInvalidStatement : BaseInvalidStatement, IInvalidStatement
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyChildren;

        public LazyInvalidStatement(Lazy<ImmutableArray<IOperation>> children, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyChildren = children;
        }
        /// <summary>
        /// Child operations.
        /// </summary>
        public override ImmutableArray<IOperation> Children => _lazyChildren.Value;
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal abstract partial class BaseInvocationExpression : Operation, IHasArgumentsExpression, IInvocationExpression
    {
        protected BaseInvocationExpression(IMethodSymbol targetMethod, bool isVirtual, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.InvocationExpression, isInvalid, syntax, type, constantValue)
        {
            TargetMethod = targetMethod;
            IsVirtual = isVirtual;
        }
        /// <summary>
        /// Method to be invoked.
        /// </summary>
        public IMethodSymbol TargetMethod { get; }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        public abstract IOperation Instance { get; }
        /// <summary>
        /// True if the invocation uses a virtual mechanism, and false otherwise.
        /// </summary>
        public bool IsVirtual { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public abstract ImmutableArray<IArgument> ArgumentsInEvaluationOrder { get; }
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
        public InvocationExpression(IMethodSymbol targetMethod, IOperation instance, bool isVirtual, ImmutableArray<IArgument> argumentsInEvaluationOrder, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(targetMethod, isVirtual, isInvalid, syntax, type, constantValue)
        {
            Instance = instance;
            ArgumentsInEvaluationOrder = argumentsInEvaluationOrder;
        }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        public override IOperation Instance { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public override ImmutableArray<IArgument> ArgumentsInEvaluationOrder { get; }
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal sealed partial class LazyInvocationExpression : BaseInvocationExpression, IHasArgumentsExpression, IInvocationExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;
        private readonly Lazy<ImmutableArray<IArgument>> _lazyArgumentsInEvaluationOrder;

        public LazyInvocationExpression(IMethodSymbol targetMethod, Lazy<IOperation> instance, bool isVirtual, Lazy<ImmutableArray<IArgument>> argumentsInEvaluationOrder, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(targetMethod, isVirtual, isInvalid, syntax, type, constantValue)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
            _lazyArgumentsInEvaluationOrder = argumentsInEvaluationOrder;
        }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;

        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public override ImmutableArray<IArgument> ArgumentsInEvaluationOrder => _lazyArgumentsInEvaluationOrder.Value;
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal abstract partial class BaseIsTypeExpression : Operation, IIsTypeExpression
    {
        protected BaseIsTypeExpression(ITypeSymbol isType, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.IsTypeExpression, isInvalid, syntax, type, constantValue)
        {
            IsType = isType;
        }
        /// <summary>
        /// Value to test.
        /// </summary>
        public abstract IOperation Operand { get; }
        /// <summary>
        /// Type for which to test.
        /// </summary>
        public ITypeSymbol IsType { get; }
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
        public IsTypeExpression(IOperation operand, ITypeSymbol isType, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isType, isInvalid, syntax, type, constantValue)
        {
            Operand = operand;
        }
        /// <summary>
        /// Value to test.
        /// </summary>
        public override IOperation Operand { get; }
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal sealed partial class LazyIsTypeExpression : BaseIsTypeExpression, IIsTypeExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyIsTypeExpression(Lazy<IOperation> operand, ITypeSymbol isType, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isType, isInvalid, syntax, type, constantValue)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException(nameof(operand));
        }
        /// <summary>
        /// Value to test.
        /// </summary>
        public override IOperation Operand => _lazyOperand.Value;
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal abstract partial class BaseLabelStatement : Operation, ILabelStatement
    {
        protected BaseLabelStatement(ILabelSymbol label, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.LabelStatement, isInvalid, syntax, type, constantValue)
        {
            Label = label;
        }
        /// <summary>
        ///  Label that can be the target of branches.
        /// </summary>
        public ILabelSymbol Label { get; }
        /// <summary>
        /// Statement that has been labeled.
        /// </summary>
        public abstract IOperation LabeledStatement { get; }
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
        public LabelStatement(ILabelSymbol label, IOperation labeledStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(label, isInvalid, syntax, type, constantValue)
        {
            LabeledStatement = labeledStatement;
        }
        /// <summary>
        /// Statement that has been labeled.
        /// </summary>
        public override IOperation LabeledStatement { get; }
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal sealed partial class LazyLabelStatement : BaseLabelStatement, ILabelStatement
    {
        private readonly Lazy<IOperation> _lazyLabeledStatement;

        public LazyLabelStatement(ILabelSymbol label, Lazy<IOperation> labeledStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(label, isInvalid, syntax, type, constantValue)
        {
            _lazyLabeledStatement = labeledStatement ?? throw new System.ArgumentNullException(nameof(labeledStatement));
        }
        /// <summary>
        /// Statement that has been labeled.
        /// </summary>
        public override IOperation LabeledStatement => _lazyLabeledStatement.Value;
    }

    /// <summary>
    /// Represents a lambda expression.
    /// </summary>
    internal abstract partial class BaseLambdaExpression : Operation, ILambdaExpression
    {
        protected BaseLambdaExpression(IMethodSymbol signature, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.LambdaExpression, isInvalid, syntax, type, constantValue)
        {
            Signature = signature;
        }
        /// <summary>
        /// Signature of the lambda.
        /// </summary>
        public IMethodSymbol Signature { get; }
        /// <summary>
        /// Body of the lambda.
        /// </summary>
        public abstract IBlockStatement Body { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLambdaExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLambdaExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a lambda expression.
    /// </summary>
    internal sealed partial class LambdaExpression : BaseLambdaExpression, ILambdaExpression
    {
        public LambdaExpression(IMethodSymbol signature, IBlockStatement body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(signature, isInvalid, syntax, type, constantValue)
        {
            Body = body;
        }
        /// <summary>
        /// Body of the lambda.
        /// </summary>
        public override IBlockStatement Body { get; }
    }

    /// <summary>
    /// Represents a lambda expression.
    /// </summary>
    internal sealed partial class LazyLambdaExpression : BaseLambdaExpression, ILambdaExpression
    {
        private readonly Lazy<IBlockStatement> _lazyBody;

        public LazyLambdaExpression(IMethodSymbol signature, Lazy<IBlockStatement> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(signature, isInvalid, syntax, type, constantValue)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }
        /// <summary>
        /// Body of the lambda.
        /// </summary>
        public override IBlockStatement Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a late-bound reference to a member of a class or struct.
    /// </summary>
    internal abstract partial class BaseLateBoundMemberReferenceExpression : Operation, ILateBoundMemberReferenceExpression
    {
        protected BaseLateBoundMemberReferenceExpression(string memberName, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.LateBoundMemberReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            MemberName = memberName;
        }
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        public abstract IOperation Instance { get; }
        /// <summary>
        /// Name of the member.
        /// </summary>
        public string MemberName { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLateBoundMemberReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLateBoundMemberReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a late-bound reference to a member of a class or struct.
    /// </summary>
    internal sealed partial class LateBoundMemberReferenceExpression : BaseLateBoundMemberReferenceExpression, ILateBoundMemberReferenceExpression
    {
        public LateBoundMemberReferenceExpression(IOperation instance, string memberName, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(memberName, isInvalid, syntax, type, constantValue)
        {
            Instance = instance;
        }
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        public override IOperation Instance { get; }
    }

    /// <summary>
    /// Represents a late-bound reference to a member of a class or struct.
    /// </summary>
    internal sealed partial class LazyLateBoundMemberReferenceExpression : BaseLateBoundMemberReferenceExpression, ILateBoundMemberReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyLateBoundMemberReferenceExpression(Lazy<IOperation> instance, string memberName, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(memberName, isInvalid, syntax, type, constantValue)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
        }
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a textual literal numeric, string, etc. expression.
    /// </summary>
    internal sealed partial class LiteralExpression : Operation, ILiteralExpression
    {
        public LiteralExpression(string text, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.LiteralExpression, isInvalid, syntax, type, constantValue)
        {
            Text = text;
        }
        /// <summary>
        /// Textual representation of the literal.
        /// </summary>
        public string Text { get; }
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
        public LocalReferenceExpression(ILocalSymbol local, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.LocalReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Local = local;
        }
        /// <summary>
        /// Referenced local variable.
        /// </summary>
        public ILocalSymbol Local { get; }
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
        protected BaseLockStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.LockStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be locked.
        /// </summary>
        public abstract IOperation LockedObject { get; }
        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        public abstract IOperation Body { get; }
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
        public LockStatement(IOperation lockedObject, IOperation body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            LockedObject = lockedObject;
            Body = body;
        }
        /// <summary>
        /// Value to be locked.
        /// </summary>
        public override IOperation LockedObject { get; }
        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        public override IOperation Body { get; }
    }

    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    internal sealed partial class LazyLockStatement : BaseLockStatement, ILockStatement
    {
        private readonly Lazy<IOperation> _lazyLockedObject;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyLockStatement(Lazy<IOperation> lockedObject, Lazy<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyLockedObject = lockedObject ?? throw new System.ArgumentNullException(nameof(lockedObject));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }
        /// <summary>
        /// Value to be locked.
        /// </summary>
        public override IOperation LockedObject => _lazyLockedObject.Value;

        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# while, for, foreach, or do statement, or a VB While, For, For Each, or Do statement.
    /// </summary>
    internal abstract partial class LoopStatement : Operation, ILoopStatement
    {
        protected LoopStatement(LoopKind loopKind, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            LoopKind = loopKind;
        }
        /// <summary>
        /// Kind of the loop.
        /// </summary>
        public LoopKind LoopKind { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public abstract IOperation Body { get; }
    }

    /// <summary>
    /// Represents a reference to a member of a class, struct, or interface.
    /// </summary>
    internal abstract partial class MemberReferenceExpression : Operation, IMemberReferenceExpression
    {
        protected MemberReferenceExpression(ISymbol member, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            Member = member;
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public abstract IOperation Instance { get; }

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
        public BaseMethodBindingExpression(IMethodSymbol method, bool isVirtual, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(member, OperationKind.MethodBindingExpression, isInvalid, syntax, type, constantValue)
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
        public MethodBindingExpression(IMethodSymbol method, bool isVirtual, IOperation instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(method, isVirtual, member, isInvalid, syntax, type, constantValue)
        {
            Instance = instance;
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance { get; }
    }

    /// <summary>
    /// Represents a reference to a method other than as the target of an invocation.
    /// </summary>
    internal sealed partial class LazyMethodBindingExpression : BaseMethodBindingExpression, IMethodBindingExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyMethodBindingExpression(IMethodSymbol method, bool isVirtual, Lazy<IOperation> instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(method, isVirtual, member, isInvalid, syntax, type, constantValue)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal abstract partial class BaseNullCoalescingExpression : Operation, INullCoalescingExpression
    {
        protected BaseNullCoalescingExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.NullCoalescingExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        public abstract IOperation PrimaryOperand { get; }
        /// <summary>
        /// Value to be evaluated if Primary evaluates to null/Nothing.
        /// </summary>
        public abstract IOperation SecondaryOperand { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNullCoalescingExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNullCoalescingExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal sealed partial class NullCoalescingExpression : BaseNullCoalescingExpression, INullCoalescingExpression
    {
        public NullCoalescingExpression(IOperation primaryOperand, IOperation secondaryOperand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            PrimaryOperand = primaryOperand;
            SecondaryOperand = secondaryOperand;
        }
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        public override IOperation PrimaryOperand { get; }
        /// <summary>
        /// Value to be evaluated if Primary evaluates to null/Nothing.
        /// </summary>
        public override IOperation SecondaryOperand { get; }
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal sealed partial class LazyNullCoalescingExpression : BaseNullCoalescingExpression, INullCoalescingExpression
    {
        private readonly Lazy<IOperation> _lazyPrimaryOperand;
        private readonly Lazy<IOperation> _lazySecondaryOperand;

        public LazyNullCoalescingExpression(Lazy<IOperation> primaryOperand, Lazy<IOperation> secondaryOperand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyPrimaryOperand = primaryOperand ?? throw new System.ArgumentNullException(nameof(primaryOperand));
            _lazySecondaryOperand = secondaryOperand ?? throw new System.ArgumentNullException(nameof(secondaryOperand));
        }
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        public override IOperation PrimaryOperand => _lazyPrimaryOperand.Value;

        /// <summary>
        /// Value to be evaluated if Primary evaluates to null/Nothing.
        /// </summary>
        public override IOperation SecondaryOperand => _lazySecondaryOperand.Value;
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    internal abstract partial class BaseObjectCreationExpression : Operation, IHasArgumentsExpression, IObjectCreationExpression
    {
        protected BaseObjectCreationExpression(IMethodSymbol constructor, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ObjectCreationExpression, isInvalid, syntax, type, constantValue)
        {
            Constructor = constructor;
        }
        /// <summary>
        /// Constructor to be invoked on the created instance.
        /// </summary>
        public IMethodSymbol Constructor { get; }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public abstract ImmutableArray<IOperation> Initializers { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public abstract ImmutableArray<IArgument> ArgumentsInEvaluationOrder { get; }
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
        public ObjectCreationExpression(IMethodSymbol constructor, ImmutableArray<IOperation> initializers, ImmutableArray<IArgument> argumentsInEvaluationOrder, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(constructor, isInvalid, syntax, type, constantValue)
        {
            Initializers = initializers;
            ArgumentsInEvaluationOrder = argumentsInEvaluationOrder;
        }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public override ImmutableArray<IOperation> Initializers { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public override ImmutableArray<IArgument> ArgumentsInEvaluationOrder { get; }
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    internal sealed partial class LazyObjectCreationExpression : BaseObjectCreationExpression, IHasArgumentsExpression, IObjectCreationExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyInitializers;
        private readonly Lazy<ImmutableArray<IArgument>> _lazyArgumentsInEvaluationOrder;

        public LazyObjectCreationExpression(IMethodSymbol constructor, Lazy<ImmutableArray<IOperation>> initializers, Lazy<ImmutableArray<IArgument>> argumentsInEvaluationOrder, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(constructor, isInvalid, syntax, type, constantValue)
        {
            _lazyInitializers = initializers;
            _lazyArgumentsInEvaluationOrder = argumentsInEvaluationOrder;
        }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public override ImmutableArray<IOperation> Initializers => _lazyInitializers.Value;

        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public override ImmutableArray<IArgument> ArgumentsInEvaluationOrder => _lazyArgumentsInEvaluationOrder.Value;
    }

    /// <summary>
    /// Represents a C# or VB new/New anonymous object creation expression.
    /// </summary>
    internal abstract partial class BaseAnonymousObjectCreationExpression : Operation, IAnonymousObjectCreationExpression
    {
        protected BaseAnonymousObjectCreationExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.AnonymousObjectCreationExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public abstract ImmutableArray<IOperation> Initializers { get; }
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
        public AnonymousObjectCreationExpression(ImmutableArray<IOperation> initializers, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Initializers = initializers;
        }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public override ImmutableArray<IOperation> Initializers { get; }
    }

    /// <summary>
    /// Represents a C# or VB new/New anonymous object creation expression.
    /// </summary>
    internal sealed partial class LazyAnonymousObjectCreationExpression : BaseAnonymousObjectCreationExpression, IAnonymousObjectCreationExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyInitializers;

        public LazyAnonymousObjectCreationExpression(Lazy<ImmutableArray<IOperation>> initializers, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyInitializers = initializers;
        }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public override ImmutableArray<IOperation> Initializers => _lazyInitializers.Value;
    }

    /// <summary>
    /// Represents an argument value that has been omitted in an invocation.
    /// </summary>
    internal sealed partial class OmittedArgumentExpression : Operation, IOmittedArgumentExpression
    {
        public OmittedArgumentExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.OmittedArgumentExpression, isInvalid, syntax, type, constantValue)
        {
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
        public BaseParameterInitializer(IParameterSymbol parameter, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            Parameter = parameter;
        }
        /// <summary>
        /// Initialized parameter.
        /// </summary>
        public IParameterSymbol Parameter { get; }

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
        public ParameterInitializer(IParameterSymbol parameter, IOperation value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(parameter, kind, isInvalid, syntax, type, constantValue)
        {
            Value = value;
        }
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents an initialization of a parameter at the point of declaration.
    /// </summary>
    internal sealed partial class LazyParameterInitializer : BaseParameterInitializer, IParameterInitializer
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyParameterInitializer(IParameterSymbol parameter, Lazy<IOperation> value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(parameter, kind, isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a reference to a parameter.
    /// </summary>
    internal sealed partial class ParameterReferenceExpression : Operation, IParameterReferenceExpression
    {
        public ParameterReferenceExpression(IParameterSymbol parameter, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.ParameterReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Parameter = parameter;
        }
        /// <summary>
        /// Referenced parameter.
        /// </summary>
        public IParameterSymbol Parameter { get; }
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
        protected BaseParenthesizedExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ParenthesizedExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        public abstract IOperation Operand { get; }
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
        public ParenthesizedExpression(IOperation operand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Operand = operand;
        }
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        public override IOperation Operand { get; }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    internal sealed partial class LazyParenthesizedExpression : BaseParenthesizedExpression, IParenthesizedExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyParenthesizedExpression(Lazy<IOperation> operand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException(nameof(operand));
        }
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        public override IOperation Operand => _lazyOperand.Value;
    }

    /// <summary>
    /// Represents a general placeholder when no more specific kind of placeholder is available.
    /// A placeholder is an expression whose meaning is inferred from context.
    /// </summary>
    internal sealed partial class PlaceholderExpression : Operation, IPlaceholderExpression
    {
        public PlaceholderExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.PlaceholderExpression, isInvalid, syntax, type, constantValue)
        {
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
        protected BasePointerIndirectionReferenceExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.PointerIndirectionReferenceExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        public abstract IOperation Pointer { get; }
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
        public PointerIndirectionReferenceExpression(IOperation pointer, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Pointer = pointer;
        }
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        public override IOperation Pointer { get; }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    internal sealed partial class LazyPointerIndirectionReferenceExpression : BasePointerIndirectionReferenceExpression, IPointerIndirectionReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyPointer;

        public LazyPointerIndirectionReferenceExpression(Lazy<IOperation> pointer, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyPointer = pointer ?? throw new System.ArgumentNullException(nameof(pointer));
        }
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        public override IOperation Pointer => _lazyPointer.Value;
    }

    /// <summary>
    /// Represents an initialization of a property.
    /// </summary>
    internal abstract partial class BasePropertyInitializer : SymbolInitializer, IPropertyInitializer
    {
        public BasePropertyInitializer(IPropertySymbol initializedProperty, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            InitializedProperty = initializedProperty;
        }
        /// <summary>
        /// Set method used to initialize the property.
        /// </summary>
        public IPropertySymbol InitializedProperty { get; }

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
        public PropertyInitializer(IPropertySymbol initializedProperty, IOperation value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(initializedProperty, kind, isInvalid, syntax, type, constantValue)
        {
            Value = value;
        }
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents an initialization of a property.
    /// </summary>
    internal sealed partial class LazyPropertyInitializer : BasePropertyInitializer, IPropertyInitializer
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyPropertyInitializer(IPropertySymbol initializedProperty, Lazy<IOperation> value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(initializedProperty, kind, isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    internal abstract partial class BasePropertyReferenceExpression : MemberReferenceExpression, IPropertyReferenceExpression, IHasArgumentsExpression
    {
        protected BasePropertyReferenceExpression(IPropertySymbol property, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(member, OperationKind.PropertyReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Property = property;
        }
        /// <summary>
        /// Referenced property.
        /// </summary>
        public IPropertySymbol Property { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public abstract ImmutableArray<IArgument> ArgumentsInEvaluationOrder { get; }

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
        public PropertyReferenceExpression(IPropertySymbol property, IOperation instance, ISymbol member, ImmutableArray<IArgument> argumentsInEvaluationOrder, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(property, member, isInvalid, syntax, type, constantValue)
        {
            Instance = instance;
            ArgumentsInEvaluationOrder = argumentsInEvaluationOrder;
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public override ImmutableArray<IArgument> ArgumentsInEvaluationOrder { get; }

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

        public LazyPropertyReferenceExpression(IPropertySymbol property, Lazy<IOperation> instance, ISymbol member, Lazy<ImmutableArray<IArgument>> argumentsInEvaluationOrder, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(property, member, isInvalid, syntax, type, constantValue)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException(nameof(instance));
            _lazyArgumentsInEvaluationOrder = argumentsInEvaluationOrder ?? throw new System.ArgumentNullException(nameof(argumentsInEvaluationOrder));
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        public override ImmutableArray<IArgument> ArgumentsInEvaluationOrder => _lazyArgumentsInEvaluationOrder.Value;

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
        public BaseRangeCaseClause(CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(caseKind, OperationKind.RangeCaseClause, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Minimum value of the case range.
        /// </summary>
        public abstract IOperation MinimumValue { get; }
        /// <summary>
        /// Maximum value of the case range.
        /// </summary>
        public abstract IOperation MaximumValue { get; }

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
        public RangeCaseClause(IOperation minimumValue, IOperation maximumValue, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(caseKind, isInvalid, syntax, type, constantValue)
        {
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
        }
        /// <summary>
        /// Minimum value of the case range.
        /// </summary>
        public override IOperation MinimumValue { get; }
        /// <summary>
        /// Maximum value of the case range.
        /// </summary>
        public override IOperation MaximumValue { get; }
    }

    /// <summary>
    /// Represents Case x To y in VB.
    /// </summary>
    internal sealed partial class LazyRangeCaseClause : BaseRangeCaseClause, IRangeCaseClause
    {
        private readonly Lazy<IOperation> _lazyMinimumValue;
        private readonly Lazy<IOperation> _lazyMaximumValue;

        public LazyRangeCaseClause(Lazy<IOperation> minimumValue, Lazy<IOperation> maximumValue, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(caseKind, isInvalid, syntax, type, constantValue)
        {
            _lazyMinimumValue = minimumValue ?? throw new System.ArgumentNullException(nameof(minimumValue));
            _lazyMaximumValue = maximumValue ?? throw new System.ArgumentNullException(nameof(maximumValue));
        }
        /// <summary>
        /// Minimum value of the case range.
        /// </summary>
        public override IOperation MinimumValue => _lazyMinimumValue.Value;

        /// <summary>
        /// Maximum value of the case range.
        /// </summary>
        public override IOperation MaximumValue => _lazyMaximumValue.Value;
    }

    /// <summary>
    /// Represents Case Is op x in VB.
    /// </summary>
    internal abstract partial class BaseRelationalCaseClause : CaseClause, IRelationalCaseClause
    {
        public BaseRelationalCaseClause(BinaryOperationKind relation, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(caseKind, OperationKind.RelationalCaseClause, isInvalid, syntax, type, constantValue)
        {
            Relation = relation;
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public abstract IOperation Value { get; }
        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        public BinaryOperationKind Relation { get; }

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
        public RelationalCaseClause(IOperation value, BinaryOperationKind relation, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(relation, caseKind, isInvalid, syntax, type, constantValue)
        {
            Value = value;
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents Case Is op x in VB.
    /// </summary>
    internal sealed partial class LazyRelationalCaseClause : BaseRelationalCaseClause, IRelationalCaseClause
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyRelationalCaseClause(Lazy<IOperation> value, BinaryOperationKind relation, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(relation, caseKind, isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    internal abstract partial class BaseReturnStatement : Operation, IReturnStatement
    {
        protected BaseReturnStatement(OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(kind, isInvalid, syntax, type, constantValue)
        {
            Debug.Assert(kind == OperationKind.ReturnStatement
                      || kind == OperationKind.YieldReturnStatement
                      || kind == OperationKind.YieldBreakStatement);
        }
        /// <summary>
        /// Value to be returned.
        /// </summary>
        public abstract IOperation ReturnedValue { get; }
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
        public ReturnStatement(OperationKind kind, IOperation returnedValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            ReturnedValue = returnedValue;
        }
        /// <summary>
        /// Value to be returned.
        /// </summary>
        public override IOperation ReturnedValue { get; }
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    internal sealed partial class LazyReturnStatement : BaseReturnStatement, IReturnStatement
    {
        private readonly Lazy<IOperation> _lazyReturnedValue;

        public LazyReturnStatement(OperationKind kind, Lazy<IOperation> returnedValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(kind, isInvalid, syntax, type, constantValue)
        {
            _lazyReturnedValue = returnedValue ?? throw new System.ArgumentNullException(nameof(returnedValue));
        }
        /// <summary>
        /// Value to be returned.
        /// </summary>
        public override IOperation ReturnedValue => _lazyReturnedValue.Value;
    }

    /// <summary>
    /// Represents case x in C# or Case x in VB.
    /// </summary>
    internal abstract partial class BaseSingleValueCaseClause : CaseClause, ISingleValueCaseClause
    {
        public BaseSingleValueCaseClause(BinaryOperationKind equality, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(caseKind, OperationKind.SingleValueCaseClause, isInvalid, syntax, type, constantValue)
        {
            Equality = equality;
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public abstract IOperation Value { get; }
        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        public BinaryOperationKind Equality { get; }

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
        public SingleValueCaseClause(IOperation value, BinaryOperationKind equality, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(equality, caseKind, isInvalid, syntax, type, constantValue)
        {
            Value = value;
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents case x in C# or Case x in VB.
    /// </summary>
    internal sealed partial class LazySingleValueCaseClause : BaseSingleValueCaseClause, ISingleValueCaseClause
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazySingleValueCaseClause(Lazy<IOperation> value, BinaryOperationKind equality, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(equality, caseKind, isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a SizeOf expression.
    /// </summary>
    internal sealed partial class SizeOfExpression : TypeOperationExpression, ISizeOfExpression
    {
        public SizeOfExpression(ITypeSymbol typeOperand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(typeOperand, OperationKind.SizeOfExpression, isInvalid, syntax, type, constantValue)
        {
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
        public StopStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.StopStatement, isInvalid, syntax, type, constantValue)
        {
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
        protected BaseSwitchCase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.SwitchCase, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Clauses of the case. For C# there is one clause per case, but for VB there can be multiple.
        /// </summary>
        public abstract ImmutableArray<ICaseClause> Clauses { get; }
        /// <summary>
        /// Statements of the case.
        /// </summary>
        public abstract ImmutableArray<IOperation> Body { get; }
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
        public SwitchCase(ImmutableArray<ICaseClause> clauses, ImmutableArray<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Clauses = clauses;
            Body = body;
        }
        /// <summary>
        /// Clauses of the case. For C# there is one clause per case, but for VB there can be multiple.
        /// </summary>
        public override ImmutableArray<ICaseClause> Clauses { get; }
        /// <summary>
        /// Statements of the case.
        /// </summary>
        public override ImmutableArray<IOperation> Body { get; }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    internal sealed partial class LazySwitchCase : BaseSwitchCase, ISwitchCase
    {
        private readonly Lazy<ImmutableArray<ICaseClause>> _lazyClauses;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyBody;

        public LazySwitchCase(Lazy<ImmutableArray<ICaseClause>> clauses, Lazy<ImmutableArray<IOperation>> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyClauses = clauses;
            _lazyBody = body;
        }
        /// <summary>
        /// Clauses of the case. For C# there is one clause per case, but for VB there can be multiple.
        /// </summary>
        public override ImmutableArray<ICaseClause> Clauses => _lazyClauses.Value;

        /// <summary>
        /// Statements of the case.
        /// </summary>
        public override ImmutableArray<IOperation> Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal abstract partial class BaseSwitchStatement : Operation, ISwitchStatement
    {
        protected BaseSwitchStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.SwitchStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        public abstract IOperation Value { get; }
        /// <summary>
        /// Cases of the switch.
        /// </summary>
        public abstract ImmutableArray<ISwitchCase> Cases { get; }
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
        public SwitchStatement(IOperation value, ImmutableArray<ISwitchCase> cases, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Value = value;
            Cases = cases;
        }
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        public override IOperation Value { get; }
        /// <summary>
        /// Cases of the switch.
        /// </summary>
        public override ImmutableArray<ISwitchCase> Cases { get; }
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal sealed partial class LazySwitchStatement : BaseSwitchStatement, ISwitchStatement
    {
        private readonly Lazy<IOperation> _lazyValue;
        private readonly Lazy<ImmutableArray<ISwitchCase>> _lazyCases;

        public LazySwitchStatement(Lazy<IOperation> value, Lazy<ImmutableArray<ISwitchCase>> cases, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
            _lazyCases = cases;
        }
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;

        /// <summary>
        /// Cases of the switch.
        /// </summary>
        public override ImmutableArray<ISwitchCase> Cases => _lazyCases.Value;
    }

    /// <summary>
    /// Represents an initializer for a field, property, or parameter.
    /// </summary>
    internal abstract partial class SymbolInitializer : Operation, ISymbolInitializer
    {
        protected SymbolInitializer(OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
        }
        public abstract IOperation Value { get; }
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    internal abstract partial class BaseSyntheticLocalReferenceExpression : Operation, ISyntheticLocalReferenceExpression
    {
        protected BaseSyntheticLocalReferenceExpression(SyntheticLocalKind syntheticLocalKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.SyntheticLocalReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            SyntheticLocalKind = syntheticLocalKind;
        }
        /// <summary>
        /// Kind of the synthetic local.
        /// </summary>
        public SyntheticLocalKind SyntheticLocalKind { get; }
        /// <summary>
        /// Statement defining the lifetime of the synthetic local.
        /// </summary>
        public abstract IOperation ContainingStatement { get; }
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
        public SyntheticLocalReferenceExpression(SyntheticLocalKind syntheticLocalKind, IOperation containingStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(syntheticLocalKind, isInvalid, syntax, type, constantValue)
        {
            ContainingStatement = containingStatement;
        }
        /// <summary>
        /// Statement defining the lifetime of the synthetic local.
        /// </summary>
        public override IOperation ContainingStatement { get; }
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    internal sealed partial class LazySyntheticLocalReferenceExpression : BaseSyntheticLocalReferenceExpression, ISyntheticLocalReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyContainingStatement;

        public LazySyntheticLocalReferenceExpression(SyntheticLocalKind syntheticLocalKind, Lazy<IOperation> containingStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(syntheticLocalKind, isInvalid, syntax, type, constantValue)
        {
            _lazyContainingStatement = containingStatement ?? throw new System.ArgumentNullException(nameof(containingStatement));
        }
        /// <summary>
        /// Statement defining the lifetime of the synthetic local.
        /// </summary>
        public override IOperation ContainingStatement => _lazyContainingStatement.Value;
    }

    /// <summary>
    /// Represents a C# throw or a VB Throw statement.
    /// </summary>
    internal abstract partial class BaseThrowStatement : Operation, IThrowStatement
    {
        protected BaseThrowStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ThrowStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be thrown.
        /// </summary>
        public abstract IOperation ThrownObject { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitThrowStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitThrowStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# throw or a VB Throw statement.
    /// </summary>
    internal sealed partial class ThrowStatement : BaseThrowStatement, IThrowStatement
    {
        public ThrowStatement(IOperation thrownObject, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            ThrownObject = thrownObject;
        }
        /// <summary>
        /// Value to be thrown.
        /// </summary>
        public override IOperation ThrownObject { get; }
    }

    /// <summary>
    /// Represents a C# throw or a VB Throw statement.
    /// </summary>
    internal sealed partial class LazyThrowStatement : BaseThrowStatement, IThrowStatement
    {
        private readonly Lazy<IOperation> _lazyThrownObject;

        public LazyThrowStatement(Lazy<IOperation> thrownObject, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyThrownObject = thrownObject ?? throw new System.ArgumentNullException(nameof(thrownObject));
        }
        /// <summary>
        /// Value to be thrown.
        /// </summary>
        public override IOperation ThrownObject => _lazyThrownObject.Value;
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal abstract partial class BaseTryStatement : Operation, ITryStatement
    {
        protected BaseTryStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.TryStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        public abstract IBlockStatement Body { get; }
        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        public abstract ImmutableArray<ICatchClause> Catches { get; }
        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        public abstract IBlockStatement FinallyHandler { get; }
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
        public TryStatement(IBlockStatement body, ImmutableArray<ICatchClause> catches, IBlockStatement finallyHandler, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Body = body;
            Catches = catches;
            FinallyHandler = finallyHandler;
        }
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        public override IBlockStatement Body { get; }
        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        public override ImmutableArray<ICatchClause> Catches { get; }
        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        public override IBlockStatement FinallyHandler { get; }
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal sealed partial class LazyTryStatement : BaseTryStatement, ITryStatement
    {
        private readonly Lazy<IBlockStatement> _lazyBody;
        private readonly Lazy<ImmutableArray<ICatchClause>> _lazyCatches;
        private readonly Lazy<IBlockStatement> _lazyFinallyHandler;

        public LazyTryStatement(Lazy<IBlockStatement> body, Lazy<ImmutableArray<ICatchClause>> catches, Lazy<IBlockStatement> finallyHandler, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
            _lazyCatches = catches;
            _lazyFinallyHandler = finallyHandler ?? throw new System.ArgumentNullException(nameof(finallyHandler));
        }
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        public override IBlockStatement Body => _lazyBody.Value;

        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        public override ImmutableArray<ICatchClause> Catches => _lazyCatches.Value;

        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        public override IBlockStatement FinallyHandler => _lazyFinallyHandler.Value;
    }

    /// <summary>
    /// Represents a tuple expression.
    /// </summary>
    internal abstract partial class BaseTupleExpression : Operation, ITupleExpression
    {
        protected BaseTupleExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.TupleExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Elements for tuple expression.
        /// </summary>
        public abstract ImmutableArray<IOperation> Elements { get; }
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
        public TupleExpression(ImmutableArray<IOperation> elements, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Elements = elements;
        }
        /// <summary>
        /// Elements for tuple expression.
        /// </summary>
        public override ImmutableArray<IOperation> Elements { get; }
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal sealed partial class LazyTupleExpression : BaseTupleExpression, ITupleExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyElements;

        public LazyTupleExpression(Lazy<ImmutableArray<IOperation>> elements, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            _lazyElements = elements;
        }
        /// <summary>
        /// Elements for tuple expression.
        /// </summary>
        public override ImmutableArray<IOperation> Elements => _lazyElements.Value;
    }

    /// <summary>
    /// Represents a TypeOf expression.
    /// </summary>
    internal sealed partial class TypeOfExpression : TypeOperationExpression, ITypeOfExpression
    {
        public TypeOfExpression(ITypeSymbol typeOperand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(typeOperand, OperationKind.TypeOfExpression, isInvalid, syntax, type, constantValue)
        {
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
        protected TypeOperationExpression(ITypeSymbol typeOperand, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            TypeOperand = typeOperand;
        }
        /// <summary>
        /// Type operand.
        /// </summary>
        public ITypeSymbol TypeOperand { get; }
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class TypeParameterObjectCreationExpression : Operation, ITypeParameterObjectCreationExpression
    {
        public TypeParameterObjectCreationExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.TypeParameterObjectCreationExpression, isInvalid, syntax, type, constantValue)
        {
        }
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
    /// Represents an operation with one operand.
    /// </summary>
    internal abstract partial class BaseUnaryOperatorExpression : Operation, IHasOperatorMethodExpression, IUnaryOperatorExpression
    {
        protected BaseUnaryOperatorExpression(UnaryOperationKind unaryOperationKind, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.UnaryOperatorExpression, isInvalid, syntax, type, constantValue)
        {
            UnaryOperationKind = unaryOperationKind;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod;
        }
        /// <summary>
        /// Kind of unary operation.
        /// </summary>
        public UnaryOperationKind UnaryOperationKind { get; }
        /// <summary>
        /// Single operand.
        /// </summary>
        public abstract IOperation Operand { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
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
        public UnaryOperatorExpression(UnaryOperationKind unaryOperationKind, IOperation operand, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(unaryOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            Operand = operand;
        }
        /// <summary>
        /// Single operand.
        /// </summary>
        public override IOperation Operand { get; }
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    internal sealed partial class LazyUnaryOperatorExpression : BaseUnaryOperatorExpression, IHasOperatorMethodExpression, IUnaryOperatorExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyUnaryOperatorExpression(UnaryOperationKind unaryOperationKind, Lazy<IOperation> operand, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(unaryOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException(nameof(operand));
        }
        /// <summary>
        /// Single operand.
        /// </summary>
        public override IOperation Operand => _lazyOperand.Value;
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal abstract partial class BaseUsingStatement : Operation, IUsingStatement
    {
        protected BaseUsingStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.UsingStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        public abstract IOperation Body { get; }

        /// <summary>
        /// Declaration introduced by the using statement. Null if the using statement does not declare any variables.
        /// </summary>
        public abstract IVariableDeclarationStatement Declaration { get; }

        /// <summary>
        /// Resource held by the using. Can be null if Declaration is not null.
        /// </summary>
        public abstract IOperation Value { get; }
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
        public UsingStatement(IOperation body, IVariableDeclarationStatement declaration, IOperation value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Body = body;
            Declaration = declaration;
            Value = value;
        }
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        public override IOperation Body { get; }

        /// <summary>
        /// Declaration introduced by the using statement. Null if the using statement does not declare any variables.
        /// </summary>
        public override IVariableDeclarationStatement Declaration { get; }

        /// <summary>
        /// Resource held by the using. Can be null if Declaration is not null.
        /// </summary>
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal sealed partial class LazyUsingStatement : BaseUsingStatement, IUsingStatement
    {
        private readonly Lazy<IOperation> _lazyBody;
        private readonly Lazy<IVariableDeclarationStatement> _lazyDeclaration;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyUsingStatement(Lazy<IOperation> body, Lazy<IVariableDeclarationStatement> declaration, Lazy<IOperation> value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
            _lazyDeclaration = declaration ?? throw new System.ArgumentNullException(nameof(declaration));
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;

        /// <summary>
        /// Declaration introduced by the using statement. Null if the using statement does not declare any variables.
        /// </summary>
        public override IVariableDeclarationStatement Declaration => _lazyDeclaration.Value;

        /// <summary>
        /// Resource held by the using. Can be null if Declaration is not null.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    internal abstract partial class BaseVariableDeclaration : Operation, IVariableDeclaration
    {
        protected BaseVariableDeclaration(ImmutableArray<ILocalSymbol> variables, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.VariableDeclaration, isInvalid, syntax, type, constantValue)
        {
            Variables = variables;
        }
        /// <summary>
        /// Symbols declared by the declaration. In VB, it's possible to declare multiple variables with the
        /// same initializer. In C#, this will always have a single symbol.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Variables { get; }

        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        public abstract IOperation Initializer { get; }
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
        public VariableDeclaration(ImmutableArray<ILocalSymbol> variables, IOperation initializer, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(variables, isInvalid, syntax, type, constantValue)
        {
            Initializer = initializer;
        }

        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        public override IOperation Initializer { get; }
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    internal sealed partial class LazyVariableDeclaration : BaseVariableDeclaration, IVariableDeclaration
    {
        private readonly Lazy<IOperation> _lazyInitializer;

        public LazyVariableDeclaration(ImmutableArray<ILocalSymbol> variables, Lazy<IOperation> initializer, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(variables, isInvalid, syntax, type, constantValue)
        {
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException(nameof(initializer));
        }

        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        public override IOperation Initializer => _lazyInitializer.Value;
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal abstract partial class BaseVariableDeclarationStatement : Operation, IVariableDeclarationStatement
    {
        protected BaseVariableDeclarationStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.VariableDeclarationStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Variables declared by the statement.
        /// </summary>
        public abstract ImmutableArray<IVariableDeclaration> Declarations { get; }
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
        public VariableDeclarationStatement(ImmutableArray<IVariableDeclaration> declarations, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Declarations = declarations;
        }
        /// <summary>
        /// Variables declared by the statement.
        /// </summary>
        public override ImmutableArray<IVariableDeclaration> Declarations { get; }
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal sealed partial class LazyVariableDeclarationStatement : BaseVariableDeclarationStatement, IVariableDeclarationStatement
    {
        private readonly Lazy<ImmutableArray<IVariableDeclaration>> _lazyDeclarations;

        public LazyVariableDeclarationStatement(Lazy<ImmutableArray<IVariableDeclaration>> declarations, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyDeclarations = declarations;
        }
        /// <summary>
        /// Variables declared by the statement.
        /// </summary>
        public override ImmutableArray<IVariableDeclaration> Declarations => _lazyDeclarations.Value;
    }

    /// <summary>
    /// Represents a C# while or do statement, or a VB While or Do statement.
    /// </summary>
    internal abstract partial class BaseWhileUntilLoopStatement : ForWhileUntilLoopStatement, IWhileUntilLoopStatement
    {
        public BaseWhileUntilLoopStatement(bool isTopTest, bool isWhile, LoopKind loopKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(loopKind, OperationKind.LoopStatement, isInvalid, syntax, type, constantValue)
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
        public WhileUntilLoopStatement(bool isTopTest, bool isWhile, IOperation condition, LoopKind loopKind, IOperation body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isTopTest, isWhile, loopKind, isInvalid, syntax, type, constantValue)
        {
            Condition = condition;
            Body = body;
        }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public override IOperation Condition { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body { get; }
    }

    /// <summary>
    /// Represents a C# while or do statement, or a VB While or Do statement.
    /// </summary>
    internal sealed partial class LazyWhileUntilLoopStatement : BaseWhileUntilLoopStatement, IWhileUntilLoopStatement
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyWhileUntilLoopStatement(bool isTopTest, bool isWhile, Lazy<IOperation> condition, LoopKind loopKind, Lazy<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isTopTest, isWhile, loopKind, isInvalid, syntax, type, constantValue)
        {
            _lazyCondition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public override IOperation Condition => _lazyCondition.Value;
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    internal abstract partial class BaseWithStatement : Operation, IWithStatement
    {
        protected BaseWithStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.WithStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Body of the with.
        /// </summary>
        public abstract IOperation Body { get; }
        /// <summary>
        /// Value to whose members leading-dot-qualified references within the with body bind.
        /// </summary>
        public abstract IOperation Value { get; }
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
        public WithStatement(IOperation body, IOperation value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Body = body;
            Value = value;
        }
        /// <summary>
        /// Body of the with.
        /// </summary>
        public override IOperation Body { get; }
        /// <summary>
        /// Value to whose members leading-dot-qualified references within the with body bind.
        /// </summary>
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    internal sealed partial class LazyWithStatement : BaseWithStatement, IWithStatement
    {
        private readonly Lazy<IOperation> _lazyBody;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyWithStatement(Lazy<IOperation> body, Lazy<IOperation> value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// Body of the with.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;

        /// <summary>
        /// Value to whose members leading-dot-qualified references within the with body bind.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a local function statement.
    /// </summary>
    internal abstract partial class BaseLocalFunctionStatement : Operation, ILocalFunctionStatement
    {
        protected BaseLocalFunctionStatement(IMethodSymbol localFunctionSymbol, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.LocalFunctionStatement, isInvalid, syntax, type, constantValue)
        {
            LocalFunctionSymbol = localFunctionSymbol;
        }
        /// <summary>
        /// Local function symbol.
        /// </summary>
        public IMethodSymbol LocalFunctionSymbol { get; }
        /// <summary>
        /// Body of the local function.
        /// </summary>
        public abstract IBlockStatement Body { get; }
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
        public LocalFunctionStatement(IMethodSymbol localFunctionSymbol, IBlockStatement body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(localFunctionSymbol, isInvalid, syntax, type, constantValue)
        {
            Body = body;
        }
        /// <summary>
        /// Body of the local function.
        /// </summary>
        public override IBlockStatement Body { get; }
    }

    /// <summary>
    /// Represents a local function statement.
    /// </summary>
    internal sealed partial class LazyLocalFunctionStatement : BaseLocalFunctionStatement, ILocalFunctionStatement
    {
        private readonly Lazy<IBlockStatement> _lazyBody;

        public LazyLocalFunctionStatement(IMethodSymbol localFunctionSymbol, Lazy<IBlockStatement> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue)
            : base(localFunctionSymbol, isInvalid, syntax, type, constantValue)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException(nameof(body));
        }
        /// <summary>
        /// Body of the local function.
        /// </summary>
        public override IBlockStatement Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# constant pattern.
    /// </summary>
    internal abstract partial class BaseConstantPattern : Operation, IConstantPattern
    {
        protected BaseConstantPattern(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ConstantPattern, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Constant value of the pattern.
        /// </summary>
        public abstract IOperation Value { get; }
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
        public ConstantPattern(IOperation value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Value = value;
        }
        /// <summary>
        /// Constant value of the pattern.
        /// </summary>
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents a C# constant pattern.
    /// </summary>
    internal sealed partial class LazyConstantPattern : BaseConstantPattern, IConstantPattern
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyConstantPattern(Lazy<IOperation> value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue)
            : base(isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// Constant value of the pattern.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a C# declaration pattern.
    /// </summary>
    internal sealed partial class DeclarationPattern : Operation, IDeclarationPattern
    {
        public DeclarationPattern(ISymbol declaredSymbol, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.DeclarationPattern, isInvalid, syntax, type, constantValue)
        {
            DeclaredSymbol = declaredSymbol;
        }
        /// <summary>
        /// Symbol declared by the pattern.
        /// </summary>
        public ISymbol DeclaredSymbol { get; }
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
        protected BasePatternCaseClause(ILabelSymbol label, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(CaseKind.Pattern, OperationKind.PatternCaseClause, isInvalid, syntax, type, constantValue)
        {
            Label = label;
        }
        /// <summary>
        /// Label associated with the case clause.
        /// </summary>
        public ILabelSymbol Label { get; }
        /// <summary>
        /// Pattern associated with case clause.
        /// </summary>
        public abstract IPattern Pattern { get; }
        /// <summary>
        /// Guard expression associated with the pattern case clause.
        /// </summary>
        public abstract IOperation GuardExpression { get; }
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
        public PatternCaseClause(ILabelSymbol label, IPattern pattern, IOperation guardExpression, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(label, isInvalid, syntax, type, constantValue)
        {
            Pattern = pattern;
            GuardExpression = guardExpression;
        }
        /// <summary>
        /// Pattern associated with case clause.
        /// </summary>
        public override IPattern Pattern { get; }
        /// <summary>
        /// Guard expression associated with the pattern case clause.
        /// </summary>
        public override IOperation GuardExpression { get; }
    }

    /// <summary>
    /// Represents a C# pattern case clause.
    /// </summary>
    internal sealed partial class LazyPatternCaseClause : BasePatternCaseClause, IPatternCaseClause
    {
        private readonly Lazy<IPattern> _lazyPattern;
        private readonly Lazy<IOperation> _lazyGuardExpression;

        public LazyPatternCaseClause(ILabelSymbol label, Lazy<IPattern> lazyPattern, Lazy<IOperation> lazyGuardExpression, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue)
            : base(label, isInvalid, syntax, type, constantValue)
        {
            _lazyPattern = lazyPattern ?? throw new System.ArgumentNullException(nameof(lazyPattern));
            _lazyGuardExpression = lazyGuardExpression ?? throw new System.ArgumentNullException(nameof(lazyGuardExpression));
        }
        /// <summary>
        /// Pattern associated with case clause.
        /// </summary>
        public override IPattern Pattern => _lazyPattern.Value;
        /// <summary>
        /// Guard expression associated with the pattern case clause.
        /// </summary>
        public override IOperation GuardExpression => _lazyGuardExpression.Value;
    }

    /// <summary>
    /// Represents a C# is pattern expression. For example, "x is int i".
    /// </summary>
    internal abstract partial class BaseIsPatternExpression : Operation, IIsPatternExpression
    {
        protected BaseIsPatternExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.IsPatternExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Expression.
        /// </summary>
        public abstract IOperation Expression { get; }
        /// <summary>
        /// Pattern.
        /// </summary>
        public abstract IPattern Pattern { get; }
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
        public IsPatternExpression(IOperation expression, IPattern pattern, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Expression = expression;
            Pattern = pattern;
        }
        /// <summary>
        /// Expression.
        /// </summary>
        public override IOperation Expression { get; }
        /// <summary>
        /// Pattern.
        /// </summary>
        public override IPattern Pattern { get; }
    }

    /// <summary>
    /// Represents a C# is pattern expression. For example, "x is int i".
    /// </summary>
    internal sealed partial class LazyIsPatternExpression : BaseIsPatternExpression, IIsPatternExpression
    {
        private readonly Lazy<IOperation> _lazyExpression;
        private readonly Lazy<IPattern> _lazyPattern;

        public LazyIsPatternExpression(Lazy<IOperation> lazyExpression, Lazy<IPattern> lazyPattern, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue)
            : base(isInvalid, syntax, type, constantValue)
        {
            _lazyExpression = lazyExpression ?? throw new System.ArgumentNullException(nameof(lazyExpression));
            _lazyPattern = lazyPattern ?? throw new System.ArgumentNullException(nameof(lazyPattern));
        }
        /// <summary>
        /// Expression.
        /// </summary>
        public override IOperation Expression => _lazyExpression.Value;
        /// <summary>
        /// Pattern.
        /// </summary>
        public override IPattern Pattern => _lazyPattern.Value;
    }
}
