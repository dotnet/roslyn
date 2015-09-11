' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.TextManager.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Friend Class SnippetExpansionClientTestsHelper
        Public Shared Sub TestProjectionBuffer(snippetExpansionClient As AbstractSnippetExpansionClient,
                 subjectBufferDocument As TestHostDocument,
                 surfaceBufferDocument As TestHostDocument,
                 expectedSurfaceBuffer As XElement)

            Dim mockExpansionSession = New TestExpansionSession()
            snippetExpansionClient.OnBeforeInsertion(mockExpansionSession)

            Dim snippetSpanInSurfaceBuffer = surfaceBufferDocument.SelectedSpans(0)
            Dim snippetStartLine = surfaceBufferDocument.TextBuffer.CurrentSnapshot.GetLineFromPosition(snippetSpanInSurfaceBuffer.Start)
            Dim snippetEndLine = surfaceBufferDocument.TextBuffer.CurrentSnapshot.GetLineFromPosition(snippetSpanInSurfaceBuffer.Start)
            Dim snippetTextSpanInSurfaceBuffer = New TextSpan() With
                    {
                        .iStartLine = snippetStartLine.LineNumber,
                        .iStartIndex = snippetSpanInSurfaceBuffer.Start - snippetStartLine.Start.Position,
                        .iEndLine = snippetEndLine.LineNumber,
                        .iEndIndex = snippetSpanInSurfaceBuffer.End - snippetEndLine.Start.Position
                    }

            mockExpansionSession.snippetSpanInSurfaceBuffer = snippetTextSpanInSurfaceBuffer

            Dim endPositionInSurfaceBuffer = surfaceBufferDocument.CursorPosition.Value
            Dim endPositionLine = surfaceBufferDocument.TextBuffer.CurrentSnapshot.GetLineFromPosition(endPositionInSurfaceBuffer)
            Dim endPositionIndex = endPositionInSurfaceBuffer - endPositionLine.Start.Position

            mockExpansionSession.endSpanInSurfaceBuffer = New TextSpan() With
                    {
                        .iStartLine = endPositionLine.LineNumber,
                        .iStartIndex = endPositionIndex,
                        .iEndLine = endPositionLine.LineNumber,
                        .iEndIndex = endPositionIndex
                    }

            snippetExpansionClient.FormatSpan(Nothing, {snippetTextSpanInSurfaceBuffer})

            Assert.Equal(expectedSurfaceBuffer.NormalizedValue, surfaceBufferDocument.TextBuffer.CurrentSnapshot.GetText)
        End Sub

        Friend Shared Sub TestFormattingAndCaretPosition(
                 snippetExpansionClient As AbstractSnippetExpansionClient,
                 document As TestHostDocument,
                 expectedResult As XElement,
                 expectedVirtualSpacing As Integer)

            Dim mockExpansionSession = New TestExpansionSession()

            Dim cursorPosition = document.CursorPosition.Value
            Dim cursorLine = document.TextBuffer.CurrentSnapshot.GetLineFromPosition(cursorPosition)

            mockExpansionSession.SetEndSpan(New TextSpan() With
                    {
                        .iStartLine = cursorLine.LineNumber,
                        .iStartIndex = cursorPosition - cursorLine.Start.Position,
                        .iEndLine = cursorLine.LineNumber,
                        .iEndIndex = cursorPosition - cursorLine.Start.Position
                    })

            snippetExpansionClient.OnBeforeInsertion(mockExpansionSession)

            Dim snippetSpan = document.SelectedSpans(0)
            Dim snippetStartLine = document.TextBuffer.CurrentSnapshot.GetLineFromPosition(snippetSpan.Start)
            Dim snippetEndLine = document.TextBuffer.CurrentSnapshot.GetLineFromPosition(snippetSpan.End)
            Dim snippetTextSpan = New TextSpan() With
                    {
                        .iStartLine = snippetStartLine.LineNumber,
                        .iStartIndex = snippetSpan.Start - snippetStartLine.Start.Position,
                        .iEndLine = snippetEndLine.LineNumber,
                        .iEndIndex = snippetSpan.End - snippetEndLine.Start.Position
                    }

            mockExpansionSession.snippetSpanInSurfaceBuffer = snippetTextSpan

            Dim endPosition = document.CursorPosition.Value
            Dim endPositionLine = document.TextBuffer.CurrentSnapshot.GetLineFromPosition(endPosition)
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
            Dim formattedEndPositionLine = document.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(finalEndSpan(0).iStartLine)

            snippetExpansionClient.PositionCaretForEditingInternal(formattedEndPositionLine.GetText(), formattedEndPositionLine.Start.Position)

            Assert.Equal(expectedVirtualSpacing, document.GetTextView().Caret.Position.VirtualSpaces)
            Assert.Equal(expectedResult.NormalizedValue, document.TextBuffer.CurrentSnapshot.GetText)
        End Sub
    End Class
End Namespace
