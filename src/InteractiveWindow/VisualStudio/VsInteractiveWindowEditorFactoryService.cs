// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Shell;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    [Export(typeof(IInteractiveWindowEditorFactoryService))]
    [Export(typeof(IVsInteractiveWindowEditorsFactoryService))]
    internal sealed class VsInteractiveWindowEditorFactoryService : IInteractiveWindowEditorFactoryService, IVsInteractiveWindowEditorsFactoryService
    {
        private readonly IOleServiceProvider _provider;
        private readonly IVsEditorAdaptersFactoryService _adapterFactory;
        private readonly IContentTypeRegistryService _contentTypeRegistry;
        private readonly IEnumerable<Lazy<IVsInteractiveWindowOleCommandTargetProvider, ContentTypeMetadata>> _oleCommandTargetProviders;

        [ImportingConstructor]
        public VsInteractiveWindowEditorFactoryService(IVsEditorAdaptersFactoryService adaptersFactory, IContentTypeRegistryService contentTypeRegistry, [ImportMany]IEnumerable<Lazy<IVsInteractiveWindowOleCommandTargetProvider, ContentTypeMetadata>> oleCommandTargetProviders)
        {
            _adapterFactory = adaptersFactory;
            _provider = (IOleServiceProvider)InteractiveWindowPackage.GetGlobalService(typeof(IOleServiceProvider));
            _contentTypeRegistry = contentTypeRegistry;
            _oleCommandTargetProviders = oleCommandTargetProviders;
        }

        IWpfTextView IInteractiveWindowEditorFactoryService.CreateTextView(IInteractiveWindow window, ITextBuffer buffer, ITextViewRoleSet roles)
        {
            var bufferAdapter = _adapterFactory.CreateVsTextBufferAdapterForSecondaryBuffer(_provider, buffer);

            // Create and initialize text view adapter.
            // WARNING: This might trigger various services like IntelliSense, margins, taggers, etc.
            var textViewAdapter = _adapterFactory.CreateVsTextViewAdapter(_provider, roles);

            var commandFilter = new VsInteractiveWindowCommandFilter(_adapterFactory, window, textViewAdapter, bufferAdapter);
            window.Properties[typeof(VsInteractiveWindowCommandFilter)] = commandFilter;
            return commandFilter.TextViewHost.TextView;
        }

        ITextBuffer IInteractiveWindowEditorFactoryService.CreateAndActivateBuffer(IInteractiveWindow window, IContentType contentType)
        {
            // create buffer adapter to support undo/redo:
            var bufferAdapter = _adapterFactory.CreateVsTextBufferAdapter(_provider, contentType);
            bufferAdapter.InitializeContent("", 0);

            var commandFilter = GetCommandFilter(window);
            if (commandFilter.currentBufferCommandHandler != null)
            {
                ((IVsPersistDocData)commandFilter.currentBufferCommandHandler).Close();
            }

            commandFilter.currentBufferCommandHandler = (IOleCommandTarget)bufferAdapter;

            return _adapterFactory.GetDocumentBuffer(bufferAdapter);
        }

        public IWpfTextViewHost GetTextViewHost(IInteractiveWindow window)
        {
            var cmdFilter = GetCommandFilter(window);
            if (cmdFilter != null)
            {
                return cmdFilter.TextViewHost;
            }
            return null;
        }

        public void SetLanguage(IInteractiveWindow window, Guid languageServiceGuid)
        {
            // REVIEW: What happens to the current input buffer here?
            // REVIEW: What happens when the window is already initialized?
            GetDispatcher(window).CheckAccess();

            var commandFilter = GetCommandFilter(window);
            commandFilter.firstLanguageServiceCommandFilter = null;
            var provider = _oleCommandTargetProviders.OfContentType(window.Evaluator.ContentType, _contentTypeRegistry);
            if (provider != null)
            {
                var targetFilter = commandFilter.firstLanguageServiceCommandFilter ?? GetCommandFilter(window).EditorServicesCommandFilter;
                var target = provider.GetCommandTarget(window.TextView, targetFilter);
                if (target != null)
                {
                    commandFilter.firstLanguageServiceCommandFilter = target;
                }
            }

            SetEditorOptions(window.TextView.Options, languageServiceGuid);
        }

        private void SetEditorOptions(IEditorOptions options, Guid languageServiceGuid)
        {
            IVsTextManager textMgr = (IVsTextManager)InteractiveWindowPackage.GetGlobalService(typeof(SVsTextManager));
            var langPrefs = new LANGPREFERENCES[1];
            langPrefs[0].guidLang = languageServiceGuid;
            ErrorHandler.ThrowOnFailure(textMgr.GetUserPreferences(null, null, langPrefs, null));

            options.SetOptionValue(DefaultTextViewHostOptions.ChangeTrackingId, false);
            options.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, langPrefs[0].fInsertTabs == 0);
            options.SetOptionValue(DefaultOptions.TabSizeOptionId, (int)langPrefs[0].uTabSize);
            options.SetOptionValue(DefaultOptions.IndentSizeOptionId, (int)langPrefs[0].uIndentSize);
        }

        private Dispatcher GetDispatcher(IInteractiveWindow window)
        {
            return ((FrameworkElement)window.TextView).Dispatcher;
        }

        public VsInteractiveWindowCommandFilter GetCommandFilter(IInteractiveWindow window)
        {
            return (VsInteractiveWindowCommandFilter)window.Properties[typeof(VsInteractiveWindowCommandFilter)];
        }
    }
}
