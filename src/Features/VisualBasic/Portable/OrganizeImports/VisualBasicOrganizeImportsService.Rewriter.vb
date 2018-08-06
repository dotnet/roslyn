' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.OrganizeImports
    Partial Friend Class VisualBasicOrganizeImportsService
        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _placeSystemNamespaceFirst As Boolean
            Private ReadOnly _separateGroups As Boolean

            Public ReadOnly TextChanges As IList(Of TextChange) = New List(Of TextChange)()

            Public Sub New(placeSystemNamespaceFirst As Boolean, separateGroups As Boolean)
                _placeSystemNamespaceFirst = placeSystemNamespaceFirst
                _separateGroups = separateGroups
            End Sub

            Public Overrides Function VisitCompilationUnit(node As CompilationUnitSyntax) As SyntaxNode
                node = DirectCast(MyBase.VisitCompilationUnit(node), CompilationUnitSyntax)
                Dim organizedImports = ImportsOrganizer.Organize(
                    node.Imports, _placeSystemNamespaceFirst, _separateGroups)

                Dim result = node.WithImports(organizedImports)
                If result IsNot node Then
                    AddTextChange(node.Imports, organizedImports)
                End If

                Return result
            End Function

            Public Overrides Function VisitImportsStatement(node As ImportsStatementSyntax) As SyntaxNode
                Dim organizedImportsClauses = ImportsOrganizer.Organize(node.ImportsClauses, _placeSystemNamespaceFirst)

                Dim result = node.WithImportsClauses(organizedImportsClauses)
                If result IsNot node Then
                    AddTextChange(node.ImportsClauses, organizedImportsClauses)
                End If

                Return result
            End Function

            Private Sub AddTextChange(Of TSyntax As SyntaxNode)(list As SeparatedSyntaxList(Of TSyntax),
                                                                organizedList As SeparatedSyntaxList(Of TSyntax))
                If list.Count > 0 Then
                    Me.TextChanges.Add(New TextChange(GetTextSpan(list), GetNewText(organizedList)))
                End If
            End Sub

            Private Sub AddTextChange(Of TSyntax As SyntaxNode)(list As SyntaxList(Of TSyntax),
                                                                organizedList As SyntaxList(Of TSyntax))
                If list.Count > 0 Then
                    Me.TextChanges.Add(New TextChange(GetTextSpan(list), GetNewText(organizedList)))
                End If
            End Sub

            Private Function GetNewText(Of TSyntax As SyntaxNode)(organizedList As SyntaxList(Of TSyntax)) As String
                Return String.Join(String.Empty, organizedList.[Select](Function(t) t.ToFullString()))
            End Function

            Private Function GetNewText(Of TSyntax As SyntaxNode)(organizedList As SeparatedSyntaxList(Of TSyntax)) As String
                Return String.Join(String.Empty, organizedList.GetWithSeparators().[Select](Function(t) t.ToFullString()))
            End Function

            Private Function GetTextSpan(Of TSyntax As SyntaxNode)(list As SyntaxList(Of TSyntax)) As TextSpan
                Return TextSpan.FromBounds(list.First().FullSpan.Start, list.Last().FullSpan.[End])
            End Function

            Private Function GetTextSpan(Of TSyntax As SyntaxNode)(list As SeparatedSyntaxList(Of TSyntax)) As TextSpan
                Return TextSpan.FromBounds(list.First().FullSpan.Start, list.Last().FullSpan.[End])
            End Function
        End Class
    End Class
End Namespace
