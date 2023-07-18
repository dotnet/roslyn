' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.OrganizeImports
Imports Microsoft.CodeAnalysis.Text
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

            Public ReadOnly TextChanges As IList(Of TextChange) = New List(Of TextChange)()

            Public Sub New(options As OrganizeImportsOptions)
                _placeSystemNamespaceFirst = options.PlaceSystemNamespaceFirst
                _separateGroups = options.SeparateImportDirectiveGroups
                _newLineTrivia = VisualBasicSyntaxGeneratorInternal.Instance.EndOfLine(options.NewLine)
            End Sub

            Public Overrides Function VisitCompilationUnit(node As CompilationUnitSyntax) As SyntaxNode
                node = DirectCast(MyBase.VisitCompilationUnit(node), CompilationUnitSyntax)
                Dim organizedImports = ImportsOrganizer.Organize(
                    node.Imports, _placeSystemNamespaceFirst, _separateGroups, _newLineTrivia)

                Dim result = node.WithImports(organizedImports)
                If result IsNot node Then
                    AddTextChange(node.Imports, organizedImports)
                End If

                Return result
            End Function

            Public Overrides Function VisitImportsStatement(node As ImportsStatementSyntax) As SyntaxNode
                Dim organizedImportsClauses = ImportsOrganizer.Organize(node.ImportsClauses)

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

            Private Shared Function GetNewText(Of TSyntax As SyntaxNode)(organizedList As SyntaxList(Of TSyntax)) As String
                Return String.Join(String.Empty, organizedList.[Select](Function(t) t.ToFullString()))
            End Function

            Private Shared Function GetNewText(Of TSyntax As SyntaxNode)(organizedList As SeparatedSyntaxList(Of TSyntax)) As String
                Return String.Join(String.Empty, organizedList.GetWithSeparators().[Select](Function(t) t.ToFullString()))
            End Function

            Private Shared Function GetTextSpan(Of TSyntax As SyntaxNode)(list As SyntaxList(Of TSyntax)) As TextSpan
                Return TextSpan.FromBounds(list.First().FullSpan.Start, list.Last().FullSpan.[End])
            End Function

            Private Shared Function GetTextSpan(Of TSyntax As SyntaxNode)(list As SeparatedSyntaxList(Of TSyntax)) As TextSpan
                Return TextSpan.FromBounds(list.First().FullSpan.Start, list.Last().FullSpan.[End])
            End Function
        End Class
    End Class
End Namespace
