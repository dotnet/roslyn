' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    Partial Friend Class ObjectCreationExpressionSignatureHelpProvider

        Private Shared Function GetNormalTypeConstructors(
            document As Document,
            objectCreationExpression As ObjectCreationExpressionSyntax,
            semanticModel As SemanticModel,
            structuralTypeDisplayService As IStructuralTypeDisplayService,
            normalType As INamedTypeSymbol,
            within As ISymbol,
            options As MemberDisplayOptions, cancellationToken As CancellationToken) As (items As IList(Of SignatureHelpItem), selectedItem As Integer?)

            Dim accessibleConstructors = normalType.InstanceConstructors.
                                                    WhereAsArray(Function(c) c.IsAccessibleWithin(within)).
                                                    FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation).
                                                    Sort(semanticModel, objectCreationExpression.SpanStart)

            If Not accessibleConstructors.Any() Then
                Return Nothing
            End If

            Dim documentationCommentFormattingService = document.GetLanguageService(Of IDocumentationCommentFormattingService)()

            Dim items = accessibleConstructors.Select(
                Function(c) ConvertNormalTypeConstructor(c, objectCreationExpression, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList()

            Dim currentConstructor = semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken)
            Dim selectedItem = TryGetSelectedIndex(accessibleConstructors, currentConstructor.Symbol)

            Return (items, selectedItem)
        End Function

        Private Shared Function ConvertNormalTypeConstructor(constructor As IMethodSymbol, objectCreationExpression As ObjectCreationExpressionSyntax, semanticModel As SemanticModel,
                                                      structuralTypeDisplayService As IStructuralTypeDisplayService,
                                                      documentationCommentFormattingService As IDocumentationCommentFormattingService) As SignatureHelpItem
            Dim position = objectCreationExpression.SpanStart
            Dim item = CreateItem(
                constructor, semanticModel, position,
                structuralTypeDisplayService,
                constructor.IsParams(),
                constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetNormalTypePreambleParts(constructor, semanticModel, position), GetSeparatorParts(),
                GetNormalTypePostambleParts(),
                constructor.Parameters.Select(Function(p) Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList())
            Return item
        End Function

        Private Shared Function GetNormalTypePreambleParts(method As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()
            result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return result
        End Function

        Private Shared Function GetNormalTypePostambleParts() As IList(Of SymbolDisplayPart)
            Return {Punctuation(SyntaxKind.CloseParenToken)}
        End Function
    End Class
End Namespace
