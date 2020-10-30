// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Use this to create IOperation when we don't have proper specific IOperation yet for given language construct
    /// </summary>
    internal abstract class BaseNoneOperation : OperationOld
    {
        protected BaseNoneOperation(SemanticModel semanticModel, SyntaxNode syntax, ConstantValue constantValue, bool isImplicit, ITypeSymbol type) :
            base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal class NoneOperation : BaseNoneOperation
    {
        public NoneOperation(ImmutableArray<IOperation> children, SemanticModel semanticModel, SyntaxNode syntax, ConstantValue constantValue, bool isImplicit, ITypeSymbol type) :
            base(semanticModel, syntax, constantValue, isImplicit, type)
        {
            Children = SetParentOperation(children, this);
        }

        public override IEnumerable<IOperation> Children { get; }
    }

    internal abstract class LazyNoneOperation : BaseNoneOperation
    {
        private ImmutableArray<IOperation> _lazyChildrenInterlocked;

        public LazyNoneOperation(SemanticModel semanticModel, SyntaxNode node, ConstantValue constantValue, bool isImplicit, ITypeSymbol type) :
            base(semanticModel, node, constantValue: constantValue, isImplicit: isImplicit, type)
        {
        }

        protected abstract ImmutableArray<IOperation> GetChildren();

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (_lazyChildrenInterlocked.IsDefault)
                {
                    ImmutableArray<IOperation> children = GetChildren();
                    SetParentOperation(children, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyChildrenInterlocked, children);
                }

                return _lazyChildrenInterlocked;
            }

        }
    }

#nullable enable
    internal partial class ConversionOperation
    {
        public IMethodSymbol? OperatorMethod => Conversion.MethodSymbol;
    }
