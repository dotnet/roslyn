using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;
using System;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    // NOTE: this type depends on Dev15 assemblies, which is why the type is in EditorFeatures.Next. But, that library
    // is rehostable and once we move .editorconfig support fully through the system, it should be moved to Workspaces
    // or perhaps even lower.
    internal sealed class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        private readonly object _gate = new object();

        /// <summary>
        /// The map of cached contexts for currently open documents. Should only be accessed if holding a monitor lock
        /// on <see cref="_gate"/>.
        /// </summary>
        private readonly Dictionary<DocumentId, Task<ICodingConventionContext>> _openDocumentContexts = new Dictionary<DocumentId, Task<ICodingConventionContext>>();

        private readonly ICodingConventionsManager _codingConventionsManager;

        internal EditorConfigDocumentOptionsProvider(Workspace workspace)
        {
            _codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();

            workspace.DocumentOpened += Workspace_DocumentOpened;
            workspace.DocumentClosed += Workspace_DocumentClosed;
        }

        private void Workspace_DocumentClosed(object sender, DocumentEventArgs e)
        {
            lock (_gate)
            {
                Task<ICodingConventionContext> contextTask;

                if (_openDocumentContexts.TryGetValue(e.Document.Id, out contextTask))
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
                _openDocumentContexts.Add(e.Document.Id, Task.Run(() => _codingConventionsManager.GetConventionContextAsync(e.Document.FilePath, CancellationToken.None)));
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
                return new DocumentOptions(context.CurrentConventions);
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
                var conventionsAsync = Task.Run(() => _codingConventionsManager.GetConventionContextAsync(path, cancellationToken));

                using (var context = await conventionsAsync.ConfigureAwait(false))
                {
                    return new DocumentOptions(context.CurrentConventions);
                }
            }
        }

        private class DocumentOptions : IDocumentOptions
        {
            private ICodingConventionsSnapshot _codingConventionSnapshot;

            public DocumentOptions(ICodingConventionsSnapshot codingConventionSnapshot)
            {
                _codingConventionSnapshot = codingConventionSnapshot;
            }

            public bool TryGetDocumentOption(Document document, OptionKey option, out object value)
            {
                var editorConfigPersistence = option.Option.StorageLocations.OfType<EditorConfigStorageLocation>().SingleOrDefault();

                if (editorConfigPersistence == null)
                {
                    value = null;
                    return false;
                }

                if (_codingConventionSnapshot.TryGetConventionValue(editorConfigPersistence.KeyName, out value))
                {
                    try
                    {
                        value = editorConfigPersistence.ParseValue(value.ToString(), option.Option.Type);
                        return true;
                    }
                    catch (Exception)
                    {
                        // TODO: report this somewhere?
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
