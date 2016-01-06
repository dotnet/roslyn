' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

    Partial Friend Class InvocationExpressionSignatureHelpProvider

        Private Function GetMemberGroupItems(invocationExpression As InvocationExpressionSyntax,
                                             semanticModel As SemanticModel,
                                             symbolDisplayService As ISymbolDisplayService,
                                             anonymousTypeDisplayService As IAnonymousTypeDisplayService,
                                             documentationCommentFormattingService As IDocumentationCommentFormattingService,
                                             within As ISymbol,
                                             memberGroup As IEnumerable(Of ISymbol),
                                             cancellationToken As CancellationToken) As IEnumerable(Of SignatureHelpItem)
            Dim throughType As ITypeSymbol = Nothing
            Dim expression = TryCast(invocationExpression.Expression, MemberAccessExpressionSyntax).GetExpressionOfMemberAccessExpression()

            ' if it is via a base expression "MyBase.", we know the "throughType" is the base class but
            ' we need to be able to tell between "New Base().M()" and "MyBase.M()".
            ' currently, Access check methods do not differentiate between them.
            ' so handle "MyBase." primary-expression here by nulling out "throughType"
            If expression IsNot Nothing AndAlso TypeOf expression IsNot MyBaseExpressionSyntax Then
                throughType = semanticModel.GetTypeInfo(expression, cancellationToken).Type
            End If

            If TypeOf invocationExpression.Expression Is SimpleNameSyntax AndAlso
               invocationExpression.IsInStaticContext() Then
                memberGroup = memberGroup.Where(Function(m) m.IsStatic)
            End If

            Dim accessibleMembers = memberGroup.Where(Function(m) m.IsAccessibleWithin(within, throughTypeOpt:=throughType)).ToList()
            If accessibleMembers.Count = 0 Then
                Return SpecializedCollections.EmptyEnumerable(Of SignatureHelpItem)()
            End If

            Return accessibleMembers.[Select](
                Function(s) ConvertMemberGroupMember(s, invocationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken))
        End Function

        Private Function ConvertMemberGroupMember(member As ISymbol,
                                                  invocationExpression As InvocationExpressionSyntax,
                                                  semanticModel As SemanticModel,
                                                  symbolDisplayService As ISymbolDisplayService,
                                                  anonymousTypeDisplayService As IAnonymousTypeDisplayService,
                                                  documentationCommentFormattingService As IDocumentationCommentFormattingService,
                                                  cancellationToken As CancellationToken) As SignatureHelpItem
            Dim position = invocationExpression.SpanStart
            Dim item = CreateItem(
                member, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                member.IsParams(),
                Function(c) member.GetDocumentationParts(semanticModel, position, documentationCommentFormattingService, c).Concat(GetAwaitableDescription(member, semanticModel, position)),
                GetMemberGroupPreambleParts(member, semanticModel, position),
                GetSeparatorParts(),
                GetMemberGroupPostambleParts(member, semanticModel, position),
                member.GetParameters().Select(Function(p) Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)))
            Return item
        End Function

        Private Function GetAwaitableDescription(member As ISymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
            If member.IsAwaitable(semanticModel, position) Then
                Return member.ToAwaitableParts(SyntaxFacts.GetText(SyntaxKind.AwaitKeyword), "r", semanticModel, position)
            End If

            Return SpecializedCollections.EmptyList(Of SymbolDisplayPart)
        End Function

        Private Function GetMemberGroupPreambleParts(symbol As ISymbol, semanticModel As SemanticModel, position As Integer) As IEnumerable(Of SymbolDisplayPart)
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
                                                      position As Integer) As IEnumerable(Of SymbolDisplayPart)
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
