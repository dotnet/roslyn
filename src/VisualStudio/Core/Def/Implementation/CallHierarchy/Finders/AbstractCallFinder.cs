// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
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
        private readonly ProjectId _projectId;
        private readonly SymbolKey _symbolKey;

        protected readonly CallHierarchyProvider Provider;
        protected readonly string SymbolName;

        // For Testing only
        internal IImmutableSet<Document> Documents;

        protected AbstractCallFinder(ISymbol symbol, ProjectId projectId, IAsynchronousOperationListener asyncListener, CallHierarchyProvider provider)
        {
            _asyncListener = asyncListener;
            _symbolKey = symbol.GetSymbolKey();
            this.SymbolName = symbol.Name;
            _projectId = projectId;
            this.Provider = provider;
        }

        internal void SetDocuments(IImmutableSet<Document> documents)
        {
            this.Documents = documents;
        }

        public abstract string DisplayName { get; }

        public virtual string SearchCategory => DisplayName;

        public void CancelSearch()
        {
            _cancellationSource.Cancel();
        }

        public void StartSearch(Workspace workspace, CallHierarchySearchScope searchScope, ICallHierarchySearchCallback callback)
        {
            var asyncToken = _asyncListener.BeginAsyncOperation(this.GetType().Name + ".Search");

            // NOTE: This task has CancellationToken.None specified, since it must complete no matter what
            // so the callback is appropriately notified that the search has terminated.
            Task.Run(async () =>
            {
                // The error message to show if we had an error. null will mean we succeeded.
                string completionErrorMessage = null;
                try
                {
                    await SearchAsync(workspace, searchScope, callback, _cancellationSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    completionErrorMessage = EditorFeaturesResources.Canceled;
                }
                catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                {
                    completionErrorMessage = e.Message;
                }
                finally
                {
                    if (completionErrorMessage != null)
                    {
                        callback.SearchFailed(completionErrorMessage);
                    }
                    else
                    {
                        callback.SearchSucceeded();
                    }
                }
            }, CancellationToken.None).CompletesAsyncOperation(asyncToken);
        }

        private async Task SearchAsync(Workspace workspace, CallHierarchySearchScope scope, ICallHierarchySearchCallback callback, CancellationToken cancellationToken)
        {
            var project = workspace.CurrentSolution.GetProject(_projectId);

            if (project == null)
            {
                throw new Exception(string.Format(WorkspacesResources.The_symbol_0_cannot_be_located_within_the_current_solution, SymbolName));
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var resolution = _symbolKey.Resolve(compilation, cancellationToken: cancellationToken);

            var symbol = resolution.Symbol;

            if (symbol == null)
            {
                throw new Exception(string.Format(WorkspacesResources.The_symbol_0_cannot_be_located_within_the_current_solution, SymbolName));
            }

            var documents = this.Documents ?? IncludeDocuments(scope, project);

            await SearchWorkerAsync(symbol, project, callback, documents, cancellationToken).ConfigureAwait(false);
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

                var activeDocument = documentTrackingService.TryGetActiveDocument();
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
            var callers = await GetCallersAsync(symbol, project, documents, cancellationToken).ConfigureAwait(false);

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
                        var callingProject = project.Solution.GetProject(caller.CallingSymbol.ContainingAssembly);
                        var item = await Provider.CreateItemAsync(caller.CallingSymbol, callingProject, caller.Locations, cancellationToken).ConfigureAwait(false);
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

        protected abstract Task<IEnumerable<SymbolCallerInfo>> GetCallersAsync(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken);
    }
}
