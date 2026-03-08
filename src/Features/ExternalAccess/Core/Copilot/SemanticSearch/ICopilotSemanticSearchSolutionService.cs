// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Copilot.SemanticSearch;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;
#endif

internal interface ICopilotSemanticSearchSolutionService
{
    DocumentId GetQueryDocumentId(Solution solution);
    string GetQueryDocumentFilePath();

    (WorkspaceChangeKind changeKind, ProjectId? projectId, DocumentId? documentId) GetWorkspaceChangeKind(Solution oldSolution, Solution newSolution);

    Solution SetQueryText(Solution solution, string? query, string? targetLanguage, string referenceAssembliesDir);
}
