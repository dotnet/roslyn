' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

    Partial Friend Class GenericNameSignatureHelpProvider

        Private Function GetPreambleParts(method As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IEnumerable(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()

            AddExtensionPreamble(method, result)

            Dim containingType = GetContainingType(method)
            If containingType IsNot Nothing Then
                result.AddRange(containingType.ToMinimalDisplayParts(semanticModel, position))
                result.Add(Punctuation(SyntaxKind.DotToken))
            End If

            result.Add(New SymbolDisplayPart(SymbolDisplayPartKind.MethodName, method, method.Name))

            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            result.Add(Keyword(SyntaxKind.OfKeyword))
            result.Add(Space())
            Return result
        End Function

        Private Function GetContainingType(method As IMethodSymbol) As ITypeSymbol
            Dim result = method.ReceiverType

            If result.Kind <> SymbolKind.NamedType OrElse Not DirectCast(result, INamedTypeSymbol).IsScriptClass Then
                Return result
            Else
                Return Nothing
            End If
        End Function

        Private Function GetPostambleParts(method As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IEnumerable(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()
            result.Add(Punctuation(SyntaxKind.CloseParenToken))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))

            Dim first = True
            For Each parameter In method.Parameters
                If Not first Then
                    result.Add(Punctuation(SyntaxKind.CommaToken))
                    result.Add(Space())
                End If

                first = False
                result.AddRange(parameter.ToMinimalDisplayParts(semanticModel, position))
            Next

            result.Add(Punctuation(SyntaxKind.CloseParenToken))

            If Not method.ReturnsVoid Then
                result.Add(Space())
                result.Add(Keyword(SyntaxKind.AsKeyword))
                result.Add(Space())
                result.AddRange(method.ReturnType.ToMinimalDisplayParts(semanticModel, position))
            End If

            Return result
        End Function
    End Class
End Namespace
