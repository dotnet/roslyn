// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
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
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal sealed class VisualStudioDocumentNavigationService : ForegroundThreadAffinitizedObject, IDocumentNavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

        public VisualStudioDocumentNavigationService(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(threadingContext)
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
            var text = document.GetTextSynchronously(CancellationToken.None);

            var boundedTextSpan = GetSpanWithinDocumentBounds(textSpan, text.Length);
            if (boundedTextSpan != textSpan)
            {
                try
                {
                    throw new ArgumentOutOfRangeException();
                }
                catch (ArgumentOutOfRangeException e) when (FatalError.ReportWithoutCrash(e))
                {
                }

                return false;
            }

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
            var text = document.GetTextSynchronously(CancellationToken.None);
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
            var text = document.GetTextSynchronously(CancellationToken.None);

            var boundedPosition = GetPositionWithinDocumentBounds(position, text.Length);
            if (boundedPosition != position)
            {
                try
                {
                    throw new ArgumentOutOfRangeException();
                }
                catch (ArgumentOutOfRangeException e) when (FatalError.ReportWithoutCrash(e))
                {
                }

                return false;
            }

            var vsTextSpan = text.GetVsTextSpanForPosition(position, virtualSpace);

            return CanMapFromSecondaryBufferToPrimaryBuffer(workspace, documentId, vsTextSpan);
        }

        public bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, OptionSet options)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.Navigation_must_be_performed_on_the_foreground_thread);
            }

            using (OpenNewDocumentStateScope(options ?? workspace.Options))
            {
                var document = OpenDocument(workspace, documentId);
                if (document == null)
                {
                    return false;
                }

                var text = document.GetTextSynchronously(CancellationToken.None);
                var textBuffer = text.Container.GetTextBuffer();

                var boundedTextSpan = GetSpanWithinDocumentBounds(textSpan, text.Length);
                if (boundedTextSpan != textSpan)
                {
                    try
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    catch (ArgumentOutOfRangeException e) when (FatalError.ReportWithoutCrash(e))
                    {
                    }
                }

                var vsTextSpan = text.GetVsTextSpanForSpan(boundedTextSpan);

                if (IsSecondaryBuffer(workspace, documentId) &&
                    !vsTextSpan.TryMapSpanFromSecondaryBufferToPrimaryBuffer(workspace, documentId, out vsTextSpan))
                {
                    return false;
                }

                return NavigateTo(textBuffer, vsTextSpan);
            }
        }

        public bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, OptionSet options)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.Navigation_must_be_performed_on_the_foreground_thread);
            }

            using (OpenNewDocumentStateScope(options ?? workspace.Options))
            {
                var document = OpenDocument(workspace, documentId);
                if (document == null)
                {
                    return false;
                }

                var text = document.GetTextSynchronously(CancellationToken.None);
                var textBuffer = text.Container.GetTextBuffer();

                var vsTextSpan = text.GetVsTextSpanForLineOffset(lineNumber, offset);

                if (IsSecondaryBuffer(workspace, documentId) &&
                    !vsTextSpan.TryMapSpanFromSecondaryBufferToPrimaryBuffer(workspace, documentId, out vsTextSpan))
                {
                    return false;
                }

                return NavigateTo(textBuffer, vsTextSpan);
            }
        }

        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, OptionSet options)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.Navigation_must_be_performed_on_the_foreground_thread);
            }

            using (OpenNewDocumentStateScope(options ?? workspace.Options))
            {
                var document = OpenDocument(workspace, documentId);
                if (document == null)
                {
                    return false;
                }

                var text = document.GetTextSynchronously(CancellationToken.None);
                var textBuffer = text.Container.GetTextBuffer();

                var boundedPosition = GetPositionWithinDocumentBounds(position, text.Length);
                if (boundedPosition != position)
                {
                    try
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    catch (ArgumentOutOfRangeException e) when (FatalError.ReportWithoutCrash(e))
                    {
                    }
                }

                var vsTextSpan = text.GetVsTextSpanForPosition(boundedPosition, virtualSpace);

                if (IsSecondaryBuffer(workspace, documentId) &&
                    !vsTextSpan.TryMapSpanFromSecondaryBufferToPrimaryBuffer(workspace, documentId, out vsTextSpan))
                {
                    return false;
                }

                return NavigateTo(textBuffer, vsTextSpan);
            }
        }

        /// <summary>
        /// It is unclear why, but we are sometimes asked to navigate to a position that is not
        /// inside the bounds of the associated <see cref="Document"/>. This method returns a
        /// position that is guaranteed to be inside the <see cref="Document"/> bounds. If the
        /// returned position is different from the given position, then the worst observable
        /// behavior is either no navigation or navigation to the end of the document. See the
        /// following bugs for more details:
        ///     https://devdiv.visualstudio.com/DevDiv/_workitems?id=112211
        ///     https://devdiv.visualstudio.com/DevDiv/_workitems?id=136895
        ///     https://devdiv.visualstudio.com/DevDiv/_workitems?id=224318
        ///     https://devdiv.visualstudio.com/DevDiv/_workitems?id=235409
        /// </summary>
        private static int GetPositionWithinDocumentBounds(int position, int documentLength)
        {
            return Math.Min(documentLength, Math.Max(position, 0));
        }

        /// <summary>
        /// It is unclear why, but we are sometimes asked to navigate to a <see cref="TextSpan"/>
        /// that is not inside the bounds of the associated <see cref="Document"/>. This method
        /// returns a span that is guaranteed to be inside the <see cref="Document"/> bounds. If
        /// the returned span is different from the given span, then the worst observable behavior
        /// is either no navigation or navigation to the end of the document.
        /// See https://github.com/dotnet/roslyn/issues/7660 for more details.
        /// </summary>
        private static TextSpan GetSpanWithinDocumentBounds(TextSpan span, int documentLength)
        {
            return TextSpan.FromBounds(GetPositionWithinDocumentBounds(span.Start, documentLength), GetPositionWithinDocumentBounds(span.End, documentLength));
        }

        private static Document OpenDocument(Workspace workspace, DocumentId documentId)
        {
            // Always open the document again, even if the document is already open in the 
            // workspace. If a document is already open in a preview tab and it is opened again 
            // in a permanent tab, this allows the document to transition to the new state.
            if (workspace.CanOpenDocuments)
            {
                workspace.OpenDocument(documentId);
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
                        vsTextBuffer,
                        VSConstants.LOGVIEWID.TextView_guid,
                        vsTextSpan.iStartLine,
                        vsTextSpan.iStartIndex,
                        vsTextSpan.iEndLine,
                        vsTextSpan.iEndIndex,
                        (uint)_VIEWFRAMETYPE.vftCodeWindow));
            }
        }

        private bool IsSecondaryBuffer(Workspace workspace, DocumentId documentId)
        {
            if (!(workspace is VisualStudioWorkspaceImpl visualStudioWorkspace))
            {
                return false;
            }

            var containedDocument = visualStudioWorkspace.TryGetContainedDocument(documentId);
            if (containedDocument == null)
            {
                return false;
            }

            return true;
        }

        private bool CanMapFromSecondaryBufferToPrimaryBuffer(Workspace workspace, DocumentId documentId, VsTextSpan spanInSecondaryBuffer)
        {
            return spanInSecondaryBuffer.TryMapSpanFromSecondaryBufferToPrimaryBuffer(workspace, documentId, out var spanInPrimaryBuffer);
        }

        private IDisposable OpenNewDocumentStateScope(OptionSet options)
        {
            if (!options.GetOption(NavigationOptions.PreferProvisionalTab))
            {
                return null;
            }

            // If we're just opening the provisional tab, then do not "activate" the document
            // (i.e. don't give it focus).  This way if a user is just arrowing through a set 
            // of FindAllReferences results, they don't have their cursor placed into the document.
            var state = __VSNEWDOCUMENTSTATE.NDS_Provisional | __VSNEWDOCUMENTSTATE.NDS_NoActivate;
            return new NewDocumentStateScope(state, VSConstants.NewDocumentStateReason.Navigation);
        }
    }
}
