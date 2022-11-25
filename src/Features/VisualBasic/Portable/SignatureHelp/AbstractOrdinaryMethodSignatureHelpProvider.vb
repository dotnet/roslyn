' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.SignatureHelp

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    friend MustInherit class AbstractOrdinaryMethodSignatureHelpProvider
        inherits AbstractVisualBasicSignatureHelpProvider

        Protected Shared Function ConvertMemberGroupMember(document As Document,
                                                    member As ISymbol,
                                                    position As Integer,
                                                    semanticModel As SemanticModel) As SignatureHelpItem

            Dim structuralTypeDisplayService = document.GetLanguageService(Of IStructuralTypeDisplayService)()
            Dim documentationCommentFormattingService = document.GetLanguageService(Of IDocumentationCommentFormattingService)()

            Return CreateItem(
                member, semanticModel, position,
                structuralTypeDisplayService,
                member.IsParams(),
                Function(c) member.GetDocumentationParts(semanticModel, position, documentationCommentFormattingService, c),
                GetMemberGroupPreambleParts(member, semanticModel, position),
                GetSeparatorParts(),
                GetMemberGroupPostambleParts(member, semanticModel, position),
                member.GetParameters().Select(Function(p) Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList())
        End Function

        Private Shared Function GetMemberGroupPreambleParts(symbol As ISymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()

            AddExtensionPreamble(symbol, result)

            result.AddRange(symbol.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            result.Add(Punctuation(SyntaxKind.DotToken))

            Dim format = MinimallyQualifiedWithoutParametersFormat
            format = format.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeContainingType)
            format = format.RemoveKindOptions(SymbolDisplayKindOptions.IncludeMemberKeyword)
            format = format.WithMiscellaneousOptions(format.MiscellaneousOptions And (Not SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers))

            result.AddRange(symbol.ToMinimalDisplayParts(semanticModel, position, format))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return result
        End Function

        Private Shared Function GetMemberGroupPostambleParts(symbol As ISymbol,
                                                      semanticModel As SemanticModel,
                                                      position As Integer) As IList(Of SymbolDisplayPart)
            Dim parts = New List(Of SymbolDisplayPart)
            parts.Add(Punctuation(SyntaxKind.CloseParenToken))

            If TypeOf symbol Is IMethodSymbol Then
                Dim method = DirectCast(symbol, IMethodSymbol)
                If Not method.ReturnsVoid Then
                    parts.Add(Space())
                    parts.Add(Keyword(SyntaxKind.AsKeyword))
                    parts.Add(Space())
                    parts.AddRange(method.ReturnType.ToMinimalDisplayParts(semanticModel, position))
                End If
            ElseIf TypeOf symbol Is IPropertySymbol Then
                Dim [property] = DirectCast(symbol, IPropertySymbol)

                parts.Add(Space())
                parts.Add(Keyword(SyntaxKind.AsKeyword))
                parts.Add(Space())
                parts.AddRange([property].Type.ToMinimalDisplayParts(semanticModel, position))
            End If

            Return parts
        End Function
    End Class
End Namespace
