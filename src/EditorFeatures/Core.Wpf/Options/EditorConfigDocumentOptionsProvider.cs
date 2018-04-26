// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    // NOTE: this type depends Microsoft.VisualStudio.CodingConventions, so for now it's living in EditorFeatures.Wpf as that assembly
    // isn't yet available outside of Visual Studio.
    internal sealed partial class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        private const int EventDelayInMillisecond = 50;

        private readonly object _gate = new object();

        /// <summary>
        /// The map of cached contexts for currently open documents. Should only be accessed if holding a monitor lock
        /// on <see cref="_gate"/>.
        /// </summary>
        private readonly Dictionary<DocumentId, Task<ICodingConventionContext>> _openDocumentContexts = new Dictionary<DocumentId, Task<ICodingConventionContext>>();

        private readonly Workspace _workspace;
        private readonly ICodingConventionsManager _codingConventionsManager;
        private readonly IErrorLoggerService _errorLogger;

        private ResettableDelay _resettableDelay;

        internal EditorConfigDocumentOptionsProvider(Workspace workspace, ICodingConventionsManager codingConventionsManager)
        {
            _workspace = workspace;

            _codingConventionsManager = codingConventionsManager;
            _errorLogger = workspace.Services.GetService<IErrorLoggerService>();

            _resettableDelay = ResettableDelay.CompletedDelay;

            workspace.DocumentOpened += Workspace_DocumentOpened;
            workspace.DocumentClosed += Workspace_DocumentClosed;
        }

        private void Workspace_DocumentClosed(object sender, DocumentEventArgs e)
        {
            lock (_gate)
            {
                if (_openDocumentContexts.TryGetValue(e.Document.Id, out var contextTask))
                {
                    _openDocumentContexts.Remove(e.Document.Id);

                    // Ensure we dispose the context, which we'll do asynchronously
                    contextTask.ContinueWith(
                        t =>
                        {
                            var context = t.Result;

                            context.CodingConventionsChangedAsync -= OnCodingConventionsChangedAsync;
                            context.Dispose();
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnRanToCompletion,
                        TaskScheduler.Default);
                }
            }
        }

        private void Workspace_DocumentOpened(object sender, DocumentEventArgs e)
        {
            lock (_gate)
            {
                var contextTask = Task.Run(async () =>
                {
                    var context = await GetConventionContextAsync(e.Document.FilePath, CancellationToken.None).ConfigureAwait(false);
                    context.CodingConventionsChangedAsync += OnCodingConventionsChangedAsync;
                    return context;
                });

                _openDocumentContexts.Add(e.Document.Id, contextTask);
            }
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

                using (var context = await conventionsAsync.ConfigureAwait(false))
                {
                    return new DocumentOptions(context.CurrentConventions, _errorLogger);
                }
            }
        }

        private Task<ICodingConventionContext> GetConventionContextAsync(string path, CancellationToken cancellationToken)
        {
            return IOUtilities.PerformIOAsync(
                () => _codingConventionsManager.GetConventionContextAsync(path, cancellationToken),
                defaultValue: EmptyCodingConventionContext.Instance);
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

            lock (_gate)
            {
                if (!_resettableDelay.Task.IsCompleted)
                {
                    _resettableDelay.Reset();
                }
                else
                {
                    // since this event gets raised for all documents that are affected by 1 editconfig file,
                    // and since for now we make that event as whole solution changed event, we don't need to update
                    // snapshot for each events. aggregate all events to 1.
                    var delay = new ResettableDelay(EventDelayInMillisecond);
                    delay.Task.ContinueWith(_ => _workspace.OnOptionChanged(),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);

                    _resettableDelay = delay;
                }
            }

            return Task.CompletedTask;
        }
    }
}
