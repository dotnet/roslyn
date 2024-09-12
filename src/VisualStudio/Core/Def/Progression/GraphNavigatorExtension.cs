// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

using Workspace = Microsoft.CodeAnalysis.Workspace;

internal sealed class GraphNavigatorExtension : ForegroundThreadAffinitizedObject, IGraphNavigateToItem
{
    private readonly Workspace _workspace;
    private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

    public GraphNavigatorExtension(
        IThreadingContext threadingContext,
        Workspace workspace,
        Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
        : base(threadingContext)
    {
        _workspace = workspace;
        _streamingPresenter = streamingPresenter;
    }

    public void NavigateTo(GraphObject graphObject)
    {

        if (graphObject is GraphNode graphNode)
        {
            var sourceLocation = graphNode.GetValue<SourceLocation>(CodeNodeProperties.SourceLocation);
            if (sourceLocation.FileName == null)
            {
                return;
            }

            var projectId = graphNode.GetValue<ProjectId>(RoslynGraphProperties.ContextProjectId);
            var symbolId = graphNode.GetValue<SymbolKey?>(RoslynGraphProperties.SymbolId);

            if (projectId != null)
            {
                var solution = _workspace.CurrentSolution;
                var project = solution.GetProject(projectId);
                if (project == null)
                    return;

                var document = project.Documents.FirstOrDefault(
                    d => string.Equals(
                        d.FilePath,
                        sourceLocation.FileName.LocalPath,
                        StringComparison.OrdinalIgnoreCase));

                if (document == null)
                    return;

                this.ThreadingContext.JoinableTaskFactory.Run(() =>
                    NavigateToAsync(sourceLocation, symbolId, project, document, CancellationToken.None));
            }
        }
    }

    private async Task NavigateToAsync(
        SourceLocation sourceLocation, SymbolKey? symbolId, Project project, Document document, CancellationToken cancellationToken)
    {
        // Notify of navigation so third parties can intercept the navigation
        if (symbolId != null)
        {
            var symbol = symbolId.Value.Resolve(await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).Symbol;
            await GoToDefinitionHelpers.TryNavigateToLocationAsync(
                symbol, project.Solution, this.ThreadingContext, _streamingPresenter.Value, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (sourceLocation.IsValid)
        {
            // We must find the right document in this project. This may not be the
            // ContextDocumentId if you have a partial member that is shown under one
            // document, but only exists in the other

            if (document != null)
            {
                var editorWorkspace = document.Project.Solution.Workspace;
                var navigationService = editorWorkspace.Services.GetService<IDocumentNavigationService>();

                // TODO: Get the platform to use and pass us an operation context, or create one ourselves.
                await navigationService.TryNavigateToLineAndOffsetAsync(
                    this.ThreadingContext,
                    editorWorkspace,
                    document.Id,
                    sourceLocation.StartPosition.Line,
                    sourceLocation.StartPosition.Character,
                    NavigationOptions.Default,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public int GetRank(GraphObject graphObject)
    {
        if (graphObject is GraphNode graphNode)
        {
            var sourceLocation = graphNode.GetValue<SourceLocation>(CodeNodeProperties.SourceLocation);
            var projectId = graphNode.GetValue<ProjectId>(RoslynGraphProperties.ContextProjectId);

            if (sourceLocation.IsValid && projectId != null)
            {
                return GraphNavigateToItemRanks.OwnItem;
            }
        }

        return GraphNavigateToItemRanks.CanNavigateToItem;
    }
}
