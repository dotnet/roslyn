' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SignatureHelp

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    friend MustInherit class AbstractOrdinaryMethodSignatureHelpProvider
        inherits AbstractVisualBasicSignatureHelpProvider

        Protected Function ConvertMemberGroupMember(document As Document,
                                                    member As ISymbol,
                                                    position As Integer,
                                                    semanticModel As SemanticModel,
                                                    cancellationToken As CancellationToken) As SignatureHelpItem

            Dim anonymousTypeDisplayService = document.GetLanguageService(Of IAnonymousTypeDisplayService)()
            Dim documentationCommentFormattingService = document.GetLanguageService(Of IDocumentationCommentFormattingService)()
            Dim symbolDisplayService = document.GetLanguageService(Of ISymbolDisplayService)()

            Return CreateItem(
                member, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                member.IsParams(),
                Function(c) member.GetDocumentationParts(semanticModel, position, documentationCommentFormattingService, c).Concat(GetAwaitableDescription(member, semanticModel, position).ToTaggedText()),
                GetMemberGroupPreambleParts(member, semanticModel, position),
                GetSeparatorParts(),
                GetMemberGroupPostambleParts(member, semanticModel, position),
                member.GetParameters().Select(Function(p) Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)).ToList())
        End Function

        Private Function GetAwaitableDescription(member As ISymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
            If member.IsAwaitableNonDynamic(semanticModel, position) Then
                Return member.ToAwaitableParts(SyntaxFacts.GetText(SyntaxKind.AwaitKeyword), "r", semanticModel, position)
            End If

            Return SpecializedCollections.EmptyList(Of SymbolDisplayPart)
        End Function

        Private Function GetMemberGroupPreambleParts(symbol As ISymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
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

        Private Function GetMemberGroupPostambleParts(symbol As ISymbol,
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
