' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Private Shared Sub AttachAndCheckDiagnostics(node As InternalSyntax.VisualBasicSyntaxNode)
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

        Public Overrides Function GetDescription(code As Integer) As LocalizableString
            Return String.Empty
        End Function

        Public Overrides Function GetMessageFormat(code As Integer) As LocalizableString
            Return String.Empty
        End Function

        Public Overrides Function GetTitle(code As Integer) As LocalizableString
            Return String.Empty
        End Function

        Public Overrides Function GetHelpLink(code As Integer) As String
            Return String.Empty
        End Function

        Public Overrides Function GetCategory(code As Integer) As String
            Return String.Empty
        End Function

        Public Overrides Function GetErrorDisplayString(symbol As ISymbol) As String
            Return MessageProvider.Instance.GetErrorDisplayString(symbol)
        End Function

        Public Overrides Function GetIsEnabledByDefault(code As Integer) As Boolean
            Return True
        End Function

#If DEBUG Then
        Friend Overrides Function ShouldAssertExpectedMessageArgumentsLength(errorCode As Integer) As Boolean
            Return False
        End Function
#End If
    End Class

    Friend Class RedIdentityRewriter
        Inherits VisualBasicSyntaxRewriter

        Public Overrides Function DefaultVisit(node As SyntaxNode) As SyntaxNode
            Return node
        End Function
    End Class

    Friend Class GreenIdentityRewriter
        Inherits InternalSyntax.VisualBasicSyntaxRewriter

        Public Overrides Function Visit(node As InternalSyntax.VisualBasicSyntaxNode) As InternalSyntax.VisualBasicSyntaxNode
            Return node
        End Function
    End Class

    Friend Class GreenNodeVisitor
        Inherits InternalSyntax.VisualBasicSyntaxVisitor
    End Class

End Namespace

