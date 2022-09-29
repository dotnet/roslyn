// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    /// <summary>
    /// Creates services on the first connection of an applicable subject buffer to an IWpfTextView. 
    /// This ensures the services are available by the time an open document or the interactive window needs them.
    /// </summary>
    internal abstract class AbstractCreateServicesOnTextViewConnection : IWpfTextViewConnectionListener
    {
        private readonly string _languageName;
        private readonly AsyncBatchingWorkQueue<DocumentId?> _workQueue;
        private bool _initialized = false;

        protected VisualStudioWorkspace Workspace { get; }
        protected IGlobalOptionService GlobalOptions { get; }

        protected virtual Task InitializeServiceForOpenedDocumentAsync(Document document)
            => Task.CompletedTask;

        public AbstractCreateServicesOnTextViewConnection(
            VisualStudioWorkspace workspace,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext,
            string languageName)
        {
            Workspace = workspace;
            GlobalOptions = globalOptions;
            _languageName = languageName;

            _workQueue = new AsyncBatchingWorkQueue<DocumentId?>(
                    TimeSpan.FromSeconds(1),
                    ProcessBatchDocumentOpenedAsync,
                    EqualityComparer<DocumentId?>.Default,
                    listenerProvider.GetListener(FeatureAttribute.Workspace),
                    threadingContext.DisposalToken);

            Workspace.DocumentOpened += QueueWorkOnDocumentOpened;
        }

        void IWpfTextViewConnectionListener.SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!_initialized)
            {
                _initialized = true;
                // use `null` to trigger per VS session intialization task
                _workQueue.AddWork((DocumentId?)null);
            }
        }

        void IWpfTextViewConnectionListener.SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }

        private async ValueTask ProcessBatchDocumentOpenedAsync(ImmutableSegmentedList<DocumentId?> documentIds, CancellationToken cancellationToken)
        {
            foreach (var documentId in documentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (documentId is null)
                {
                    InitializePerVSSessionServices();
                }
                else if (Workspace.CurrentSolution.GetDocument(documentId) is Document document && document.Project.Language == _languageName)
                {
                    await InitializeServiceForOpenedDocumentAsync(document).ConfigureAwait(false);
                }
            }
        }

        private void QueueWorkOnDocumentOpened(object sender, DocumentEventArgs e)
            => _workQueue.AddWork(e.Document.Id);

        private void InitializePerVSSessionServices()
        {
            var languageServices = Workspace.Services.GetExtendedLanguageServices(_languageName);

            _ = languageServices.GetService<ISnippetInfoService>();

            // Preload completion providers on a background thread since assembly loads can be slow
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1242321
            _ = languageServices.GetService<CompletionService>()?.GetLazyImportedProviders().SelectAsArray(p => p.Value);
        }
    }
}
