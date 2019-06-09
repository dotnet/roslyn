' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddImports
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddImports
    <ExportLanguageService(GetType(IAddImportsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddImportsService
        Inherits AbstractAddImportsService(Of
            CompilationUnitSyntax,
            NamespaceBlockSyntax,
            ImportsStatementSyntax,
            ImportsStatementSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetGlobalImports(compilation As Compilation) As ImmutableArray(Of SyntaxNode)
            Dim generator = VisualBasicSyntaxGenerator.Instance
            Dim result = ArrayBuilder(Of SyntaxNode).GetInstance()

            For Each import In compilation.MemberImports()
                If TypeOf import Is INamespaceSymbol Then
                    result.Add(generator.NamespaceImportDeclaration(import.ToDisplayString()))
                End If
            Next

            Return result.ToImmutableAndFree()
        End Function

        Protected Overrides Function GetAlias(usingOrAlias As ImportsStatementSyntax) As SyntaxNode
            Return usingOrAlias.ImportsClauses.OfType(Of SimpleImportsClauseSyntax).
                                               Where(Function(c) c.Alias IsNot Nothing).
                                               FirstOrDefault()?.Alias
        End Function

        Protected Overrides Function IsStaticUsing(usingOrAlias As ImportsStatementSyntax) As Boolean
            ' Visual Basic doesn't support static imports
            Return False
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
                staticUsingDirectives() As ImportsStatementSyntax,
                aliasDirectives() As ImportsStatementSyntax,
                externContainer As SyntaxNode,
                usingContainer As SyntaxNode,
                staticUsingContainer As SyntaxNode,
                aliasContainer As SyntaxNode,
                placeSystemNamespaceFirst As Boolean,
                root As SyntaxNode) As SyntaxNode

            Dim compilationUnit = DirectCast(root, CompilationUnitSyntax)

            Return compilationUnit.AddImportsStatements(
                usingDirectives.Concat(aliasDirectives).ToList(),
                placeSystemNamespaceFirst,
                Array.Empty(Of SyntaxAnnotation))
        End Function
    End Class
End Namespace
