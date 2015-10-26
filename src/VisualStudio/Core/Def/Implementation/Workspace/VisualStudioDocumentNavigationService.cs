// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class VisualStudioDocumentNavigationService : ForegroundThreadAffinitizedObject, IDocumentNavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

        public VisualStudioDocumentNavigationService(
            SVsServiceProvider serviceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
        {
            _serviceProvider = serviceProvider;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
        }

        public bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsSecondaryBuffer(workspace, documentId))
            {
                return true;
            }

            var document = workspace.CurrentSolution.GetDocument(documentId);
            var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var vsTextSpan = text.GetVsTextSpanForSpan(textSpan);

            return CanMapFromSecondaryBufferToPrimaryBuffer(workspace, documentId, vsTextSpan);
        }

        public bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsSecondaryBuffer(workspace, documentId))
            {
                return true;
            }

            var document = workspace.CurrentSolution.GetDocument(documentId);
            var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var vsTextSpan = text.GetVsTextSpanForLineOffset(lineNumber, offset);

            return CanMapFromSecondaryBufferToPrimaryBuffer(workspace, documentId, vsTextSpan);
        }

        public bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsSecondaryBuffer(workspace, documentId))
            {
                return true;
            }

            var document = workspace.CurrentSolution.GetDocument(documentId);
            var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var vsTextSpan = text.GetVsTextSpanForPosition(position, virtualSpace);

            return CanMapFromSecondaryBufferToPrimaryBuffer(workspace, documentId, vsTextSpan);
        }

        public bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, OptionSet options)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.NavigationMustBePerformedOnTheForegroundThread);
            }

            var document = OpenDocument(workspace, documentId, options);
            if (document == null)
            {
                return false;
            }

            var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var textBuffer = text.Container.GetTextBuffer();

            var vsTextSpan = text.GetVsTextSpanForSpan(textSpan);
            if (IsSecondaryBuffer(workspace, documentId) &&
                !vsTextSpan.TryMapSpanFromSecondaryBufferToPrimaryBuffer(workspace, documentId, out vsTextSpan))
            {
                return false;
            }

            return NavigateTo(textBuffer, vsTextSpan);
        }

        public bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, OptionSet options)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.NavigationMustBePerformedOnTheForegroundThread);
            }

            var document = OpenDocument(workspace, documentId, options);
            if (document == null)
            {
                return false;
            }

            var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var textBuffer = text.Container.GetTextBuffer();

            var vsTextSpan = text.GetVsTextSpanForLineOffset(lineNumber, offset);

            if (IsSecondaryBuffer(workspace, documentId) &&
                !vsTextSpan.TryMapSpanFromSecondaryBufferToPrimaryBuffer(workspace, documentId, out vsTextSpan))
            {
                return false;
            }

            return NavigateTo(textBuffer, vsTextSpan);
        }

        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, OptionSet options)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.NavigationMustBePerformedOnTheForegroundThread);
            }

            var document = OpenDocument(workspace, documentId, options);
            if (document == null)
            {
                return false;
            }

            var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var textBuffer = text.Container.GetTextBuffer();

            var vsTextSpan = text.GetVsTextSpanForPosition(position, virtualSpace);

            if (IsSecondaryBuffer(workspace, documentId) &&
                !vsTextSpan.TryMapSpanFromSecondaryBufferToPrimaryBuffer(workspace, documentId, out vsTextSpan))
            {
                return false;
            }

            return NavigateTo(textBuffer, vsTextSpan);
        }

        private static Document OpenDocument(Workspace workspace, DocumentId documentId, OptionSet options)
        {
            // Always open the document again, even if the document is already open in the 
            // workspace. If a document is already open in a preview tab and it is opened again 
            // in a permanent tab, this allows the document to transition to the new state.
            if (workspace.CanOpenDocuments)
            {
                if (options.GetOption(NavigationOptions.UsePreviewTab))
                {
                    using (NewDocumentStateScope ndss = new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Provisional, VSConstants.NewDocumentStateReason.Navigation))
                    {
                        workspace.OpenDocument(documentId);
                    }
                }
                else
                {
                    workspace.OpenDocument(documentId);
                }
            }

            if (!workspace.IsDocumentOpen(documentId))
            {
                return null;
            }

            return workspace.CurrentSolution.GetDocument(documentId);
        }

        private bool NavigateTo(ITextBuffer textBuffer, VsTextSpan vsTextSpan)
        {
            using (Logger.LogBlock(FunctionId.NavigationService_VSDocumentNavigationService_NavigateTo, CancellationToken.None))
            {
                var vsTextBuffer = _editorAdaptersFactoryService.GetBufferAdapter(textBuffer);
                if (vsTextBuffer == null)
                {
                    Debug.Fail("Could not get IVsTextBuffer for document!");
                    return false;
                }

                var textManager = (IVsTextManager2)_serviceProvider.GetService(typeof(SVsTextManager));
                if (textManager == null)
                {
                    Debug.Fail("Could not get IVsTextManager service!");
                    return false;
                }

                return ErrorHandler.Succeeded(
                    textManager.NavigateToLineAndColumn2(
                        vsTextBuffer, VSConstants.LOGVIEWID.TextView_guid, vsTextSpan.iStartLine, vsTextSpan.iStartIndex, vsTextSpan.iEndLine, vsTextSpan.iEndIndex, (uint)_VIEWFRAMETYPE.vftCodeWindow));
            }
        }

        private bool IsSecondaryBuffer(Workspace workspace, DocumentId documentId)
        {
            var visualStudioWorkspace = workspace as VisualStudioWorkspaceImpl;
            if (visualStudioWorkspace == null)
            {
                return false;
            }

            var containedDocument = visualStudioWorkspace.GetHostDocument(documentId) as ContainedDocument;
            if (containedDocument == null)
            {
                return false;
            }

            return true;
        }

        private bool CanMapFromSecondaryBufferToPrimaryBuffer(Workspace workspace, DocumentId documentId, VsTextSpan spanInSecondaryBuffer)
        {
            VsTextSpan spanInPrimaryBuffer;
            return spanInSecondaryBuffer.TryMapSpanFromSecondaryBufferToPrimaryBuffer(workspace, documentId, out spanInPrimaryBuffer);
        }
    }
}
