// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

#if WORKSPACE
        if (documentId.IsSourceGenerated)
        {
            // If we get a source-generated DocumentId, we can give a different exception to make it clear the type of failure this is; otherwise a failure of
            // this in the wild is hard to guess whether this is because of a logic bug in the feature (where it tried to use a DocumentId for a document that disappeared)
            // or whether it hasn't been correctly updated to handle source generated files.
            throw new ArgumentException($"{nameof(GetRequiredDocument)} was given a source-generated DocumentId, but it will never return a source generated document. The caller needs to be calling some other method.");
        }
#endif

        return solution.GetDocument(documentId) ?? throw CreateDocumentNotFoundException(documentId.DebugName ?? "Unknown");
    }

#if WORKSPACE
    /// <summary>
    /// Returns the <see cref="SourceGeneratedDocument"/> for the given <see cref="DocumentId"/> if it exists and has been generated.
    /// </summary>
    /// <remarks>
    /// This method is intended to be called on generated document that are "frozen", and hence there is a 100% guarantee that their content
    /// is available. If the document is not generated, or if it is not frozen, there is an inherent race condition that could cause this method
    /// to throw an exception at essentially random times.
    /// </remarks>
    public static SourceGeneratedDocument GetRequiredSourceGeneratedDocumentForAlreadyGeneratedId(this Solution solution, DocumentId documentId)
    {
        if (documentId is null)
            throw new ArgumentNullException(nameof(documentId));

        var project = solution.GetRequiredProject(documentId.ProjectId);
        var sourceGeneratedDocument = project.TryGetSourceGeneratedDocumentForAlreadyGeneratedId(documentId);
        if (sourceGeneratedDocument == null)
            throw CreateDocumentNotFoundException(documentId.DebugName ?? "Unknown");

        return sourceGeneratedDocument;
    }

    public static ValueTask<Document> GetRequiredDocumentAsync(this Solution solution, DocumentId documentId, CancellationToken cancellationToken)
        => GetRequiredDocumentAsync(solution, documentId, includeSourceGenerated: false, cancellationToken);

    public static async ValueTask<Document> GetRequiredDocumentAsync(this Solution solution, DocumentId documentId, bool includeSourceGenerated, CancellationToken cancellationToken)
        => (await solution.GetDocumentAsync(documentId, includeSourceGenerated, cancellationToken).ConfigureAwait(false)) ?? throw CreateDocumentNotFoundException(documentId.DebugName ?? "Unknown");

    public static async ValueTask<TextDocument> GetRequiredTextDocumentAsync(this Solution solution, DocumentId documentId, CancellationToken cancellationToken = default)
        => (await solution.GetTextDocumentAsync(documentId, cancellationToken).ConfigureAwait(false)) ?? throw CreateDocumentNotFoundException(documentId.DebugName ?? "Unknown");
#endif

    public static TextDocument GetRequiredAdditionalDocument(this Solution solution, DocumentId documentId)
        => solution.GetAdditionalDocument(documentId) ?? throw CreateDocumentNotFoundException(documentId.DebugName ?? "Unknown");

    public static TextDocument GetRequiredAnalyzerConfigDocument(this Solution solution, DocumentId documentId)
        => solution.GetAnalyzerConfigDocument(documentId) ?? throw CreateDocumentNotFoundException(documentId.DebugName ?? "Unknown");

    public static TextDocument GetRequiredTextDocument(this Solution solution, DocumentId documentId)
    {
        var document = solution.GetTextDocument(documentId);
        if (document != null)
            return document;

#if WORKSPACE
        if (documentId.IsSourceGenerated)
            throw new InvalidOperationException($"Use {nameof(GetRequiredTextDocumentAsync)} to get the {nameof(TextDocument)} for a `.{nameof(DocumentId.IsSourceGenerated)}=true` {nameof(DocumentId)}");
#endif

        throw CreateDocumentNotFoundException(documentId.DebugName ?? "Unknown");
    }

    public static Exception CreateDocumentNotFoundException(string documentPath)
        => new InvalidOperationException(string.Format(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document, documentPath));

#if WORKSPACE
    public static Solution WithUpToDateSourceGeneratorDocuments(this Solution solution, IEnumerable<ProjectId> projectIds)
    {
        // If the solution is already in automatic mode, then SG documents are already always up to date.
        var configuration = solution.Services.GetRequiredService<IWorkspaceConfigurationService>().Options;
        if (configuration.SourceGeneratorExecution is SourceGeneratorExecutionPreference.Automatic)
            return solution;

        var projectIdToSourceGenerationVersion = ImmutableSortedDictionary.CreateBuilder<ProjectId, SourceGeneratorExecutionVersion>();

        foreach (var projectId in projectIds)
        {
            if (!projectIdToSourceGenerationVersion.ContainsKey(projectId))
            {
                var currentVersion = solution.GetSourceGeneratorExecutionVersion(projectId);
                projectIdToSourceGenerationVersion.Add(projectId, currentVersion.IncrementMinorVersion());
            }
        }

        return solution.UpdateSpecificSourceGeneratorExecutionVersions(
            new SourceGeneratorExecutionVersionMap(projectIdToSourceGenerationVersion.ToImmutable()));
    }
#endif

    public static TextDocument? GetTextDocumentForLocation(this Solution solution, Location location)
    {
        switch (location.Kind)
        {
            case LocationKind.SourceFile:
                return solution.GetDocument(location.SourceTree);
            case LocationKind.ExternalFile:
                var documentId = solution.GetDocumentIdsWithFilePath(location.GetLineSpan().Path).FirstOrDefault();
                return solution.GetTextDocument(documentId);
            default:
                return null;
        }
    }

    public static TLanguageService? GetLanguageService<TLanguageService>(this Solution? solution, string languageName) where TLanguageService : ILanguageService
        => solution is null ? default : solution.GetExtendedLanguageServices(languageName).GetService<TLanguageService>();

    public static TLanguageService GetRequiredLanguageService<TLanguageService>(this Solution solution, string languageName) where TLanguageService : ILanguageService
        => solution.GetExtendedLanguageServices(languageName).GetRequiredService<TLanguageService>();

#pragma warning disable RS0030 // Do not used banned API 'Project.LanguageServices', use 'GetExtendedLanguageServices' instead - allow in this helper.

    /// <summary>
    /// Gets extended host language services, which includes language services from <see cref="Project.LanguageServices"/>.
    /// </summary>
    public static HostLanguageServices GetExtendedLanguageServices(this Solution solution, string languageName)
#if !WORKSPACE
        => solution.Workspace.Services.GetExtendedLanguageServices(languageName);
#else
        => solution.Services.GetExtendedLanguageServices(languageName);
#endif

#pragma warning restore RS0030 // Do not used banned APIs
}
