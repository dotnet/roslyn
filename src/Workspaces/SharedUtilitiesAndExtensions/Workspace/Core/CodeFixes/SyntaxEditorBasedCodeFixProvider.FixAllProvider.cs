// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.CodeFixes;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal partial class SyntaxEditorBasedCodeFixProvider : CodeFixProvider
    {
        /// <summary>
        /// A simple implementation of <see cref="FixAllProvider"/> that takes care of collecting
        /// all the diagnostics and fixes all documents in parallel.  The only functionality a 
        /// subclass needs to provide is how each document will apply all the fixes to all the 
        /// diagnostics in that document.
        /// </summary>
        internal sealed class SyntaxEditorBasedFixAllProvider : AbstractParallelFixAllProvider
        {
            private readonly SyntaxEditorBasedCodeFixProvider _codeFixProvider;

            public SyntaxEditorBasedFixAllProvider(SyntaxEditorBasedCodeFixProvider codeFixProvider)
                => _codeFixProvider = codeFixProvider;

            protected override bool IncludeDiagnosticDuringFixAll(FixAllContext fixAllContext, Diagnostic diagnostic)
                => _codeFixProvider.IncludeDiagnosticDuringFixAll(diagnostic, fixAllContext.Document, fixAllContext.CodeActionEquivalenceKey, fixAllContext.CancellationToken);

            protected override Task<Document> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> filteredDiagnostics)
                => _codeFixProvider.FixAllAsync(document, filteredDiagnostics, fixAllContext.CancellationToken);
        }
    }
}
