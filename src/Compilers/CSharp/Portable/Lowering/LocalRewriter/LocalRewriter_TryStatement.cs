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

            var optimizing = this._compilation.Options.OptimizationLevel == OptimizationLevel.Release;
            ImmutableArray<BoundCatchBlock> catchBlocks =
                // When optimizing and we have a try block without side-effects, we can discard the catch blocks.
                (optimizing && !HasSideEffects(tryBlock)) ? ImmutableArray<BoundCatchBlock>.Empty
                : this.VisitList(node.CatchBlocks);
            BoundBlock finallyBlockOpt = (BoundBlock)this.Visit(node.FinallyBlockOpt);

            _sawAwaitInExceptionHandler |= _sawAwait;
            _sawAwait |= origSawAwait;

            if (optimizing && !HasSideEffects(finallyBlockOpt))
            {
                finallyBlockOpt = null;
            }

            return (catchBlocks.IsDefaultOrEmpty && finallyBlockOpt == null)
                ? (BoundNode)tryBlock
                : (BoundNode)node.Update(tryBlock, catchBlocks, finallyBlockOpt, node.PreferFaultHandler);
        }

        /// <summary>
        /// Is there any code to execute in the given statement that could have side-effects,
        /// such as throwing an exception? This implementation is conservative, in the sense
        /// that it may return true when the statement actually may have no side effects.
        /// </summary>
        private static bool HasSideEffects(BoundStatement statement)
        {
            if (statement == null) return false;
            switch (statement.Kind)
            {
                case BoundKind.NoOpStatement:
                    return true;
                case BoundKind.Block:
                    {
                        var block = (BoundBlock)statement;
                        foreach (var stmt in block.Statements)
                        {
                            if (HasSideEffects(stmt)) return true;
                        }
                        return false;
                    }
                case BoundKind.SequencePoint:
                    {
                        var sequence = (BoundSequencePoint)statement;
                        return HasSideEffects(sequence.StatementOpt);
                    }
                case BoundKind.SequencePointWithSpan:
                    {
                        var sequence = (BoundSequencePointWithSpan)statement;
                        return HasSideEffects(sequence.StatementOpt);
                    }
                default:
                    return true;
            }

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
