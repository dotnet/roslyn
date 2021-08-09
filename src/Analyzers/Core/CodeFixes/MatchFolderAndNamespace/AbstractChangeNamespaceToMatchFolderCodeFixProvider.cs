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

            var targetFolders = PathMetadataUtilities.BuildFoldersFromNamespace(targetNamespace, document.Project.DefaultNamespace);
            var action = SyncNamespaceDocumentAction.TryCreate(document, targetFolders, cancellationToken);
            if (action is not null)
            {
                return await action.GetModifiedSolutionAsync(document, document.Project.Solution.Options, cancellationToken).ConfigureAwait(false);
            }

            return document.Project.Solution;
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
