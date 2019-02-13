' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.EmbeddedLanguages.VirtualChars
    Public Class VisualBasicVirtualCharServiceTests
        Private Const _statementPrefix As String = "dim v = "

        Private Function GetStringToken(text As String) As SyntaxToken
            Dim statement = _statementPrefix + text
            Dim parsedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement(statement), LocalDeclarationStatementSyntax)
            Dim expression = parsedStatement.Declarators(0).Initializer.Value

            Dim token = If(TypeOf expression Is LiteralExpressionSyntax,
                            DirectCast(expression, LiteralExpressionSyntax).Token,
                            DirectCast(expression, InterpolatedStringExpressionSyntax).Contents(0).ChildTokens().First())
            Assert.True(token.Kind() = SyntaxKind.StringLiteralToken OrElse
                        token.Kind() = SyntaxKind.InterpolatedStringTextToken)

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
        Public Sub TestCurliesInSimpleString()
            Test("""{{""", "['{',[1,2]]['{',[2,3]]")
        End Sub

        <Fact>
        Public Sub TestCurliesInInterpolatedSimpleString()
            Test("$""{{""", "['{',[2,4]]")
        End Sub

        <Fact>
        Public Sub TestCurliesInInterpolatedSimpleString2()
            Test("$""{{1}}""", "['{',[2,4]]['1',[4,5]]['}',[5,7]]")
        End Sub

        <Fact>
        Public Sub TestStringWithDoubleQuoteInIt()
            Test("""a""""b""", "['a',[1,2]]['""',[2,4]]['b',[4,5]]")
        End Sub

        <Fact>
        Public Sub TestInterpolatedStringWithDoubleQuoteInIt()
            Test("$""a""""b""", "['a',[2,3]]['""',[3,5]]['b',[5,6]]")
        End Sub

        Private Function ConvertToString(virtualChars As ImmutableArray(Of VirtualChar)) As String
            Return String.Join("", virtualChars.Select(AddressOf ConvertToString))
        End Function

        Private Function ConvertToString(vc As VirtualChar) As String
            Return $"[{ConvertToString(vc.Char)},[{vc.Span.Start - _statementPrefix.Length},{vc.Span.End - _statementPrefix.Length}]]"
        End Function

        Private Function ConvertToString(c As Char) As String
            Return "'" + c + "'"
        End Function
    End Class
End Namespace
