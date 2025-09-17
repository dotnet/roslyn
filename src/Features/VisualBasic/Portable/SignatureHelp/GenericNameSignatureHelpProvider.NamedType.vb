' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    Partial Friend Class GenericNameSignatureHelpProvider

        Private Shared Function GetPreambleParts(namedType As INamedTypeSymbol, semanticModel As SemanticModel, position As Integer) As ImmutableArray(Of SymbolDisplayPart)
            Dim result = ArrayBuilder(Of SymbolDisplayPart).GetInstance()
            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or SymbolDisplayMiscellaneousOptions.UseSpecialTypes)
            result.AddRange(namedType.ToMinimalDisplayParts(semanticModel, position, format))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            result.Add(Keyword(SyntaxKind.OfKeyword))
            result.Add(Space())
            Return result.ToImmutableAndFree()
        End Function

        Private Shared Function GetPostambleParts() As ImmutableArray(Of SymbolDisplayPart)
            Return ImmutableArray.Create(Punctuation(SyntaxKind.CloseParenToken))
        End Function
    End Class
End Namespace
