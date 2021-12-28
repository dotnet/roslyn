' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    Partial Friend Class GenericNameSignatureHelpProvider

        Private Shared Function GetPreambleParts(namedType As INamedTypeSymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()
            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or SymbolDisplayMiscellaneousOptions.UseSpecialTypes)
            result.AddRange(namedType.ToMinimalDisplayParts(semanticModel, position, format))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            result.Add(Keyword(SyntaxKind.OfKeyword))
            result.Add(Space())
            Return result
        End Function

        Private Shared Function GetPostambleParts() As IList(Of SymbolDisplayPart)
            Return {Punctuation(SyntaxKind.CloseParenToken)}
        End Function
    End Class
End Namespace
