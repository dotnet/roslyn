// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            // NOTE: we will apply sequence points to synthesized return statements if they are contained in lambdas and have expressions.
            // We do this to ensure that expression lambdas have sequence points, as in Dev10.
            if (this.generateDebugInfo &&
                (!rewritten.WasCompilerGenerated || (node.ExpressionOpt != null && this.factory.CurrentMethod is LambdaSymbol)))
            {
                // We're not calling AddSequencePoint since it ignores compiler-generated nodes.
                return new BoundSequencePoint(rewritten.Syntax, rewritten);
            }

            return rewritten;
        }
    }
}
