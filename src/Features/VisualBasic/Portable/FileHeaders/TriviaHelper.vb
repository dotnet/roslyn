' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.FileHeaders
    Friend Module TriviaHelper
        Friend Function IndexOfFirstNonWhitespaceTrivia(Of T As IReadOnlyList(Of SyntaxTrivia))(triviaList As T, Optional endOfLineIsWhitespace As Boolean = True) As Integer
            For index = 0 To triviaList.Count - 1
                Dim currentTrivia = triviaList(index)
                Select Case currentTrivia.Kind()
                    Case SyntaxKind.EndOfLineTrivia
                        If Not endOfLineIsWhitespace Then
                            Return index
                        End If

                    Case SyntaxKind.WhitespaceTrivia
                        ' No action necessary

                    Case Else
                        ' encountered non-whitespace trivia -> the search is done
                        Return index
                End Select
            Next

            Return -1
        End Function
    End Module
End Namespace
