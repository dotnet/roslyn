' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.BraceCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<Export(LanguageNames.VisualBasic, GetType(IBraceCompletionService)), [Shared]>
Friend Class LessAndGreaterThanCompletionService
    Inherits AbstractBraceCompletionService

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

    Protected Overrides Function IsValidOpenBraceTokenAtPositionAsync(token As SyntaxToken, position As Integer, document As Document, cancellationToken As CancellationToken) As Task(Of Boolean)
        If Not token.CheckParent(Of AttributeListSyntax)(Function(n) n.LessThanToken = token) AndAlso
               Not token.CheckParent(Of XmlNamespaceImportsClauseSyntax)(Function(n) n.LessThanToken = token) AndAlso
               Not token.CheckParent(Of XmlBracketedNameSyntax)(Function(n) n.LessThanToken = token) Then
            Return SpecializedTasks.False
        End If

        Return SpecializedTasks.True
    End Function

    Public Overrides Function AllowOverTypeAsync(context As BraceCompletionContext, cancellationToken As CancellationToken) As Task(Of Boolean)
        Return AllowOverTypeInUserCodeWithValidClosingTokenAsync(context, cancellationToken)
    End Function
End Class
