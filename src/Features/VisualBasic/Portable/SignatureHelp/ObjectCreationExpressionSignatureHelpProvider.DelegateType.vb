' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    Partial Friend Class ObjectCreationExpressionSignatureHelpProvider
        Private Shared Function GetDelegateTypeConstructors(
                objectCreationExpression As ObjectCreationExpressionSyntax,
                semanticModel As SemanticModel,
                structuralTypeDisplayService As IStructuralTypeDisplayService,
                documentationCommentFormattingService As IDocumentationCommentFormattingService,
                delegateType As INamedTypeSymbol) As (items As ImmutableArray(Of SignatureHelpItem), selectedItem As Integer?)
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

            Return (ImmutableArray.Create(item), 0)
        End Function

        Private Shared Function GetDelegateTypePreambleParts(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As ImmutableArray(Of SymbolDisplayPart)
            Dim result = ArrayBuilder(Of SymbolDisplayPart).GetInstance()
            result.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return result.ToImmutableAndFree()
        End Function

        Private Shared Function GetDelegateTypeParameters(invokeMethod As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As ImmutableArray(Of SignatureHelpSymbolParameter)
            Const TargetName As String = "target"

            Dim parts = ArrayBuilder(Of SymbolDisplayPart).GetInstance()

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

            Return ImmutableArray.Create(New SignatureHelpSymbolParameter(
                TargetName,
                isOptional:=False,
                documentationFactory:=Nothing,
                displayParts:=parts.ToImmutableAndFree()))
        End Function

        Private Shared Function GetDelegateTypePostambleParts() As ImmutableArray(Of SymbolDisplayPart)
            Return ImmutableArray.Create(Punctuation(SyntaxKind.CloseParenToken))
        End Function
    End Class
End Namespace

