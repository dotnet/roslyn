// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            BoundBlock? tryBlock = (BoundBlock?)this.Visit(node.TryBlock);
            Debug.Assert(tryBlock is { });

            var origSawAwait = _sawAwait;
            _sawAwait = false;

            var optimizing = _compilation.Options.OptimizationLevel == OptimizationLevel.Release;
            ImmutableArray<BoundCatchBlock> catchBlocks =
                // When optimizing and we have a try block without side-effects, we can discard the catch blocks.
                (optimizing && !HasSideEffects(tryBlock)) ? ImmutableArray<BoundCatchBlock>.Empty
                : this.VisitList(node.CatchBlocks);
            BoundBlock? finallyBlockOpt = (BoundBlock?)this.Visit(node.FinallyBlockOpt);

            _sawAwaitInExceptionHandler |= _sawAwait;
            _sawAwait |= origSawAwait;

            if (optimizing && !HasSideEffects(finallyBlockOpt))
            {
                finallyBlockOpt = null;
            }

            return (catchBlocks.IsDefaultOrEmpty && finallyBlockOpt == null)
                ? (BoundNode)tryBlock
                : (BoundNode)node.Update(tryBlock, catchBlocks, finallyBlockOpt, node.FinallyLabelOpt, node.PreferFaultHandler);
        }

        /// <summary>
        /// Is there any code to execute in the given statement that could have side-effects,
        /// such as throwing an exception? This implementation is conservative, in the sense
        /// that it may return true when the statement actually may have no side effects.
        /// </summary>
        private static bool HasSideEffects([NotNullWhen(true)] BoundStatement? statement)
        {
            if (statement == null) return false;
            switch (statement.Kind)
            {
                case BoundKind.NoOpStatement:
                    return false;
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

        public override BoundNode? VisitCatchBlock(BoundCatchBlock node)
        {
            if (node.ExceptionFilterOpt?.ConstantValueOpt?.BooleanValue == false)
            {
                return null;
            }

            BoundExpression? rewrittenExceptionSourceOpt = (BoundExpression?)this.Visit(node.ExceptionSourceOpt);
            BoundStatementList? rewrittenFilterPrologue = (BoundStatementList?)this.Visit(node.ExceptionFilterPrologueOpt);
            BoundExpression? rewrittenFilter = (BoundExpression?)this.Visit(node.ExceptionFilterOpt);
            BoundBlock? rewrittenBody = (BoundBlock?)this.Visit(node.Body);
            Debug.Assert(rewrittenBody is { });
            TypeSymbol? rewrittenExceptionTypeOpt = this.VisitType(node.ExceptionTypeOpt);

            if (Instrument)
            {
                Instrumenter.InstrumentCatchBlock(
                    node,
                    ref rewrittenExceptionSourceOpt,
                    ref rewrittenFilterPrologue,
                    ref rewrittenFilter,
                    ref rewrittenBody,
                    ref rewrittenExceptionTypeOpt,
                    _factory);
            }

            return node.Update(
                node.Locals,
                rewrittenExceptionSourceOpt,
                rewrittenExceptionTypeOpt,
                rewrittenFilterPrologue,
                rewrittenFilter,
                rewrittenBody,
                node.IsSynthesizedAsyncCatchAll);
        }
    }
}
