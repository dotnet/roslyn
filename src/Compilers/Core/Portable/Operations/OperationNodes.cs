// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Use this to create IOperation when we don't have proper specific IOperation yet for given language construct
    /// </summary>
    internal sealed class NoneOperation : Operation
    {
        public NoneOperation(ImmutableArray<IOperation> children, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit) :
            base(semanticModel, syntax, isImplicit)
        {
            Children = SetParentOperation(children, this);
            Type = type;
            OperationConstantValue = constantValue;
        }

        internal ImmutableArray<IOperation> Children { get; }

        protected override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Children.Length
                    => Children[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index))
            };

        protected override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Children.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Children.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class ConversionOperation
    {
        public IMethodSymbol? OperatorMethod => Conversion.MethodSymbol;
    }

    internal sealed partial class InvalidOperation : Operation, IInvalidOperation
    {
        public InvalidOperation(ImmutableArray<IOperation> children, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit) :
            base(semanticModel, syntax, isImplicit)
        {
            // we don't allow null children.
            Debug.Assert(children.All(o => o != null));
            Children = SetParentOperation(children, this);
            Type = type;
            OperationConstantValue = constantValue;
        }

        internal ImmutableArray<IOperation> Children { get; }

        protected override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Children.Length
                    => Children[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index))
            };

        protected override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Children.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Children.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.Invalid;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalid(this);
        }

        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default
        {
            return visitor.VisitInvalid(this, argument);
        }
    }

    internal sealed class FlowAnonymousFunctionOperation : Operation, IFlowAnonymousFunctionOperation
    {
        public readonly ControlFlowGraphBuilder.Context Context;
        public readonly IAnonymousFunctionOperation Original;

        public FlowAnonymousFunctionOperation(in ControlFlowGraphBuilder.Context context, IAnonymousFunctionOperation original, bool isImplicit) :
            base(semanticModel: null, original.Syntax, isImplicit)
        {
            Context = context;
            Original = original;
        }
        public IMethodSymbol Symbol => Original.Symbol;

        protected override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        protected override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);

        public override OperationKind Kind => OperationKind.FlowAnonymousFunction;
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFlowAnonymousFunction(this);
        }
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default
        {
            return visitor.VisitFlowAnonymousFunction(this, argument);
        }
    }

    internal abstract partial class BaseMemberReferenceOperation : IMemberReferenceOperation
    {
        public abstract ISymbol Member { get; }
    }

    internal sealed partial class MethodReferenceOperation
    {
        public override ISymbol Member => Method;
    }

    internal sealed partial class PropertyReferenceOperation
    {
        public override ISymbol Member => Property;
    }

    internal sealed partial class EventReferenceOperation
    {
        public override ISymbol Member => Event;
    }

    internal sealed partial class FieldReferenceOperation
    {
        public override ISymbol Member => Field;
    }

    internal sealed partial class RangeCaseClauseOperation
    {
        public override CaseKind CaseKind => CaseKind.Range;
    }

    internal sealed partial class SingleValueCaseClauseOperation
    {
        public override CaseKind CaseKind => CaseKind.SingleValue;
    }

    internal sealed partial class RelationalCaseClauseOperation
    {
        public override CaseKind CaseKind => CaseKind.Relational;
    }

    internal sealed partial class DefaultCaseClauseOperation
    {
        public override CaseKind CaseKind => CaseKind.Default;
    }

    internal sealed partial class PatternCaseClauseOperation
    {
        public override CaseKind CaseKind => CaseKind.Pattern;
    }

    internal abstract partial class HasDynamicArgumentsExpression : Operation
    {
        protected HasDynamicArgumentsExpression(ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit) :
            base(semanticModel, syntax, isImplicit)
        {
            Arguments = SetParentOperation(arguments, this);
            ArgumentNames = argumentNames;
            ArgumentRefKinds = argumentRefKinds;
            Type = type;
        }

        public ImmutableArray<string> ArgumentNames { get; }
        public ImmutableArray<RefKind> ArgumentRefKinds { get; }
        public ImmutableArray<IOperation> Arguments { get; }
        public override ITypeSymbol? Type { get; }
    }

    internal sealed partial class DynamicObjectCreationOperation : HasDynamicArgumentsExpression, IDynamicObjectCreationOperation
    {
        public DynamicObjectCreationOperation(IObjectOrCollectionInitializerOperation? initializer, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit) :
            base(arguments, argumentNames, argumentRefKinds, semanticModel, syntax, type, isImplicit)
        {
            Initializer = SetParentOperation(initializer, this);
        }

        public IObjectOrCollectionInitializerOperation? Initializer { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DynamicObjectCreation;

        protected override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Arguments.Length
                    => Arguments[index],
                1 when Initializer != null
                    => Initializer,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };

        protected override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Arguments.IsEmpty) return (true, 0, 0);
                    else goto case 0;

                case 0 when previousIndex + 1 < Arguments.Length:
                    return (true, 0, previousIndex + 1);

                case 0:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;

                case 1:
                case 2:
                    return (false, 2, 0);

                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicObjectCreation(this);
        }
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default
        {
            return visitor.VisitDynamicObjectCreation(this, argument);
        }
    }

    internal sealed partial class DynamicInvocationOperation : HasDynamicArgumentsExpression, IDynamicInvocationOperation
    {
        public DynamicInvocationOperation(IOperation operation, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit) :
            base(arguments, argumentNames, argumentRefKinds, semanticModel, syntax, type, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
        }

        protected override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operation != null
                    => Operation,
                1 when index < Arguments.Length
                    => Arguments[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };

        protected override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;

                case 0:
                    if (!Arguments.IsEmpty) return (true, 1, 0);
                    else goto case 1;

                case 1 when previousIndex + 1 < Arguments.Length:
                    return (true, 1, previousIndex + 1);

                case 1:
                case 2:
                    return (false, 2, 0);

                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public IOperation Operation { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DynamicInvocation;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicInvocation(this);
        }
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default
        {
            return visitor.VisitDynamicInvocation(this, argument);
        }
    }

    internal sealed partial class DynamicIndexerAccessOperation : HasDynamicArgumentsExpression, IDynamicIndexerAccessOperation
    {
        public DynamicIndexerAccessOperation(IOperation operation, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit) :
            base(arguments, argumentNames, argumentRefKinds, semanticModel, syntax, type, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
        }

        public IOperation Operation { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DynamicIndexerAccess;

        protected override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operation != null
                    => Operation,
                1 when index < Arguments.Length
                    => Arguments[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };

        protected override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;

                case 0:
                    if (!Arguments.IsEmpty) return (true, 1, 0);
                    else goto case 1;

                case 1 when previousIndex + 1 < Arguments.Length:
                    return (true, 1, previousIndex + 1);

                case 1:
                case 2:
                    return (false, 2, 0);

                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicIndexerAccess(this);
        }
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default
        {
            return visitor.VisitDynamicIndexerAccess(this, argument);
        }
    }

    internal sealed partial class ForEachLoopOperation
    {
        public override LoopKind LoopKind => LoopKind.ForEach;
    }

    internal sealed partial class ForLoopOperation
    {
        public override LoopKind LoopKind => LoopKind.For;
    }

    internal sealed partial class ForToLoopOperation
    {
        public override LoopKind LoopKind => LoopKind.ForTo;
    }

    internal sealed partial class WhileLoopOperation
    {
        protected override IOperation GetCurrent(int slot, int index)
        {
            return ConditionIsTop ? getCurrentSwitchTop() : getCurrentSwitchBottom();

            IOperation getCurrentSwitchTop()
                => slot switch
                {
                    0 when Condition != null
                        => Condition,
                    1 when Body != null
                        => Body,
                    2 when IgnoredCondition != null
                        => IgnoredCondition,
                    _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
                };

            IOperation getCurrentSwitchBottom()
                => slot switch
                {
                    0 when Body != null
                        => Body,
                    1 when Condition != null
                        => Condition,
                    2 when IgnoredCondition != null
                        => IgnoredCondition,
                    _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
                };
        }

        protected override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            return ConditionIsTop ? moveNextConditionIsTop() : moveNextConditionIsBottom();

            (bool hasNext, int nextSlot, int nextIndex) moveNextConditionIsTop()
            {
                switch (previousSlot)
                {
                    case -1:
                        if (Condition != null) return (true, 0, 0);
                        else goto case 0;
                    case 0:
                        if (Body != null) return (true, 1, 0);
                        else goto case 1;
                    case 1:
                        if (IgnoredCondition != null) return (true, 2, 0);
                        else goto case 2;
                    case 2:
                    case 3:
                        return (false, 3, 0);
                    default:
                        throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
                }
            }

            (bool hasNext, int nextSlot, int nextIndex) moveNextConditionIsBottom()
            {
                switch (previousSlot)
                {
                    case -1:
                        if (Body != null) return (true, 0, 0);
                        else goto case 0;
                    case 0:
                        if (Condition != null) return (true, 1, 0);
                        else goto case 1;
                    case 1:
                        if (IgnoredCondition != null) return (true, 2, 0);
                        else goto case 2;
                    case 2:
                    case 3:
                        return (false, 3, 0);
                    default:
                        throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
                }
            }
        }

        public override LoopKind LoopKind => LoopKind.While;
    }

    internal sealed partial class FlowCaptureReferenceOperation
    {
        public FlowCaptureReferenceOperation(int id, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue) :
            this(new CaptureId(id), semanticModel: null, syntax: syntax, type: type, constantValue: constantValue, isImplicit: true)
        {
        }
    }

    internal sealed partial class FlowCaptureOperation
    {
        public FlowCaptureOperation(int id, SyntaxNode syntax, IOperation value) :
            this(new CaptureId(id), value, semanticModel: null, syntax: syntax, isImplicit: true)
        {
            Debug.Assert(value != null);
        }
    }

    internal sealed partial class IsNullOperation
    {
        public IsNullOperation(SyntaxNode syntax, IOperation operand, ITypeSymbol type, ConstantValue? constantValue) :
            this(operand, semanticModel: null, syntax: syntax, type: type, constantValue: constantValue, isImplicit: true)
        {
            Debug.Assert(operand != null);
        }
    }

    internal sealed partial class CaughtExceptionOperation
    {
        public CaughtExceptionOperation(SyntaxNode syntax, ITypeSymbol type) :
            this(semanticModel: null, syntax: syntax, type: type, isImplicit: true)
        {
        }
    }

    internal sealed partial class StaticLocalInitializationSemaphoreOperation
    {
        public StaticLocalInitializationSemaphoreOperation(ILocalSymbol local, SyntaxNode syntax, ITypeSymbol type) :
            this(local, semanticModel: null, syntax, type, isImplicit: true)
        {
        }
    }

    internal sealed partial class BlockOperation
    {
        /// <summary>
        /// This creates a block that can be used for temporary, internal applications that require a block composed of
        /// statements from another block. Blocks created by this API violate IOperation tree constraints and should
        /// never be exposed from a public API.
        /// </summary>
        public static BlockOperation CreateTemporaryBlock(ImmutableArray<IOperation> statements, SemanticModel semanticModel, SyntaxNode syntax)
            => new BlockOperation(statements, semanticModel, syntax);

        private BlockOperation(ImmutableArray<IOperation> statements, SemanticModel semanticModel, SyntaxNode syntax)
            : base(semanticModel, syntax, isImplicit: true)
        {
            // Intentionally skipping SetParentOperation: this is used by CreateTemporaryBlock for the purposes of the
            // control flow factory, to temporarily create a new block for use in emulating the "block" a using variable
            // declaration introduces. These statements already have a parent node, and `SetParentOperation`'s verification
            // would fail because that parent isn't this.
            Debug.Assert(statements.All(s => s.Parent != this && s.Parent!.Kind == OperationKind.Block));
            Operations = statements;
            Locals = ImmutableArray<ILocalSymbol>.Empty;
        }
    }
}
