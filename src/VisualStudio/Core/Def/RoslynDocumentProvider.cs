// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal class RoslynDocumentProvider : DocumentProvider
    {
        private VisualStudioDocumentTrackingService _documentTrackingService;

        public RoslynDocumentProvider(
            IVisualStudioHostProjectContainer projectContainer,
            IServiceProvider serviceProvider,
            IDocumentTrackingService documentTrackingService = null)
            : base(projectContainer, serviceProvider, signUpForFileChangeNotification: true)
        {
            _documentTrackingService = documentTrackingService as VisualStudioDocumentTrackingService;
        }

        protected override void OnBeforeDocumentWindowShow(IVsWindowFrame frame, DocumentId id, bool firstShow)
        {
            base.OnBeforeDocumentWindowShow(frame, id, firstShow);

            _documentTrackingService?.DocumentFrameShowing(frame, id, firstShow);
        }

        protected override void OnBeforeNonRoslynDocumentWindowShow(IVsWindowFrame frame, bool firstShow)
        {
            base.OnBeforeNonRoslynDocumentWindowShow(frame, firstShow);

            if (!firstShow)
            {
                return;
            }

            var view = GetTextViewFromFrame(frame);
            if (view != null)
            {
                _documentTrackingService?.OnNonRoslynViewOpened(view);
            }
        }

        private ITextView GetTextViewFromFrame(IVsWindowFrame frame)
        {
            IntPtr unknown;
            if (ErrorHandler.Failed(frame.QueryViewInterface(typeof(IVsCodeWindow).GUID, out unknown)) || unknown == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var codeWindow = Marshal.GetTypedObjectForIUnknown(unknown, typeof(IVsCodeWindow)) as IVsCodeWindow;

                IVsTextView vsTextView;
                if (ErrorHandler.Failed(codeWindow.GetLastActiveView(out vsTextView)))
                {
                    return null;
                }

                return EditorAdaptersFactoryService.GetWpfTextView(vsTextView);
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }
    }
}
