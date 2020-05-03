' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis.Syntax
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Public Class SyntaxNodeRemover
        Friend Shared Function RemoveNodes(Of TRoot As SyntaxNode)(root As TRoot, nodes As IEnumerable(Of SyntaxNode), options As SyntaxRemoveOptions) As TRoot
            Dim nodesToRemove As SyntaxNode() = nodes.ToArray()

            If nodesToRemove.Length = 0 Then
                Return root
            End If

            Dim remover = New SyntaxRemover(nodes.ToArray(), options)
            Dim result = remover.Visit(root)

            Dim residualTrivia = remover.ResidualTrivia

            If residualTrivia.Count > 0 Then
                result = result.WithTrailingTrivia(result.GetTrailingTrivia().Concat(residualTrivia))
            End If

            Return DirectCast(result, TRoot)
        End Function

        Private Class SyntaxRemover
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _nodesToRemove As HashSet(Of SyntaxNode)
            Private ReadOnly _options As SyntaxRemoveOptions
            Private ReadOnly _searchSpan As TextSpan
            Private ReadOnly _residualTrivia As SyntaxTriviaListBuilder
            Private _directivesToKeep As HashSet(Of SyntaxNode)

            Public Sub New(nodes As SyntaxNode(), options As SyntaxRemoveOptions)
                MyBase.New(nodes.Any(Function(n) n.IsPartOfStructuredTrivia()))
                Me._nodesToRemove = New HashSet(Of SyntaxNode)(nodes)
                Me._options = options
                Me._searchSpan = ComputeTotalSpan(nodes)
                Me._residualTrivia = SyntaxTriviaListBuilder.Create()
            End Sub

            Private Shared Function ComputeTotalSpan(nodes As SyntaxNode()) As TextSpan
                Dim span0 = nodes(0).FullSpan
                Dim start As Integer = span0.Start
                Dim [end] As Integer = span0.End
                Dim i As Integer = 1

                While i < nodes.Length
                    Dim span = nodes(i).FullSpan
                    start = Math.Min(start, span.Start)
                    [end] = Math.Max([end], span.End)
                    i = i + 1
                End While

                Return New TextSpan(start, [end] - start)
            End Function

            Friend ReadOnly Property ResidualTrivia As SyntaxTriviaList
                Get
                    If Me._residualTrivia IsNot Nothing Then
                        Return Me._residualTrivia.ToList()
                    Else
                        Return Nothing
                    End If
                End Get
            End Property

            Private Sub AddResidualTrivia(trivia As SyntaxTriviaList, Optional requiresNewLine As Boolean = False)
                If requiresNewLine Then
                    AddEndOfLine()
                End If

                Me._residualTrivia.Add(trivia)
            End Sub

            Private Sub AddEndOfLine()
                If Me._residualTrivia.Count = 0 OrElse Not IsEndOfLine(Me._residualTrivia(Me._residualTrivia.Count - 1)) Then
                    Me._residualTrivia.Add(SyntaxFactory.CarriageReturnLineFeed)
                End If
            End Sub
            Private Shared Function IsEndOfLine(trivia As SyntaxTrivia) As Boolean
                Return trivia.Kind = SyntaxKind.EndOfLineTrivia OrElse trivia.Kind = SyntaxKind.CommentTrivia OrElse trivia.IsDirective
            End Function

            Private Shared Function HasEndOfLine(trivia As SyntaxTriviaList) As Boolean
                Return trivia.Any(Function(t) IsEndOfLine(t))
            End Function

            Private Function IsForRemoval(node As SyntaxNode) As Boolean
                Return Me._nodesToRemove.Contains(node)
            End Function

            Private Function ShouldVisit(node As SyntaxNode) As Boolean
                Return node.FullSpan.IntersectsWith(Me._searchSpan) OrElse (Me._residualTrivia IsNot Nothing AndAlso Me._residualTrivia.Count > 0)
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                Dim result = node

                If node IsNot Nothing Then
                    If Me.IsForRemoval(node) Then
                        Me.AddTrivia(node)
                        result = Nothing
                    ElseIf Me.ShouldVisit(node) Then
                        result = MyBase.Visit(node)
                    End If
                End If

                Return result
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Dim result = token

                If Me.VisitIntoStructuredTrivia Then ' only bother visiting trivia if we are removing a node in structured trivia
                    result = MyBase.VisitToken(token)
                End If

                ' the next token gets the accrued trivia.
                If result.Kind <> SyntaxKind.None AndAlso Me._residualTrivia IsNot Nothing AndAlso Me._residualTrivia.Count > 0 Then
                    Me._residualTrivia.Add(result.LeadingTrivia)
                    result = result.WithLeadingTrivia(Me._residualTrivia.ToList())
                    Me._residualTrivia.Clear()
                End If

                Return result
            End Function

            ' deal with separated lists and removal of associated separators
            Public Overrides Function VisitList(Of TNode As SyntaxNode)(list As SeparatedSyntaxList(Of TNode)) As SeparatedSyntaxList(Of TNode)
                Dim withSeps = list.GetWithSeparators()
                Dim removeNextSeparator = False

                Dim alternate As SyntaxNodeOrTokenListBuilder = Nothing

                Dim n = withSeps.Count
                For i As Integer = 0 To n - 1
                    Dim item = withSeps(i)
                    Dim visited As SyntaxNodeOrToken = Nothing

                    If item.IsToken Then ' separator
                        If removeNextSeparator Then
                            removeNextSeparator = False
                            visited = Nothing
                        Else
                            visited = Me.VisitListSeparator(item.AsToken())
                        End If
                    Else
                        Dim node = DirectCast(item.AsNode(), TNode)

                        If Me.IsForRemoval(node) Then
                            If alternate Is Nothing Then
                                alternate = New SyntaxNodeOrTokenListBuilder(n)
                                alternate.Add(withSeps, 0, i)
                            End If

                            Dim nextTokenIsSeparator, nextSeparatorBelongsToNode As Boolean

                            CommonSyntaxNodeRemover.GetSeparatorInfo(
                                withSeps, i, SyntaxKind.EndOfLineTrivia,
                                nextTokenIsSeparator, nextSeparatorBelongsToNode)

                            If Not nextSeparatorBelongsToNode AndAlso
                               alternate.Count > 0 AndAlso
                               alternate(alternate.Count - 1).IsToken Then

                                Dim separator = alternate(alternate.Count - 1).AsToken()
                                Me.AddTrivia(separator, node)
                                alternate.RemoveLast()
                            ElseIf nextTokenIsSeparator Then
                                Dim separator = withSeps(i + 1).AsToken()
                                Me.AddTrivia(node, separator)
                                removeNextSeparator = True
                            Else
                                Me.AddTrivia(node)
                            End If

                            visited = Nothing
                        Else
                            visited = Me.VisitListElement(node)
                        End If
                    End If

                    If item <> visited AndAlso alternate Is Nothing Then
                        alternate = New SyntaxNodeOrTokenListBuilder(n)
                        alternate.Add(withSeps, 0, i)
                    End If

                    If alternate IsNot Nothing AndAlso Not visited.IsKind(SyntaxKind.None) Then
                        alternate.Add(visited)
                    End If
                Next

                If alternate IsNot Nothing Then
                    Return alternate.ToList().AsSeparatedList(Of TNode)()
                End If

                Return list
            End Function

            Private Sub AddTrivia(node As SyntaxNode)
                If (Me._options And SyntaxRemoveOptions.KeepLeadingTrivia) <> 0 Then
                    Me.AddResidualTrivia(node.GetLeadingTrivia())
                ElseIf (Me._options And SyntaxRemoveOptions.KeepEndOfLine) <> 0 AndAlso HasEndOfLine(node.GetLeadingTrivia()) Then
                    Me.AddEndOfLine()
                End If

                If (Me._options And (SyntaxRemoveOptions.KeepDirectives Or SyntaxRemoveOptions.KeepUnbalancedDirectives)) <> 0 Then
                    Me.AddDirectives(node, GetRemovedSpan(node.Span, node.FullSpan))
                End If

                If (Me._options And SyntaxRemoveOptions.KeepTrailingTrivia) <> 0 Then
                    Me.AddResidualTrivia(node.GetTrailingTrivia())
                ElseIf (Me._options And SyntaxRemoveOptions.KeepEndOfLine) <> 0 AndAlso HasEndOfLine(node.GetTrailingTrivia()) Then
                    Me.AddEndOfLine()
                End If

                If (Me._options And SyntaxRemoveOptions.AddElasticMarker) <> 0 Then
                    Me.AddResidualTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker))
                End If
            End Sub

            Private Sub AddTrivia(token As SyntaxToken, node As SyntaxNode)
                If (Me._options And SyntaxRemoveOptions.KeepLeadingTrivia) <> 0 Then
                    Me.AddResidualTrivia(token.LeadingTrivia)
                    Me.AddResidualTrivia(token.TrailingTrivia)
                    Me.AddResidualTrivia(node.GetLeadingTrivia())
                ElseIf (Me._options And SyntaxRemoveOptions.KeepEndOfLine) <> 0 AndAlso
                    (HasEndOfLine(token.LeadingTrivia) OrElse HasEndOfLine(token.TrailingTrivia) OrElse HasEndOfLine(node.GetLeadingTrivia())) Then
                    Me.AddEndOfLine()
                End If

                If (Me._options And (SyntaxRemoveOptions.KeepDirectives Or SyntaxRemoveOptions.KeepUnbalancedDirectives)) <> 0 Then
                    Dim fullSpan = TextSpan.FromBounds(token.FullSpan.Start, node.FullSpan.End)
                    Dim span = TextSpan.FromBounds(token.Span.Start, node.Span.End)
                    Me.AddDirectives(node.Parent, GetRemovedSpan(span, fullSpan))
                End If

                If (Me._options And SyntaxRemoveOptions.KeepTrailingTrivia) <> 0 Then
                    Me.AddResidualTrivia(node.GetTrailingTrivia())
                ElseIf (Me._options And SyntaxRemoveOptions.KeepEndOfLine) <> 0 AndAlso HasEndOfLine(node.GetTrailingTrivia()) Then
                    Me.AddEndOfLine()
                End If

                If (Me._options And SyntaxRemoveOptions.AddElasticMarker) <> 0 Then
                    Me.AddResidualTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker))
                End If
            End Sub

            Private Sub AddTrivia(node As SyntaxNode, token As SyntaxToken)
                If (Me._options And SyntaxRemoveOptions.KeepLeadingTrivia) <> 0 Then
                    Me.AddResidualTrivia(node.GetLeadingTrivia())
                ElseIf (Me._options And SyntaxRemoveOptions.KeepEndOfLine) <> 0 AndAlso HasEndOfLine(node.GetLeadingTrivia()) Then
                    Me.AddEndOfLine()
                End If

                If (Me._options And (SyntaxRemoveOptions.KeepDirectives Or SyntaxRemoveOptions.KeepUnbalancedDirectives)) <> 0 Then
                    Dim fullSpan = TextSpan.FromBounds(node.FullSpan.Start, token.FullSpan.End)
                    Dim span = TextSpan.FromBounds(node.Span.Start, token.Span.End)
                    Me.AddDirectives(node.Parent, GetRemovedSpan(span, fullSpan))
                End If

                If (Me._options And SyntaxRemoveOptions.KeepTrailingTrivia) <> 0 Then
                    Me.AddResidualTrivia(node.GetTrailingTrivia())
                    Me.AddResidualTrivia(token.LeadingTrivia)
                    Me.AddResidualTrivia(token.TrailingTrivia)
                ElseIf (Me._options And SyntaxRemoveOptions.KeepEndOfLine) <> 0 AndAlso
                        (HasEndOfLine(node.GetTrailingTrivia()) OrElse HasEndOfLine(token.LeadingTrivia) OrElse HasEndOfLine(token.TrailingTrivia)) Then
                    Me.AddEndOfLine()
                End If

                If (Me._options And SyntaxRemoveOptions.AddElasticMarker) <> 0 Then
                    Me.AddResidualTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker))
                End If
            End Sub

            Private Function GetRemovedSpan(span As TextSpan, fullSpan As TextSpan) As TextSpan
                Dim removedSpan = fullSpan
                If (Me._options And SyntaxRemoveOptions.KeepLeadingTrivia) <> 0 Then
                    removedSpan = TextSpan.FromBounds(span.Start, removedSpan.End)
                End If
                If (Me._options And SyntaxRemoveOptions.KeepTrailingTrivia) <> 0 Then
                    removedSpan = TextSpan.FromBounds(removedSpan.Start, span.End)
                End If
                Return removedSpan
            End Function

            Private Sub AddDirectives(node As SyntaxNode, span As TextSpan)
                If node.ContainsDirectives Then
                    If Me._directivesToKeep Is Nothing Then
                        Me._directivesToKeep = New HashSet(Of SyntaxNode)()
                    Else
                        Me._directivesToKeep.Clear()
                    End If

                    Dim directivesInSpan = node.DescendantTrivia(span, Function(n) n.ContainsDirectives, descendIntoTrivia:=True) _
                                                .Where(Function(tr) tr.IsDirective) _
                                                .Select(Function(tr) DirectCast(tr.GetStructure(), DirectiveTriviaSyntax))

                    For Each directive In directivesInSpan
                        If (Me._options And SyntaxRemoveOptions.KeepDirectives) <> 0 Then
                            Me._directivesToKeep.Add(directive)
                        ElseIf HasRelatedDirectives(directive) Then
                            ' a balanced directive with respect to a given node has all related directives rooted under that node
                            Dim relatedDirectives = directive.GetRelatedDirectives()
                            Dim balanced = relatedDirectives.All(Function(rd) rd.FullSpan.OverlapsWith(span))

                            If Not balanced Then
                                ' if not fully balanced, all related directives under the node are considered unbalanced.
                                For Each unbalancedDirective In relatedDirectives.Where(Function(rd) rd.FullSpan.OverlapsWith(span))
                                    Me._directivesToKeep.Add(unbalancedDirective)
                                Next
                            End If
                        End If

                        If Me._directivesToKeep.Contains(directive) Then
                            AddResidualTrivia(SyntaxFactory.TriviaList(directive.ParentTrivia), requiresNewLine:=True)
                        End If
                    Next
                End If
            End Sub

            Private Shared Function HasRelatedDirectives(directive As DirectiveTriviaSyntax) As Boolean
                Select Case directive.Kind
                    Case SyntaxKind.IfDirectiveTrivia,
                         SyntaxKind.ElseDirectiveTrivia,
                         SyntaxKind.ElseIfDirectiveTrivia,
                         SyntaxKind.EndIfDirectiveTrivia,
                         SyntaxKind.RegionDirectiveTrivia,
                         SyntaxKind.EndRegionDirectiveTrivia
                        Return True
                    Case Else
                        Return False
                End Select
            End Function
        End Class

    End Class
End Namespace
