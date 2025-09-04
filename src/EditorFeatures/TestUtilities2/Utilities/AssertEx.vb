' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
    Public NotInheritable Class AssertEx

        Public Shared Sub TokensAreEqual(expected As String, fixedText As String, language As String)
            Dim expectedNewTokens = ParseTokens(expected, language)
            Dim actualNewTokens = ParseTokens(fixedText, language)

            If expectedNewTokens.Count <> actualNewTokens.Count Then
                Dim expectedDisplay = String.Join(" ", expectedNewTokens.Select(Function(t) t.ToString()))
                Dim actualDisplay = String.Join(" ", actualNewTokens.Select(Function(t) t.ToString()))
                Roslyn.Test.Utilities.AssertEx.Fail("Wrong token count. Expected '{0}', Actual '{1}', Expected Text: '{2}', Actual Text: '{3}'",
                    expectedNewTokens.Count, actualNewTokens.Count, expectedDisplay, actualDisplay)
            End If

            For i = 0 To actualNewTokens.Count - 1
                Dim expectedToken As SyntaxToken = expectedNewTokens(i)
                Dim actualToken = actualNewTokens(i)

                If expectedToken.IsKind(SyntaxKind.StatementTerminatorToken) AndAlso actualToken.IsKind(SyntaxKind.StatementTerminatorToken) Then
                    Continue For
                End If

                Assert.Equal(expectedToken.ToString(), actualToken.ToString())
            Next
        End Sub

        Private Shared Function ParseTokens(expected As String, language As String) As IList(Of SyntaxToken)
            If language = LanguageNames.CSharp Then
                Return Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseTokens(expected).Select(Function(t) CType(t, SyntaxToken)).ToList()
            Else
                Return SyntaxFactory.ParseTokens(expected).Select(Function(t) CType(t, SyntaxToken)).ToList()
            End If
        End Function
    End Class
End Namespace
