// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Text;
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
            IVsCodeWindow codeWindow)
        {
            threadingContext.ThrowIfNotOnUIThread();
            var (textViewEventSource, textBuffer) = CreateEventSource(asyncListener, editorAdaptersFactoryService, codeWindow);
            var viewModel = new DocumentOutlineViewModel(languageServiceBroker, asyncListener, textViewEventSource, textBuffer, threadingContext);
            return new DocumentOutlineView(viewModel, editorAdaptersFactoryService, codeWindow);
        }

        private static (CompilationAvailableTaggerEventSource, ITextBuffer) CreateEventSource(
            IAsynchronousOperationListener asyncListener,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IVsCodeWindow codeWindow)
        {
            if (ErrorHandler.Failed(codeWindow.GetLastActiveView(out var textView)))
            {
                FailFast.Fail("Unable to get the last active text view. IVsCodeWindow implementation we are given is invalid.");
            }

            var wpfTextView = editorAdaptersFactoryService.GetWpfTextView(textView);
            Assumes.NotNull(wpfTextView);

            var subjectBuffer = wpfTextView.TextBuffer;
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
            return (textViewEventSource, subjectBuffer);
        }
    }
}
