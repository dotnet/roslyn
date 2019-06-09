' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.SignatureHelp

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    Friend MustInherit Class AbstractVisualBasicSignatureHelpProvider
        Inherits AbstractSignatureHelpProvider

        Protected Shared Function SynthesizedParameter(s As String) As SymbolDisplayPart
            Return New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, s)
        End Function

        Protected Shared Function Keyword(kind As SyntaxKind) As SymbolDisplayPart
            Return New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, SyntaxFacts.GetText(kind))
        End Function

        Protected Shared Function Punctuation(kind As SyntaxKind) As SymbolDisplayPart
            Return New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, SyntaxFacts.GetText(kind))
        End Function

        Protected Shared Function Text(_text As String) As SymbolDisplayPart
            Return New SymbolDisplayPart(SymbolDisplayPartKind.Text, Nothing, _text)
        End Function

        Protected Shared Function Space() As SymbolDisplayPart
            Return New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " ")
        End Function

        Protected Shared Function NewLine() As SymbolDisplayPart
            Return New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, vbCrLf)
        End Function

        Protected Shared Function GetSeparatorParts() As IList(Of SymbolDisplayPart)
            Return {Punctuation(SyntaxKind.CommaToken), Space()}
        End Function

        Protected Shared Function Convert(parameter As IParameterSymbol,
                                          semanticModel As SemanticModel,
                                          position As Integer, documentationCommentFormattingService As IDocumentationCommentFormattingService, cancellationToken As CancellationToken) As SignatureHelpSymbolParameter
            Return New SignatureHelpSymbolParameter(
                parameter.Name,
                parameter.IsOptional,
                parameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                parameter.ToMinimalDisplayParts(semanticModel, position))
        End Function

        Protected Shared Sub AddExtensionPreamble(symbol As ISymbol, result As IList(Of SymbolDisplayPart))
            If symbol.GetOriginalUnreducedDefinition().IsExtensionMethod() Then
                result.Add(Punctuation(SyntaxKind.LessThanToken))
                result.Add(Text(VBFeaturesResources.Extension))
                result.Add(Punctuation(SyntaxKind.GreaterThanToken))
                result.Add(Space())
            End If
        End Sub
    End Class
End Namespace
