' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Partial Class ImportsOrganizer
        Public Shared Function Organize([imports] As SyntaxList(Of ImportsStatementSyntax),
                                        placeSystemNamespaceFirst As Boolean) As SyntaxList(Of ImportsStatementSyntax)
            If [imports].Count > 1 Then
                Dim initialList = New List(Of ImportsStatementSyntax)([imports])
                If Not [imports].SpansPreprocessorDirective() Then
                    ' If there is a banner comment that precedes the nodes,
                    ' then remove it and store it for later.
                    Dim leadingTrivia As IEnumerable(Of SyntaxTrivia) = Nothing
                    initialList(0) = initialList(0).GetNodeWithoutLeadingBannerAndPreprocessorDirectives(leadingTrivia)

                    Dim comparer = If(placeSystemNamespaceFirst, ImportsStatementComparer.SystemFirstInstance, ImportsStatementComparer.NormalInstance)

                    Dim finalList = initialList.OrderBy(comparer).ToList()

                    ' Check if sorting the list actually changed anything. If 
                    ' not, then we don't need to make any changes to the file.
                    If Not finalList.SequenceEqual(initialList) Then
                        '' Make sure newlines are correct between nodes.
                        EnsureNewLines(finalList)

                        ' Reattach the banner.
                        finalList(0) = finalList(0).WithLeadingTrivia(leadingTrivia.Concat(finalList(0).GetLeadingTrivia()).ToSyntaxTriviaList())

                        Return SyntaxFactory.List(finalList)
                    End If
                End If
            End If

            Return [imports]
        End Function

        Private Shared Sub EnsureNewLines(list As List(Of ImportsStatementSyntax))
            Dim endOfLine = GetExistingEndOfLineTrivia(list)
            endOfLine = If(endOfLine.Kind = SyntaxKind.None, SyntaxFactory.CarriageReturnLineFeed, endOfLine)

            For i = 0 To list.Count - 1
                If Not list(i).GetTrailingTrivia().Any(SyntaxKind.EndOfLineTrivia) Then
                    list(i) = list(i).WithAppendedTrailingTrivia(endOfLine)
                End If

                list(i) = list(i).WithLeadingTrivia(list(i).GetLeadingTrivia().SkipWhile(
                    Function(t) t.Kind = SyntaxKind.WhitespaceTrivia OrElse t.Kind = SyntaxKind.EndOfLineTrivia))
            Next
        End Sub

        Private Shared Function GetExistingEndOfLineTrivia(list As List(Of ImportsStatementSyntax)) As SyntaxTrivia
            Dim endOfLine As SyntaxTrivia
            For Each node In list
                For Each token In node.DescendantTokens()
                    endOfLine = token.LeadingTrivia.FirstOrDefault(Function(t) t.Kind = SyntaxKind.EndOfLineTrivia)
                    If endOfLine.Kind <> SyntaxKind.None Then
                        Return endOfLine
                    End If

                    endOfLine = token.TrailingTrivia.FirstOrDefault(Function(t) t.Kind = SyntaxKind.EndOfLineTrivia)
                    If endOfLine.Kind <> SyntaxKind.None Then
                        Return endOfLine
                    End If
                Next
            Next

            Return Nothing
        End Function

        Public Shared Function Organize(clauses As SeparatedSyntaxList(Of ImportsClauseSyntax),
                                        placeSystemNamespaceFirst As Boolean) As SeparatedSyntaxList(Of ImportsClauseSyntax)
            If clauses.Count > 0 Then
                Dim result = clauses.OrderBy(ImportsClauseComparer.Instance).ToList()

                If Not result.SequenceEqual(clauses) Then
                    Return SyntaxFactory.SeparatedList(result, clauses.GetSeparators())
                End If
            End If

            Return clauses
        End Function
    End Class
End Namespace
