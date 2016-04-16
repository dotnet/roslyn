// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy.Finders
{
    internal abstract class AbstractCallFinder
    {
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        private readonly Project _project;
        private readonly SymbolKey _symbol;

        protected readonly CallHierarchyProvider Provider;
        protected readonly string SymbolName;

        // For Testing only
        internal IImmutableSet<Document> Documents;

        protected AbstractCallFinder(ISymbol symbol, Project project, IAsynchronousOperationListener asyncListener, CallHierarchyProvider provider)
        {
            _asyncListener = asyncListener;
            _symbol = symbol.GetSymbolKey();
            this.SymbolName = symbol.Name;
            _project = project;
            this.Provider = provider;
        }

        internal void SetDocuments(IImmutableSet<Document> documents)
        {
            this.Documents = documents;
        }

        public abstract string DisplayName { get; }

        public virtual string SearchCategory
        {
            get
            {
                return DisplayName;
            }
        }

        public void CancelSearch()
        {
            _cancellationSource.Cancel();
        }

        public void StartSearch(CallHierarchySearchScope searchScope, ICallHierarchySearchCallback callback)
        {
            Task.Run(async () => await SearchAsync(callback, searchScope, _cancellationSource.Token).ConfigureAwait(false), _cancellationSource.Token);
        }

        private async Task SearchAsync(ICallHierarchySearchCallback callback, CallHierarchySearchScope scope, CancellationToken cancellationToken)
        {
            callback.ReportProgress(0, 1);

            var asyncToken = _asyncListener.BeginAsyncOperation(this.GetType().Name + ".Search");

            // Follow the search task with task that lets the callback know we're done and which
            // marks the async operation as complete.  Note that we pass CancellationToken.None
            // here.  That's intentional.  This operation is *not* cancellable.

            var workspace = _project.Solution.Workspace;
            var currentProject = workspace.CurrentSolution.GetProject(_project.Id);
            var compilation = await currentProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var resolution = _symbol.Resolve(compilation, cancellationToken: cancellationToken);

            var documents = this.Documents ?? IncludeDocuments(scope, currentProject);

            var currentSymbol = resolution.Symbol;

            if (currentSymbol == null)
            {
                return;
            }

            await SearchWorkerAsync(currentSymbol, currentProject, callback, documents, cancellationToken).SafeContinueWith(
                t =>
            {
                callback.ReportProgress(1, 1);

                if (t.Status == TaskStatus.RanToCompletion)
                {
                    callback.SearchSucceeded();
                }
                else
                {
                    callback.SearchFailed(EditorFeaturesResources.Canceled);
                }

                asyncToken.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).ConfigureAwait(false);
        }

        private IImmutableSet<Document> IncludeDocuments(CallHierarchySearchScope scope, Project project)
        {
            if (scope == CallHierarchySearchScope.CurrentDocument || scope == CallHierarchySearchScope.CurrentProject)
            {
                var documentTrackingService = project.Solution.Workspace.Services.GetService<IDocumentTrackingService>();
                if (documentTrackingService == null)
                {
                    return null;
                }

                var activeDocument = documentTrackingService.GetActiveDocument();
                if (activeDocument != null)
                {
                    if (scope == CallHierarchySearchScope.CurrentProject)
                    {
                        var currentProject = project.Solution.GetProject(activeDocument.ProjectId);
                        if (currentProject != null)
                        {
                            return ImmutableHashSet.CreateRange<Document>(currentProject.Documents);
                        }
                    }
                    else
                    {
                        var currentDocument = project.Solution.GetDocument(activeDocument);
                        if (currentDocument != null)
                        {
                            return ImmutableHashSet.Create<Document>(currentDocument);
                        }
                    }

                    return ImmutableHashSet<Document>.Empty;
                }
            }

            return null;
        }

        protected virtual async Task SearchWorkerAsync(ISymbol symbol, Project project, ICallHierarchySearchCallback callback, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            var callers = await GetCallers(symbol, project, documents, cancellationToken).ConfigureAwait(false);

            var initializerLocations = new List<CallHierarchyDetail>();

            foreach (var caller in callers)
            {
                if (caller.IsDirect)
                {
                    if (caller.CallingSymbol.Kind == SymbolKind.Field)
                    {
                        initializerLocations.AddRange(caller.Locations.Select(l => new CallHierarchyDetail(l, project.Solution.Workspace)));
                    }
                    else
                    {
                        var item = await Provider.CreateItem(caller.CallingSymbol, project, caller.Locations, cancellationToken).ConfigureAwait(false);
                        callback.AddResult(item);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }

            if (initializerLocations.Any())
            {
                var initializerItem = Provider.CreateInitializerItem(initializerLocations);
                callback.AddResult(initializerItem);
            }
        }

        protected abstract Task<IEnumerable<SymbolCallerInfo>> GetCallers(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken);
    }
}
