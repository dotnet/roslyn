// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.NamespaceSync
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SyncNamespace), Shared]
    internal class CSharpNamespaceSyncCodeFixProvider : CodeFixProvider
    {

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpNamespaceSyncCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.NamespaceSyncAnalyzerDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(cancellationToken => FixAsync(context.Document, cancellationToken)),
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
            var documentWithNoFolders = document.WithFolders(new string[0]);
            var renameActionSet = await Renamer.RenameDocumentAsync(
                documentWithNoFolders,
                documentWithNoFolders.Name,
                newDocumentFolders: currentFolders,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return await renameActionSet.UpdateSolutionAsync(documentWithNoFolders.Project.Solution, cancellationToken).ConfigureAwait(false);
        }

        public override FixAllProvider? GetFixAllProvider()
        {
            return new MyFixAllProvider();
        }

        private sealed class MyFixAllProvider : FixAllProvider
        {
            private static string CodeActionTitle => "Sync namespaces";

            public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                CodeAction? fixAction;
                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                        fixAction = new CustomCodeActions.SolutionChangeAction(
                            CodeActionTitle,
                            cancellationToken => GetDocumentFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
                            nameof(DocumentBasedFixAllProvider));
                        break;

                    case FixAllScope.Project:
                        fixAction = new CustomCodeActions.SolutionChangeAction(
                            CodeActionTitle,
                            cancellationToken => GetProjectFixesAsync(fixAllContext.WithCancellationToken(cancellationToken), fixAllContext.Project),
                            nameof(DocumentBasedFixAllProvider));
                        break;

                    case FixAllScope.Solution:
                        fixAction = new CustomCodeActions.SolutionChangeAction(
                            CodeActionTitle,
                            cancellationToken => GetSolutionFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
                            nameof(DocumentBasedFixAllProvider));
                        break;

                    case FixAllScope.Custom:
                    default:
                        fixAction = null;
                        break;
                }

                return Task.FromResult(fixAction);
            }

            private static Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext)
            {
                var documents = fixAllContext.Solution.Projects.SelectMany(i => i.Documents).ToImmutableArray();
                return GetSolutionFixesAsync(fixAllContext, documents);
            }

            private static Task<Solution> GetProjectFixesAsync(FixAllContext fixAllContext, Project project)
                => GetSolutionFixesAsync(fixAllContext, project.Documents.ToImmutableArray());

            private static Task<Solution> GetDocumentFixesAsync(FixAllContext fixAllContext)
            {
                RoslynDebug.AssertNotNull(fixAllContext.Document);
                return FixAsync(fixAllContext.Document, fixAllContext.CancellationToken);
            }

            private static async Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext, ImmutableArray<Document> documents)
            {
                var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext, progressTrackerOpt: null).ConfigureAwait(false);

                using var _ = PooledHashSet<DocumentId>.GetInstance(out var documentIds);
                foreach (var document in documents)
                {
                    // Don't bother examining any documents that aren't in the list of docs that
                    // actually have diagnostics.
                    if (!documentDiagnosticsToFix.TryGetValue(document, out var diagnostics))
                        continue;

                    documentIds.Add(document.Id);
                }

                var solution = fixAllContext.Solution;

                // The fixes have to be applied one at a time since they can cause merge conflicts
                // by modifying other documents referencing the definitions in the namespaces that are changing.
                foreach (var documentId in documentIds)
                {
                    var document = solution.GetRequiredDocument(documentId);
                    solution = await FixAsync(document, fixAllContext.CancellationToken).ConfigureAwait(false);
                }

                return solution;
            }
        }

        private sealed class MyCodeAction : CustomCodeActions.SolutionChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(CSharpAnalyzersResources.Namespace_must_match_folder_structure, createChangedSolution, CSharpAnalyzersResources.Namespace_must_match_folder_structure)
            {

            }
        }
    }
}
