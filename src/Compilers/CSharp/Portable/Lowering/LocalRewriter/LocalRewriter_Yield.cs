// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitYieldBreakStatement(BoundYieldBreakStatement node)
        {
            var result = (BoundStatement)base.VisitYieldBreakStatement(node)!;

            // We also add sequence points for the implicit "yield break" statement at the end of the method body
            // (added by FlowAnalysisPass.AppendImplicitReturn). Implicitly added "yield break" for async method 
            // does not need sequence points added here since it would be done later (presumably during Async rewrite).
            if (this.Instrument &&
                (!node.WasCompilerGenerated || (node.Syntax.Kind() == SyntaxKind.Block && _factory.CurrentFunction?.IsAsync == false)))
            {
                result = Instrumenter.InstrumentYieldBreakStatement(node, result);
            }

            return result;
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            var result = (BoundStatement)base.VisitYieldReturnStatement(node)!;
            if (this.Instrument && !node.WasCompilerGenerated)
            {
                result = Instrumenter.InstrumentYieldReturnStatement(node, result);
            }

            return result;
        }
    }
}
