' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.BraceCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.BraceCompletion
    <ExportBraceCompletionService(LanguageNames.VisualBasic), [Shared]>
    Friend Class LessAndGreaterThanCompletionService
        Inherits AbstractVisualBasicBraceCompletionService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New()
        End Sub

        Protected Overrides ReadOnly Property OpeningBrace As Char = LessAndGreaterThan.OpenCharacter
        Protected Overrides ReadOnly Property ClosingBrace As Char = LessAndGreaterThan.CloseCharacter

        Protected Overrides Function IsValidOpeningBraceToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.LessThanToken)
        End Function

        Protected Overrides Function IsValidClosingBraceToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.LessThanGreaterThanToken)
        End Function

        Protected Overrides Function IsValidOpenBraceTokenAtPosition(text As SourceText, token As SyntaxToken, position As Integer) As Boolean
            If Not token.CheckParent(Of AttributeListSyntax)(Function(n) n.LessThanToken = token) AndAlso
               Not token.CheckParent(Of XmlNamespaceImportsClauseSyntax)(Function(n) n.LessThanToken = token) AndAlso
               Not token.CheckParent(Of XmlBracketedNameSyntax)(Function(n) n.LessThanToken = token) Then
                Return False
            End If

            Return True
        End Function

        Public Overrides Function AllowOverType(context As BraceCompletionContext, cancellationToken As CancellationToken) As Boolean
            Return AllowOverTypeInUserCodeWithValidClosingToken(context, cancellationToken)
        End Function
    End Class
End Namespace
