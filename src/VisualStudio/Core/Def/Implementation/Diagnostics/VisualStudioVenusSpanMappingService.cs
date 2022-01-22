// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [ExportWorkspaceService(typeof(IWorkspaceVenusSpanMappingService), ServiceLayer.Default), Shared]
    internal partial class VisualStudioVenusSpanMappingService : IWorkspaceVenusSpanMappingService
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioVenusSpanMappingService(VisualStudioWorkspaceImpl workspace)
            => _workspace = workspace;

        public void GetAdjustedDiagnosticSpan(
            DocumentId documentId, Location location,
            out TextSpan sourceSpan, out FileLinePositionSpan originalLineInfo, out FileLinePositionSpan mappedLineInfo)
        {
            sourceSpan = location.SourceSpan;
            originalLineInfo = location.GetLineSpan();
            mappedLineInfo = location.GetMappedLineSpan();

            // check quick bail out case.
            if (location == Location.None)
            {
                return;
            }

            // Update the original source span, if required.
            if (!TryAdjustSpanIfNeededForVenus(documentId, originalLineInfo, mappedLineInfo, out var originalSpan, out var mappedSpan))
            {
                return;
            }

            if (originalSpan.Start != originalLineInfo.StartLinePosition || originalSpan.End != originalLineInfo.EndLinePosition)
            {
                originalLineInfo = new FileLinePositionSpan(originalLineInfo.Path, originalSpan.Start, originalSpan.End);

                var textLines = GetTextLines(documentId, location);
                if (textLines != null)
                {
                    // adjust sourceSpan only if we could get text lines
                    var startPos = textLines.GetPosition(originalSpan.Start);
                    var endPos = textLines.GetPosition(originalSpan.End);

                    sourceSpan = TextSpan.FromBounds(startPos, Math.Max(startPos, endPos));
                }
            }

            if (mappedSpan.Start != mappedLineInfo.StartLinePosition || mappedSpan.End != mappedLineInfo.EndLinePosition)
            {
                mappedLineInfo = new FileLinePositionSpan(mappedLineInfo.Path, mappedSpan.Start, mappedSpan.End);
            }
        }

        private TextLineCollection GetTextLines(DocumentId currentDocumentId, Location location)
        {
            // normal case - all C# and VB should hit this
            if (location.SourceTree != null)
            {
                return location.SourceTree.GetText().Lines;
            }

            // special case for typescript and etc that don't use our compilations.
            var filePath = location.GetLineSpan().Path;
            if (filePath != null)
            {
                // as a sanity check, make sure given location is on the current document
                // we do the check down the stack for C# and VB using SyntaxTree in location
                // but for typescript and other, we don't have the tree, so adding this as
                // sanity check. later we could convert this to Contract to crash VS and
                // know about the issue.
                var documentIds = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath);
                if (documentIds.Contains(currentDocumentId))
                {
                    // text most likely already read in
                    return _workspace.CurrentSolution.GetDocument(currentDocumentId).State.GetTextSynchronously(CancellationToken.None).Lines;
                }
            }

            // we don't know how to get text lines for the given location
            return null;
        }

        private bool TryAdjustSpanIfNeededForVenus(
            DocumentId documentId, FileLinePositionSpan originalLineInfo, FileLinePositionSpan mappedLineInfo, out LinePositionSpan originalSpan, out LinePositionSpan mappedSpan)
        {
            var startChanged = true;
            if (!TryAdjustSpanIfNeededForVenus(_workspace, documentId, originalLineInfo.StartLinePosition.Line, originalLineInfo.StartLinePosition.Character, out var startLineColumn))
            {
                startChanged = false;
                startLineColumn = new MappedSpan(originalLineInfo.StartLinePosition.Line, originalLineInfo.StartLinePosition.Character, mappedLineInfo.StartLinePosition.Line, mappedLineInfo.StartLinePosition.Character);
            }

            var endChanged = true;
            if (!TryAdjustSpanIfNeededForVenus(_workspace, documentId, originalLineInfo.EndLinePosition.Line, originalLineInfo.EndLinePosition.Character, out var endLineColumn))
            {
                endChanged = false;
                endLineColumn = new MappedSpan(originalLineInfo.EndLinePosition.Line, originalLineInfo.EndLinePosition.Character, mappedLineInfo.EndLinePosition.Line, mappedLineInfo.EndLinePosition.Character);
            }

            // start and end position can be swapped when mapped between primary and secondary buffer if start position is within visible span (at the edge)
            // but end position is outside of visible span. in that case, swap start and end position.
            originalSpan = GetLinePositionSpan(startLineColumn.OriginalLinePosition, endLineColumn.OriginalLinePosition);
            mappedSpan = GetLinePositionSpan(startLineColumn.MappedLinePosition, endLineColumn.MappedLinePosition);

            return startChanged || endChanged;
        }

        private static LinePositionSpan GetLinePositionSpan(LinePosition position1, LinePosition position2)
        {
            if (position1 <= position2)
            {
                return new LinePositionSpan(position1, position2);
            }

            return new LinePositionSpan(position2, position1);
        }

        public static LinePosition GetAdjustedLineColumn(Workspace workspace, DocumentId documentId, int originalLine, int originalColumn, int mappedLine, int mappedColumn)
        {
            if (workspace is not VisualStudioWorkspaceImpl vsWorkspace)
            {
                return new LinePosition(mappedLine, mappedColumn);
            }

            if (TryAdjustSpanIfNeededForVenus(vsWorkspace, documentId, originalLine, originalColumn, out var span))
            {
                return span.MappedLinePosition;
            }

            return new LinePosition(mappedLine, mappedColumn);
        }

        private static bool TryAdjustSpanIfNeededForVenus(VisualStudioWorkspaceImpl workspace, DocumentId documentId, int originalLine, int originalColumn, out MappedSpan mappedSpan)
        {
            mappedSpan = default;

            if (documentId == null)
            {
                return false;
            }

            var containedDocument = workspace.TryGetContainedDocument(documentId);
            if (containedDocument == null)
            {
                return false;
            }

            var originalSpanOnSecondaryBuffer = new TextManager.Interop.TextSpan()
            {
                iStartLine = originalLine,
                iStartIndex = originalColumn,
                iEndLine = originalLine,
                iEndIndex = originalColumn
            };

            var bufferCoordinator = containedDocument.BufferCoordinator;
            var containedLanguageHost = containedDocument.ContainedLanguageHost;

            var spansOnPrimaryBuffer = new TextManager.Interop.TextSpan[1];
            if (VSConstants.S_OK == bufferCoordinator.MapSecondaryToPrimarySpan(originalSpanOnSecondaryBuffer, spansOnPrimaryBuffer))
            {
                // easy case, we can map span in subject buffer to surface buffer. no need to adjust any span
                mappedSpan = new MappedSpan(originalLine, originalColumn, spansOnPrimaryBuffer[0].iStartLine, spansOnPrimaryBuffer[0].iStartIndex);
                return true;
            }

            // we can't directly map span in subject buffer to surface buffer. see whether there is any visible span we can use from the subject buffer span
            if (containedLanguageHost != null &&
                VSConstants.S_OK != containedLanguageHost.GetNearestVisibleToken(originalSpanOnSecondaryBuffer, spansOnPrimaryBuffer))
            {
                // no visible span we can use.
                return false;
            }

            // We need to map both the original and mapped location into visible code so that features such as error list, squiggle, etc. points to user visible area
            // We have the mapped location in the primary buffer.
            var nearestVisibleSpanOnPrimaryBuffer = new TextManager.Interop.TextSpan()
            {
                iStartLine = spansOnPrimaryBuffer[0].iStartLine,
                iStartIndex = spansOnPrimaryBuffer[0].iStartIndex,
                iEndLine = spansOnPrimaryBuffer[0].iStartLine,
                iEndIndex = spansOnPrimaryBuffer[0].iStartIndex
            };

            // Map this location back to the secondary span to re-adjust the original location to be in user-code in secondary buffer.
            var spansOnSecondaryBuffer = new TextManager.Interop.TextSpan[1];
            if (VSConstants.S_OK != bufferCoordinator.MapPrimaryToSecondarySpan(nearestVisibleSpanOnPrimaryBuffer, spansOnSecondaryBuffer))
            {
                // we can't adjust original position but we can adjust mapped one
                mappedSpan = new MappedSpan(originalLine, originalColumn, nearestVisibleSpanOnPrimaryBuffer.iStartLine, nearestVisibleSpanOnPrimaryBuffer.iStartIndex);
                return true;
            }

            var nearestVisibleSpanOnSecondaryBuffer = spansOnSecondaryBuffer[0];
            var originalLocationMovedAboveInFile = IsOriginalLocationMovedAboveInFile(originalLine, originalColumn, nearestVisibleSpanOnSecondaryBuffer.iStartLine, nearestVisibleSpanOnSecondaryBuffer.iStartIndex);

            if (!originalLocationMovedAboveInFile)
            {
                mappedSpan = new MappedSpan(nearestVisibleSpanOnSecondaryBuffer.iStartLine, nearestVisibleSpanOnSecondaryBuffer.iStartIndex, nearestVisibleSpanOnPrimaryBuffer.iStartLine, nearestVisibleSpanOnPrimaryBuffer.iStartIndex);
                return true;
            }

            if (TryFixUpNearestVisibleSpan(bufferCoordinator, nearestVisibleSpanOnSecondaryBuffer.iStartLine, nearestVisibleSpanOnSecondaryBuffer.iStartIndex, out var adjustedPosition))
            {
                // span has changed yet again, re-calculate span
                return TryAdjustSpanIfNeededForVenus(workspace, documentId, adjustedPosition.Line, adjustedPosition.Character, out mappedSpan);
            }

            mappedSpan = new MappedSpan(nearestVisibleSpanOnSecondaryBuffer.iStartLine, nearestVisibleSpanOnSecondaryBuffer.iStartIndex, nearestVisibleSpanOnPrimaryBuffer.iStartLine, nearestVisibleSpanOnPrimaryBuffer.iStartIndex);
            return true;
        }

        private static bool TryFixUpNearestVisibleSpan(
            TextManager.Interop.IVsTextBufferCoordinator bufferCoordinator,
            int originalLine, int originalColumn, out LinePosition adjustedPosition)
        {
            // GetNearestVisibleToken gives us the position right at the end of visible span.
            // Move the position one position to the left so that squiggle can show up on last token.
            if (originalColumn > 1)
            {
                adjustedPosition = new LinePosition(originalLine, originalColumn - 1);
                return true;
            }

            if (originalLine > 1)
            {
                if (VSConstants.S_OK == bufferCoordinator.GetSecondaryBuffer(out var secondaryBuffer) &&
                    VSConstants.S_OK == secondaryBuffer.GetLengthOfLine(originalLine - 1, out var length))
                {
                    adjustedPosition = new LinePosition(originalLine - 1, length);
                    return true;
                }
            }

            adjustedPosition = LinePosition.Zero;
            return false;
        }

        private static bool IsOriginalLocationMovedAboveInFile(int originalLine, int originalColumn, int movedLine, int movedColumn)
        {
            if (movedLine < originalLine)
            {
                return true;
            }

            if (movedLine == originalLine && movedColumn < originalColumn)
            {
                return true;
            }

            return false;
        }

        private struct MappedSpan
        {
            private readonly int _originalLine;
            private readonly int _originalColumn;
            private readonly int _mappedLine;
            private readonly int _mappedColumn;

            public MappedSpan(int originalLine, int originalColumn, int mappedLine, int mappedColumn)
            {
                _originalLine = originalLine;
                _originalColumn = originalColumn;
                _mappedLine = mappedLine;
                _mappedColumn = mappedColumn;
            }

            public LinePosition OriginalLinePosition
            {
                get { return new LinePosition(_originalLine, _originalColumn); }
            }

            public LinePosition MappedLinePosition
            {
                get { return new LinePosition(_mappedLine, _mappedColumn); }
            }
        }
    }
}
