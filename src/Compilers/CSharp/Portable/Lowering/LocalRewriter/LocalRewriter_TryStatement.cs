// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            BoundBlock tryBlock = (BoundBlock)this.Visit(node.TryBlock);

            var origSawAwait = _sawAwait;
            _sawAwait = false;

            // When optimizing and we have an empty try block, we can discard the catch blocks.
            // In theory we could do better by detecting *effectively* empty catch blocks, but this is the most common case.
            ImmutableArray<BoundCatchBlock> catchBlocks =
                node.TryBlock.Statements.Length == 0 && this._compilation.Options.OptimizationLevel == OptimizationLevel.Release
                    ? ImmutableArray<BoundCatchBlock>.Empty
                    : (ImmutableArray<BoundCatchBlock>)this.VisitList(node.CatchBlocks);
            BoundBlock finallyBlockOpt = (BoundBlock)this.Visit(node.FinallyBlockOpt);

            _sawAwaitInExceptionHandler |= _sawAwait;
            _sawAwait |= origSawAwait;

            return (catchBlocks.IsDefaultOrEmpty && finallyBlockOpt == null)
                ? (BoundNode)tryBlock
                : (BoundNode)node.Update(tryBlock, catchBlocks, finallyBlockOpt, node.PreferFaultHandler);
        }

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            if (node.ExceptionFilterOpt == null)
            {
                return base.VisitCatchBlock(node);
            }

            BoundExpression rewrittenExceptionSourceOpt = (BoundExpression)this.Visit(node.ExceptionSourceOpt);
            BoundExpression rewrittenFilter = (BoundExpression)this.Visit(node.ExceptionFilterOpt);
            BoundBlock rewrittenBody = (BoundBlock)this.Visit(node.Body);
            TypeSymbol rewrittenExceptionTypeOpt = this.VisitType(node.ExceptionTypeOpt);

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            return node.Update(
                node.LocalOpt,
                rewrittenExceptionSourceOpt,
                rewrittenExceptionTypeOpt,
                AddConditionSequencePoint(rewrittenFilter, node),
                rewrittenBody,
                node.IsSynthesizedAsyncCatchAll);
        }
    }
}
