' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    Partial Friend Class InvocationExpressionSignatureHelpProvider

        Private Shared Function GetDelegateInvokeItems(invocationExpression As InvocationExpressionSyntax,
                                                semanticModel As SemanticModel,
                                                structuralTypeDisplayService As IStructuralTypeDisplayService,
                                                documentationCommentFormattingService As IDocumentationCommentFormattingService,
                                                delegateType As INamedTypeSymbol,
                                                cancellationToken As CancellationToken) As IEnumerable(Of SignatureHelpItem)
            Dim invokeMethod = delegateType.DelegateInvokeMethod
            If invokeMethod Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of SignatureHelpItem)()
            End If

            Dim position = invocationExpression.SpanStart
            Dim item = CreateItem(
                invokeMethod, semanticModel, position,
                structuralTypeDisplayService,
                isVariadic:=invokeMethod.IsParams(),
                documentationFactory:=Nothing,
                prefixParts:=GetDelegateInvokePreambleParts(invokeMethod, semanticModel, position),
                separatorParts:=GetSeparatorParts(),
                suffixParts:=GetDelegateInvokePostambleParts(invokeMethod, semanticModel, position),
                parameters:=GetDelegateInvokeParameters(invokeMethod, semanticModel, position, documentationCommentFormattingService, cancellationToken))
            Return SpecializedCollections.SingletonEnumerable(item)
        End Function

        Private Shared Function GetDelegateInvokePreambleParts(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
            Dim displayParts = New List(Of SymbolDisplayPart)()

            If invokeMethod.ContainingType.IsAnonymousType Then
                displayParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.MethodName, invokeMethod, invokeMethod.Name))
            Else
                displayParts.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            End If

            displayParts.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return displayParts
        End Function

        Private Shared Function GetDelegateInvokeParameters(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer, documentationCommentFormattingService As IDocumentationCommentFormattingService, cancellationToken As CancellationToken) As IList(Of SignatureHelpSymbolParameter)
            Dim parameters = New List(Of SignatureHelpSymbolParameter)
            For Each parameter In invokeMethod.Parameters
                cancellationToken.ThrowIfCancellationRequested()
                parameters.Add(New SignatureHelpSymbolParameter(
                    parameter.Name,
                    isOptional:=False,
                    documentationFactory:=parameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                    displayParts:=parameter.ToMinimalDisplayParts(semanticModel, position)))
            Next

            Return parameters
        End Function

        Private Shared Function GetDelegateInvokePostambleParts(invokeMethod As IMethodSymbol,
                                                         semanticModel As SemanticModel,
                                                         position As Integer) As IList(Of SymbolDisplayPart)
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
