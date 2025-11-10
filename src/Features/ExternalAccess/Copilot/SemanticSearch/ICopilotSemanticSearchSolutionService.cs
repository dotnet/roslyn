// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;

internal interface ICopilotSemanticSearchSolutionService
{
    DocumentId GetQueryDocumentId(Solution solution);
    string GetQueryDocumentFilePath();

    (WorkspaceChangeKind changeKind, ProjectId? projectId, DocumentId? documentId) GetWorkspaceChangeKind(Solution oldSolution, Solution newSolution);

    Solution SetQueryText(Solution solution, string? query, string? targetLanguage, string referenceAssembliesDir);
}
