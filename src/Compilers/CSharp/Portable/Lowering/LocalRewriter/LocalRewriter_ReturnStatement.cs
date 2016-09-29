// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            BoundStatement rewritten = (BoundStatement)base.VisitReturnStatement(node);

            // NOTE: we will apply sequence points to synthesized return 
            // statements if they are contained in lambdas and have expressions
            // or if they are expression-bodied properties.
            // We do this to ensure that expression lambdas and expression-bodied
            // properties have sequence points.
            // We also add sequence points for the implicit "return" statement at the end of the method body
            // (added by FlowAnalysisPass.AppendImplicitReturn). Implicitly added return for async method 
            // does not need sequence points added here since it would be done later (presumably during Async rewrite).
            if (this.Instrument &&
                (!node.WasCompilerGenerated ||
                 (node.ExpressionOpt != null ? 
                        IsLambdaOrExpressionBodiedMember :
                        (node.Syntax.Kind() == SyntaxKind.Block && _factory.CurrentMethod?.IsAsync == false))))
            {
                rewritten = _instrumenter.InstrumentReturnStatement(node, rewritten);
            }

            return rewritten;
        }

        private bool IsLambdaOrExpressionBodiedMember
        {
            get
            {
                var method = _factory.CurrentMethod;
                if (method is LambdaSymbol)
                {
                    return true;
                }
                var sourceMethod = method as SourceMethodSymbol;
                return sourceMethod != null
                    && sourceMethod.IsExpressionBodied;
            }
        }
    }
}
