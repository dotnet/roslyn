// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess
{
    internal partial class InvokeDelegateWithConditionalAccessCodeFixProvider : CodeFixProvider
    {
        private class InvokeDelegateWithConditionalAccessFixAllProvider : BatchFixAllProvider
        {
            private readonly InvokeDelegateWithConditionalAccessCodeFixProvider _provider;

            public InvokeDelegateWithConditionalAccessFixAllProvider(InvokeDelegateWithConditionalAccessCodeFixProvider provider)
            {
                _provider = provider;
            }

            internal override async Task<CodeAction> GetFixAsync(
                ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
                FixAllState fixAllState,
                CancellationToken cancellationToken)
            {
                // Process all documents in parallel.
                var updatedDocumentTasks = documentsAndDiagnosticsToFixMap.Select(
                    kvp => FixAsync(fixAllState, kvp.Key, kvp.Value, cancellationToken));

                await Task.WhenAll(updatedDocumentTasks).ConfigureAwait(false);

                var currentSolution = fixAllState.Solution;
                foreach (var task in updatedDocumentTasks)
                {
                    var updatedDocument = await task.ConfigureAwait(false);
                    currentSolution = currentSolution.WithDocumentSyntaxRoot(
                        updatedDocument.Id,
                        await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
                }

                var title = GetFixAllTitle(fixAllState);
                return new CodeAction.SolutionChangeAction(title, _ => Task.FromResult(currentSolution));
            }

            private Task<Document> FixAsync(
                FixAllState fixAllState, Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
            {
                // Filter out the diagnostics we created for the faded out code.  We don't want
                // to try to fix those as well as the normal diagnostics we created.
                var filteredDiagnostics = diagnostics.WhereAsArray(
                    d => !d.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary));

                // Defer to the actual SimplifyNullCheckCodeFixProvider to process htis
                // document.  It can process all the diagnostics and apply them properly.
                return _provider.FixAllAsync(document, filteredDiagnostics, cancellationToken);
            }
        }
    }
}
