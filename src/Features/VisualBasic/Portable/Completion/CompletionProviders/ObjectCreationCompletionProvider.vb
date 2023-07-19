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
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(ObjectCreationCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(ObjectInitializerCompletionProvider))>
    <[Shared]>
    Partial Friend Class ObjectCreationCompletionProvider
        Inherits AbstractObjectCreationCompletionProvider(Of VisualBasicSyntaxContext)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CompletionUtilities.SpaceTriggerChar

        Protected Overrides Function GetObjectCreationNewExpression(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxNode
            Dim newExpression As SyntaxNode = Nothing

            If tree IsNot Nothing AndAlso Not tree.IsInNonUserCode(position, cancellationToken) AndAlso Not tree.IsInSkippedText(position, cancellationToken) Then
                Dim newToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken)
                newToken = newToken.GetPreviousTokenIfTouchingWord(position)

                ' Only after 'new'.
                If newToken.Kind = SyntaxKind.NewKeyword Then
                    ' Only if the 'new' belongs to an object creation expression.
                    If tree.IsObjectCreationTypeContext(position, cancellationToken) Then
                        newExpression = TryCast(newToken.Parent, ExpressionSyntax)
                    End If
                End If
            End If

            Return newExpression
        End Function

        Private Shared ReadOnly s_rules As CompletionItemRules =
            CompletionItemRules.Create(
                commitCharacterRules:=ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, " "c, "("c)),
                matchPriority:=MatchPriority.Preselect,
                selectionBehavior:=CompletionItemSelectionBehavior.HardSelection)

        Protected Overrides Function GetCompletionItemRules(symbols As ImmutableArray(Of SymbolAndSelectionInfo)) As CompletionItemRules
            Return s_rules
        End Function

        Protected Overrides Function GetDisplayAndSuffixAndInsertionText(symbol As ISymbol, context As VisualBasicSyntaxContext) As (displayText As String, suffix As String, insertionText As String)
            Dim displayString = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position)
            Return (displayString, "", displayString)
        End Function
    End Class
End Namespace
