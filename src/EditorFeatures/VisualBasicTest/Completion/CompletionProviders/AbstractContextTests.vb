' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders

    Public MustInherit Class AbstractContextTests
        Protected MustOverride Sub CheckResult(validLocation As Boolean, position As Integer, syntaxTree As SyntaxTree)

        Private Sub VerifyWorker(markup As String, validLocation As Boolean)
            Dim text As String = Nothing
            Dim position As Integer = Nothing
            MarkupTestFile.GetPosition(markup, text, position)

            'VerifyWithPlaceHolderRemoved(text, validLocation)
            'VerifyAtEndOfFile(text, validLocation)
            VerifyAtPosition_TypePartiallyWritten(text, position, validLocation)
            VerifyAtEndOfFile_TypePartiallyWritten(text, position, validLocation)
        End Sub

        Private Sub VerifyAtPosition(text As String, position As Integer, validLocation As Boolean, insertText As String)
            text = text.Substring(0, position) & insertText & text.Substring(position)

            position += insertText.Length

            Dim tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(text))
            CheckResult(validLocation, position, tree)
        End Sub

        Private Sub VerifyAsPosition(text As String, position As Integer, validLocation As Boolean)
            VerifyAtPosition(text, position, validLocation, "")
        End Sub

        Private Sub VerifyAtPosition_TypePartiallyWritten(text As String, position As Integer, validLocation As Boolean)
            VerifyAtPosition(text, position, validLocation, "Str")
        End Sub

        Private Sub VerifyAtEndOfFile(text As String, position As Integer, validLocation As Boolean, insertText As String)
            ' only do this if the placeholder was at the end of the text.
            If text.Length <> position Then
                Return
            End If

            text = text.Substring(startIndex:=0, length:=position) & insertText

            position += insertText.Length

            Dim tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(text))
            CheckResult(validLocation, position, tree)
        End Sub

        Private Sub VerifyAtEndOfFile(text As String, position As Integer, validLocation As Boolean)
            VerifyAtEndOfFile(text, position, validLocation, "")
        End Sub

        Private Sub VerifyAtEndOfFile_TypePartiallyWritten(text As String, position As Integer, validLocation As Boolean)
            VerifyAtEndOfFile(text, position, validLocation, "Str")
        End Sub

        Protected Sub VerifyTrue(text As String)
            VerifyWorker(text, validLocation:=True)
        End Sub

        Protected Sub VerifyFalse(text As String)
            VerifyWorker(text, validLocation:=False)
        End Sub

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
