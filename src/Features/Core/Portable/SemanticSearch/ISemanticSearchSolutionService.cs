// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal interface ISemanticSearchSolutionService
{
    /// <summary>
    /// Returns the id of the document that contains the query.
    /// </summary>
    DocumentId GetQueryDocumentId(Solution solution);

    /// <summary>
    /// Retunrs the file path of the document that contains the query..
    /// </summary>
    string GetQueryDocumentFilePath();

    /// <summary>
    /// Transforms given <paramref name="solution"/> to a new one with given <paramref name="query"/> text.
    /// </summary>
    /// <param name="solution">Original solution.</param>
    /// <param name="query">New query, or null to use default query.</param>
    /// <param name="targetLanguage">Language of the target projects the query executes against, or null if it should execute against all supported projects.</param>
    /// <param name="referenceAssembliesDir">Directory containing reference assemblies.</param>
    Solution SetQueryText(Solution solution, string? query, string? targetLanguage, string referenceAssembliesDir);

    (WorkspaceChangeKind changeKind, ProjectId? projectId, DocumentId? documentId) GetWorkspaceChangeKind(Solution oldSolution, Solution newSolution);
}
