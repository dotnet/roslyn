// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.FixAllOccurrences;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal partial class AbstractUseExpressionBodyCodeFixProvider<TDeclaration> : CodeFixProvider
        where TDeclaration : SyntaxNode
    {
        private class UseExpressionBodyFixAllProvider : DocumentBasedFixAllProvider
        {
            private readonly AbstractUseExpressionBodyCodeFixProvider<TDeclaration> _provider;

            public UseExpressionBodyFixAllProvider(AbstractUseExpressionBodyCodeFixProvider<TDeclaration> provider)
            {
                _provider = provider;
            }

            protected override Task<Document> FixDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
                => _provider.FixAllAsync(document, diagnostics, cancellationToken);
        }
    }
}