using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    // NOTE: this type depends on Dev15 assemblies, which is why the type is in EditorFeatures.Next. But, that library
    // is rehostable and once we move .editorconfig support fully through the system, it should be moved to Workspaces
    // or perhaps even lower.
    internal sealed class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        private readonly object _gate = new object();

        /// <summary>
        /// The map of cached contexts for currently open documents.
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
                    contextTask.ContinueWith(t => t.Result.Dispose(), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
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
                // The file is open, let's reuse our cached data for that file. The task might still be running, but we want to allow for eager cancellation
                // if the caller doesn't need it
                await Task.WhenAny(contextTask, Task.FromCanceled(cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return new DocumentOptions(contextTask.Result);
            }
            else
            {
                return null;
            }
        }

        private class DocumentOptions : IDocumentOptions
        {
            private ICodingConventionContext _codingConventionContext;

            public DocumentOptions(ICodingConventionContext codingConventionContext)
            {
                _codingConventionContext = codingConventionContext;
            }

            public bool TryGetDocumentOption(Document document, OptionKey option, out object value)
            {
                var editorConfigPersistence = option.Option.StorageLocations.OfType<EditorConfigStorageLocation>().SingleOrDefault();

                if (editorConfigPersistence == null)
                {
                    value = null;
                    return false;
                }

                if (_codingConventionContext.CurrentConventions.TryGetConventionValue(editorConfigPersistence.KeyName, out value))
                {
                    value = editorConfigPersistence.ParseFunction(value.ToString());
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
