// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Responsible for wiring up all the different components of the document outline feature.
    /// </summary>
    internal static class DocumentOutlineViewFactory
    {
        public static DocumentOutlineView CreateView(
            ILanguageServiceBroker2 languageServiceBroker,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IVsCodeWindow codeWindow,
            Workspace workspace,
            IDocumentNavigationService documentNavigationService)
        {
            threadingContext.ThrowIfNotOnUIThread();
            var visualStudioCodeWindowInfoService = new VisualStudioCodeWindowInfoService(codeWindow, editorAdaptersFactoryService, threadingContext);
            var textViewEventSource = CreateEventSource(asyncListener, visualStudioCodeWindowInfoService);
            var viewModel = new DocumentOutlineViewModel(languageServiceBroker, asyncListener, visualStudioCodeWindowInfoService, textViewEventSource, workspace, documentNavigationService);
            return new DocumentOutlineView(viewModel, editorAdaptersFactoryService, codeWindow);
        }

        private static CompilationAvailableTaggerEventSource CreateEventSource(
            IAsynchronousOperationListener asyncListener,
            VisualStudioCodeWindowInfoService visualStudioCodeWindowInfoService)
        {
            var service = visualStudioCodeWindowInfoService.GetServiceAndThrowIfNotOnUIThread();
            var wpfView = service.GetLastActiveIWpfTextView();
            RoslynDebug.AssertNotNull(wpfView);
            var subjectBuffer = wpfView.TextBuffer;
            var textViewEventSource = new CompilationAvailableTaggerEventSource(
                subjectBuffer,
                asyncListener,
                // Any time an edit happens, recompute as the document symbols may have changed.
                TaggerEventSources.OnTextChanged(subjectBuffer),
                // Switching what is the active context may change the document symbols.
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer),
                // Many workspace changes may need us to change the document symbols (like options changing, or project renaming).
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, asyncListener),
                // Once we hook this buffer up to the workspace, then we can start computing the document symbols.
                TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer));
            return textViewEventSource;
        }
    }
}
