' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.SignatureHelp

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    Friend MustInherit Class AbstractOrdinaryMethodSignatureHelpProvider
        Inherits AbstractVisualBasicSignatureHelpProvider

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
                member.GetParameters().SelectAsArray(Function(p) Convert(p, semanticModel, position, documentationCommentFormattingService)))
        End Function

        Private Shared Function GetMemberGroupPreambleParts(symbol As ISymbol, semanticModel As SemanticModel, position As Integer) As ImmutableArray(Of SymbolDisplayPart)
            Dim result = ArrayBuilder(Of SymbolDisplayPart).GetInstance()

            AddExtensionPreamble(symbol, result)

            result.AddRange(symbol.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            result.Add(Punctuation(SyntaxKind.DotToken))

            Dim format = MinimallyQualifiedWithoutParametersFormat
            format = format.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeContainingType)
            format = format.RemoveKindOptions(SymbolDisplayKindOptions.IncludeMemberKeyword)
            format = format.WithMiscellaneousOptions(format.MiscellaneousOptions And (Not SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers))

            result.AddRange(symbol.ToMinimalDisplayParts(semanticModel, position, format))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return result.ToImmutableAndFree()
        End Function

        Private Shared Function GetMemberGroupPostambleParts(
                symbol As ISymbol,
                semanticModel As SemanticModel,
                position As Integer) As ImmutableArray(Of SymbolDisplayPart)
            Dim parts = ArrayBuilder(Of SymbolDisplayPart).GetInstance()
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

            Return parts.ToImmutableAndFree()
        End Function
    End Class
End Namespace
