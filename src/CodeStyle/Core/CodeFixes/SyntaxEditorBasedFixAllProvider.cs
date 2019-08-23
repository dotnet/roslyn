// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal abstract partial class SyntaxEditorBasedCodeFixProvider : CodeFixProvider
    {
        private sealed class SyntaxEditorBasedFixAllProvider : DocumentBasedFixAllProvider
        {
            public SyntaxEditorBasedCodeFixProvider _codeFixProvider;
            public SyntaxEditorBasedFixAllProvider(SyntaxEditorBasedCodeFixProvider codeFixProvider)
            {
                _codeFixProvider = codeFixProvider;
            }

            protected override string GetCodeActionTitle(FixAllContext fixAllContext) => fixAllContext.Scope.ToString();

            protected override Task<Document> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
                => _codeFixProvider.FixAllAsync(document, diagnostics, fixAllContext.CancellationToken);
        }
    }
}
