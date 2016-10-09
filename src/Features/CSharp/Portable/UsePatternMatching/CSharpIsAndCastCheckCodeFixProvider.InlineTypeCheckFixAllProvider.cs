// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.FixAllOccurrences;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    internal partial class CSharpIsAndCastCheckCodeFixProvider : CodeFixProvider
    {
        private class InlineTypeCheckFixAllProvider : DocumentBasedFixAllProvider
        {
            private readonly CSharpIsAndCastCheckCodeFixProvider _provider;

            public InlineTypeCheckFixAllProvider(CSharpIsAndCastCheckCodeFixProvider provider)
            {
                _provider = provider;
            }

            protected override Task<Document> FixDocumentAsync(
                Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
            {
                return _provider.FixAllAsync(document, diagnostics, cancellationToken);
            }
        }
    }
}