// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal abstract class SemanticSearchWorkspace(HostServices services, ISemanticSearchSolutionService solutionService)
    : Workspace(services, WorkspaceKind.SemanticSearch)
{
    public override bool CanOpenDocuments
        => true;

    public override bool CanApplyChange(ApplyChangesKind feature)
        => feature == ApplyChangesKind.ChangeDocument;

    public async Task<Document> UpdateQueryDocumentAsync(string? query, CancellationToken cancellationToken)
    {
        var (updated, newSolution) = await this.SetCurrentSolutionAsync(
            useAsync: true,
            transformation: oldSolution => solutionService.SetQueryText(oldSolution, query),
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
