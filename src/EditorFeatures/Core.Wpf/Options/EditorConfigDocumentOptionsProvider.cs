// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.CodingConventions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    // NOTE: this type depends Microsoft.VisualStudio.CodingConventions, so for now it's living in EditorFeatures.Wpf as that assembly
    // isn't yet available outside of Visual Studio.
    internal sealed partial class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        // this lock guard _openDocumentContexts mutation
        private readonly object _gate = new object();

        /// <summary>
        /// The map of cached contexts for currently open documents. Should only be accessed if holding a monitor lock
        /// on <see cref="_gate"/>.
        /// </summary>
        private readonly Dictionary<DocumentId, Task<EventTracker>> _openDocumentContexts = new Dictionary<DocumentId, Task<EventTracker>>();

        private readonly Workspace _workspace;
        private readonly IErrorLoggerService _errorLogger;

        // due to an issue where OnCodingConventionsChangedAsync expecting callers to use JoinableTaskFactory
        // we can't use it in free-thread fashion. so as a workaround until compiler editorconfig work finishes,
        // we use both dynamic and static manager. one to get data, the other to get eventing.
        private readonly ICodingConventionsManager _dataCodingConventionsManager;
        private readonly ICodingConventionsManager _eventingCodingConventionsManager;

        internal EditorConfigDocumentOptionsProvider(Workspace workspace, ICodingConventionsManager codingConventionsManager)
        {
            _workspace = workspace;

            _dataCodingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();
            _eventingCodingConventionsManager = codingConventionsManager;

            _errorLogger = workspace.Services.GetService<IErrorLoggerService>();

            workspace.DocumentOpened += OnDocumentOpened;
            workspace.DocumentClosed += OnDocumentClosed;

            // workaround until this is fixed.
            // https://github.com/dotnet/roslyn/issues/26377
            // otherwise, we will leak files in _openDocumentContexts
            workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
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

        private void OnDocumentClosed(object sender, DocumentEventArgs e)
        {
            lock (_gate)
            {
                if (_openDocumentContexts.TryGetValue(e.Document.Id, out var eventTrackingTask))
                {
                    _openDocumentContexts.Remove(e.Document.Id);

                    // Ensure we dispose the context, which we'll do asynchronously
                    EnsureContextCleanup(eventTrackingTask);
                }
            }
        }

        private void OnDocumentOpened(object sender, DocumentEventArgs e)
        {
            lock (_gate)
            {
                var eventTrackingTask = Task.Run(async () =>
                {
                    var eventingContext = await GetEventingConventionContextAsync(e.Document.FilePath, CancellationToken.None).ConfigureAwait(false);

                    return new EventTracker(_workspace, eventingContext);
                });

                Contract.Requires(!_openDocumentContexts.ContainsKey(e.Document.Id));
                _openDocumentContexts.Add(e.Document.Id, eventTrackingTask);
            }
        }

        private void ClearOpenFileCache(ProjectId projectId = null)
        {
            var eventTrackingTasks = new List<Task<EventTracker>>();

            lock (_gate)
            {
                if (projectId == null)
                {
                    eventTrackingTasks.AddRange(_openDocumentContexts.Values);
                    _openDocumentContexts.Clear();
                }
                else
                {
                    foreach (var kv in _openDocumentContexts.Where(kv => kv.Key.ProjectId == projectId).ToList())
                    {
                        _openDocumentContexts.Remove(kv.Key);
                        eventTrackingTasks.Add(kv.Value);
                    }
                }
            }

            foreach (var contextTask in eventTrackingTasks)
            {
                EnsureContextCleanup(contextTask);
            }
        }

        private void EnsureContextCleanup(Task<EventTracker> eventTrackingTask)
        {
            eventTrackingTask.ContinueWith(t => t.Result.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

        public async Task<IDocumentOptions> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
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
            var conventionsAsync = Task.Run(() => GetDataConventionContextAsync(path, cancellationToken));

            using (var context = await conventionsAsync.ConfigureAwait(false))
            {
                return new DocumentOptions(context.CurrentConventions, _errorLogger);
            }
        }

        private Task<ICodingConventionContext> GetDataConventionContextAsync(string path, CancellationToken cancellationToken)
        {
            return IOUtilities.PerformIOAsync(
                () => _dataCodingConventionsManager.GetConventionContextAsync(path, cancellationToken),
                defaultValue: EmptyCodingConventionContext.Instance);
        }

        private Task<ICodingConventionContext> GetEventingConventionContextAsync(string path, CancellationToken cancellationToken)
        {
            return IOUtilities.PerformIOAsync(
                () => _eventingCodingConventionsManager.GetConventionContextAsync(path, cancellationToken),
                defaultValue: EmptyCodingConventionContext.Instance);
        }

        private sealed class EventTracker : IDisposable
        {
            private const int EventDelayInMillisecond = 50;

            // this lock guard _resettableDelay
            private readonly object _eventGate = new object();

            private readonly Workspace _workspace;
            public readonly ICodingConventionContext _eventingContext;

            /// <summary>
            /// this is used to aggregate OnCodingConventionsChangedAsync event
            /// the event will be raised to all open documents that is affected by same editorconfig files
            /// </summary>
            private ResettableDelay _resettableDelay;

            public EventTracker(Workspace workspace, ICodingConventionContext eventingContext)
            {
                _resettableDelay = ResettableDelay.CompletedDelay;

                _workspace = workspace;
                _eventingContext = eventingContext;

                _eventingContext.CodingConventionsChangedAsync += OnCodingConventionsChangedAsync;
            }

            public void Dispose()
            {
                _eventingContext.CodingConventionsChangedAsync -= OnCodingConventionsChangedAsync;
                _eventingContext.Dispose();
            }

            private Task OnCodingConventionsChangedAsync(object sender, CodingConventionsChangedEventArgs arg)
            {
                // this is a temporary workaround. once we finish the work to put editorconfig file as a part of roslyn solution snapshot,
                // that system will automatically pick up option changes and update snapshot. and it will work regardless
                // whether a file is opened in editor or not.
                // 
                // but until then, we need to explicitly touch workspace to update snapshot. and 
                // only works for open files. it is not easy to track option changes for closed files with current model.
                // related tracking issue - https://github.com/dotnet/roslyn/issues/26250
                //
                // use its own lock to remove dead lock possibility
                ResettableDelay delay;
                lock (_eventGate)
                {
                    if (!_resettableDelay.Task.IsCompleted)
                    {
                        _resettableDelay.Reset();
                        return Task.CompletedTask;
                    }

                    // since this event gets raised for all documents that are affected by 1 editconfig file,
                    // and since for now we make that event as whole solution changed event, we don't need to update
                    // snapshot for each events. aggregate all events to 1.
                    delay = new ResettableDelay(EventDelayInMillisecond);
                    _resettableDelay = delay;
                }

                delay.Task.ContinueWith(_ => _workspace.OnOptionChanged(),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                return Task.CompletedTask;
            }
        }
    }
}
