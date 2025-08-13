// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SemanticSearch;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.SemanticSearch;

[Export(typeof(ISemanticSearchSolutionService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CopilotSemanticSearchSolutionService(
    [Import(AllowDefault = true)] ICopilotSemanticSearchSolutionService? impl) : ISemanticSearchSolutionService
{
    private ICopilotSemanticSearchSolutionService GetImpl()
    {
        Contract.ThrowIfNull(impl);
        return impl;
    }

    public string GetQueryDocumentFilePath()
        => GetImpl().GetQueryDocumentFilePath();

    public DocumentId GetQueryDocumentId(Solution solution)
        => GetImpl().GetQueryDocumentId(solution);

    public (WorkspaceChangeKind changeKind, ProjectId? projectId, DocumentId? documentId) GetWorkspaceChangeKind(Solution oldSolution, Solution newSolution)
        => GetImpl().GetWorkspaceChangeKind(oldSolution, newSolution);

    public Solution SetQueryText(Solution solution, string? query, string? targetLanguage, string referenceAssembliesDir)
        => GetImpl().SetQueryText(solution, query, targetLanguage, referenceAssembliesDir);
}
