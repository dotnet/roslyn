// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitYieldBreakStatement(BoundYieldBreakStatement node)
        {
            var result = (BoundStatement)base.VisitYieldBreakStatement(node);

            // We also add sequence points for the implicit "yield break" statement at the end of the method body
            // (added by FlowAnalysisPass.AppendImplicitReturn). Implicitly added "yield break" for async method 
            // does not need sequence points added here since it would be done later (presumably during Async rewrite).
            if (this.Instrument &&
                (!node.WasCompilerGenerated || (node.Syntax.Kind() == SyntaxKind.Block && _factory.CurrentFunction?.IsAsync == false)))
            {
                result = _instrumenter.InstrumentYieldBreakStatement(node, result);
            }

            return result;
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            var result = (BoundStatement)base.VisitYieldReturnStatement(node);
            if (this.Instrument && !node.WasCompilerGenerated)
            {
                result = _instrumenter.InstrumentYieldReturnStatement(node, result);
            }

            return result;
        }
    }
}
