' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module INamespaceOrTypeSymbolExtensions
        <Extension>
        Public Function GenerateTypeSyntax(symbol As INamespaceOrTypeSymbol, Optional addGlobal As Boolean = True) As TypeSyntax
            Return symbol.Accept(TypeSyntaxGeneratorVisitor.Create(addGlobal)).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function

        <Extension>
        Public Function GetAliasForSymbol(symbol As INamespaceOrTypeSymbol, node As SyntaxNode, semanticModel As SemanticModel) As IAliasSymbol
            ' NOTE(cyrusn): If we're in an imports clause, we can't use aliases.
            Dim clause = node.AncestorsAndSelf().OfType(Of ImportsClauseSyntax).FirstOrDefault()
            If clause IsNot Nothing Then
                Return Nothing
            End If

            Dim originalSemanticModel = semanticModel.GetOriginalSemanticModel()
            If Not originalSemanticModel.SyntaxTree.HasCompilationUnitRoot Then
                Return Nothing
            End If

            Dim aliasSymbol As IAliasSymbol = Nothing
            If Not AliasSymbolCache.TryGetAliasSymbol(originalSemanticModel, 0, symbol, aliasSymbol) Then
                ' build cache first
                AliasSymbolCache.AddAliasSymbols(originalSemanticModel, 0, originalSemanticModel.GetAliasSymbols())

                ' retry
                AliasSymbolCache.TryGetAliasSymbol(originalSemanticModel, 0, symbol, aliasSymbol)
            End If

            Return aliasSymbol
        End Function
    End Module

End Namespace
