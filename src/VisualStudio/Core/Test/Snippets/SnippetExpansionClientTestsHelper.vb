' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.TextManager.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Friend Class SnippetExpansionClientTestsHelper
        Public Shared Sub Test(snippetExpansionClient As AbstractSnippetExpansionClient,
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
    End Class
End Namespace
