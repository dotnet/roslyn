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
    Partial Friend Class ObjectCreationCompletionProvider
        Inherits AbstractObjectCreationCompletionProvider

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options)
        End Function

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

        Protected Overrides Async Function CreateContext(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Dim semanticModel = Await document.GetSemanticModelForSpanAsync(New TextSpan(position, 0), cancellationToken).ConfigureAwait(False)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function

        Private Shared s_rules As CompletionItemRules =
            CompletionItemRules.Create(
                commitCharacterRules:=ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, " "c, "("c)),
                matchPriority:=MatchPriority.Preselect,
                selectionBehavior:=CompletionItemSelectionBehavior.HardSelection)

        Protected Overrides Function GetCompletionItemRules(symbols As IReadOnlyList(Of ISymbol), preselect As Boolean) As CompletionItemRules
            Return s_rules
        End Function
    End Class
End Namespace
