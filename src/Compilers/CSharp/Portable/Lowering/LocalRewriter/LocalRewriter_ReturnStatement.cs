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
            if (this.Instrument &&
                (!rewritten.WasCompilerGenerated ||
                 (node.ExpressionOpt != null && IsLambdaOrExpressionBodiedMember)))
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
