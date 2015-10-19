// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class PlaceholderLocalRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        internal static BoundNode Rewrite(CSharpCompilation compilation, EENamedTypeSymbol container, HashSet<LocalSymbol> declaredLocals, BoundNode node, DiagnosticBag diagnostics)
        {
            var rewriter = new PlaceholderLocalRewriter(compilation, container, declaredLocals, diagnostics);
            return rewriter.Visit(node);
        }

        private readonly CSharpCompilation _compilation;
        private readonly EENamedTypeSymbol _container;
        private readonly HashSet<LocalSymbol> _declaredLocals;
        private readonly DiagnosticBag _diagnostics;

        private PlaceholderLocalRewriter(CSharpCompilation compilation, EENamedTypeSymbol container, HashSet<LocalSymbol> declaredLocals, DiagnosticBag diagnostics)
        {
            _compilation = compilation;
            _container = container;
            _declaredLocals = declaredLocals;
            _diagnostics = diagnostics;
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            var result = RewriteLocal(node);
            Debug.Assert(result.Type == node.Type);
            return result;
        }

        private BoundExpression RewriteLocal(BoundLocal node)
        {
            var local = node.LocalSymbol;
            var placeholder = local as PlaceholderLocalSymbol;
            if ((object)placeholder != null)
            {
                return placeholder.RewriteLocal(_compilation, _container, node.Syntax, _diagnostics);
            }
            if (_declaredLocals.Contains(local))
            {
                return ObjectIdLocalSymbol.RewriteLocal(_compilation, _container, node.Syntax, local);
            }
            return node;
        }
    }
}
