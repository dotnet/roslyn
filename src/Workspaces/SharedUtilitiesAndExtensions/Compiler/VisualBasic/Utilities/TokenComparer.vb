' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Class TokenComparer
        Implements IComparer(Of SyntaxToken)

        Private Const s_systemNamespace = "System"

        Public Shared ReadOnly NormalInstance As TokenComparer = New TokenComparer(specialCaseSystem:=False)
        Public Shared ReadOnly SystemFirstInstance As TokenComparer = New TokenComparer(specialCaseSystem:=True)

        Private ReadOnly _specialCaseSystem As Boolean

        Private Sub New(specialCaseSystem As Boolean)
            Me._specialCaseSystem = specialCaseSystem
        End Sub

        Public Function Compare(token1 As SyntaxToken,
                                token2 As SyntaxToken) As Integer Implements IComparer(Of SyntaxToken).Compare
            If _specialCaseSystem AndAlso
                token1.GetPreviousToken().Kind = SyntaxKind.ImportsKeyword AndAlso
                token2.GetPreviousToken().Kind = SyntaxKind.ImportsKeyword Then

                Dim token1IsSystem = IsSystem(token1.ToString())
                Dim token2IsSystem = IsSystem(token2.ToString())

                If token1IsSystem AndAlso Not token2IsSystem Then
                    Return -1
                ElseIf Not token1IsSystem And token2IsSystem Then
                    Return 1
                End If
            End If

            Return CompareWorker(token1, token2)
        End Function

        Private Shared Function IsSystem(s As String) As Boolean
            Return s = s_systemNamespace
        End Function

        Private Shared Function CompareWorker(x As SyntaxToken, y As SyntaxToken) As Integer
            ' By using 'ValueText' we get the value that is normalized.  i.e.
            ' [class] will be 'class', and unicode escapes will be converted
            ' to actual unicode.  This allows sorting to work properly across
            ' tokens that have different source representations, but which
            ' mean the same thing.
            Dim string1 = x.GetIdentifierText()
            Dim string2 = y.GetIdentifierText()

            ' First check in a case insensitive manner.  This will put 
            ' everything that starts with an 'a' or 'A' above everything
            ' that starts with a 'b' or 'B'.
            Dim value = CultureInfo.InvariantCulture.CompareInfo.Compare(string1, string2,
                CompareOptions.IgnoreCase Or CompareOptions.IgnoreNonSpace Or CompareOptions.IgnoreWidth)
            If (value <> 0) Then
                Return value
            End If

            ' Now, once we've grouped such that 'a' words and 'A' words are
            ' together, sort such that 'a' words come before 'A' words.
            Return CultureInfo.InvariantCulture.CompareInfo.Compare(string1, string2,
                CompareOptions.IgnoreNonSpace Or CompareOptions.IgnoreWidth)
        End Function
    End Class
End Namespace
