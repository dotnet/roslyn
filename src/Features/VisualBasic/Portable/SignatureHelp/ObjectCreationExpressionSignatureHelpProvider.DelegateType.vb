' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    Partial Friend Class ObjectCreationExpressionSignatureHelpProvider

        Private Shared Function GetDelegateTypeConstructors(objectCreationExpression As ObjectCreationExpressionSyntax,
                                                     semanticModel As SemanticModel,
                                                     structuralTypeDisplayService As IStructuralTypeDisplayService,
                                                     documentationCommentFormattingService As IDocumentationCommentFormattingService,
                                                     delegateType As INamedTypeSymbol) As (items As IList(Of SignatureHelpItem), selectedItem As Integer?)
            Dim invokeMethod = delegateType.DelegateInvokeMethod
            If invokeMethod Is Nothing Then
                Return (Nothing, Nothing)
            End If

            Dim position = objectCreationExpression.SpanStart
            Dim item = CreateItem(
                invokeMethod, semanticModel, position,
                structuralTypeDisplayService,
                isVariadic:=False,
                documentationFactory:=invokeMethod.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                prefixParts:=GetDelegateTypePreambleParts(invokeMethod, semanticModel, position),
                separatorParts:=GetSeparatorParts(),
                suffixParts:=GetDelegateTypePostambleParts(),
                parameters:=GetDelegateTypeParameters(invokeMethod, semanticModel, position))

            Return (SpecializedCollections.SingletonList(item), 0)
        End Function

        Private Shared Function GetDelegateTypePreambleParts(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()
            result.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return result
        End Function

        Private Shared Function GetDelegateTypeParameters(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SignatureHelpSymbolParameter)
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

            Return {New SignatureHelpSymbolParameter(
                TargetName,
                isOptional:=False,
                documentationFactory:=Nothing,
                displayParts:=parts)}
        End Function

        Private Shared Function GetDelegateTypePostambleParts() As IList(Of SymbolDisplayPart)
            Return {Punctuation(SyntaxKind.CloseParenToken)}
        End Function
    End Class
End Namespace

