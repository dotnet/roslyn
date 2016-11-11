' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddImports
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddImports
    <ExportLanguageService(GetType(IAddImportsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddImportsService
        Inherits AbstractAddImportsService(Of
            CompilationUnitSyntax,
            NamespaceBlockSyntax,
            ImportsStatementSyntax,
            ImportsStatementSyntax)

        Protected Overrides Function GetAlias(usingOrAlias As ImportsStatementSyntax) As SyntaxNode
            Return usingOrAlias.ImportsClauses.OfType(Of SimpleImportsClauseSyntax).
                                               Where(Function(c) c.Alias IsNot Nothing).
                                               FirstOrDefault()?.Alias
        End Function

        Protected Overrides Function GetExterns(node As SyntaxNode) As SyntaxList(Of ImportsStatementSyntax)
            Return Nothing
        End Function

        Protected Overrides Function GetUsingsAndAliases(node As SyntaxNode) As SyntaxList(Of ImportsStatementSyntax)
            If node.Kind() = SyntaxKind.CompilationUnit Then
                Return DirectCast(node, CompilationUnitSyntax).Imports
            End If

            Return Nothing
        End Function

        Protected Overrides Function Rewrite(
                externAliases() As ImportsStatementSyntax,
                usingDirectives() As ImportsStatementSyntax,
                aliasDirectives() As ImportsStatementSyntax,
                externContainer As SyntaxNode,
                usingContainer As SyntaxNode,
                aliasContainer As SyntaxNode,
                placeSystemNamespaceFirst As Boolean,
                root As SyntaxNode) As SyntaxNode

            Dim compilationUnit = DirectCast(root, CompilationUnitSyntax)

            Return compilationUnit.AddImportsStatements(
                usingDirectives.Concat(aliasDirectives).ToList(),
                placeSystemNamespaceFirst)
        End Function
    End Class
End Namespace
