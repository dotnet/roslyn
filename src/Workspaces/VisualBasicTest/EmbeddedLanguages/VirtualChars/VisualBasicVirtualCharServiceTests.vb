' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.EmbeddedLanguages.VirtualChars
    Public Class VisualBasicVirtualCharServiceTests
        Private Const _statementPrefix As String = "dim v = "

        Private Shared Function GetStringToken(text As String) As SyntaxToken
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

        Private Shared Sub Test(stringText As String, expected As String)
            Dim token = GetStringToken(stringText)
            Dim virtualChars = VisualBasicVirtualCharService.Instance.TryConvertToVirtualChars(token)
            For Each ch In virtualChars
                For i = ch.Span.Start To ch.Span.End - 1
                    Assert.Equal(ch, virtualChars.Find(i))
                Next
            Next
            Dim actual = ConvertToString(virtualChars)
            Assert.Equal(expected, actual)
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
        Public Sub TestSimpleMultiCharString()
            Test("""abc""", "['a',[1,2]]['b',[2,3]]['c',[3,4]]")
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

        Private Shared Function ConvertToString(virtualChars As VirtualCharSequence) As String
            Dim strings = ArrayBuilder(Of String).GetInstance()
            For Each ch In virtualChars
                strings.Add(ConvertToString(ch))
            Next

            Return String.Join("", strings.ToImmutableAndFree())
        End Function

        Private Shared Function ConvertToString(vc As VirtualChar) As String
            Return $"[{ConvertToString(ChrW(vc.Rune.Value))},[{vc.Span.Start - _statementPrefix.Length},{vc.Span.End - _statementPrefix.Length}]]"
        End Function

        Private Shared Function ConvertToString(c As Char) As String
            Return "'" + c + "'"
        End Function
    End Class
End Namespace
