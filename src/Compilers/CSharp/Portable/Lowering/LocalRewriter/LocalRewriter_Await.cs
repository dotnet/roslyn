// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            return VisitAwaitExpression(node, true);
        }

        public BoundExpression VisitAwaitExpression(BoundAwaitExpression node, bool used)
        {
            return RewriteAwaitExpression((BoundAwaitExpression)base.VisitAwaitExpression(node)!, used);
        }

        private BoundExpression RewriteAwaitExpression(SyntaxNode syntax, BoundExpression rewrittenExpression, BoundAwaitableInfo awaitableInfo, TypeSymbol type, BoundAwaitExpressionDebugInfo debugInfo, bool used)
        {
            return RewriteAwaitExpression(new BoundAwaitExpression(syntax, rewrittenExpression, awaitableInfo, debugInfo, type) { WasCompilerGenerated = true }, used);
        }

        /// <summary>
        /// Lower an await expression that has already had its components rewritten.
        /// </summary>
        private BoundExpression RewriteAwaitExpression(BoundAwaitExpression rewrittenAwait, bool used)
        {
            _sawAwait = true;

            if (_factory.CurrentFunction?.IsAsync2 == true)
            {
                // REWRITE: 
                // await arg
                //
                // == INTO ===> 
                //
                // sequence
                // {
                //   var awaiter = arg.GetAwaiter();
                //   if (awaiter.IsComplete())
                //   {
                //       UnsafeAwaitAwaiterFromRuntimeAsync(awaiter)
                //   }
                //   awaiter.GetResult()
                // }

                BoundCall getAwaiter = _factory.Call(rewrittenAwait.Expression, (MethodSymbol)rewrittenAwait.AwaitableInfo.GetAwaiter!.ExpressionSymbol!);
                BoundLocal awaiterTmp = _factory.StoreToTemp(getAwaiter, out var awaiterTempAssignment);

                BoundCall isCompleted = _factory.Call(awaiterTmp, rewrittenAwait.AwaitableInfo.IsCompleted!.GetMethod);

                MethodSymbol helperMethod = (MethodSymbol)_factory.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__UnsafeAwaitAwaiterFromRuntimeAsync_TAwaiter)!;
                helperMethod = helperMethod.Construct(getAwaiter.Type);
                BoundCall helperCall = _factory.Call(null, helperMethod, awaiterTmp);

                BoundExpression ifNotCompleteCallHelper = _factory.Conditional(isCompleted, _factory.Default(helperCall.Type), helperCall, helperCall.Type);

                MethodSymbol getResultMethod = (MethodSymbol)rewrittenAwait.AwaitableInfo.GetResult!;
                BoundCall getResult = _factory.Call(awaiterTmp, getResultMethod);

                return _factory.Sequence(
                    ImmutableArray.Create(awaiterTmp.LocalSymbol),
                    ImmutableArray.Create(
                        awaiterTempAssignment,
                        ifNotCompleteCallHelper),
                    getResult);
            }

            if (!used)
            {
                // Await expression is already at the statement level.
                return rewrittenAwait;
            }

            // The await expression will be lowered to code that involves the use of side-effects
            // such as jumps and labels, which we can only emit with an empty stack, so we require
            // that the await expression itself is produced only when the stack is empty.
            // Therefore it is represented by a BoundSpillSequence.  The resulting nodes will be "spilled" to move
            // such statements to the top level (i.e. into the enclosing statement list).  Here we ensure
            // that the await result itself is stored into a temp at the statement level, as that is
            // the form handled by async lowering.
            _needsSpilling = true;
            var tempAccess = _factory.StoreToTemp(rewrittenAwait, out BoundAssignmentOperator tempAssignment, syntaxOpt: rewrittenAwait.Syntax,
                kind: SynthesizedLocalKind.Spill);
            return new BoundSpillSequence(
                syntax: rewrittenAwait.Syntax,
                locals: ImmutableArray.Create<LocalSymbol>(tempAccess.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignment),
                value: tempAccess,
                type: tempAccess.Type);
        }
    }
}
