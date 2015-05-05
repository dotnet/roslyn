// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;

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

        protected override void OnBeforeNonRoslynDocumentWindowShow(ITextBuffer buffer, bool firstShow)
        {
            base.OnBeforeNonRoslynDocumentWindowShow(buffer, firstShow);

            if (!firstShow)
            {
                return;
            }

            _documentTrackingService?.OnNonRoslynBufferOpened(buffer);
        }

        protected override void OnBeforeNonRoslynDocumentClose(ITextBuffer buffer)
        {
            base.OnBeforeNonRoslynDocumentClose(buffer);

            _documentTrackingService?.OnNonRoslynBufferClosed(buffer);
        }
    }
}
