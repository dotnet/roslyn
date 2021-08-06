// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
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
        private readonly VisualStudioWorkspace _workspace;
        private readonly IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> _languageServices;
        private readonly string _languageName;
        private bool _initialized = false;

        public AbstractCreateServicesOnTextViewConnection(
            VisualStudioWorkspace workspace,
            IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> languageServices,
            string languageName)
        {
            _workspace = workspace;
            _languageName = languageName;
            _languageServices = languageServices;
        }

        void IWpfTextViewConnectionListener.SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!_initialized)
            {
                CreateServicesOnUIThread();
                CreateServicesInBackground();
                _initialized = true;
            }
        }

        void IWpfTextViewConnectionListener.SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }

        /// <summary>
        /// Must be invoked from the UI thread.
        /// </summary>
        private void CreateServicesOnUIThread()
        {
            var serviceTypeAssemblyQualifiedName = typeof(ISnippetInfoService).AssemblyQualifiedName;
            foreach (var languageService in _languageServices)
            {
                if (languageService.Metadata.ServiceType == serviceTypeAssemblyQualifiedName &&
                    languageService.Metadata.Language == _languageName)
                {
                    _ = languageService.Value;
                    break;
                }
            }
        }

        private void CreateServicesInBackground()
        {
            _ = Task.Run(ImportCompletionProviders);

            // Preload completion providers on a background thread since assembly loads can be slow
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1242321
            void ImportCompletionProviders()
            {
                if (_workspace.Services.GetLanguageService<CompletionService>(_languageName) is CompletionServiceWithProviders service)
                {
                    _ = service.GetImportedProviders().SelectAsArray(p => p.Value);
                }
            }
        }
    }
}
