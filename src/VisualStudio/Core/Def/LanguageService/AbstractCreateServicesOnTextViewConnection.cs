// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    /// <summary>
    /// Creates services on the first connection of an applicable subject buffer to an IWpfTextView. 
    /// This ensures the services are available by the time an open document or the interactive window needs them.
    /// </summary>
    internal abstract class AbstractCreateServicesOnTextViewConnection : IWpfTextViewConnectionListener
    {
        private readonly IAsynchronousOperationListener _listener;
        private readonly IThreadingContext _threadingContext;
        private readonly string _languageName;
        private bool _initialized = false;

        protected VisualStudioWorkspace Workspace { get; }
        protected IGlobalOptionService GlobalOptions { get; }

        protected virtual Task InitializeServiceForOpenedDocumentAsync(Document document)
            => Task.CompletedTask;

        protected virtual void OnSolutionRemoved()
        {
            return;
        }

        public AbstractCreateServicesOnTextViewConnection(
            VisualStudioWorkspace workspace,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext,
            string languageName)
        {
            Workspace = workspace;
            GlobalOptions = globalOptions;

            _listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
            _threadingContext = threadingContext;
            _languageName = languageName;

            Workspace.DocumentOpened += InitializeServiceOnDocumentOpened;
            Workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        void IWpfTextViewConnectionListener.SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!_initialized)
            {
                var token = _listener.BeginAsyncOperation(nameof(InitializeServicesAsync));
                InitializeServicesAsync().CompletesAsyncOperation(token);

                _initialized = true;
            }
        }

        void IWpfTextViewConnectionListener.SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.SolutionRemoved)
            {
                OnSolutionRemoved();
            }
        }

        private void InitializeServiceOnDocumentOpened(object sender, DocumentEventArgs e)
        {
            if (e.Document.Project.Language != _languageName)
            {
                return;
            }

            var token = _listener.BeginAsyncOperation(nameof(InitializeServiceForOpenedDocumentOnBackgroundAsync));
            InitializeServiceForOpenedDocumentOnBackgroundAsync(e.Document).CompletesAsyncOperation(token);

            async Task InitializeServiceForOpenedDocumentOnBackgroundAsync(Document document)
            {
                await TaskScheduler.Default;
                await InitializeServiceForOpenedDocumentAsync(document).ConfigureAwait(false);
            }
        }

        private async Task InitializeServicesAsync()
        {
            await TaskScheduler.Default;

            var languageServices = Workspace.Services.GetExtendedLanguageServices(_languageName);

            _ = languageServices.GetService<ISnippetInfoService>();

            // Preload completion providers on a background thread since assembly loads can be slow
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1242321
            if (languageServices.GetService<CompletionService>() is CompletionServiceWithProviders service)
            {
                _ = service.GetImportedProviders().SelectAsArray(p => p.Value);
            }
        }
    }
}
