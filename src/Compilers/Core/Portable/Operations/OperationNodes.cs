// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
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

        public override IEnumerable<IOperation> Children { get; }
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

        public override IEnumerable<IOperation> Children { get; }
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
        public override IEnumerable<IOperation> Children => SpecializedCollections.EmptyEnumerable<IOperation>();

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
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default
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
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default
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
}
