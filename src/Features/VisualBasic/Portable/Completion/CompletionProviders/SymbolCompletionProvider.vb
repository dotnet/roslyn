' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Recommendations
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Partial Friend Class SymbolCompletionProvider
        Inherits AbstractSymbolCompletionProvider

        Protected Overrides Function GetSymbolsWorker(context As AbstractSyntaxContext, position As Integer, options As OptionSet, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of ISymbol))
            Return Task.FromResult(Recommender.GetRecommendedSymbolsAtPosition(context.SemanticModel, position, context.Workspace, options, cancellationToken))
        End Function

        Protected Overrides Function GetTextChangeSpan(text As SourceText, position As Integer) As TextSpan
            Return CompletionUtilities.GetTextChangeSpan(text, position)
        End Function

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Protected Overrides Async Function IsSemanticTriggerCharacterAsync(document As Document, characterPosition As Integer, cancellationToken As CancellationToken) As Task(Of Boolean)
            If document Is Nothing Then
                Return False
            End If

            Dim result = Await IsTriggerOnDotAsync(document, characterPosition, cancellationToken).ConfigureAwait(False)
            If result.HasValue Then
                Return result.Value
            End If

            Return True
        End Function

        Private Async Function IsTriggerOnDotAsync(document As Document, characterPosition As Integer, cancellationToken As CancellationToken) As Task(Of Boolean?)
            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            If text(characterPosition) <> "."c Then
                Return Nothing
            End If

            ' don't want to trigger after a number.  All other cases after dot are ok.
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim token = root.FindToken(characterPosition)
            Return IsValidTriggerToken(token)
        End Function

        Private Function IsValidTriggerToken(token As SyntaxToken) As Boolean
            If token.Kind <> SyntaxKind.DotToken Then
                Return False
            End If

            Dim previousToken = token.GetPreviousToken()
            If previousToken.Kind = SyntaxKind.IntegerLiteralToken Then
                Return token.Parent.Kind <> SyntaxKind.SimpleMemberAccessExpression OrElse Not DirectCast(token.Parent, MemberAccessExpressionSyntax).Expression.IsKind(SyntaxKind.NumericLiteralExpression)
            End If

            Return True
        End Function

        Protected Overrides Function GetDisplayAndInsertionText(symbol As ISymbol, context As AbstractSyntaxContext) As ValueTuple(Of String, String)
            Return CompletionUtilities.GetDisplayAndInsertionText(symbol, context.IsAttributeNameContext, context.IsRightOfNameSeparator, DirectCast(context, VisualBasicSyntaxContext).WithinAsyncMethod, context.GetLanguageService(Of ISyntaxFactsService))
        End Function

        Protected Overrides Async Function CreateContext(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of AbstractSyntaxContext)
            Dim semanticModel = Await document.GetSemanticModelForSpanAsync(New TextSpan(position, 0), cancellationToken).ConfigureAwait(False)
            Return VisualBasicSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken)
        End Function

        Protected Overrides Function GetFilterText(symbol As ISymbol, displayText As String, context As AbstractSyntaxContext) As String
            ' Filter on New if we have a ctor
            If symbol.IsConstructor() Then
                Return "New"
            End If

            Return MyBase.GetFilterText(symbol, displayText, context)
        End Function

        Protected Overrides Function GetCompletionItemRules() As CompletionItemRules
            Return ItemRules.Instance
        End Function

    End Class
End Namespace
