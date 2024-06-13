// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ISolutionExtensions
{
    public static IEnumerable<DocumentId> GetChangedDocuments(this Solution? newSolution, Solution oldSolution)
    {
        if (newSolution != null)
        {
            var solutionChanges = newSolution.GetChanges(oldSolution);

            foreach (var projectChanges in solutionChanges.GetProjectChanges())
            {
                foreach (var documentId in projectChanges.GetChangedDocuments())
                {
                    yield return documentId;
                }
            }
        }
    }

    public static TextDocument? GetTextDocument(this Solution solution, DocumentId? documentId)
        => solution.GetDocument(documentId) ?? solution.GetAdditionalDocument(documentId) ?? solution.GetAnalyzerConfigDocument(documentId);

    public static Document GetRequiredDocument(this Solution solution, SyntaxTree syntaxTree)
        => solution.GetDocument(syntaxTree) ?? throw new InvalidOperationException();

    public static Project GetRequiredProject(this Solution solution, ProjectId projectId)
    {
        var project = solution.GetProject(projectId);
        if (project == null)
        {
            throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.Project_of_ID_0_is_required_to_accomplish_the_task_but_is_not_available_from_the_solution, projectId));
        }

        return project;
    }

    public static Document GetRequiredDocument(this Solution solution, DocumentId documentId)
    {
        if (documentId is null)
            throw new ArgumentNullException(nameof(documentId));

#if !CODE_STYLE
        // If we get a source-generated DocumentId, we can give a different exception to make it clear the type of failure this is; otherwise a failure of
        // this in the wild is hard to guess whether this is because of a logic bug in the feature (where it tried to use a DocumentId for a document that disappeared)
        // or whether it hasn't been correctly updated to handle source generated files.
        if (documentId.IsSourceGenerated)
            throw new ArgumentException($"{nameof(GetRequiredDocument)} was given a source-generated DocumentId, but it will never return a source generated document. The caller needs to be calling some other method.");
#endif

        return solution.GetDocument(documentId) ?? throw CreateDocumentNotFoundException();
    }

#if !CODE_STYLE
    public static async ValueTask<Document> GetRequiredDocumentAsync(this Solution solution, DocumentId documentId, bool includeSourceGenerated = false, CancellationToken cancellationToken = default)
        => (await solution.GetDocumentAsync(documentId, includeSourceGenerated, cancellationToken).ConfigureAwait(false)) ?? throw CreateDocumentNotFoundException();

    public static async ValueTask<TextDocument> GetRequiredTextDocumentAsync(this Solution solution, DocumentId documentId, CancellationToken cancellationToken = default)
        => (await solution.GetTextDocumentAsync(documentId, cancellationToken).ConfigureAwait(false)) ?? throw CreateDocumentNotFoundException();
#endif

    public static TextDocument GetRequiredAdditionalDocument(this Solution solution, DocumentId documentId)
        => solution.GetAdditionalDocument(documentId) ?? throw CreateDocumentNotFoundException();

    public static TextDocument GetRequiredAnalyzerConfigDocument(this Solution solution, DocumentId documentId)
        => solution.GetAnalyzerConfigDocument(documentId) ?? throw CreateDocumentNotFoundException();

    public static TextDocument GetRequiredTextDocument(this Solution solution, DocumentId documentId)
        => solution.GetTextDocument(documentId) ?? throw CreateDocumentNotFoundException();

    private static Exception CreateDocumentNotFoundException()
        => new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

#if !CODE_STYLE
    public static Solution WithUpToDateSourceGeneratorDocuments(this Solution solution, ImmutableArray<ProjectId> projectIds)
    {
        // If the solution is already in automatic mode, then SG documents are already always up to date.
        var configuration = solution.Services.GetRequiredService<IWorkspaceConfigurationService>().Options;
        if (configuration.SourceGeneratorExecution is SourceGeneratorExecutionPreference.Automatic)
            return solution;

        var projectIdToSourceGenerationVersion = ImmutableSortedDictionary.CreateBuilder<ProjectId, SourceGeneratorExecutionVersion>();

        foreach (var projectId in projectIds)
        {
            var currentVersion = solution.GetSourceGeneratorExecutionVersion(projectId);
            projectIdToSourceGenerationVersion.Add(projectId, currentVersion.IncrementMinorVersion());
        }

        return solution.UpdateSpecificSourceGeneratorExecutionVersions(
            new SourceGeneratorExecutionVersionMap(projectIdToSourceGenerationVersion.ToImmutable()));
    }
#endif
}
