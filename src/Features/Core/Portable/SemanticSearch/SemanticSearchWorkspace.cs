// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal abstract class SemanticSearchWorkspace(HostServices services, ISemanticSearchSolutionService solutionService)
    : Workspace(services, WorkspaceKind.SemanticSearch)
{
    /// <summary>
    /// Location of the directory containing reference assemblies used for semantic search queries.
    /// The assemblies are shared between design-time (in-proc workspace) and compile-time (OOP service).
    /// </summary>
    public static readonly string ReferenceAssembliesDirectory = Path.Combine(Path.GetDirectoryName(typeof(SemanticSearchWorkspace).Assembly.Location)!, "SemanticSearchRefs");

    public override bool CanOpenDocuments
        => true;

    public override bool CanApplyChange(ApplyChangesKind feature)
        => feature == ApplyChangesKind.ChangeDocument;

    public async Task<Document> UpdateQueryDocumentAsync(string? query, string? targetLanguage, CancellationToken cancellationToken)
    {
        var (updated, newSolution) = await this.SetCurrentSolutionAsync(
            useAsync: true,
            transformation: oldSolution => solutionService.SetQueryText(oldSolution, query, targetLanguage, ReferenceAssembliesDirectory),
            changeKind: solutionService.GetWorkspaceChangeKind,
            onBeforeUpdate: null,
            onAfterUpdate: null,
            cancellationToken).ConfigureAwait(false);

        var queryDocument = newSolution.GetRequiredDocument(solutionService.GetQueryDocumentId(newSolution));

        if (updated)
        {
            var newText = await queryDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            ApplyQueryDocumentTextChanged(newText);
        }

        return queryDocument;
    }

    protected virtual void ApplyQueryDocumentTextChanged(SourceText newText)
    {
    }
}
