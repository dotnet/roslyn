// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.MatchFolderAndNamespace;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.MatchFolderAndNamespace
{
    internal abstract partial class AbstractChangeNamespaceToMatchFolderCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.MatchFolderAndNamespaceDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocumentInfo))
            {
                context.RegisterCodeFix(
                    new MyCodeAction(AnalyzersResources.Change_namespace_to_match_folder_structure, cancellationToken => FixAllInDocumentAsync(context.Document, context.Diagnostics, cancellationToken)),
                    context.Diagnostics);
            }

            return Task.CompletedTask;
        }

        private static async Task<Solution> FixAllInDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            // All the target namespaces should be the same for a given document
            Debug.Assert(diagnostics.Select(diagnostic => diagnostic.Properties[MatchFolderAndNamespaceConstants.TargetNamespace]).Distinct().Count() == 1);

            var targetNamespace = diagnostics.First().Properties[MatchFolderAndNamespaceConstants.TargetNamespace];
            RoslynDebug.AssertNotNull(targetNamespace);

            // Use the Renamer.RenameDocumentAsync API to sync namespaces in the document. This allows
            // us to keep in line with the sync methodology that we have as a public API and not have 
            // to rewrite or move the complex logic. RenameDocumentAsync is designed to behave the same
            // as the intent of this analyzer/codefix pair.
            var targetFolders = PathMetadataUtilities.BuildFoldersFromNamespace(targetNamespace, document.Project.DefaultNamespace);
            var documentWithInvalidFolders = document.WithFolders(document.Folders.Concat("Force-Namespace-Change"));
            var renameActionSet = await Renamer.RenameDocumentAsync(
                documentWithInvalidFolders,
                new DocumentRenameOptions(),
                documentWithInvalidFolders.Name,
                newDocumentFolders: targetFolders,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var newSolution = await renameActionSet.UpdateSolutionAsync(documentWithInvalidFolders.Project.Solution, cancellationToken).ConfigureAwait(false);
            Debug.Assert(newSolution != document.Project.Solution);
            return newSolution;
        }

        public override FixAllProvider? GetFixAllProvider()
            => CustomFixAllProvider.Instance;

        private sealed class MyCodeAction : CustomCodeActions.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution, title)
            {
            }
        }
    }
}
