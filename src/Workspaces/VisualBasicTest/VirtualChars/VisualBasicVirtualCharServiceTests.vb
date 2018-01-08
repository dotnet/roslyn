' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VirtualChars
Imports Microsoft.CodeAnalysis.VisualBasic.VirtualChars
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.VirtualChars
    Public Class VisualBasicVirtualCharServiceTests
        Private Const _statmentPrefix As String = "dim v = "

        Private Function GetStringToken(text As String) As SyntaxToken
            Dim statement = _statmentPrefix + text
            Dim parsedStatement = SyntaxFactory.ParseExecutableStatement(statement)
            Dim token = parsedStatement.DescendantTokens().ToArray()(3)
            Assert.True(token.Kind() = SyntaxKind.StringLiteralToken)

            Return token
        End Function

        Private Sub Test(stringText As String, expected As String)
            Dim token = GetStringToken(stringText)
            Dim virtualChars = VisualBasicVirtualCharService.Instance.TryConvertToVirtualChars(token)
            Dim actual = ConvertToString(virtualChars)
            Assert.Equal(expected, actual)
        End Sub

        Private Sub TestFailure(stringText As String)
            Dim token = GetStringToken(stringText)
            Dim virtualChars = VisualBasicVirtualCharService.Instance.TryConvertToVirtualChars(token)
            Assert.True(virtualChars.IsDefault)
        End Sub

        <Fact>
        Public Sub TestEmptyString()
            Test("""""", "")
        End Sub

        <Fact>
        Public Sub TestSimpleString()
            Test("""a""", "['a',[1,2]]")
        End Sub

        <Fact>
        Public Sub TestStringWithDoubleQuoteInIt()
            Test("""a""""b""", "['a',[1,2]]['""',[2,4]]['b',[4,5]]")
        End Sub

        Private Function ConvertToString(virtualChars As ImmutableArray(Of VirtualChar)) As String
            Return String.Join("", virtualChars.Select(AddressOf ConvertToString))
        End Function

        Private Function ConvertToString(vc As VirtualChar) As String
            Return $"[{ConvertToString(vc.Char)},[{vc.Span.Start - _statmentPrefix.Length},{vc.Span.End - _statmentPrefix.Length}]]"
        End Function

        Private Function ConvertToString(c As Char) As String
            Return "'" + c + "'"
        End Function
    End Class
End Namespace
