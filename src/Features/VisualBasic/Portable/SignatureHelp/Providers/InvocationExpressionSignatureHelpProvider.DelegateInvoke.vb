' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp.Providers
    Partial Friend Class InvocationExpressionSignatureHelpProvider

        Private Function GetDelegateInvokeItems(invocationExpression As InvocationExpressionSyntax,
                                                semanticModel As SemanticModel,
                                                symbolDisplayService As ISymbolDisplayService,
                                                anonymousTypeDisplayService As IAnonymousTypeDisplayService,
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
                prefixParts:=GetDelegateInvokePreambleParts(invokeMethod, semanticModel, position),
                separatorParts:=GetSeparatorParts(),
                suffixParts:=GetDelegateInvokePostambleParts(invokeMethod, semanticModel, position),
                parameters:=GetDelegateInvokeParameters(invokeMethod, semanticModel, position, cancellationToken))
            item = item.WithSymbol(Nothing) ' forces documentation to be empty
            Return SpecializedCollections.SingletonEnumerable(item)
        End Function

        Private Function GetDelegateInvokePreambleParts(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
            Dim displayParts = New List(Of SymbolDisplayPart)()

            If invokeMethod.ContainingType.IsAnonymousType Then
                displayParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.MethodName, invokeMethod, invokeMethod.Name))
            Else
                displayParts.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            End If

            displayParts.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return displayParts
        End Function

        Private Function GetDelegateInvokeParameters(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As IList(Of CommonParameterData)
            Dim parameters = New List(Of CommonParameterData)
            For Each parameter In invokeMethod.Parameters
                cancellationToken.ThrowIfCancellationRequested()
                parameters.Add(New CommonParameterData(
                    parameter.Name,
                    isOptional:=False,
                    symbol:=parameter,
                    position:=position,
                    displayParts:=parameter.ToMinimalDisplayParts(semanticModel, position)))
            Next

            Return parameters
        End Function

        Private Function GetDelegateInvokePostambleParts(invokeMethod As IMethodSymbol,
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