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
        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            BoundStatement rewritten = (BoundStatement)base.VisitReturnStatement(node)!;

            // NOTE: we will apply sequence points to synthesized return 
            // statements if they are contained in lambdas and have expressions
            // or if they are expression-bodied properties.
            // We do this to ensure that expression lambdas and expression-bodied
            // properties have sequence points.
            // We also add sequence points for the implicit "return" statement at the end of the method body
            // (added by FlowAnalysisPass.AppendImplicitReturn). Implicitly added return for async method
            // does not need sequence points added here since it would be done later (presumably during Async rewrite),
            // except in runtime async where the method body is emitted directly.
            var currentFunction = _factory.CurrentFunction;
            var isRuntimeAsync = currentFunction is not null && _compilation.IsRuntimeAsyncEnabledIn(currentFunction);
            if (this.Instrument &&
                (!node.WasCompilerGenerated ||
                 (node.ExpressionOpt != null ?
                        IsLambdaOrExpressionBodiedMember :
                        (node.Syntax.Kind() == SyntaxKind.Block && (currentFunction?.IsAsync == false || isRuntimeAsync)))))
            {
                rewritten = Instrumenter.InstrumentReturnStatement(node, rewritten);
            }

            return rewritten;
        }

        private bool IsLambdaOrExpressionBodiedMember
        {
            get
            {
                var method = _factory.CurrentFunction;
                if (method is LambdaSymbol)
                {
                    return true;
                }

                return
                    (method as SourceMemberMethodSymbol)?.IsExpressionBodied ??
                    (method as LocalFunctionSymbol)?.IsExpressionBodied ?? false;
            }
        }
    }
}
