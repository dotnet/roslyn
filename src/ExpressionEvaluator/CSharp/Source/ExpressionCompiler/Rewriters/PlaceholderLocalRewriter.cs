// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class PlaceholderLocalRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        internal static BoundNode Rewrite(CSharpCompilation compilation, HashSet<LocalSymbol> declaredLocals, BoundNode node, DiagnosticBag diagnostics)
        {
            var rewriter = new PlaceholderLocalRewriter(compilation, declaredLocals, diagnostics);
            return rewriter.Visit(node);
        }

        private readonly CSharpCompilation _compilation;
        private readonly HashSet<LocalSymbol> _declaredLocals;
        private readonly DiagnosticBag _diagnostics;

        private PlaceholderLocalRewriter(CSharpCompilation compilation, HashSet<LocalSymbol> declaredLocals, DiagnosticBag diagnostics)
        {
            _compilation = compilation;
            _declaredLocals = declaredLocals;
            _diagnostics = diagnostics;
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            var result = RewriteLocal(node);
            Debug.Assert(TypeSymbol.Equals(result.Type, node.Type, TypeCompareKind.ConsiderEverything2));
            return result;
        }

        private BoundExpression RewriteLocal(BoundLocal node)
        {
            var local = node.LocalSymbol;
            var placeholder = local as PlaceholderLocalSymbol;
            if ((object)placeholder != null)
            {
                return placeholder.RewriteLocal(_compilation, node.Syntax, _diagnostics);
            }
            if (_declaredLocals.Contains(local))
            {
                return ObjectIdLocalSymbol.RewriteLocal(_compilation, node.Syntax, local);
            }
            return node;
        }
    }
}
