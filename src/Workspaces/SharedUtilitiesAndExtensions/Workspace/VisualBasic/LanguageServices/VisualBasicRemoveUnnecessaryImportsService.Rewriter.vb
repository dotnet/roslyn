' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports
    Partial Friend Class VisualBasicRemoveUnnecessaryImportsService
        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _unnecessaryImports As ISet(Of ImportsClauseSyntax)
            Private ReadOnly _cancellationToken As CancellationToken
            Private ReadOnly _annotation As New SyntaxAnnotation()
            Private ReadOnly _document As Document

            Public Sub New(document As Document,
                           unnecessaryImports As ISet(Of ImportsClauseSyntax),
                           cancellationToken As CancellationToken)
                _document = document
                _unnecessaryImports = unnecessaryImports
                _cancellationToken = cancellationToken
            End Sub

            Public Overrides Function DefaultVisit(node As SyntaxNode) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()
                Return MyBase.DefaultVisit(node)
            End Function

            Public Overrides Function VisitImportsStatement(node As ImportsStatementSyntax) As SyntaxNode
                If Not node.ImportsClauses.All(AddressOf _unnecessaryImports.Contains) Then
                    Return node.RemoveNodes(node.ImportsClauses.Where(AddressOf _unnecessaryImports.Contains), SyntaxRemoveOptions.KeepNoTrivia)
                Else
                    Return node.WithAdditionalAnnotations(_annotation)
                End If
            End Function

            Private Function ProcessImports(compilationUnit As CompilationUnitSyntax) As CompilationUnitSyntax
                Dim oldImports = compilationUnit.Imports.ToList()

                Dim remainingTrivia As SyntaxTriviaList = Nothing
                For i = 0 To oldImports.Count - 1
                    Dim oldImport = oldImports(i)
                    If oldImport.HasAnnotation(_annotation) Then
                        ' Found a node we marked to delete. Remove it.
                        oldImports(i) = Nothing

                        Dim leadingTrivia = oldImport.GetLeadingTrivia()
                        If ShouldPreserveTrivia(leadingTrivia) Then
                            ' This import had trivia we want to preserve. If we're the last import,
                            ' then copy this trivia out so that our caller can place it on the next token.
                            ' If there is any import following us, then place it on that.
                            If i < oldImports.Count - 1 Then
                                Dim nextIndex = i + 1
                                Dim nextImport = oldImports(nextIndex)

                                If ShouldPreserveTrivia(nextImport.GetLeadingTrivia()) Then
                                    ' If we need to preserve the next trivia too then, prepend
                                    ' the two together.
                                    oldImports(nextIndex) = nextImport.WithPrependedLeadingTrivia(leadingTrivia)
                                Else
                                    ' Otherwise, replace the next trivia with this trivia that we
                                    ' want to preserve.
                                    oldImports(nextIndex) = nextImport.WithLeadingTrivia(leadingTrivia)
                                End If
                            Else
                                remainingTrivia = leadingTrivia
                            End If
                        End If

                        If i > 0 Then
                            ' We should replace the trailing trivia of the previous import
                            ' with the trailing trivia of this import.
                            Dim index = i - 1
                            Dim previousImport = oldImports(index)
                            If previousImport Is Nothing AndAlso index > 0 Then
                                index -= 1
                                previousImport = oldImports(index)
                            End If

                            If previousImport IsNot Nothing Then
                                Dim trailingTrivia = oldImport.GetTrailingTrivia()
                                oldImports(index) = previousImport.WithTrailingTrivia(trailingTrivia)
                            End If
                        End If
                    End If
                Next

                Dim newImports = SyntaxFactory.List(oldImports.WhereNotNull())

                If remainingTrivia.Count > 0 Then
                    Dim nextToken = compilationUnit.Imports.Last().GetLastToken().GetNextTokenOrEndOfFile()
                    compilationUnit = compilationUnit.ReplaceToken(nextToken, nextToken.WithPrependedLeadingTrivia(remainingTrivia))
                End If

                Return compilationUnit.WithImports(newImports)
            End Function

            Private Shared Function ShouldPreserveTrivia(trivia As SyntaxTriviaList) As Boolean
                Return trivia.Any(Function(t) Not t.IsWhitespaceOrEndOfLine())
            End Function

            Public Overrides Function VisitCompilationUnit(node As CompilationUnitSyntax) As SyntaxNode
                Dim compilationUnit = DirectCast(MyBase.VisitCompilationUnit(node), CompilationUnitSyntax)

                If Not compilationUnit.Imports.Any(Function(i) i.HasAnnotation(_annotation)) Then
                    Return compilationUnit
                End If

                Dim newCompilationUnit = ProcessImports(compilationUnit)

                If newCompilationUnit.Imports.Count = 0 AndAlso newCompilationUnit.Options.Count = 0 Then
                    If newCompilationUnit.Attributes.Count > 0 OrElse newCompilationUnit.Members.Count > 0 Then
                        Dim firstToken = newCompilationUnit.GetFirstToken()
                        Dim newFirstToken = StripNewLines(_document, firstToken)
                        newCompilationUnit = newCompilationUnit.ReplaceToken(firstToken, newFirstToken)
                    End If
                End If

                Return newCompilationUnit
            End Function
        End Class
    End Class
End Namespace
