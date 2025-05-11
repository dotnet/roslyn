' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    <ExportSignatureHelpProvider("AttributeSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class AttributeSignatureHelpProvider
        Inherits AbstractVisualBasicSignatureHelpProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableArray(Of Char) = ImmutableArray.Create("("c, ","c)

        Public Overrides ReadOnly Property RetriggerCharacters As ImmutableArray(Of Char) = ImmutableArray.Create(")"c)

        Private Function TryGetAttributeExpression(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerReason, cancellationToken As CancellationToken, ByRef attribute As AttributeSyntax) As Boolean
            If Not CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, AddressOf IsTriggerToken, AddressOf IsArgumentListToken, cancellationToken, attribute) Then
                Return False
            End If

            Return attribute.ArgumentList IsNot Nothing
        End Function

        Private Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) AndAlso
                TypeOf token.Parent Is ArgumentListSyntax AndAlso
                TypeOf token.Parent.Parent Is AttributeSyntax
        End Function

        Private Shared Function IsArgumentListToken(node As AttributeSyntax, token As SyntaxToken) As Boolean
            Return node.ArgumentList IsNot Nothing AndAlso
                node.ArgumentList.Span.Contains(token.SpanStart) AndAlso
                token <> node.ArgumentList.CloseParenToken
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, options As MemberDisplayOptions, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim attribute As AttributeSyntax = Nothing
            If Not TryGetAttributeExpression(root, position, document.GetLanguageService(Of ISyntaxFactsService), triggerInfo.TriggerReason, cancellationToken, attribute) Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim attributeType = TryCast(semanticModel.GetTypeInfo(attribute, cancellationToken).Type, INamedTypeSymbol)
            If attributeType Is Nothing Then
                Return Nothing
            End If

            Dim within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken)
            If within Is Nothing Then
                Return Nothing
            End If

            Dim accessibleConstructors = attributeType.InstanceConstructors.
                                                       WhereAsArray(Function(c) c.IsAccessibleWithin(within)).
                                                       FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation).
                                                       Sort(semanticModel, attribute.SpanStart)

            If Not accessibleConstructors.Any() Then
                Return Nothing
            End If

            Dim structuralTypeDisplayService = document.GetLanguageService(Of IStructuralTypeDisplayService)()
            Dim documentationCommentFormattingService = document.GetLanguageService(Of IDocumentationCommentFormattingService)()
            Dim textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(attribute.ArgumentList)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            Dim symbolInfo = semanticModel.GetSymbolInfo(attribute, cancellationToken)
            Dim selectedItem = TryGetSelectedIndex(accessibleConstructors, symbolInfo.Symbol)

            Return CreateSignatureHelpItems(accessibleConstructors.Select(
                Function(c) Convert(c, within, attribute, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList(),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem, parameterIndexOverride:=-1)
        End Function

        Private Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState?
            Dim expression As AttributeSyntax = Nothing
            If TryGetAttributeExpression(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, expression) AndAlso
                currentSpan.Start = SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start Then

                Return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position)
            End If

            Return Nothing
        End Function

        Private Overloads Shared Function Convert(constructor As IMethodSymbol,
                                           within As ISymbol,
                                           attribute As AttributeSyntax,
                                           semanticModel As SemanticModel,
                                           structuralTypeDisplayService As IStructuralTypeDisplayService,
                                           documentationCommentFormattingService As IDocumentationCommentFormattingService) As SignatureHelpItem
            Dim position = attribute.SpanStart
            Dim namedParameters = constructor.ContainingType.GetAttributeNamedParameters(semanticModel.Compilation, within).
                                                             OrderBy(Function(s) s.Name).
                                                             ToList()

            Dim isVariadic =
                constructor.Parameters.Length > 0 AndAlso constructor.Parameters.Last().IsParams AndAlso namedParameters.Count = 0

            Dim item = CreateItem(
                constructor, semanticModel, position,
                structuralTypeDisplayService,
                isVariadic,
                constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(constructor, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(),
                GetParameters(constructor, semanticModel, position, namedParameters, documentationCommentFormattingService))
            Return item
        End Function

        Private Shared Function GetParameters(constructor As IMethodSymbol,
                                       semanticModel As SemanticModel,
                                       position As Integer,
                                       namedParameters As List(Of ISymbol),
                                       documentationCommentFormattingService As IDocumentationCommentFormattingService) As IList(Of SignatureHelpSymbolParameter)
            Dim result = New List(Of SignatureHelpSymbolParameter)

            For Each parameter In constructor.Parameters
                result.Add(Convert(parameter, semanticModel, position, documentationCommentFormattingService))
            Next

            For i = 0 To namedParameters.Count - 1
                Dim namedParameter = namedParameters(i)

                Dim type = If(TypeOf namedParameter Is IFieldSymbol,
                               DirectCast(namedParameter, IFieldSymbol).Type,
                               DirectCast(namedParameter, IPropertySymbol).Type)

                Dim displayParts = New List(Of SymbolDisplayPart)

                displayParts.Add(New SymbolDisplayPart(
                    If(TypeOf namedParameter Is IFieldSymbol, SymbolDisplayPartKind.FieldName, SymbolDisplayPartKind.PropertyName),
                    namedParameter, namedParameter.Name.ToIdentifierToken.ToString()))
                displayParts.Add(Punctuation(SyntaxKind.ColonEqualsToken))
                displayParts.AddRange(type.ToMinimalDisplayParts(semanticModel, position))

                result.Add(New SignatureHelpSymbolParameter(
                    namedParameter.Name,
                    isOptional:=True,
                    documentationFactory:=namedParameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                    displayParts:=displayParts,
                    prefixDisplayParts:=GetParameterPrefixDisplayParts(i)))
            Next

            Return result
        End Function

        Private Shared Function GetParameterPrefixDisplayParts(i As Integer) As List(Of SymbolDisplayPart)
            If i = 0 Then
                Return New List(Of SymbolDisplayPart) From {
                    New SymbolDisplayPart(SymbolDisplayPartKind.Text, Nothing, FeaturesResources.Properties),
                    Punctuation(SyntaxKind.ColonToken),
                    Space()
                }
            End If

            Return Nothing
        End Function

        Private Shared Function GetPreambleParts(method As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()
            result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return result
        End Function

        Private Shared Function GetPostambleParts() As IList(Of SymbolDisplayPart)
            Return {Punctuation(SyntaxKind.CloseParenToken)}
        End Function
    End Class
End Namespace
