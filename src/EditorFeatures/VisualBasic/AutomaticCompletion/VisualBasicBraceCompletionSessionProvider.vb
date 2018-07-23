' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion.Sessions
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion
    <ExportLanguageService(GetType(IEditorBraceCompletionSessionFactory), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEditorBraceCompletionSessionFactory
        Inherits AbstractEditorBraceCompletionSessionFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As IThreadingContext)
            MyBase.New(threadingContext)
        End Sub

        Protected Overrides Function IsSupportedOpeningBrace(openingBrace As Char) As Boolean
            Select Case openingBrace
                Case BraceCompletionSessionProvider.CurlyBrace.OpenCharacter,
                     BraceCompletionSessionProvider.Bracket.OpenCharacter,
                     BraceCompletionSessionProvider.Parenthesis.OpenCharacter,
                     BraceCompletionSessionProvider.LessAndGreaterThan.OpenCharacter,
                     BraceCompletionSessionProvider.DoubleQuote.OpenCharacter
                    Return True
            End Select

            Return False
        End Function

        Protected Overrides Function CheckCodeContext(document As Document, position As Integer, openingBrace As Char, cancellationToken As CancellationToken) As Boolean
            ' SPECIAL CASE: Allow " after $ (skipped token) to support interpolated strings and { inside of an interpolated string.
            If openingBrace = BraceCompletionSessionProvider.DoubleQuote.OpenCharacter AndAlso
               InterpolatedStringCompletionSession.IsContext(document, position, cancellationToken) Then

                Return True
            ElseIf openingBrace = BraceCompletionSessionProvider.CurlyBrace.OpenCharacter AndAlso
                   InterpolationCompletionSession.IsContext(document, position, cancellationToken) Then

                Return True
            End If

            ' Otherwise, defer to the base implementation.
            Return MyBase.CheckCodeContext(document, position, openingBrace, cancellationToken)
        End Function

        Protected Overrides Function CreateEditorSession(document As Document, openingPosition As Integer, openingBrace As Char, cancellationToken As CancellationToken) As IEditorBraceCompletionSession
            Dim syntaxFactsService = document.GetLanguageService(Of ISyntaxFactsService)
            Select Case openingBrace
                Case BraceCompletionSessionProvider.CurlyBrace.OpenCharacter
                    If InterpolationCompletionSession.IsContext(document, openingPosition, cancellationToken) Then
                        Return New InterpolationCompletionSession(syntaxFactsService)
                    Else
                        Return New CurlyBraceCompletionSession(syntaxFactsService)
                    End If

                Case BraceCompletionSessionProvider.Bracket.OpenCharacter
                    Return New BracketCompletionSession(syntaxFactsService)
                Case BraceCompletionSessionProvider.Parenthesis.OpenCharacter
                    Return New ParenthesisCompletionSession(syntaxFactsService)
                Case BraceCompletionSessionProvider.LessAndGreaterThan.OpenCharacter
                    Return New LessAndGreaterThanCompletionSession(syntaxFactsService)
                Case BraceCompletionSessionProvider.DoubleQuote.OpenCharacter
                    If InterpolatedStringCompletionSession.IsContext(document, openingPosition, cancellationToken) Then
                        Return New InterpolatedStringCompletionSession(syntaxFactsService)
                    Else
                        Return New StringLiteralCompletionSession(syntaxFactsService)
                    End If
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(openingBrace)
            End Select
        End Function
    End Class
End Namespace
