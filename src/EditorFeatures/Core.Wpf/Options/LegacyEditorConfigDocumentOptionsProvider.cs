// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.CodingConventions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    // This class is currently linked into both EditorFeatures.Wpf (VS in-process) and RemoteWorkspaces (Roslyn out-of-process).
    internal sealed partial class LegacyEditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        private readonly object _gate = new object();

        /// <summary>
        /// The map of cached contexts for currently open documents. Should only be accessed if holding a monitor lock
        /// on <see cref="_gate"/>.
        /// </summary>
        private readonly Dictionary<DocumentId, Task<ICodingConventionContext>> _openDocumentContexts = new Dictionary<DocumentId, Task<ICodingConventionContext>>();

        private readonly Workspace _workspace;
        private readonly IAsynchronousOperationListener _listener;
        private readonly ICodingConventionsManager _codingConventionsManager;
        private readonly IErrorLoggerService _errorLogger;

        internal LegacyEditorConfigDocumentOptionsProvider(Workspace workspace, ICodingConventionsManager codingConventionsManager, IAsynchronousOperationListenerProvider listenerProvider)
        {
            _workspace = workspace;
            _listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
            _codingConventionsManager = codingConventionsManager;
            _errorLogger = workspace.Services.GetService<IErrorLoggerService>();

            workspace.DocumentOpened += Workspace_DocumentOpened;
            workspace.DocumentClosed += Workspace_DocumentClosed;

            // workaround until this is fixed.
            // https://github.com/dotnet/roslyn/issues/26377
            // otherwise, we will leak files in _openDocumentContexts
            workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
        }

        /// <summary>
        /// This partial method allows implementations of <see cref="LegacyEditorConfigDocumentOptionsProvider"/> (which are
        /// linked into both the in-process and out-of-process implementations as source files) to handle the creation
        /// of <see cref="ICodingConventionContext"/> in different ways.
        /// </summary>
        partial void OnCodingConventionContextCreated(DocumentId documentId, ICodingConventionContext context);

        private void Workspace_DocumentClosed(object sender, DocumentEventArgs e)
        {
            lock (_gate)
            {
                if (_openDocumentContexts.TryGetValue(e.Document.Id, out var contextTask))
                {
                    _openDocumentContexts.Remove(e.Document.Id);
                    ReleaseContext_NoLock(contextTask);
                }
            }
        }

        private void Workspace_DocumentOpened(object sender, DocumentEventArgs e)
        {
            lock (_gate)
            {
                var documentId = e.Document.Id;
                var filePath = e.Document.FilePath;
                _openDocumentContexts.Add(documentId, Task.Run(async () =>
                {
                    var context = await GetConventionContextAsync(filePath, CancellationToken.None).ConfigureAwait(false);
                    OnCodingConventionContextCreated(documentId, context);
                    return context;
                }));
            }
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.SolutionCleared:
                    ClearOpenFileCache();
                    break;

                case WorkspaceChangeKind.ProjectRemoved:
                    ClearOpenFileCache(e.ProjectId);
                    break;

                default:
                    break;
            }
        }

        private void ClearOpenFileCache(ProjectId projectId = null)
        {
            lock (_gate)
            {
                var itemsToRemove = new List<DocumentId>();
                foreach (var (documentId, contextTask) in _openDocumentContexts)
                {
                    if (projectId is null || documentId.ProjectId == projectId)
                    {
                        itemsToRemove.Add(documentId);
                        ReleaseContext_NoLock(contextTask);
                    }
                }

                // If any items were released due to the Clear operation, we need to remove them from the map.
                foreach (var documentId in itemsToRemove)
                {
                    _openDocumentContexts.Remove(documentId);
                }
            }
        }

        private void ReleaseContext_NoLock(Task<ICodingConventionContext> contextTask)
        {
            Debug.Assert(Monitor.IsEntered(_gate));

            // Ensure we dispose the context, which we'll do asynchronously
            contextTask.ContinueWith(
                t => t.Result.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

        public async Task<IDocumentOptions> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            Task<ICodingConventionContext> contextTask;

            lock (_gate)
            {
                _openDocumentContexts.TryGetValue(document.Id, out contextTask);
            }

            if (contextTask != null)
            {
                // The file is open, let's reuse our cached data for that file. That task might be running, but we don't want to await
                // it as awaiting it wouldn't respect the cancellation of our caller. By creating a trivial continuation like this
                // that uses eager cancellation, if the cancellationToken is cancelled our await will end early.
                var cancellableContextTask = contextTask.ContinueWith(
                    t => t.Result,
                    cancellationToken,
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                var context = await cancellableContextTask.ConfigureAwait(false);
                return new DocumentOptions(context.CurrentConventions, _errorLogger);
            }
            else
            {
                var path = document.FilePath;

                // The file might not actually have a path yet, if it's a file being proposed by a code action. We'll guess a file path to use
                if (path == null)
                {
                    if (document.Name != null && document.Project.FilePath != null)
                    {
                        path = Path.Combine(Path.GetDirectoryName(document.Project.FilePath), document.Name);
                    }
                    else
                    {
                        // Really no idea where this is going, so bail
                        return null;
                    }
                }

                // We don't have anything cached, so we'll just get it now lazily and not hold onto it. The workspace layer will ensure
                // that we maintain snapshot rules for the document options. We'll also run it on the thread pool
                // as in some builds the ICodingConventionsManager captures the thread pool.
                var conventionsAsync = Task.Run(() => GetConventionContextAsync(path, cancellationToken));

                using var context = await conventionsAsync.ConfigureAwait(false);

                return new DocumentOptions(context.CurrentConventions, _errorLogger);
            }
        }

        private Task<ICodingConventionContext> GetConventionContextAsync(string path, CancellationToken cancellationToken)
        {
            return IOUtilities.PerformIOAsync(
                () => _codingConventionsManager.GetConventionContextAsync(path, cancellationToken),
                defaultValue: EmptyCodingConventionContext.Instance);
        }
    }
}
