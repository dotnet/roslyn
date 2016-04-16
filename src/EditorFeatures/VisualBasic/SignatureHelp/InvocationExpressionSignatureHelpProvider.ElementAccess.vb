' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

    Partial Friend Class InvocationExpressionSignatureHelpProvider

        Private Function GetElementAccessItems(leftExpression As ExpressionSyntax,
                                               semanticModel As SemanticModel,
                                               symbolDisplayService As ISymbolDisplayService,
                                               anonymousTypeDisplayService As IAnonymousTypeDisplayService,
                                               documentationCommentFormattingService As IDocumentationCommentFormattingService,
                                               within As ISymbol,
                                               defaultProperties As IList(Of IPropertySymbol),
                                               cancellationToken As CancellationToken) As IEnumerable(Of SignatureHelpItem)
            Dim throughType As ITypeSymbol = Nothing
            If leftExpression IsNot Nothing Then
                throughType = semanticModel.GetTypeInfo(leftExpression, cancellationToken).Type
            End If

            Dim accessibleDefaultProperties = defaultProperties.Where(Function(m) m.IsAccessibleWithin(within, throughTypeOpt:=throughType)).ToList()
            If accessibleDefaultProperties.Count = 0 Then
                Return SpecializedCollections.EmptyEnumerable(Of SignatureHelpItem)()
            End If

            Return accessibleDefaultProperties.Select(
                Function(s) ConvertIndexer(s, leftExpression.SpanStart, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken))
        End Function

        Private Function ConvertIndexer(indexer As IPropertySymbol,
                                        position As Integer,
                                        semanticModel As SemanticModel,
                                        symbolDisplayService As ISymbolDisplayService,
                                        anonymousTypeDisplayService As IAnonymousTypeDisplayService,
                                        documentationCommentFormattingService As IDocumentationCommentFormattingService,
                                        cancellationToken As CancellationToken) As SignatureHelpItem
            Dim item = CreateItem(
                indexer, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                indexer.IsParams(),
                indexer.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetIndexerPreambleParts(indexer, semanticModel, position),
                GetSeparatorParts(),
                GetIndexerPostambleParts(indexer, semanticModel, position),
                indexer.Parameters.[Select](Function(p) Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)))
            Return item
        End Function

        Private Function GetIndexerPreambleParts(symbol As IPropertySymbol, semanticModel As SemanticModel, position As Integer) As IEnumerable(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()
            result.AddRange(symbol.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return result
        End Function

        Private Function GetIndexerPostambleParts(symbol As IPropertySymbol,
                                                  semanticModel As SemanticModel,
                                                  position As Integer) As IEnumerable(Of SymbolDisplayPart)
            Dim parts = New List(Of SymbolDisplayPart)
            parts.Add(Punctuation(SyntaxKind.CloseParenToken))

            Dim [property] = DirectCast(symbol, IPropertySymbol)

            parts.Add(Space())
            parts.Add(Keyword(SyntaxKind.AsKeyword))
            parts.Add(Space())
            parts.AddRange([property].Type.ToMinimalDisplayParts(semanticModel, position))

            Return parts
        End Function

    End Class
End Namespace
