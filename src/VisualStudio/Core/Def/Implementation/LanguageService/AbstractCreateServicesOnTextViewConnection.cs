// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    /// <summary>
    /// Creates services on the first connection of an applicable subject buffer to an IWpfTextView. 
    /// This ensures the services are available by the time an open document or the interactive window needs them.
    /// </summary>
    internal abstract class AbstractCreateServicesOnTextViewConnection : IWpfTextViewConnectionListener
    {
        private readonly IComponentModel _componentModel;
        private readonly string _languageName;
        private bool _initialized = false;

        public AbstractCreateServicesOnTextViewConnection(IServiceProvider serviceProvider, string languageName)
        {
            _componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            _languageName = languageName;
        }

        void IWpfTextViewConnectionListener.SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!_initialized)
            {
                CreateServices(_componentModel, _languageName);
                _initialized = true;
            }
        }

        void IWpfTextViewConnectionListener.SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }

        /// <summary>
        /// Must be invoked from the UI thread.
        /// </summary>
        private static void CreateServices(IComponentModel componentModel, string languageName)
        {
            var serviceTypeAssemblyQualifiedName = typeof(ISnippetInfoService).AssemblyQualifiedName;
            var languageServices = componentModel.DefaultExportProvider.GetExports<ILanguageService, LanguageServiceMetadata>();
            foreach (var languageService in languageServices)
            {
                if (languageService.Metadata.ServiceType == serviceTypeAssemblyQualifiedName &&
                    languageService.Metadata.Language == languageName)
                {
                    _ = languageService.Value;
                    break;
                }
            }
        }
    }
}
