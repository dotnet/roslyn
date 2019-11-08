// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    /// <summary>
    /// Ensures services that must be constructed on the UI thread are appropriately created during
    /// the first connection of an applicable subject buffer to an IWpfTextView. This ensures the
    /// services are available by the time an open document or the interactive window needs them.
    /// The <see cref="CreateServicesOnUIThread(IComponentModel, string)"/> method should also be
    /// called during package load to front load some of the work.
    /// </summary>
    internal abstract class HACK_AbstractCreateServicesOnUiThread : ForegroundThreadAffinitizedObject, IWpfTextViewConnectionListener
    {
        private readonly IComponentModel _componentModel;
        private readonly string _languageName;
        private bool _initialized = false;

        public HACK_AbstractCreateServicesOnUiThread(IThreadingContext threadingContext, IServiceProvider serviceProvider, string languageName)
            : base(threadingContext)
        {
            _componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            _languageName = languageName;
        }

        void IWpfTextViewConnectionListener.SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!_initialized)
            {
                AssertIsForeground();
                CreateServicesOnUIThread(_componentModel, _languageName);
                _initialized = true;
            }
        }

        void IWpfTextViewConnectionListener.SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }

        /// <summary>
        /// Must be invoked from the UI thread.
        /// </summary>
        public static void CreateServicesOnUIThread(IComponentModel componentModel, string languageName)
        {
            var serviceTypeAssemblyQualifiedName = typeof(ISnippetInfoService).AssemblyQualifiedName;
            var languageServices = componentModel.DefaultExportProvider.GetExports<ILanguageService, LanguageServiceMetadata>();
            foreach (var languageService in languageServices)
            {
                if (languageService is
                {
                    Metadata: { ServiceType: serviceTypeAssemblyQualifiedName, Language: languageName }
                }
)
                {
                    var unused = languageService.Value;
                    break;
                }
            }
        }
    }
}
