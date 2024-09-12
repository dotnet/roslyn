// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.MatchFolderAndNamespace;

/// <summary>
/// Custom fix all provider for namespace sync. Does fix all on per document level. Since
/// multiple documents may be updated when changing a single namespace, it happens 
/// on a sequential level instead of batch fixing and merging the changes. This prevents
/// collisions that the batch fixer won't handle correctly but is slower.
/// </summary>
internal abstract partial class AbstractChangeNamespaceToMatchFolderCodeFixProvider
{
    private class CustomFixAllProvider : FixAllProvider
    {
        public static readonly CustomFixAllProvider Instance = new();

        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            var diagnostics = fixAllContext.Scope switch
            {
                FixAllScope.Document when fixAllContext.Document is not null => await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false),
                FixAllScope.Project => await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project).ConfigureAwait(false),
                FixAllScope.Solution => await GetSolutionDiagnosticsAsync(fixAllContext).ConfigureAwait(false),
                _ => default
            };

            if (diagnostics.IsDefaultOrEmpty)
                return null;

            var title = fixAllContext.GetDefaultFixAllTitle();
            return CodeAction.Create(
                title,
                cancellationToken => FixAllByDocumentAsync(
                    fixAllContext.Project.Solution,
                    diagnostics,
                    fixAllContext.Progress,
#if CODE_STYLE
                    CodeActionOptions.DefaultProvider,
#else
                    fixAllContext.State.CodeActionOptionsProvider,
#endif
                    cancellationToken),
                title);

            static async Task<ImmutableArray<Diagnostic>> GetSolutionDiagnosticsAsync(FixAllContext fixAllContext)
            {
                var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

                foreach (var project in fixAllContext.Solution.Projects)
                {
                    var projectDiagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                    diagnostics.AddRange(projectDiagnostics);
                }

                return diagnostics.ToImmutable();
            }
        }

        private static async Task<Solution> FixAllByDocumentAsync(
            Solution solution,
            ImmutableArray<Diagnostic> diagnostics,
            IProgress<CodeAnalysisProgress> progressTracker,
            CodeActionOptionsProvider options,
            CancellationToken cancellationToken)
        {
            // Use documentId instead of tree here because the
            // FixAsync call can modify more than one document per call. The
            // important thing is that the fix works on fixing the namespaces in a single document,
            // but references in other documents will be updated to be correct. Id will remain
            // across this mutation, but lookup via SyntaxTree directly will not work because
            // the tree won't be the same.
            var documentIdToDiagnosticsMap = diagnostics
                .GroupBy(diagnostic => diagnostic.Location.SourceTree)
                .Where(group => group.Key is not null)
                .SelectAsArray(group => (id: solution.GetRequiredDocument(group.Key!).Id, diagnostics: group.ToImmutableArray()));

            var newSolution = solution;

            progressTracker.AddItems(documentIdToDiagnosticsMap.Length);

            foreach (var (documentId, diagnosticsInTree) in documentIdToDiagnosticsMap)
            {
                var document = newSolution.GetRequiredDocument(documentId);
                using var _ = progressTracker.ItemCompletedScope(document.Name);

                newSolution = await FixAllInDocumentAsync(document, diagnosticsInTree, options, cancellationToken).ConfigureAwait(false);
            }

            return newSolution;
        }
    }
}
