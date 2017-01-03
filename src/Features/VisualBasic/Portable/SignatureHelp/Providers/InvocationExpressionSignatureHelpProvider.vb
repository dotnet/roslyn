' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp.Providers
    Partial Friend Class InvocationExpressionSignatureHelpProvider
        Inherits AbstractVisualBasicSignatureHelpProvider

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Protected Overrides Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim expression As InvocationExpressionSyntax = Nothing
            If TryGetInvocationExpression(root, position, syntaxFacts, SignatureHelpTriggerKind.Other, cancellationToken, expression) AndAlso
                currentSpan.Start = SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start Then

                Return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position)
            End If

            Return Nothing
        End Function

        Private Function TryGetInvocationExpression(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerKind, cancellationToken As CancellationToken, ByRef expression As InvocationExpressionSyntax) As Boolean
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

        Protected Overrides Async Function ProvideSignaturesWorkerAsync(context As SignatureContext) As Task
            Dim document = context.Document
            Dim position = context.Position
            Dim trigger = context.Trigger
            Dim cancellationToken = context.CancellationToken

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim invocationExpression As InvocationExpressionSyntax = Nothing
            If Not TryGetInvocationExpression(root, position, document.GetLanguageService(Of ISyntaxFactsService), trigger.Kind, cancellationToken, invocationExpression) Then
                Return
            End If

            Dim semanticModel = Await document.GetSemanticModelForNodeAsync(invocationExpression, cancellationToken).ConfigureAwait(False)
            Dim within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken)
            If within Is Nothing Then
                Return
            End If

            Dim targetExpression = If(invocationExpression.Expression Is Nothing AndAlso invocationExpression.Parent.IsKind(SyntaxKind.ConditionalAccessExpression),
                DirectCast(invocationExpression.Parent, ConditionalAccessExpressionSyntax).Expression,
                invocationExpression.Expression)

            ' get the regular signature help items
            Dim symbolDisplayService = document.Project.LanguageServices.GetService(Of ISymbolDisplayService)()
            Dim memberGroup = semanticModel.GetMemberGroup(targetExpression, cancellationToken).
                                            FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation)

            ' try to bind to the actual method
            Dim symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken)
            Dim matchedMethodSymbol = TryCast(symbolInfo.Symbol, IMethodSymbol)

            ' if the symbol could be bound, replace that item in the symbol list
            If matchedMethodSymbol IsNot Nothing AndAlso matchedMethodSymbol.IsGenericMethod Then
                memberGroup = memberGroup.SelectAsArray(Function(m) If(matchedMethodSymbol.OriginalDefinition Is m, matchedMethodSymbol, m))
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

            Dim anonymousTypeDisplayService = document.Project.LanguageServices.GetService(Of IAnonymousTypeDisplayService)()

            If memberGroup.Count > 0 Then
                context.AddItems(GetMemberGroupItems(invocationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, within, memberGroup, cancellationToken))
            End If

            If expressionType.IsDelegateType() Then
                context.AddItems(GetDelegateInvokeItems(invocationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, within, DirectCast(expressionType, INamedTypeSymbol), cancellationToken))
            End If

            If defaultProperties.Count > 0 Then
                context.AddItems(GetElementAccessItems(targetExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, within, defaultProperties, cancellationToken))
            End If

            Dim textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            context.SetSpan(textSpan)
            context.SetState(GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken))
        End Function
    End Class
End Namespace
