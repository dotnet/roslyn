' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    <ExportSignatureHelpProvider("InvocationExpressionSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class InvocationExpressionSignatureHelpProvider
        Inherits AbstractVisualBasicSignatureHelpProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Public Overrides Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim expression As InvocationExpressionSyntax = Nothing
            If TryGetInvocationExpression(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, expression) AndAlso
                currentSpan.Start = SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start Then

                Return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position)
            End If

            Return Nothing
        End Function

        Private Function TryGetInvocationExpression(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerReason, cancellationToken As CancellationToken, ByRef expression As InvocationExpressionSyntax) As Boolean
            If Not CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, AddressOf IsTriggerToken, AddressOf IsArgumentListToken, cancellationToken, expression) Then
                Return False
            End If

            Return expression.ArgumentList IsNot Nothing
        End Function

        Private Shared Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return (token.Kind = SyntaxKind.OpenParenToken OrElse token.Kind = SyntaxKind.CommaToken) AndAlso
                    TypeOf token.Parent Is ArgumentListSyntax AndAlso
                    TypeOf token.Parent.Parent Is InvocationExpressionSyntax
        End Function

        Private Shared Function IsArgumentListToken(node As InvocationExpressionSyntax, token As SyntaxToken) As Boolean
            Return node.ArgumentList IsNot Nothing AndAlso
                node.ArgumentList.Span.Contains(token.SpanStart) AndAlso
                token <> node.ArgumentList.CloseParenToken
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim invocationExpression As InvocationExpressionSyntax = Nothing
            If Not TryGetInvocationExpression(root, position, document.GetLanguageService(Of ISyntaxFactsService), triggerInfo.TriggerReason, cancellationToken, invocationExpression) Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetSemanticModelForNodeAsync(invocationExpression, cancellationToken).ConfigureAwait(False)
            Dim within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken)
            If within Is Nothing Then
                Return Nothing
            End If

            Dim targetExpression = If(invocationExpression.Expression Is Nothing AndAlso invocationExpression.Parent.IsKind(SyntaxKind.ConditionalAccessExpression),
                DirectCast(invocationExpression.Parent, ConditionalAccessExpressionSyntax).Expression,
                invocationExpression.Expression)

            ' get the regular signature help items
            Dim symbolDisplayService = document.GetLanguageService(Of ISymbolDisplayService)()
            Dim memberGroup = semanticModel.GetMemberGroup(targetExpression, cancellationToken).
                                            FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation)

            ' try to bind to the actual method
            Dim symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken)
            Dim matchedMethodSymbol = TryCast(symbolInfo.Symbol, IMethodSymbol)

            ' if the symbol could be bound, replace that item in the symbol list
            If matchedMethodSymbol IsNot Nothing AndAlso matchedMethodSymbol.IsGenericMethod Then
                memberGroup = memberGroup.SelectAsArray(Function(m) If(Equals(matchedMethodSymbol.OriginalDefinition, m), matchedMethodSymbol, m))
            End If

            Dim enclosingSymbol = semanticModel.GetEnclosingSymbol(position)
            If enclosingSymbol.IsConstructor() Then
                memberGroup = memberGroup.WhereAsArray(Function(m) Not m.Equals(enclosingSymbol))
            End If

            memberGroup = memberGroup.Sort(symbolDisplayService, semanticModel, invocationExpression.SpanStart)

            Dim typeInfo = semanticModel.GetTypeInfo(targetExpression, cancellationToken)
            Dim expressionType = If(typeInfo.Type, typeInfo.ConvertedType)
            Dim defaultProperties =
                If(expressionType Is Nothing,
                   SpecializedCollections.EmptyList(Of IPropertySymbol),
                   semanticModel.LookupSymbols(position, expressionType, includeReducedExtensionMethods:=True).
                                 OfType(Of IPropertySymbol).
                                 ToImmutableArrayOrEmpty().
                                 WhereAsArray(Function(p) p.IsIndexer).
                                 FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation).
                                 Sort(symbolDisplayService, semanticModel, invocationExpression.SpanStart))

            Dim anonymousTypeDisplayService = document.GetLanguageService(Of IAnonymousTypeDisplayService)()
            Dim documentationCommentFormattingService = document.GetLanguageService(Of IDocumentationCommentFormattingService)()

            Dim items = New List(Of SignatureHelpItem)
            If memberGroup.Length > 0 Then
                items.AddRange(GetMemberGroupItems(invocationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, within, memberGroup, cancellationToken))
            End If

            If expressionType.IsDelegateType() Then
                items.AddRange(GetDelegateInvokeItems(invocationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, within, DirectCast(expressionType, INamedTypeSymbol), cancellationToken))
            End If

            If defaultProperties.Count > 0 Then
                items.AddRange(GetElementAccessItems(targetExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, within, defaultProperties, cancellationToken))
            End If

            Dim textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            Dim selectedItem = TryGetSelectedIndex(memberGroup, symbolInfo)
            Return CreateSignatureHelpItems(items, textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem)
        End Function
    End Class
End Namespace
