' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class SymbolCompletionProvider
        Inherits AbstractRecommendationServiceBasedCompletionProvider

        Protected Overrides Function GetInsertionText(item As CompletionItem, ch As Char) As String
            Return CompletionUtilities.GetInsertionTextAtInsertionTime(item, ch)
        End Function

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
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

        Protected Overrides Function GetDisplayAndSuffixAndInsertionText(symbol As ISymbol, context As SyntaxContext) As (displayText As String, suffix As String, insertionText As String)
            Return CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context)
        End Function

        Protected Overrides Async Function CreateContext(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Dim semanticModel = Await document.GetSemanticModelForSpanAsync(New TextSpan(position, 0), cancellationToken).ConfigureAwait(False)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function GetFilterText(symbol As ISymbol, displayText As String, context As SyntaxContext) As String
            ' Filter on New if we have a ctor
            If symbol.IsConstructor() Then
                Return "New"
            End If

            Return MyBase.GetFilterText(symbol, displayText, context)
        End Function

        Private Shared cachedRules As Dictionary(Of ValueTuple(Of Boolean, Boolean, Boolean), CompletionItemRules) =
            InitCachedRules()

        Private Shared Function InitCachedRules() As Dictionary(Of ValueTuple(Of Boolean, Boolean, Boolean), CompletionItemRules)

            Dim result = New Dictionary(Of ValueTuple(Of Boolean, Boolean, Boolean), CompletionItemRules)()

            For importDirective = 0 To 1
                For preselect = 0 To 1
                    For tuple = 0 To 1
                        Dim context = ValueTuple.Create(importDirective = 1, preselect = 1, tuple = 1)
                        result(context) = MakeRule(importDirective, preselect, tuple)
                    Next
                Next
            Next

            Return result
        End Function

        Private Shared Function MakeRule(importDirective As Integer, preselect As Integer, tuple As Integer) As CompletionItemRules
            Return MakeRule(importDirective = 1, preselect = 1, tuple = 1)
        End Function

        Private Shared Function MakeRule(importDirective As Boolean, preselect As Boolean, tuple As Boolean) As CompletionItemRules

            ' '(' should not filter the completion list, even though it's in generic items like IList(Of...)
            Dim generalBaseline = CompletionItemRules.Default.
                WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, "("c)).
                WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, "("c))

            Dim importDirectBasline As CompletionItemRules =
                CompletionItemRules.Create(commitCharacterRules:=
                    ImmutableArray.Create(
                        CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, "."c)))

            Dim rule = If(importDirective, importDirectBasline, generalBaseline)

            If preselect Then
                rule = rule.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection)
            End If

            If tuple Then
                rule = rule.WithCommitCharacterRule(
                    CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ":"c))
            End If

            Return rule
        End Function

        Protected Overrides Function GetCompletionItemRules(symbols As List(Of ISymbol),
                                                            context As SyntaxContext, preselect As Boolean) As CompletionItemRules

            Return If(cachedRules(
                        ValueTuple.Create(context.IsInImportsDirective, preselect, context.IsPossibleTupleContext)),
                    CompletionItemRules.Default)
        End Function

        Protected Overrides Function IsInstrinsic(s As ISymbol) As Boolean
            Return If(TryCast(s, ITypeSymbol)?.IsIntrinsicType(), False)
        End Function

        Protected Overrides ReadOnly Property PreselectedItemSelectionBehavior As CompletionItemSelectionBehavior
            Get
                Return CompletionItemSelectionBehavior.SoftSelection
            End Get
        End Property
    End Class
End Namespace
