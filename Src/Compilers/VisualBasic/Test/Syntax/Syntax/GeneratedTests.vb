' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Partial Public Class GeneratedTests

        Private Shared Function GenerateGreenIdentifierToken() As IdentifierTokenSyntax
            Return InternalSyntax.SyntaxFactory.Identifier(String.Empty, GenerateGreenWhitespaceTrivia(), GenerateGreenWhitespaceTrivia())
        End Function

        Private Shared Function GenerateGreenIntegerLiteralToken() As InternalSyntax.SyntaxToken
            Return InternalSyntax.SyntaxFactory.IntegerLiteralToken(String.Empty, LiteralBase.Decimal, TypeCharacter.IntegerLiteral, 23, GenerateGreenWhitespaceTrivia(), GenerateGreenWhitespaceTrivia())
        End Function

        Private Shared Function GenerateRedIdentifierToken() As SyntaxToken
            Return SyntaxFactory.Identifier(String.Empty)
        End Function

        Private Shared Function GenerateRedIntegerLiteralToken() As SyntaxToken
            Return SyntaxFactory.IntegerLiteralToken(String.Empty, LiteralBase.Decimal, TypeCharacter.None, 42)
        End Function

        Private Shared Sub AttachAndCheckDiagnostics(node As InternalSyntax.VBSyntaxNode)
            Dim msgProvider = New MyMessageProvider()

            Dim nodeWithDiags = node.SetDiagnostics(New DiagnosticInfo() {New DiagnosticInfo(msgProvider, ERRID.ERR_AccessMismatch6)})
            Dim diags = nodeWithDiags.GetDiagnostics()

            Assert.NotEqual(node, nodeWithDiags)
            Assert.Equal(1, diags.Length)
            Assert.Equal(ERRID.ERR_AccessMismatch6, diags(0).Code)
        End Sub

    End Class

    Friend Class MyMessageProvider
        Inherits TestMessageProvider

        Public Overrides ReadOnly Property CodePrefix As String
            Get
                Return String.Empty
            End Get
        End Property

        Public Overrides Function GetSeverity(code As Integer) As DiagnosticSeverity
            Return 0
        End Function

        Public Overrides Function GetWarningLevel(code As Integer) As Integer
            Return 0
        End Function

        Public Overrides Function LoadMessage(code As Integer, language As Globalization.CultureInfo) As String
            Return String.Empty
        End Function

        Public Overrides Function ConvertSymbolToString(errorCode As Integer, symbol As ISymbol) As String
            Return MessageProvider.Instance.ConvertSymbolToString(errorCode, symbol)
        End Function
    End Class

    Friend Class RedIdentityRewriter
        Inherits VBSyntaxRewriter

        Public Overrides Function DefaultVisit(node As SyntaxNode) As SyntaxNode
            Return node
        End Function
    End Class

    Friend Class GreenIdentityRewriter
        Inherits InternalSyntax.VBSyntaxRewriter

        Public Overrides Function Visit(node As InternalSyntax.VBSyntaxNode) As InternalSyntax.VBSyntaxNode
            Return node
        End Function
    End Class

    Friend Class GreenNodeVisitor
        Inherits InternalSyntax.VBSyntaxVisitor
    End Class

End Namespace

