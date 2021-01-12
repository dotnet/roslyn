// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.NamespaceSync
{
    internal abstract class AbstractSyncNamespaceCodeFixProvider : CodeFixProvider
    {
        protected abstract CodeAction CreateCodeAction(Func<CancellationToken, Task<Solution>> createChangedSolution);

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.NamespaceSyncAnalyzerDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CreateCodeAction(cancellationToken => FixAsync(context.Document, context.Diagnostics, cancellationToken)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected static async Task<Solution> FixAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var currentDocument = document;

            foreach (var diagnostic in diagnostics)
            {
                currentDocument = await FixSingleDiagnosticAsync(currentDocument, diagnostic, cancellationToken).ConfigureAwait(false);
            }

            return currentDocument.Project.Solution;
        }

        private static async Task<Document> FixSingleDiagnosticAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var targetNamespace = diagnostic.Properties["TargetNamespace"];

            // Use the Renamer.RenameDocumentAsync API to sync namespaces in the document. This allows
            // us to keep in line with the sync methodology that we have as a public API and not have 
            // to rewrite or move the complex logic. RenameDocumentAsync is designed to behave the same
            // as the intent of this analyzer/codefix pair.
            var targetFolders = PathMetadataUtilities.BuildFoldersFromNamespace(targetNamespace);
            var documentWithNoFolders = document.WithFolders(Array.Empty<string>());
            var renameActionSet = await Renamer.RenameDocumentAsync(
                documentWithNoFolders,
                documentWithNoFolders.Name,
                newDocumentFolders: targetFolders,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var newSolution = await renameActionSet.UpdateSolutionAsync(documentWithNoFolders.Project.Solution, cancellationToken).ConfigureAwait(false);
            Debug.Assert(newSolution != document.Project.Solution);

            return newSolution.GetRequiredDocument(document.Id);
        }

        public override FixAllProvider? GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;
    }
}
