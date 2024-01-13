// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            Debug.Assert(node != null);

            var rewrittenCondition = VisitExpression(node.Condition);
            var rewrittenBody = VisitStatement(node.Body);
            Debug.Assert(rewrittenBody is { });
            var startLabel = new GeneratedLabelSymbol("start");

            var syntax = node.Syntax;

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            if (!node.WasCompilerGenerated && this.Instrument)
            {
                rewrittenCondition = Instrumenter.InstrumentDoStatementCondition(node, rewrittenCondition, _factory);
            }

            BoundStatement ifConditionGotoStart = new BoundConditionalGoto(syntax, rewrittenCondition, true, startLabel);

            if (!node.WasCompilerGenerated && this.Instrument)
            {
                ifConditionGotoStart = Instrumenter.InstrumentDoStatementConditionalGotoStart(node, ifConditionGotoStart);
            }

            // do
            //   body
            // while (condition);
            //
            // becomes
            //
            // start: 
            // {
            //   body
            //   continue:
            //   sequence point
            //   GotoIfTrue condition start;
            // }
            // break:

            if (node.Locals.IsEmpty)
            {
                return BoundStatementList.Synthesized(syntax, node.HasErrors,
                    new BoundLabelStatement(syntax, startLabel),
                    rewrittenBody,
                    new BoundLabelStatement(syntax, node.ContinueLabel),
                    ifConditionGotoStart,
                    new BoundLabelStatement(syntax, node.BreakLabel));
            }

            return BoundStatementList.Synthesized(syntax, node.HasErrors,
                new BoundLabelStatement(syntax, startLabel),
                new BoundBlock(syntax,
                               node.Locals,
                               ImmutableArray.Create<BoundStatement>(rewrittenBody,
                                                                     new BoundLabelStatement(syntax, node.ContinueLabel),
                                                                     ifConditionGotoStart)),
                new BoundLabelStatement(syntax, node.BreakLabel));
        }
    }
}