#nullable disable

    internal abstract partial class BaseInvalidOperation : OperationOld, IInvalidOperation
    {
        protected BaseInvalidOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(OperationKind.Invalid, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalid(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvalid(this, argument);
        }
    }

    internal sealed partial class InvalidOperation : BaseInvalidOperation, IInvalidOperation
    {
        public InvalidOperation(ImmutableArray<IOperation> children, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            // we don't allow null children.
            Debug.Assert(children.All(o => o != null));
            Children = SetParentOperation(children, this);
        }
        public override IEnumerable<IOperation> Children { get; }
    }

    internal abstract class LazyInvalidOperation : BaseInvalidOperation, IInvalidOperation
    {
        private ImmutableArray<IOperation> _lazyChildrenInterlocked;

        public LazyInvalidOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> CreateChildren();

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (_lazyChildrenInterlocked.IsDefault)
                {
                    ImmutableArray<IOperation> children = CreateChildren();
                    SetParentOperation(children, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyChildrenInterlocked, children);
                }

                return _lazyChildrenInterlocked;
            }
        }
    }

    internal sealed class FlowAnonymousFunctionOperation : OperationOld, IFlowAnonymousFunctionOperation
    {
        public readonly ControlFlowGraphBuilder.Context Context;
        public readonly IAnonymousFunctionOperation Original;

        public FlowAnonymousFunctionOperation(in ControlFlowGraphBuilder.Context context, IAnonymousFunctionOperation original, bool isImplicit) :
            base(OperationKind.FlowAnonymousFunction, semanticModel: null, original.Syntax, original.Type, original.GetConstantValue(), isImplicit)
        {
            Context = context;
            Original = original;
        }
        public IMethodSymbol Symbol => Original.Symbol;
        public override IEnumerable<IOperation> Children
        {
            get
            {
                return ImmutableArray<IOperation>.Empty;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFlowAnonymousFunction(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFlowAnonymousFunction(this, argument);
        }
    }

#nullable enable
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
        private IEnumerable<IOperation>? _lazyChildren;
        public DynamicObjectCreationOperation(IObjectOrCollectionInitializerOperation? initializer, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit) :
            base(arguments, argumentNames, argumentRefKinds, semanticModel, syntax, type, isImplicit)
        {
            Initializer = SetParentOperation(initializer, this);
        }

        public IObjectOrCollectionInitializerOperation? Initializer { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DynamicObjectCreation;

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (_lazyChildren is null)
                {
                    var builder = ArrayBuilder<IOperation>.GetInstance(2);
                    if (!Arguments.IsEmpty) builder.AddRange(Arguments);
                    builder.AddIfNotNull(Initializer);
                    Interlocked.CompareExchange(ref _lazyChildren, builder.ToImmutableAndFree(), null);
                }
                return _lazyChildren;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicObjectCreation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicObjectCreation(this, argument);
        }
    }

    internal sealed partial class DynamicInvocationOperation : HasDynamicArgumentsExpression, IDynamicInvocationOperation
    {
        private IEnumerable<IOperation>? _lazyChildren;
        public DynamicInvocationOperation(IOperation operation, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit) :
            base(arguments, argumentNames, argumentRefKinds, semanticModel, syntax, type, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (_lazyChildren is null)
                {
                    var builder = ArrayBuilder<IOperation>.GetInstance(2);
                    builder.AddIfNotNull(Operation);
                    if (!Arguments.IsEmpty) builder.AddRange(Arguments);
                    Interlocked.CompareExchange(ref _lazyChildren, builder.ToImmutableAndFree(), null);
                }
                return _lazyChildren;
            }
        }

        public IOperation Operation { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DynamicInvocation;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicInvocation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicInvocation(this, argument);
        }
    }

    internal sealed partial class DynamicIndexerAccessOperation : HasDynamicArgumentsExpression, IDynamicIndexerAccessOperation
    {
        private IEnumerable<IOperation>? _lazyChildren;
        public DynamicIndexerAccessOperation(IOperation operation, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit) :
            base(arguments, argumentNames, argumentRefKinds, semanticModel, syntax, type, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
        }

        public IOperation Operation { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DynamicIndexerAccess;

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (_lazyChildren is null)
                {
                    var builder = ArrayBuilder<IOperation>.GetInstance(2);
                    builder.AddIfNotNull(Operation);
                    if (!Arguments.IsEmpty) builder.AddRange(Arguments);
                    Interlocked.CompareExchange(ref _lazyChildren, builder.ToImmutableAndFree(), null);
                }
                return _lazyChildren;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicIndexerAccess(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
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
        public override IEnumerable<IOperation> Children
        {
            get
            {
                // PROTOTYPE(iop): Look at making the implementation of these better.
                if (_lazyChildren is null)
                {
                    var builder = ArrayBuilder<IOperation>.GetInstance(6);
                    if (ConditionIsTop) builder.AddIfNotNull(Condition);
                    builder.AddIfNotNull(Body);
                    if (!ConditionIsTop) builder.AddIfNotNull(Condition);
                    builder.AddIfNotNull(IgnoredCondition);
                    Interlocked.CompareExchange(ref _lazyChildren, builder.ToImmutableAndFree(), null);
                }

                return _lazyChildren;
            }
        }

        public override LoopKind LoopKind => LoopKind.While;
    }
#nullable disable

    internal sealed partial class FlowCaptureReferenceOperation : OperationOld, IFlowCaptureReferenceOperation
    {
        public FlowCaptureReferenceOperation(int id, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue) :
            base(OperationKind.FlowCaptureReference, semanticModel: null, syntax: syntax, type: type, constantValue: constantValue, isImplicit: true)
        {
            Id = new CaptureId(id);
        }

        public FlowCaptureReferenceOperation(CaptureId id, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue) :
            base(OperationKind.FlowCaptureReference, semanticModel: null, syntax: syntax, type: type, constantValue: constantValue, isImplicit: true)
        {
            Id = id;
        }
    }

    internal sealed partial class FlowCaptureOperation : OperationOld, IFlowCaptureOperation
    {
        public FlowCaptureOperation(int id, SyntaxNode syntax, IOperation value) :
            base(OperationKind.FlowCapture, semanticModel: null, syntax: syntax, type: null, constantValue: null, isImplicit: true)
        {
            Debug.Assert(value != null);
            Id = new CaptureId(id);
            Value = SetParentOperation(value, this);
        }

        public CaptureId Id { get; }
        public IOperation Value { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFlowCapture(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFlowCapture(this, argument);
        }
    }

    internal sealed partial class IsNullOperation : OperationOld, IIsNullOperation
    {
        public IsNullOperation(SyntaxNode syntax, IOperation operand, ITypeSymbol type, ConstantValue constantValue) :
            base(OperationKind.IsNull, semanticModel: null, syntax: syntax, type: type, constantValue: constantValue, isImplicit: true)
        {
            Debug.Assert(operand != null);
            Operand = SetParentOperation(operand, this);
        }

        public IOperation Operand { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Operand;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIsNull(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIsNull(this, argument);
        }
    }

    internal sealed partial class CaughtExceptionOperation : OperationOld, ICaughtExceptionOperation
    {
        public CaughtExceptionOperation(SyntaxNode syntax, ITypeSymbol type) :
            this(semanticModel: null, syntax: syntax, type: type, constantValue: null, isImplicit: true)
        {
        }
    }

    internal sealed partial class StaticLocalInitializationSemaphoreOperation : OperationOld, IStaticLocalInitializationSemaphoreOperation
    {
        public StaticLocalInitializationSemaphoreOperation(ILocalSymbol local, SyntaxNode syntax, ITypeSymbol type) :
            base(OperationKind.StaticLocalInitializationSemaphore, semanticModel: null, syntax, type, constantValue: null, isImplicit: true)
        {
            Local = local;
        }
    }
}
