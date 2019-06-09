// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundObjectCreationExpression : IBoundInvalidNode
    {
        internal static ImmutableArray<BoundExpression> GetChildInitializers(BoundExpression objectOrCollectionInitializer)
        {
            var objectInitializerExpression = objectOrCollectionInitializer as BoundObjectInitializerExpression;
            if (objectInitializerExpression != null)
            {
                return objectInitializerExpression.Initializers;
            }

            var collectionInitializerExpression = objectOrCollectionInitializer as BoundCollectionInitializerExpression;
            if (collectionInitializerExpression != null)
            {
                return collectionInitializerExpression.Initializers;
            }

            return ImmutableArray<BoundExpression>.Empty;
        }

        ImmutableArray<BoundNode> IBoundInvalidNode.InvalidNodeChildren => CSharpOperationFactory.CreateInvalidChildrenFromArgumentsExpression(receiverOpt: null, Arguments, InitializerExpressionOpt);
    }

    internal sealed partial class BoundObjectInitializerMember : IBoundInvalidNode
    {
        ImmutableArray<BoundNode> IBoundInvalidNode.InvalidNodeChildren => StaticCast<BoundNode>.From(Arguments);
    }

    internal sealed partial class BoundCollectionElementInitializer : IBoundInvalidNode
    {
        ImmutableArray<BoundNode> IBoundInvalidNode.InvalidNodeChildren => CSharpOperationFactory.CreateInvalidChildrenFromArgumentsExpression(ImplicitReceiverOpt, Arguments);
    }

    internal sealed partial class BoundDeconstructionAssignmentOperator : BoundExpression
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Left, this.Right);
    }

    internal partial class BoundBadExpression : IBoundInvalidNode
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.ChildBoundNodes);

        ImmutableArray<BoundNode> IBoundInvalidNode.InvalidNodeChildren => StaticCast<BoundNode>.From(this.ChildBoundNodes);
    }

    internal partial class BoundCall : IBoundInvalidNode
    {
        ImmutableArray<BoundNode> IBoundInvalidNode.InvalidNodeChildren => CSharpOperationFactory.CreateInvalidChildrenFromArgumentsExpression(ReceiverOpt, Arguments);
    }

    internal partial class BoundIndexerAccess : IBoundInvalidNode
    {
        ImmutableArray<BoundNode> IBoundInvalidNode.InvalidNodeChildren => CSharpOperationFactory.CreateInvalidChildrenFromArgumentsExpression(ReceiverOpt, Arguments);
    }

    internal partial class BoundDynamicIndexerAccess
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.Arguments.Insert(0, this.ReceiverOpt));
    }

    internal partial class BoundAnonymousObjectCreationExpression
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.Arguments);
    }

    internal partial class BoundAttribute
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.ConstructorArguments.AddRange(this.NamedArguments));
    }

    internal partial class BoundQueryClause
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Value);
    }

    internal partial class BoundArgListOperator
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.Arguments);
    }

    internal partial class BoundNameOfOperator
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Argument);
    }

    internal partial class BoundPointerElementAccess
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Expression, this.Index);
    }

    internal partial class BoundRefTypeOperator
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Operand);
    }

    internal partial class BoundDynamicMemberAccess
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Receiver);
    }

    internal partial class BoundMakeRefOperator
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Operand);
    }

    internal partial class BoundRefValueOperator
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Operand);
    }

    internal partial class BoundDynamicInvocation
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.Arguments.Insert(0, this.Expression));
    }

    internal partial class BoundFixedLocalCollectionInitializer
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Expression);
    }

    internal partial class BoundStackAllocArrayCreation
    {
        internal static ImmutableArray<BoundExpression> GetChildInitializers(BoundArrayInitialization arrayInitializer)
        {
            return arrayInitializer?.Initializers ?? ImmutableArray<BoundExpression>.Empty;
        }

        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(GetChildInitializers(this.InitializerOpt).Insert(0, this.Count));
    }

    internal partial class BoundConvertedStackAllocExpression
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(GetChildInitializers(this.InitializerOpt).Insert(0, this.Count));
    }

    internal partial class BoundDynamicObjectCreationExpression
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.Arguments.AddRange(BoundObjectCreationExpression.GetChildInitializers(this.InitializerExpressionOpt)));
    }

    partial class BoundThrowExpression
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Expression);
    }

    internal abstract partial class BoundMethodOrPropertyGroup
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.ReceiverOpt);
    }

    internal partial class BoundSequence
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.SideEffects.Add(this.Value));
    }

    internal partial class BoundStatementList
    {
        protected override ImmutableArray<BoundNode> Children =>
            (this.Kind == BoundKind.StatementList || this.Kind == BoundKind.Scope) ? StaticCast<BoundNode>.From(this.Statements) : ImmutableArray<BoundNode>.Empty;
    }

    internal partial class BoundPassByCopy
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Expression);
    }

    internal partial class BoundIndexOrRangePatternIndexerAccess
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(Receiver, Argument);
    }
}
