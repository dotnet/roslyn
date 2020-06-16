' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions
Imports System.Threading
Imports Microsoft.VisualStudio.Text.BraceCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion.Sessions
    Friend Class LessAndGreaterThanCompletionSession
        Inherits AbstractTokenBraceCompletionSession

        Public Sub New(syntaxFactsService As ISyntaxFactsService)
            MyBase.New(syntaxFactsService, SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken)
        End Sub

        Public Overrides Function CheckOpeningPoint(session As IBraceCompletionSession, cancellationToken As CancellationToken) As Boolean
            Dim snapshot = session.SubjectBuffer.CurrentSnapshot
            Dim position = session.OpeningPoint.GetPosition(snapshot)
            Dim token = snapshot.FindToken(position, cancellationToken)

            If Not token.CheckParent(Of AttributeListSyntax)(Function(n) n.LessThanToken = token) AndAlso
               Not token.CheckParent(Of XmlNamespaceImportsClauseSyntax)(Function(n) n.LessThanToken = token) AndAlso
               Not token.CheckParent(Of XmlBracketedNameSyntax)(Function(n) n.LessThanToken = token) Then
                Return False
            End If

            Return True
        End Function

        Public Overrides Function AllowOverType(session As IBraceCompletionSession, cancellationToken As CancellationToken) As Boolean
            Return CheckCurrentPosition(session, cancellationToken)
        End Function
    End Class
End Namespace
