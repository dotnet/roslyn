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
    Partial Friend Class PartialTypeCompletionProvider
        Inherits AbstractPartialTypeCompletionProvider

        Private Const InsertionTextOnOpenParen As String = NameOf(InsertionTextOnOpenParen)

        Private Shared ReadOnly _insertionTextFormatWithGenerics As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                miscellaneousOptions:=
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
                genericsOptions:=
                    SymbolDisplayGenericsOptions.IncludeTypeParameters Or
                    SymbolDisplayGenericsOptions.IncludeVariance Or
                    SymbolDisplayGenericsOptions.IncludeTypeConstraints)

        Private Shared ReadOnly _displayTextFormat As SymbolDisplayFormat =
            _insertionTextFormatWithGenerics.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Protected Overrides Function GetPartialTypeSyntaxNode(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxNode
            Dim statement As TypeStatementSyntax = Nothing
            Return If(tree.IsPartialTypeDeclarationNameContext(position, cancellationToken, statement), statement, Nothing)
        End Function

        Protected Overrides Async Function CreateSyntaxContextAsync(document As Document, semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function GetDisplayAndSuffixAndInsertionText(symbol As INamedTypeSymbol, context As SyntaxContext) As (displayText As String, suffix As String, insertionText As String)
            Dim displayText = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, format:=_displayTextFormat)
            Dim insertionText = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, format:=_insertionTextFormatWithGenerics)
            Return (displayText, "", insertionText)
        End Function

        Protected Overrides Function GetProperties(symbol As INamedTypeSymbol,
                                                   context As SyntaxContext) As ImmutableDictionary(Of String, String)
            Return ImmutableDictionary(Of String, String).Empty.Add(
                InsertionTextOnOpenParen, symbol.Name.EscapeIdentifier())
        End Function

        Public Overrides Async Function GetTextChangeAsync(document As Document, selectedItem As CompletionItem, ch As Char?, cancellationToken As CancellationToken) As Task(Of TextChange?)
            If ch = "("c Then
                Dim insertionText As String = Nothing
                If selectedItem.Properties.TryGetValue(InsertionTextOnOpenParen, insertionText) Then
                    Return New TextChange(selectedItem.Span, insertionText)
                End If
            End If

            Return Await MyBase.GetTextChangeAsync(document, selectedItem, ch, cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
