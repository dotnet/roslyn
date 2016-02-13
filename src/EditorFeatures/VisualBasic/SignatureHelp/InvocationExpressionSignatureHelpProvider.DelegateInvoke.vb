' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

    Partial Friend Class InvocationExpressionSignatureHelpProvider

        Private Function GetDelegateInvokeItems(invocationExpression As InvocationExpressionSyntax,
                                                semanticModel As SemanticModel,
                                                symbolDisplayService As ISymbolDisplayService,
                                                anonymousTypeDisplayService As IAnonymousTypeDisplayService,
                                                documentationCommentFormattingService As IDocumentationCommentFormattingService,
                                                within As ISymbol,
                                                delegateType As INamedTypeSymbol,
                                                cancellationToken As CancellationToken) As IEnumerable(Of SignatureHelpItem)
            Dim invokeMethod = delegateType.DelegateInvokeMethod
            If invokeMethod Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of SignatureHelpItem)()
            End If

            Dim position = invocationExpression.SpanStart
            Dim item = CreateItem(
                invokeMethod, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                isVariadic:=invokeMethod.IsParams(),
                documentationFactory:=Nothing,
                prefixParts:=GetDelegateInvokePreambleParts(invokeMethod, semanticModel, position),
                separatorParts:=GetSeparatorParts(),
                suffixParts:=GetDelegateInvokePostambleParts(invokeMethod, semanticModel, position),
                parameters:=GetDelegateInvokeParameters(invokeMethod, semanticModel, position, documentationCommentFormattingService, cancellationToken))
            Return SpecializedCollections.SingletonEnumerable(item)
        End Function

        Private Function GetDelegateInvokePreambleParts(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IEnumerable(Of SymbolDisplayPart)
            Dim displayParts = New List(Of SymbolDisplayPart)()

            If invokeMethod.ContainingType.IsAnonymousType Then
                displayParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.MethodName, invokeMethod, invokeMethod.Name))
            Else
                displayParts.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            End If

            displayParts.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return displayParts
        End Function

        Private Function GetDelegateInvokeParameters(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer, documentationCommentFormattingService As IDocumentationCommentFormattingService, cancellationToken As CancellationToken) As IEnumerable(Of SignatureHelpParameter)
            Dim parameters = New List(Of SignatureHelpParameter)
            For Each parameter In invokeMethod.Parameters
                cancellationToken.ThrowIfCancellationRequested()
                parameters.Add(New SignatureHelpParameter(
                    parameter.Name,
                    isOptional:=False,
                    documentationFactory:=parameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                    displayParts:=parameter.ToMinimalDisplayParts(semanticModel, position)))
            Next

            Return parameters
        End Function

        Private Function GetDelegateInvokePostambleParts(invokeMethod As IMethodSymbol,
                                                         semanticModel As SemanticModel,
                                                         position As Integer) As IEnumerable(Of SymbolDisplayPart)
            Dim parts = New List(Of SymbolDisplayPart)

            parts.Add(Punctuation(SyntaxKind.CloseParenToken))

            If Not invokeMethod.ReturnsVoid Then
                parts.Add(Space())
                parts.Add(Keyword(SyntaxKind.AsKeyword))
                parts.Add(Space())
                parts.AddRange(invokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position))
            End If

            Return parts
        End Function
    End Class
End Namespace

