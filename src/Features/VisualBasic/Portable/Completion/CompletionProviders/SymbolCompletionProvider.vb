' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Recommendations
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(SymbolCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(AwaitCompletionProvider))>
    <[Shared]>
    Friend NotInheritable Class SymbolCompletionProvider
        Inherits AbstractRecommendationServiceBasedCompletionProvider(Of VisualBasicSyntaxContext)

        Private Shared ReadOnly s_cachedRules As New Dictionary(Of (importDirective As Boolean, preselect As Boolean, tuple As Boolean), CompletionItemRules)

        Shared Sub New()
            For importDirective = 0 To 1
                For preselect = 0 To 1
                    For tuple = 0 To 1
                        Dim context = (importDirective:=importDirective = 1, preselect:=preselect = 1, tuple:=tuple = 1)
                        s_cachedRules(context) = MakeRule(context)
                    Next
                Next
            Next
        End Sub

        Private Shared Function MakeRule(context As (importDirective As Boolean, preselect As Boolean, tuple As Boolean)) As CompletionItemRules
            ' '(' should not filter the completion list, even though it's in generic items like IList(Of...)
            Dim generalBaseline = CompletionItemRules.Default.
                WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, "("c)).
                WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, "("c))

            Dim importDirectBasline = CompletionItemRules.Create(
                commitCharacterRules:=ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, "."c)))

            Dim rule = If(context.importDirective, importDirectBasline, generalBaseline)

            If context.preselect Then
                rule = rule.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection)
            End If

            If context.tuple Then
                rule = rule.WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ":"c))
            End If

            Return rule
        End Function

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides ReadOnly Property PreselectedItemSelectionBehavior As CompletionItemSelectionBehavior = CompletionItemSelectionBehavior.SoftSelection

        Protected Overrides Function ShouldPreselectInferredTypesAsync(completionContext As CompletionContext, position As Integer, options As CompletionOptions, cancellationToken As CancellationToken) As Task(Of Boolean)
            Return SpecializedTasks.True
        End Function

        Protected Overrides Function GetInsertionText(item As CompletionItem, ch As Char) As String
            Return GetInsertionTextAtInsertionTime(item, ch)
        End Function

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CompletionUtilities.CommonTriggerCharsAndParen

        Protected Overrides Function IsTriggerOnDot(token As SyntaxToken, characterPositoin As Integer) As Boolean
            If token.Kind() <> SyntaxKind.DotToken Then
                Return False
            End If

            Dim previousToken = token.GetPreviousToken()
            If previousToken.Kind = SyntaxKind.IntegerLiteralToken Then
                Return token.Parent.Kind <> SyntaxKind.SimpleMemberAccessExpression OrElse
                    Not DirectCast(token.Parent, MemberAccessExpressionSyntax).Expression.IsKind(SyntaxKind.NumericLiteralExpression)
            End If

            Return True
        End Function

        Protected Overrides Function GetDisplayAndSuffixAndInsertionText(symbol As ISymbol, context As VisualBasicSyntaxContext) As (displayText As String, suffix As String, insertionText As String)
            Return CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context)
        End Function

        Protected Overrides Function GetFilterText(symbol As ISymbol, displayText As String, context As VisualBasicSyntaxContext) As String
            ' Filter on New if we have a ctor
            If symbol.IsConstructor() Then
                Return "New"
            End If

            Return MyBase.GetFilterText(symbol, displayText, context)
        End Function

        Protected Overrides Function GetCompletionItemRules(symbols As ImmutableArray(Of (symbol As ISymbol, preselect As Boolean)), context As VisualBasicSyntaxContext) As CompletionItemRules
            Dim preselect = symbols.Any(Function(s) s.preselect)
            Return If(s_cachedRules(ValueTuple.Create(context.IsInImportsDirective, preselect, context.IsPossibleTupleContext)),
                      CompletionItemRules.Default)
        End Function

        Protected Overrides Function IsInstrinsic(s As ISymbol) As Boolean
            Return If(TryCast(s, ITypeSymbol)?.IsIntrinsicType(), False)
        End Function
    End Class
End Namespace
