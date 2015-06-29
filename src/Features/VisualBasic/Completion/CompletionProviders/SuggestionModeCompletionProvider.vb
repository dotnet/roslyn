' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class SuggestionModeCompletionProvider
        Implements ICompletionProvider

        Public Function GetTextChange(selectedItem As CompletionItem, Optional ch As Char? = Nothing, Optional textTypedSoFar As String = Nothing) As TextChange Implements ICompletionProvider.GetTextChange
            Return New TextChange(selectedItem.FilterSpan, selectedItem.DisplayText)
        End Function

        Public Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean Implements ICompletionProvider.IsCommitCharacter
            Return True
        End Function

        Public Function IsFilterCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean Implements ICompletionProvider.IsFilterCharacter
            Return False
        End Function

        Public Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean Implements ICompletionProvider.IsTriggerCharacter
            Return False
        End Function

        Public Function SendEnterThroughToEditor(completionItem As CompletionItem, textTypedSoFar As String) As Boolean Implements ICompletionProvider.SendEnterThroughToEditor
            Return False
        End Function

        Public Async Function GetGroupAsync(document As Document, position As Integer, triggerInfo As CompletionTriggerInfo, Optional cancellationToken As CancellationToken = Nothing) As Task(Of CompletionItemGroup) Implements ICompletionProvider.GetGroupAsync
            If Not triggerInfo.IsAugment Then
                Return Nothing
            End If

            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            If triggerInfo.IsDebugger Then
                Return New CompletionItemGroup(
                    SpecializedCollections.EmptyEnumerable(Of CompletionItem)(),
                    New CompletionItem(Me, "", CompletionUtilities.GetTextChangeSpan(text, position), isBuilder:=True))
            End If

            Dim span = New TextSpan(position, 0)
            Dim semanticModel = Await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(False)
            Dim syntaxTree = semanticModel.SyntaxTree

            ' If we're option explicit off, then basically any expression context can have a
            ' builder, since it might be an implicit local declaration.
            Dim targetToken = syntaxTree.GetTargetToken(position, cancellationToken)
            If semanticModel.OptionExplicit = False AndAlso (syntaxTree.IsExpressionContext(position, targetToken, cancellationToken) OrElse syntaxTree.IsSingleLineStatementContext(position, targetToken, cancellationToken)) Then
                Return GetBuilderCompletionGroup(VBFeaturesResources.EmptyString1, VBFeaturesResources.EmptyString1, text, position)
            End If

            ' Builder if we're typing a field
            Dim description = VBFeaturesResources.TypeANameHereToDeclareA & vbCrLf &
                VBFeaturesResources.NoteSpaceCompletionIsDisa
            If syntaxTree.IsFieldNameDeclarationContext(position, targetToken, cancellationToken) Then
                Return GetBuilderCompletionGroup(VBFeaturesResources.NewField, description, text, position)
            End If

            If targetToken.Kind = SyntaxKind.None OrElse targetToken.FollowsEndOfStatement(position) Then
                Return Nothing
            End If

            ' Builder if we're typing a parameter
            If syntaxTree.IsParameterNameDeclarationContext(position, cancellationToken) Then
                ' Don't provide a builder if only the "Optional" keyword is recommended --
                ' it's mandatory in that case!
                Dim methodDeclaration = targetToken.GetAncestor(Of MethodBaseSyntax)()
                If methodDeclaration IsNot Nothing Then
                    If targetToken.Kind = SyntaxKind.CommaToken AndAlso targetToken.Parent.Kind = SyntaxKind.ParameterList Then
                        For Each parameter In methodDeclaration.ParameterList.Parameters.Where(Function(p) p.FullSpan.End < position)
                            ' A previous parameter was Optional, so the suggested Optional is an offer they can't refuse. No builder.
                            If parameter.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.OptionalKeyword) Then
                                Return Nothing
                            End If
                        Next
                    End If
                End If

                description = VBFeaturesResources.TypeANameHereToDeclareA0 & vbCrLf &
                              VBFeaturesResources.NoteSpaceCompletionIsDisa

                ' Otherwise just return a builder. It won't show up unless other modifiers are
                ' recommended, which is what we want.
                Return GetBuilderCompletionGroup(VBFeaturesResources.ParameterName, description, text, position)
            End If

            ' Builder in select clause: after Select, after comma
            If targetToken.Parent.Kind = SyntaxKind.SelectClause Then
                If targetToken.IsKind(SyntaxKind.SelectKeyword, SyntaxKind.CommaToken) Then
                    description = VBFeaturesResources.TypeANewNameForTheColumn & vbCrLf &
                                  VBFeaturesResources.NoteUseTabForAutomaticCo
                    Return GetBuilderCompletionGroup(VBFeaturesResources.ResultAlias, description, text, position)
                End If
            End If

            ' Build after For
            If targetToken.IsKindOrHasMatchingText(SyntaxKind.ForKeyword) AndAlso
               targetToken.Parent.IsKind(SyntaxKind.ForStatement) Then

                description = VBFeaturesResources.TypeANewVariableName & vbCrLf &
                        VBFeaturesResources.NoteSpaceAndCompletion

                Return GetBuilderCompletionGroup(VBFeaturesResources.NewVariable, description, text, position)
            End If

            ' Build after Using
            If targetToken.IsKindOrHasMatchingText(SyntaxKind.UsingKeyword) AndAlso
               targetToken.Parent.IsKind(SyntaxKind.UsingStatement) Then

                description = VBFeaturesResources.TypeANewVariableName & vbCrLf &
                        VBFeaturesResources.NoteSpaceAndCompletion

                Return GetBuilderCompletionGroup(VBFeaturesResources.NewResource, description, text, position)
            End If

            Return Nothing
        End Function

        Private Function GetBuilderCompletionGroup(itemText As String, description As String, text As SourceText, position As Integer) As CompletionItemGroup
            Return New CompletionItemGroup(
                SpecializedCollections.EmptyEnumerable(Of CompletionItem)(),
                New CompletionItem(
                    Me, itemText,
                    CompletionUtilities.GetTextChangeSpan(text, position),
                    description.ToSymbolDisplayParts,
                    isBuilder:=True))
        End Function
    End Class
End Namespace
