' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Partial Friend Class ImportsOrganizer
        Private Shared ReadOnly s_newLine As SyntaxTrivia = SyntaxFactory.ElasticCarriageReturnLineFeed

        Public Shared Function Organize([imports] As SyntaxList(Of ImportsStatementSyntax),
                                        placeSystemNamespaceFirst As Boolean,
                                        separateGroups As Boolean) As SyntaxList(Of ImportsStatementSyntax)

            [imports] = OrganizeWorker([imports], placeSystemNamespaceFirst)

            If separateGroups Then
                For i = 1 To [imports].Count - 1
                    Dim lastImport = [imports](i - 1)
                    Dim currentImport = [imports](i)

                    If NeedsGrouping(lastImport, currentImport) AndAlso
                       Not currentImport.GetLeadingTrivia().Any(Function(t) t.IsEndOfLine()) Then
                        [imports] = [imports].Replace(
                        currentImport, currentImport.WithPrependedLeadingTrivia(s_newLine))
                    End If
                Next
            End If

            Return [imports]
        End Function

        Private Shared Function NeedsGrouping(import1 As ImportsStatementSyntax,
                                              import2 As ImportsStatementSyntax) As Boolean
            If import1.ImportsClauses.Count = 0 OrElse import2.ImportsClauses.Count = 0 Then
                Return False
            End If

            Dim importClause1 = import1.ImportsClauses(0)
            Dim importClause2 = import2.ImportsClauses(0)

            Dim simpleClause1 = TryCast(importClause1, SimpleImportsClauseSyntax)
            Dim simpleClause2 = TryCast(importClause2, SimpleImportsClauseSyntax)

            If simpleClause1 Is Nothing AndAlso simpleClause2 Is Nothing Then
                Return False
            End If

            If simpleClause1 IsNot Nothing AndAlso simpleClause2 IsNot Nothing Then
                Dim isAlias1 = simpleClause1.Alias IsNot Nothing
                Dim isAlias2 = simpleClause2.Alias IsNot Nothing

                If isAlias1 AndAlso isAlias2 Then
                    Return False
                End If

                If Not isAlias1 AndAlso Not isAlias2 Then
                    ' named imports
                    Dim name1 = simpleClause1.Name.GetFirstToken().ValueText
                    Dim name2 = simpleClause2.Name.GetFirstToken().ValueText

                    Return Not VisualBasicSyntaxFactsService.Instance.StringComparer.Equals(name1, name2)
                End If
            End If

            ' Different kinds of imports.  Definitely place into separate groups.
            Return True
        End Function

        Public Shared Function OrganizeWorker([imports] As SyntaxList(Of ImportsStatementSyntax),
                                              placeSystemNamespaceFirst As Boolean) As SyntaxList(Of ImportsStatementSyntax)
            If [imports].Count > 1 Then
                Dim initialList = New List(Of ImportsStatementSyntax)([imports])
                If Not [imports].SpansPreprocessorDirective() Then
                    ' If there is a banner comment that precedes the nodes,
                    ' then remove it and store it for later.
                    Dim leadingTrivia As ImmutableArray(Of SyntaxTrivia) = Nothing
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
            endOfLine = If(endOfLine.Kind = SyntaxKind.None, s_newLine, endOfLine)

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
                Dim result = clauses.OrderBy(ImportsClauseComparer.NormalInstance).ToList()

                If Not result.SequenceEqual(clauses) Then
                    Return SyntaxFactory.SeparatedList(result, clauses.GetSeparators())
                End If
            End If

            Return clauses
        End Function
    End Class
End Namespace
