// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal sealed class GraphNavigatorExtension : ForegroundThreadAffinitizedObject, IGraphNavigateToItem
    {
        private readonly Workspace _workspace;

        public GraphNavigatorExtension(IThreadingContext threadingContext, Workspace workspace)
            : base(threadingContext)
        {
            _workspace = workspace;
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
                    {
                        return;
                    }

                    var document = project.Documents.FirstOrDefault(
                        d => string.Equals(
                            d.FilePath,
                            sourceLocation.FileName.LocalPath,
                            StringComparison.OrdinalIgnoreCase));

                    if (document == null)
                    {
                        return;
                    }

                    if (IsForeground())
                    {
                        // If we are already on the UI thread, invoke NavigateOnForegroundThread
                        // directly to preserve any existing NewDocumentStateScope.
                        NavigateOnForegroundThread(sourceLocation, symbolId, project, document);
                    }
                    else
                    {
                        // Navigation must be performed on the UI thread. If we are invoked from a
                        // background thread then the current NewDocumentStateScope is unrelated to
                        // this navigation and it is safe to continue on the UI thread 
                        // asynchronously.
                        Task.Factory.SafeStartNewFromAsync(
                            async () =>
                            {
                                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                                NavigateOnForegroundThread(sourceLocation, symbolId, project, document);
                            },
                            CancellationToken.None,
                            TaskScheduler.Default);
                    }
                }
            }
        }

        private void NavigateOnForegroundThread(
            SourceLocation sourceLocation, SymbolKey? symbolId, Project project, Document document)
        {
            AssertIsForeground();

            // Notify of navigation so third parties can intercept the navigation
            if (symbolId != null)
            {
                var symbolNavigationService = _workspace.Services.GetService<ISymbolNavigationService>();
                var symbol = symbolId.Value.Resolve(project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None)).Symbol;

                // Do not allow third party navigation to types or constructors
                if (symbol != null &&
                    !(symbol is ITypeSymbol) &&
                    !symbol.IsConstructor() &&
                    symbolNavigationService.TrySymbolNavigationNotify(symbol, project, CancellationToken.None))
                {
                    return;
                }
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
                    navigationService.TryNavigateToLineAndOffset(
                        editorWorkspace,
                        document.Id,
                        sourceLocation.StartPosition.Line,
                        sourceLocation.StartPosition.Character);
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
}
