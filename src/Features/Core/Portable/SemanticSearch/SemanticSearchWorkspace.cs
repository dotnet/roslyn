// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal abstract class SemanticSearchWorkspace(HostServices services, SemanticSearchProjectConfiguration config)
    : Workspace(services, WorkspaceKind.SemanticSearch)
{
    public override bool CanOpenDocuments
        => true;

    public override bool CanApplyChange(ApplyChangesKind feature)
        => feature == ApplyChangesKind.ChangeDocument;

    public async Task<Document> UpdateQueryDocumentAsync(string? query, CancellationToken cancellationToken)
    {
        SourceText? newText = null;

        var (_, newSolution) = await SetCurrentSolutionAsync(
            useAsync: true,
            transformation: oldSolution =>
            {
                if (oldSolution.Projects.Any())
                {
                    if (query == null)
                    {
                        // already have a content, don't reset it to default:
                        return oldSolution;
                    }

                    newText = SemanticSearchUtilities.CreateSourceText(query);
                    return oldSolution.WithDocumentText(SemanticSearchUtilities.GetQueryDocumentId(oldSolution), newText);
                }

                newText = SemanticSearchUtilities.CreateSourceText(query ?? config.Query);
                var metadataService = oldSolution.Services.GetRequiredService<IMetadataService>();

                return oldSolution
                    .AddProject(name: SemanticSearchUtilities.QueryProjectName, assemblyName: SemanticSearchUtilities.QueryProjectName, config.Language)
                    .WithCompilationOptions(config.CompilationOptions)
                    .WithParseOptions(config.ParseOptions)
                    .AddMetadataReferences(SemanticSearchUtilities.GetMetadataReferences(metadataService, SemanticSearchUtilities.ReferenceAssembliesDirectory))
                    .AddDocument(name: SemanticSearchUtilities.QueryDocumentName, newText, filePath: SemanticSearchUtilities.GetDocumentFilePath(config.Language)).Project
                    .AddDocument(name: SemanticSearchUtilities.GlobalUsingsDocumentName, SemanticSearchUtilities.CreateSourceText(config.GlobalUsings), filePath: null).Project
                    .AddAnalyzerConfigDocument(name: SemanticSearchUtilities.ConfigDocumentName, SemanticSearchUtilities.CreateSourceText(config.EditorConfig), filePath: SemanticSearchUtilities.GetConfigDocumentFilePath()).Project.Solution;
            },
            changeKind: (oldSolution, newSolution) =>
                oldSolution.Projects.Any()
                    ? (WorkspaceChangeKind.DocumentChanged, projectId: null, documentId: SemanticSearchUtilities.GetQueryDocumentId(newSolution))
                    : (WorkspaceChangeKind.ProjectAdded, projectId: SemanticSearchUtilities.GetQueryProjectId(newSolution), documentId: null),
            onBeforeUpdate: null,
            onAfterUpdate: null,
            cancellationToken).ConfigureAwait(false);

        var queryDocument = SemanticSearchUtilities.GetQueryDocument(newSolution);

        if (newText != null)
        {
            ApplyQueryDocumentTextChanged(newText);
        }

        return queryDocument;
    }

    protected virtual void ApplyQueryDocumentTextChanged(SourceText newText)
    {
    }
}
