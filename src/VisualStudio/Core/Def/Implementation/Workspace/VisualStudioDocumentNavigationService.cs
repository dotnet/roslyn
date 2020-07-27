// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal sealed class VisualStudioDocumentNavigationService : ForegroundThreadAffinitizedObject, IDocumentNavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;

        public VisualStudioDocumentNavigationService(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(threadingContext)
        {
            _serviceProvider = serviceProvider;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _runningDocumentTable = (IVsRunningDocumentTable4)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
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
            return TryNavigateToLocation(workspace,
                documentId,
                _ => textSpan,
                text => GetVsTextSpan(text, textSpan),
                options);

            static VsTextSpan GetVsTextSpan(SourceText text, TextSpan textSpan)
            {
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

                return text.GetVsTextSpanForSpan(boundedTextSpan);
            }
        }

        public bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, OptionSet options)
        {
            return TryNavigateToLocation(workspace,
                documentId,
                (document) => GetTextSpanFromLineAndOffset(document, lineNumber, offset),
                (text) => GetVsTextSpan(text, lineNumber, offset),
                options);

            static TextSpan GetTextSpanFromLineAndOffset(Document document, int lineNumber, int offset)
            {
                var text = document.GetTextSynchronously(CancellationToken.None);

                var linePosition = new LinePosition(lineNumber, offset);
                return text.Lines.GetTextSpan(new LinePositionSpan(linePosition, linePosition));
            }

            static VsTextSpan GetVsTextSpan(SourceText text, int lineNumber, int offset)
            {
                return text.GetVsTextSpanForLineOffset(lineNumber, offset);
            }
        }

        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, OptionSet options)
        {
            return TryNavigateToLocation(workspace,
                documentId,
                (document) => GetTextSpanFromPosition(document, position, virtualSpace),
                (text) => GetVsTextSpan(text, position, virtualSpace),
                options);

            static TextSpan GetTextSpanFromPosition(Document document, int position, int virtualSpace)
            {
                var text = document.GetTextSynchronously(CancellationToken.None);
                text.GetLineAndOffset(position, out var lineNumber, out var offset);

                offset += virtualSpace;

                var linePosition = new LinePosition(lineNumber, offset);
                return text.Lines.GetTextSpan(new LinePositionSpan(linePosition, linePosition));
            }

            static VsTextSpan GetVsTextSpan(SourceText text, int position, int virtualSpace)
            {
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

                return text.GetVsTextSpanForPosition(boundedPosition, virtualSpace);
            }
        }

        private bool TryNavigateToLocation(Workspace workspace, DocumentId documentId, Func<Document, TextSpan> getTextSpanForMapping, Func<SourceText, VsTextSpan> getVsTextSpan, OptionSet options)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.Navigation_must_be_performed_on_the_foreground_thread);
            }

            using (OpenNewDocumentStateScope(options ?? workspace.Options))
            {
                // Before attempting to open the document, check if the location maps to a different file that should be opened instead.
                var document = workspace.CurrentSolution.GetDocument(documentId);
                var spanMappingService = document?.Services.GetService<ISpanMappingService>();
                if (spanMappingService != null)
                {
                    var mappedSpan = GetMappedSpan(spanMappingService, document, getTextSpanForMapping(document));
                    if (mappedSpan.HasValue)
                    {
                        return TryNavigateToMappedFile(workspace, document, mappedSpan.Value);
                    }
                }

                document = OpenDocument(workspace, documentId);
                if (document == null)
                {
                    return false;
                }

                var text = document.GetTextSynchronously(CancellationToken.None);
                var textBuffer = text.Container.GetTextBuffer();

                var vsTextSpan = getVsTextSpan(text);
                if (IsSecondaryBuffer(workspace, documentId) &&
                    !vsTextSpan.TryMapSpanFromSecondaryBufferToPrimaryBuffer(workspace, documentId, out vsTextSpan))
                {
                    return false;
                }

                return NavigateTo(textBuffer, vsTextSpan);
            }
        }

        private bool TryNavigateToMappedFile(Workspace workspace, Document generatedDocument, MappedSpanResult mappedSpanResult)
        {
            var vsWorkspace = (VisualStudioWorkspaceImpl)workspace;
            // TODO - Move to IOpenDocumentService - https://github.com/dotnet/roslyn/issues/45954
            // Pass the original result's project context so that if the mapped file has the same context available, we navigate
            // to the mapped file with a consistent project context.
            vsWorkspace.OpenDocumentFromPath(mappedSpanResult.FilePath, generatedDocument.Project.Id);
            if (_runningDocumentTable.TryGetBufferFromMoniker(_editorAdaptersFactoryService, mappedSpanResult.FilePath, out var textBuffer))
            {
                var vsTextSpan = new VsTextSpan
                {
                    iStartIndex = mappedSpanResult.LinePositionSpan.Start.Character,
                    iStartLine = mappedSpanResult.LinePositionSpan.Start.Line,
                    iEndIndex = mappedSpanResult.LinePositionSpan.End.Character,
                    iEndLine = mappedSpanResult.LinePositionSpan.End.Line
                };

                return NavigateTo(textBuffer, vsTextSpan);
            }

            return false;
        }

        private static MappedSpanResult? GetMappedSpan(ISpanMappingService spanMappingService, Document generatedDocument, TextSpan textSpan)
        {
            var results = System.Threading.Tasks.Task.Run(async () =>
            {
                return await spanMappingService.MapSpansAsync(generatedDocument, SpecializedCollections.SingletonEnumerable(textSpan), CancellationToken.None).ConfigureAwait(true);
            }).WaitAndGetResult(CancellationToken.None);

            if (!results.IsDefaultOrEmpty)
            {
                return results.First();
            }

            return null;
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
            => Math.Min(documentLength, Math.Max(position, 0));

        /// <summary>
        /// It is unclear why, but we are sometimes asked to navigate to a <see cref="TextSpan"/>
        /// that is not inside the bounds of the associated <see cref="Document"/>. This method
        /// returns a span that is guaranteed to be inside the <see cref="Document"/> bounds. If
        /// the returned span is different from the given span, then the worst observable behavior
        /// is either no navigation or navigation to the end of the document.
        /// See https://github.com/dotnet/roslyn/issues/7660 for more details.
        /// </summary>
        private static TextSpan GetSpanWithinDocumentBounds(TextSpan span, int documentLength)
            => TextSpan.FromBounds(GetPositionWithinDocumentBounds(span.Start, documentLength), GetPositionWithinDocumentBounds(span.End, documentLength));

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
            => spanInSecondaryBuffer.TryMapSpanFromSecondaryBufferToPrimaryBuffer(workspace, documentId, out _);

        private IDisposable OpenNewDocumentStateScope(OptionSet options)
        {
            var state = options.GetOption(NavigationOptions.PreferProvisionalTab)
                ? __VSNEWDOCUMENTSTATE.NDS_Provisional
                : __VSNEWDOCUMENTSTATE.NDS_Permanent;

            if (!options.GetOption(NavigationOptions.ActivateTab))
            {
                state |= __VSNEWDOCUMENTSTATE.NDS_NoActivate;
            }

            return new NewDocumentStateScope(state, VSConstants.NewDocumentStateReason.Navigation);
        }
    }
}
