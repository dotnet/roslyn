﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Navigation;
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

    [ExportWorkspaceService(typeof(IDocumentNavigationService), ServiceLayer.Host), Shared]
    [Export(typeof(VisualStudioDocumentNavigationService))]
    internal sealed class VisualStudioDocumentNavigationService : ForegroundThreadAffinitizedObject, IDocumentNavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;
        private readonly IThreadingContext _threadingContext;
        private readonly Lazy<SourceGeneratedFileManager> _sourceGeneratedFileManager;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDocumentNavigationService(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            Lazy<SourceGeneratedFileManager> sourceGeneratedFileManager /* lazy to avoid circularities */)
            : base(threadingContext)
        {
            _serviceProvider = serviceProvider;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _runningDocumentTable = (IVsRunningDocumentTable4)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            _threadingContext = threadingContext;
            _sourceGeneratedFileManager = sourceGeneratedFileManager;
        }

        public async Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsSecondaryBuffer(documentId))
                return true;

            var document = workspace.CurrentSolution.GetRequiredDocument(documentId);
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var vsTextSpan = GetVsTextSpan(text, textSpan, allowInvalidSpan);
            return await CanMapFromSecondaryBufferToPrimaryBufferAsync(
                documentId, vsTextSpan, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> CanNavigateToLineAndOffsetAsync(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsSecondaryBuffer(documentId))
            {
                return true;
            }

            var document = workspace.CurrentSolution.GetRequiredDocument(documentId);
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var vsTextSpan = text.GetVsTextSpanForLineOffset(lineNumber, offset);

            return await CanMapFromSecondaryBufferToPrimaryBufferAsync(
                documentId, vsTextSpan, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> CanNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            if (!IsSecondaryBuffer(documentId))
            {
                return true;
            }

            var document = workspace.CurrentSolution.GetRequiredDocument(documentId);
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var boundedPosition = GetPositionWithinDocumentBounds(position, text.Length);
            if (boundedPosition != position)
            {
                try
                {
                    throw new ArgumentOutOfRangeException();
                }
                catch (ArgumentOutOfRangeException e) when (FatalError.ReportAndCatch(e))
                {
                }

                return false;
            }

            var vsTextSpan = text.GetVsTextSpanForPosition(position, virtualSpace);

            return await CanMapFromSecondaryBufferToPrimaryBufferAsync(
                documentId, vsTextSpan, cancellationToken).ConfigureAwait(false);
        }

        public async Task<INavigableLocation?> GetLocationForSpanAsync(
            Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken)
        {
            if (!await this.CanNavigateToSpanAsync(workspace, documentId, textSpan, allowInvalidSpan, cancellationToken).ConfigureAwait(false))
                return null;

            return await GetNavigableLocationAsync(workspace,
                documentId,
                _ => Task.FromResult(textSpan),
                text => GetVsTextSpan(text, textSpan, allowInvalidSpan),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<INavigableLocation?> GetLocationForLineAndOffsetAsync(
            Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken)
        {
            if (!await this.CanNavigateToLineAndOffsetAsync(workspace, documentId, lineNumber, offset, cancellationToken).ConfigureAwait(false))
                return null;

            return await GetNavigableLocationAsync(workspace,
                documentId,
                document => GetTextSpanFromLineAndOffsetAsync(document, lineNumber, offset, cancellationToken),
                text => GetVsTextSpan(text, lineNumber, offset),
                cancellationToken).ConfigureAwait(false);

            static async Task<TextSpan> GetTextSpanFromLineAndOffsetAsync(Document document, int lineNumber, int offset, CancellationToken cancellationToken)
            {
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                var linePosition = new LinePosition(lineNumber, offset);
                return text.Lines.GetTextSpan(new LinePositionSpan(linePosition, linePosition));
            }

            static VsTextSpan GetVsTextSpan(SourceText text, int lineNumber, int offset)
            {
                return text.GetVsTextSpanForLineOffset(lineNumber, offset);
            }
        }

        public async Task<INavigableLocation?> GetLocationForPositionAsync(
            Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken)
        {
            if (!await this.CanNavigateToPositionAsync(workspace, documentId, position, virtualSpace, cancellationToken).ConfigureAwait(false))
                return null;

            return await GetNavigableLocationAsync(workspace,
                documentId,
                document => GetTextSpanFromPositionAsync(document, position, virtualSpace, cancellationToken),
                text => GetVsTextSpan(text, position, virtualSpace),
                cancellationToken).ConfigureAwait(false);

            static async Task<TextSpan> GetTextSpanFromPositionAsync(Document document, int position, int virtualSpace, CancellationToken cancellationToken)
            {
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
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
                    catch (ArgumentOutOfRangeException e) when (FatalError.ReportAndCatch(e))
                    {
                    }
                }

                return text.GetVsTextSpanForPosition(boundedPosition, virtualSpace);
            }
        }

        private async Task<INavigableLocation?> GetNavigableLocationAsync(
            Workspace workspace,
            DocumentId documentId,
            Func<Document, Task<TextSpan>> getTextSpanForMappingAsync,
            Func<SourceText, VsTextSpan> getVsTextSpan,
            CancellationToken cancellationToken)
        {
            var callback = await GetNavigationCallbackAsync(
                workspace, documentId, getTextSpanForMappingAsync, getVsTextSpan, cancellationToken).ConfigureAwait(true);
            if (callback == null)
                return null;

            return new NavigableLocation(async (options, cancellationToken) =>
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                using (OpenNewDocumentStateScope(options))
                {
                    // Ensure we come back to the UI Thread after navigating so we close the state scope.
                    return await callback(cancellationToken).ConfigureAwait(true);
                }
            });
        }

        private async Task<Func<CancellationToken, Task<bool>>?> GetNavigationCallbackAsync(
            Workspace workspace,
            DocumentId documentId,
            Func<Document, Task<TextSpan>> getTextSpanForMappingAsync,
            Func<SourceText, VsTextSpan> getVsTextSpan,
            CancellationToken cancellationToken)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            var solution = workspace.CurrentSolution;
            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                var project = solution.GetProject(documentId.ProjectId);
                if (project is null)
                {
                    // This is a source generated document shown in Solution Explorer, but is no longer valid since
                    // the configuration and/or platform changed since the last generation completed.
                    return null;
                }

                var generatedDocument = await project.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
                if (generatedDocument == null)
                    return null;

                return _sourceGeneratedFileManager.Value.GetNavigationCallback(
                    generatedDocument,
                    await getTextSpanForMappingAsync(generatedDocument).ConfigureAwait(false));
            }

            // Before attempting to open the document, check if the location maps to a different file that should be opened instead.
            var spanMappingService = document.Services.GetService<ISpanMappingService>();
            if (spanMappingService != null)
            {
                var mappedSpan = await GetMappedSpanAsync(
                    spanMappingService,
                    document,
                    await getTextSpanForMappingAsync(document).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
                if (mappedSpan.HasValue)
                {
                    // Check if the mapped file matches one already in the workspace.
                    // If so use the workspace APIs to navigate to it.  Otherwise use VS APIs to navigate to the file path.
                    var documentIdsForFilePath = solution.GetDocumentIdsWithFilePath(mappedSpan.Value.FilePath);
                    if (!documentIdsForFilePath.IsEmpty)
                    {
                        // If the mapped file maps to the same document that was passed in, then re-use the documentId to preserve context.
                        // Otherwise, just pick one of the ids to use for navigation.
                        var documentIdToNavigate = documentIdsForFilePath.Contains(documentId) ? documentId : documentIdsForFilePath.First();
                        return GetNavigationCallback(documentIdToNavigate, workspace, getVsTextSpan);
                    }

                    return await GetNavigableLocationForMappedFileAsync(
                        workspace, document, mappedSpan.Value, cancellationToken).ConfigureAwait(false);
                }
            }

            return GetNavigationCallback(documentId, workspace, getVsTextSpan);
        }

        private Func<CancellationToken, Task<bool>>? GetNavigationCallback(
            DocumentId documentId,
            Workspace workspace,
            Func<SourceText, VsTextSpan> getVsTextSpan)
        {
            return async cancellationToken =>
            {
                // Always open the document again, even if the document is already open in the 
                // workspace. If a document is already open in a preview tab and it is opened again 
                // in a permanent tab, this allows the document to transition to the new state.

                if (workspace.CanOpenDocuments)
                    await OpenDocumentAsync(_threadingContext, workspace, documentId, cancellationToken).ConfigureAwait(false);

                if (!workspace.IsDocumentOpen(documentId))
                    return false;

                // Now that we've opened the document reacquire the corresponding Document in the current solution.
                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (document == null)
                    return false;

                // Reacquire the SourceText for it as well.  This will be a practically free as this just wraps
                // the open text buffer.  So it's ok to do this in the navigation step.
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                // Map the given span to the right location in the buffer.  If we're in a projection scenario, ensure
                // the span reflects that.
                var vsTextSpan = getVsTextSpan(text);
                if (IsSecondaryBuffer(documentId))
                {
                    var mapped = await vsTextSpan.MapSpanFromSecondaryBufferToPrimaryBufferAsync(
                        _threadingContext, documentId, cancellationToken).ConfigureAwait(false);
                    if (mapped == null)
                        return false;

                    vsTextSpan = mapped.Value;
                }

                return await NavigateToTextBufferAsync(
                    text.Container.GetTextBuffer(), vsTextSpan, cancellationToken).ConfigureAwait(false);
            };

            async static Task OpenDocumentAsync(
                IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                // OpenDocument must be called on the UI thread.
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                workspace.OpenDocument(documentId);
            }
        }

        private async Task<Func<CancellationToken, Task<bool>>?> GetNavigableLocationForMappedFileAsync(
            Workspace workspace, Document generatedDocument, MappedSpanResult mappedSpanResult, CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var vsWorkspace = (VisualStudioWorkspaceImpl)workspace;
            // TODO - Move to IOpenDocumentService - https://github.com/dotnet/roslyn/issues/45954
            // Pass the original result's project context so that if the mapped file has the same context available, we navigate
            // to the mapped file with a consistent project context.
            vsWorkspace.OpenDocumentFromPath(mappedSpanResult.FilePath, generatedDocument.Project.Id);
            if (!_runningDocumentTable.TryGetBufferFromMoniker(_editorAdaptersFactoryService, mappedSpanResult.FilePath, out var textBuffer))
                return null;

            var vsTextSpan = new VsTextSpan
            {
                iStartIndex = mappedSpanResult.LinePositionSpan.Start.Character,
                iStartLine = mappedSpanResult.LinePositionSpan.Start.Line,
                iEndIndex = mappedSpanResult.LinePositionSpan.End.Character,
                iEndLine = mappedSpanResult.LinePositionSpan.End.Line
            };

            return cancellationToken => NavigateToTextBufferAsync(textBuffer, vsTextSpan, cancellationToken);
        }

        private static async Task<MappedSpanResult?> GetMappedSpanAsync(
            ISpanMappingService spanMappingService, Document generatedDocument, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var results = await spanMappingService.MapSpansAsync(
                generatedDocument, SpecializedCollections.SingletonEnumerable(textSpan), cancellationToken).ConfigureAwait(false);

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

        private static VsTextSpan GetVsTextSpan(SourceText text, TextSpan textSpan, bool allowInvalidSpan)
        {
            var boundedTextSpan = GetSpanWithinDocumentBounds(textSpan, text.Length);
            if (boundedTextSpan != textSpan && !allowInvalidSpan)
            {
                try
                {
                    throw new ArgumentOutOfRangeException();
                }
                catch (ArgumentOutOfRangeException e) when (FatalError.ReportAndCatch(e))
                {
                }
            }

            return text.GetVsTextSpanForSpan(boundedTextSpan);
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
            => TextSpan.FromBounds(GetPositionWithinDocumentBounds(span.Start, documentLength), GetPositionWithinDocumentBounds(span.End, documentLength));

        public async Task<bool> NavigateToTextBufferAsync(
            ITextBuffer textBuffer, VsTextSpan vsTextSpan, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(textBuffer);
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            using (Logger.LogBlock(FunctionId.NavigationService_VSDocumentNavigationService_NavigateTo, cancellationToken))
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

        private static bool IsSecondaryBuffer(DocumentId documentId)
            => ContainedDocument.TryGetContainedDocument(documentId) != null;

        private async Task<bool> CanMapFromSecondaryBufferToPrimaryBufferAsync(
            DocumentId documentId, VsTextSpan spanInSecondaryBuffer, CancellationToken cancellationToken)
        {
            var mapped = await spanInSecondaryBuffer.MapSpanFromSecondaryBufferToPrimaryBufferAsync(
                _threadingContext, documentId, cancellationToken).ConfigureAwait(false);
            return mapped != null;
        }

        private static IDisposable OpenNewDocumentStateScope(NavigationOptions options)
        {
            var state = options.PreferProvisionalTab
                ? __VSNEWDOCUMENTSTATE.NDS_Provisional
                : __VSNEWDOCUMENTSTATE.NDS_Permanent;

            if (!options.ActivateTab)
            {
                state |= __VSNEWDOCUMENTSTATE.NDS_NoActivate;
            }

            return new NewDocumentStateScope(state, VSConstants.NewDocumentStateReason.Navigation);
        }
    }
}
