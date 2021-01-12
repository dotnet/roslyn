// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.CodeFixes.NamespaceSync
{
    internal abstract class AbstractSyncNamespaceCodeFixProvider : CodeFixProvider
    {
        protected abstract CodeAction CreateCodeAction(Func<CancellationToken, Task<Solution>> createChangedSolution);

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.NamespaceSyncAnalyzerDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CreateCodeAction(cancellationToken => FixAsync(context.Document, cancellationToken)),
                context.Diagnostics);

            return Task.CompletedTask;
        }


        protected static async Task<Solution> FixAsync(Document document, CancellationToken cancellationToken)
        {
            // Use the Renamer.RenameDocumentAsync API to sync namespaces in the document. This allows
            // us to keep in line with the sync methodology that we have as a public API and not have 
            // to rewrite or move the complex logic. RenameDocumentAsync is designed to behave the same
            // as the intent of this analyzer/codefix pair.
            var currentFolders = document.Folders;
            var documentWithNoFolders = document.WithFolders(Array.Empty<string>());
            var renameActionSet = await Renamer.RenameDocumentAsync(
                documentWithNoFolders,
                documentWithNoFolders.Name,
                newDocumentFolders: currentFolders,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return await renameActionSet.UpdateSolutionAsync(documentWithNoFolders.Project.Solution, cancellationToken).ConfigureAwait(false);
        }

        public override FixAllProvider? GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;
    }
}
