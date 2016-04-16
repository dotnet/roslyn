' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders

    Public MustInherit Class AbstractContextTests
        Protected MustOverride Function CheckResultAsync(validLocation As Boolean, position As Integer, syntaxTree As SyntaxTree) As Task

        Private Async Function VerifyWorkerAsync(markup As String, validLocation As Boolean) As Threading.Tasks.Task
            Dim text As String = Nothing
            Dim position As Integer = Nothing
            MarkupTestFile.GetPosition(markup, text, position)

            'VerifyWithPlaceHolderRemoved(text, validLocation)
            'VerifyAtEndOfFile(text, validLocation)
            Await VerifyAtPosition_TypePartiallyWrittenAsync(text, position, validLocation)
            Await VerifyAtEndOfFile_TypePartiallyWrittenAsync(text, position, validLocation)
        End Function

        Private Function VerifyAtPositionAsync(text As String, position As Integer, validLocation As Boolean, insertText As String) As Threading.Tasks.Task
            text = text.Substring(0, position) & insertText & text.Substring(position)

            position += insertText.Length

            Dim tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(text))
            Return CheckResultAsync(validLocation, position, tree)
        End Function

        Private Function VerifyAsPositionAsync(text As String, position As Integer, validLocation As Boolean) As Task
            Return VerifyAtPositionAsync(text, position, validLocation, "")
        End Function

        Private Function VerifyAtPosition_TypePartiallyWrittenAsync(text As String, position As Integer, validLocation As Boolean) As Threading.Tasks.Task
            Return VerifyAtPositionAsync(text, position, validLocation, "Str")
        End Function

        Private Async Function VerifyAtEndOfFileAsync(text As String, position As Integer, validLocation As Boolean, insertText As String) As Task
            ' only do this if the placeholder was at the end of the text.
            If text.Length <> position Then
                Return
            End If

            text = text.Substring(startIndex:=0, length:=position) & insertText

            position += insertText.Length

            Dim tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(text))
            Await CheckResultAsync(validLocation, position, tree)
        End Function

        Private Function VerifyAtEndOfFileAsync(text As String, position As Integer, validLocation As Boolean) As Task
            Return VerifyAtEndOfFileAsync(text, position, validLocation, "")
        End Function

        Private Function VerifyAtEndOfFile_TypePartiallyWrittenAsync(text As String, position As Integer, validLocation As Boolean) As Task
            Return VerifyAtEndOfFileAsync(text, position, validLocation, "Str")
        End Function

        Protected Function VerifyTrueAsync(text As String) As Threading.Tasks.Task
            Return VerifyWorkerAsync(text, validLocation:=True)
        End Function

        Protected Function VerifyFalseAsync(text As String) As Threading.Tasks.Task
            Return VerifyWorkerAsync(text, validLocation:=False)
        End Function

        Protected Function AddInsideMethod(text As String) As String
            Return "Class C" & vbCrLf &
                   "    Function F()" & vbCrLf &
                   "        " & text & vbCrLf &
                   "    End Function" & vbCrLf &
                   "End Class"
        End Function

        Protected Function CreateContent(ParamArray contents As String()) As String
            Return String.Join(vbCrLf, contents)
        End Function
    End Class
End Namespace
