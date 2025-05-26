// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

using Workspace = Microsoft.CodeAnalysis.Workspace;

internal sealed class GraphNavigatorExtension(
    IThreadingContext threadingContext,
    Workspace workspace,
    Lazy<IStreamingFindUsagesPresenter> streamingPresenter) : IGraphNavigateToItem
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly Workspace _workspace = workspace;
    // private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter = streamingPresenter;

    public void NavigateTo(GraphObject graphObject)
    {
        if (graphObject is not GraphNode graphNode)
            return;

        _threadingContext.JoinableTaskFactory.Run(() => NavigateToAsync(graphNode, CancellationToken.None));
    }

    private async Task NavigateToAsync(GraphNode graphNode, CancellationToken cancellationToken)
    {
        var projectId = graphNode.GetValue<ProjectId>(RoslynGraphProperties.ContextProjectId);

        if (projectId is null)
            return;

        var solution = _workspace.CurrentSolution;
        var project = solution.GetProject(projectId);
        if (project is null)
            return;

        // Go through the mainline symbol id path if we have it.  That way we notify third parties, and we can navigate
        // to metadata.
        //var symbolId = graphNode.GetValue<SymbolKey?>(RoslynGraphProperties.SymbolId);
        //if (symbolId is not null)
        //{
        //    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        //    if (compilation is not null)
        //    {
        //        var symbol = symbolId.Value.Resolve(compilation, cancellationToken: cancellationToken).GetAnySymbol();
        //        if (symbol is not null)
        //        {
        //            await GoToDefinitionHelpers.TryNavigateToLocationAsync(
        //                symbol, project.Solution, _threadingContext, _streamingPresenter.Value, cancellationToken).ConfigureAwait(false);
        //            return;
        //        }
        //    }
        //}

        // If we didn't have a symbol id, attempt to navigate to the source location directly if the node includes one.
        var sourceLocation = graphNode.GetValue<SourceLocation>(CodeNodeProperties.SourceLocation);
        if (sourceLocation.FileName is null || !sourceLocation.IsValid)
            return;

        var document = project.Documents.FirstOrDefault(
            d => string.Equals(
                d.FilePath,
                sourceLocation.FileName.LocalPath,
                StringComparison.OrdinalIgnoreCase));

        if (document == null)
            return;

        // We must find the right document in this project. This may not be the
        // ContextDocumentId if you have a partial member that is shown under one
        // document, but only exists in the other

        var editorWorkspace = document.Project.Solution.Workspace;
        var navigationService = editorWorkspace.Services.GetRequiredService<IDocumentNavigationService>();

        // TODO: Get the platform to use and pass us an operation context, or create one ourselves.
        await navigationService.TryNavigateToLineAndOffsetAsync(
            _threadingContext,
            editorWorkspace,
            document.Id,
            sourceLocation.StartPosition.Line,
            sourceLocation.StartPosition.Character,
            NavigationOptions.Default,
            cancellationToken).ConfigureAwait(false);
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
