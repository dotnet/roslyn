' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.TextManager.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Friend Class SnippetExpansionClientTestsHelper
        Public Shared Sub TestProjectionBuffer(snippetExpansionClient As AbstractSnippetExpansionClient,
                 surfaceBufferDocument As EditorTestHostDocument,
                 expectedSurfaceBuffer As XElement)

            Dim mockExpansionSession = New TestExpansionSession()
            snippetExpansionClient.OnBeforeInsertion(mockExpansionSession)

            Dim snippetSpanInSurfaceBuffer = surfaceBufferDocument.SelectedSpans(0)
            Dim snippetStartLine = surfaceBufferDocument.GetTextBuffer().CurrentSnapshot.GetLineFromPosition(snippetSpanInSurfaceBuffer.Start)
            Dim snippetEndLine = surfaceBufferDocument.GetTextBuffer().CurrentSnapshot.GetLineFromPosition(snippetSpanInSurfaceBuffer.Start)
            Dim snippetTextSpanInSurfaceBuffer = New TextSpan() With
                    {
                        .iStartLine = snippetStartLine.LineNumber,
                        .iStartIndex = snippetSpanInSurfaceBuffer.Start - snippetStartLine.Start.Position,
                        .iEndLine = snippetEndLine.LineNumber,
                        .iEndIndex = snippetSpanInSurfaceBuffer.End - snippetEndLine.Start.Position
                    }

            mockExpansionSession.snippetSpanInSurfaceBuffer = snippetTextSpanInSurfaceBuffer

            Dim endPositionInSurfaceBuffer = surfaceBufferDocument.CursorPosition.Value
            Dim endPositionLine = surfaceBufferDocument.GetTextBuffer().CurrentSnapshot.GetLineFromPosition(endPositionInSurfaceBuffer)
            Dim endPositionIndex = endPositionInSurfaceBuffer - endPositionLine.Start.Position

            mockExpansionSession.endSpanInSurfaceBuffer = New TextSpan() With
                    {
                        .iStartLine = endPositionLine.LineNumber,
                        .iStartIndex = endPositionIndex,
                        .iEndLine = endPositionLine.LineNumber,
                        .iEndIndex = endPositionIndex
                    }

            snippetExpansionClient.FormatSpan(Nothing, {snippetTextSpanInSurfaceBuffer})

            Assert.Equal(expectedSurfaceBuffer.NormalizedValue, surfaceBufferDocument.GetTextBuffer().CurrentSnapshot.GetText)
        End Sub

        Friend Shared Sub TestFormattingAndCaretPosition(
                 snippetExpansionClient As AbstractSnippetExpansionClient,
                 document As EditorTestHostDocument,
                 expectedResult As XElement,
                 expectedVirtualSpacing As Integer)

            Dim mockExpansionSession = New TestExpansionSession()

            Dim cursorPosition = document.CursorPosition.Value
            Dim cursorLine = document.GetTextBuffer().CurrentSnapshot.GetLineFromPosition(cursorPosition)

            mockExpansionSession.SetEndSpan(New TextSpan() With
                    {
                        .iStartLine = cursorLine.LineNumber,
                        .iStartIndex = cursorPosition - cursorLine.Start.Position,
                        .iEndLine = cursorLine.LineNumber,
                        .iEndIndex = cursorPosition - cursorLine.Start.Position
                    })

            snippetExpansionClient.OnBeforeInsertion(mockExpansionSession)

            Dim snippetSpan = document.SelectedSpans(0)
            Dim snippetStartLine = document.GetTextBuffer().CurrentSnapshot.GetLineFromPosition(snippetSpan.Start)
            Dim snippetEndLine = document.GetTextBuffer().CurrentSnapshot.GetLineFromPosition(snippetSpan.End)
            Dim snippetTextSpan = New TextSpan() With
                    {
                        .iStartLine = snippetStartLine.LineNumber,
                        .iStartIndex = snippetSpan.Start - snippetStartLine.Start.Position,
                        .iEndLine = snippetEndLine.LineNumber,
                        .iEndIndex = snippetSpan.End - snippetEndLine.Start.Position
                    }

            mockExpansionSession.snippetSpanInSurfaceBuffer = snippetTextSpan

            Dim endPosition = document.CursorPosition.Value
            Dim endPositionLine = document.GetTextBuffer().CurrentSnapshot.GetLineFromPosition(endPosition)
            Dim endPositionIndex = endPosition - endPositionLine.Start.Position

            mockExpansionSession.endSpanInSurfaceBuffer = New TextSpan() With
                    {
                        .iStartLine = endPositionLine.LineNumber,
                        .iStartIndex = endPositionIndex,
                        .iEndLine = endPositionLine.LineNumber,
                        .iEndIndex = endPositionIndex
                    }

            snippetExpansionClient.FormatSpan(Nothing, {snippetTextSpan})

            Dim finalEndSpan(1) As TextSpan
            mockExpansionSession.GetEndSpan(finalEndSpan)
            Dim formattedEndPositionLine = document.GetTextBuffer().CurrentSnapshot.GetLineFromLineNumber(finalEndSpan(0).iStartLine)

            snippetExpansionClient.PositionCaretForEditingInternal(formattedEndPositionLine.GetText(), formattedEndPositionLine.Start.Position)

            Assert.Equal(expectedVirtualSpacing, document.GetTextView().Caret.Position.VirtualSpaces)
            Assert.Equal(expectedResult.NormalizedValue, document.GetTextBuffer().CurrentSnapshot.GetText)
        End Sub
    End Class
End Namespace
