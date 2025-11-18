' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.OrganizeImports
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.OrganizeImports
    Partial Friend Class VisualBasicOrganizeImportsService
        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _placeSystemNamespaceFirst As Boolean
            Private ReadOnly _separateGroups As Boolean
            Private ReadOnly _newLineTrivia As SyntaxTrivia

            Public Sub New(options As OrganizeImportsOptions)
                _placeSystemNamespaceFirst = options.PlaceSystemNamespaceFirst
                _separateGroups = options.SeparateImportDirectiveGroups
                _newLineTrivia = VisualBasicSyntaxGeneratorInternal.Instance.EndOfLine(options.NewLine)
            End Sub

            Public Overrides Function VisitCompilationUnit(node As CompilationUnitSyntax) As SyntaxNode
                node = DirectCast(MyBase.VisitCompilationUnit(node), CompilationUnitSyntax)
                Dim organizedImports = ImportsOrganizer.Organize(
                    node.Imports, _placeSystemNamespaceFirst, _separateGroups, _newLineTrivia)

                Return node.WithImports(organizedImports)
            End Function

            Public Overrides Function VisitImportsStatement(node As ImportsStatementSyntax) As SyntaxNode
                Dim organizedImportsClauses = ImportsOrganizer.Organize(node.ImportsClauses)

                Return node.WithImportsClauses(organizedImportsClauses)
            End Function
        End Class
    End Class
End Namespace
