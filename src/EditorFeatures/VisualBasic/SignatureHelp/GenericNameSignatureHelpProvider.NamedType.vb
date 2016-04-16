' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

    Partial Friend Class GenericNameSignatureHelpProvider

        Private Function GetPreambleParts(namedType As INamedTypeSymbol, semanticModel As SemanticModel, position As Integer) As IEnumerable(Of SymbolDisplayPart)
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

        Private Function GetPostambleParts(namedType As INamedTypeSymbol) As IEnumerable(Of SymbolDisplayPart)
            Return {Punctuation(SyntaxKind.CloseParenToken)}
        End Function
    End Class
End Namespace

