' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend MustInherit Class VisualBasicSyntaxVisitor
        Public Overridable Function VisitSyntaxToken(token As SyntaxToken) As SyntaxToken
            Debug.Assert(token IsNot Nothing)
            Return token
        End Function

        Public Overridable Function VisitSyntaxTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
            Debug.Assert(trivia IsNot Nothing)
            Return trivia
        End Function
    End Class

    Partial Friend Class VisualBasicSyntaxRewriter
        Inherits VisualBasicSyntaxVisitor

        Public Function VisitList(Of TNode As VisualBasicSyntaxNode)(list As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of TNode)) As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of TNode)
            Dim alternate As SyntaxListBuilder(Of TNode) = Nothing
            Dim i As Integer = 0
            Dim n As Integer = list.Count
            Do While (i < n)
                Dim item As TNode = list.Item(i)
                Dim visited As TNode = DirectCast(Me.Visit(item), TNode)
                If item IsNot visited AndAlso alternate.IsNull Then
                    alternate = New SyntaxListBuilder(Of TNode)(n)
                    alternate.AddRange(list, 0, i)
                End If

                If Not alternate.IsNull Then
                    If visited IsNot Nothing AndAlso visited.Kind <> SyntaxKind.None Then
                        alternate.Add(visited)
                    End If
                End If
                i += 1
            Loop
            If Not alternate.IsNull Then
                Return alternate.ToList()
            End If
            Return list
        End Function

        Public Function VisitList(Of TNode As VisualBasicSyntaxNode)(list As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of TNode)) As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of TNode)
            Dim alternate As SeparatedSyntaxListBuilder(Of TNode) = Nothing
            Dim i As Integer = 0
            Dim itemCount As Integer = list.Count
            Dim separatorCount As Integer = list.SeparatorCount

            While i < itemCount
                Dim item = list(i)
                Dim visitedItem = Me.Visit(item)

                Dim separator As GreenNode = Nothing
                Dim visitedSeparator As GreenNode = Nothing

                If (i < separatorCount) Then
                    separator = list.GetSeparator(i)
                    ' LastTokenReplacer depends on us calling Visit rather than VisitToken for separators.
                    ' It is not clear whether this is desirable/acceptable.
                    Dim visitedSeparatorNode = Me.Visit(DirectCast(separator, VisualBasicSyntaxNode))
                    Debug.Assert(TypeOf visitedSeparatorNode Is SyntaxToken, "Cannot replace a separator with a non-separator")

                    visitedSeparator = DirectCast(visitedSeparatorNode, SyntaxToken)

                    Debug.Assert((separator Is Nothing AndAlso separator.RawKind = SyntaxKind.None) OrElse
                        (visitedSeparator IsNot Nothing AndAlso visitedSeparator.RawKind <> SyntaxKind.None),
                    "Cannot delete a separator from a separated list. Removing an element will remove the corresponding separator.")
                End If

                If (item IsNot visitedItem OrElse separator IsNot visitedSeparator) AndAlso alternate.IsNull Then
                    alternate = New SeparatedSyntaxListBuilder(Of TNode)(itemCount)
                    alternate.AddRange(list, i)
                End If

                If Not alternate.IsNull Then
                    If visitedItem IsNot Nothing AndAlso visitedItem.Kind <> SyntaxKind.None Then
                        alternate.Add(DirectCast(visitedItem, TNode))
                        If visitedSeparator IsNot Nothing Then
                            alternate.AddSeparator(visitedSeparator)
                        End If
                    ElseIf i >= separatorCount AndAlso alternate.Count > 0 Then ' last element deleted
                        alternate.RemoveLast() ' delete *preceding* separator
                    End If
                End If

                i += 1
            End While

            If Not alternate.IsNull Then
                Return alternate.ToList()
            End If

            Return list
        End Function
    End Class
End Namespace
