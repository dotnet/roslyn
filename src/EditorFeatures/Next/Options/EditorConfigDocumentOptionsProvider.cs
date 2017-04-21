// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    // NOTE: this type depends on Dev15 assemblies, which is why the type is in EditorFeatures.Next. But, that library
    // is rehostable and once we move .editorconfig support fully through the system, it should be moved to Workspaces
    // or perhaps even lower.
    internal sealed partial class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        private readonly object _gate = new object();

        /// <summary>
        /// The map of cached contexts for currently open documents. Should only be accessed if holding a monitor lock
        /// on <see cref="_gate"/>.
        /// </summary>
        private readonly Dictionary<DocumentId, Task<ICodingConventionContext>> _openDocumentContexts = new Dictionary<DocumentId, Task<ICodingConventionContext>>();

        private readonly ICodingConventionsManager _codingConventionsManager;
        private readonly IErrorLoggerService _errorLogger;

        internal EditorConfigDocumentOptionsProvider(Workspace workspace)
        {
            _codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();
            _errorLogger = workspace.Services.GetService<IErrorLoggerService>();

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
                        t => t.Result.Dispose(),
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
                _openDocumentContexts.Add(e.Document.Id, Task.Run(() => GetConventionContextAsync(e.Document.FilePath, CancellationToken.None)));
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
    }
}
