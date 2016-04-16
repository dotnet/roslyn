' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

    Partial Friend Class ObjectCreationExpressionSignatureHelpProvider

        Private Function GetDelegateTypeConstructors(objectCreationExpression As ObjectCreationExpressionSyntax,
                                                     semanticModel As SemanticModel,
                                                     symbolDisplayService As ISymbolDisplayService,
                                                     anonymousTypeDisplayService As IAnonymousTypeDisplayService,
                                                     documentationCommentFormattingService As IDocumentationCommentFormattingService,
                                                     delegateType As INamedTypeSymbol,
                                                     within As ISymbol,
                                                     cancellationToken As CancellationToken) As IEnumerable(Of SignatureHelpItem)
            Dim invokeMethod = delegateType.DelegateInvokeMethod
            If invokeMethod Is Nothing Then
                Return Nothing
            End If

            Dim position = objectCreationExpression.SpanStart
            Dim item = CreateItem(
                invokeMethod, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                isVariadic:=False,
                documentationFactory:=invokeMethod.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                prefixParts:=GetDelegateTypePreambleParts(invokeMethod, semanticModel, position), separatorParts:=GetSeparatorParts(), suffixParts:=GetDelegateTypePostambleParts(invokeMethod), parameters:=GetDelegateTypeParameters(invokeMethod, semanticModel, position, cancellationToken))
            Return SpecializedCollections.SingletonEnumerable(item)
        End Function

        Private Function GetDelegateTypePreambleParts(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IEnumerable(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()
            result.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return result
        End Function

        Private Function GetDelegateTypeParameters(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As IEnumerable(Of SignatureHelpParameter)
            Const TargetName As String = "target"

            Dim parts = New List(Of SymbolDisplayPart)()

            If invokeMethod.ReturnsVoid Then
                parts.Add(Keyword(SyntaxKind.SubKeyword))
            Else
                parts.Add(Keyword(SyntaxKind.FunctionKeyword))
            End If

            parts.Add(Space())
            parts.Add(Punctuation(SyntaxKind.OpenParenToken))

            Dim first = True
            For Each parameter In invokeMethod.Parameters
                If Not first Then
                    parts.Add(Punctuation(SyntaxKind.CommaToken))
                    parts.Add(Space())
                End If

                first = False
                parts.AddRange(parameter.Type.ToMinimalDisplayParts(semanticModel, position))
            Next

            parts.Add(Punctuation(SyntaxKind.CloseParenToken))

            If Not invokeMethod.ReturnsVoid Then
                parts.Add(Space())
                parts.Add(Keyword(SyntaxKind.AsKeyword))
                parts.Add(Space())
                parts.AddRange(invokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position))
            End If

            Return {New SignatureHelpParameter(
                TargetName,
                isOptional:=False,
                documentationFactory:=Nothing,
                displayParts:=parts)}
        End Function

        Private Function GetDelegateTypePostambleParts(invokeMethod As IMethodSymbol) As IEnumerable(Of SymbolDisplayPart)
            Return {Punctuation(SyntaxKind.CloseParenToken)}
        End Function
    End Class
End Namespace

