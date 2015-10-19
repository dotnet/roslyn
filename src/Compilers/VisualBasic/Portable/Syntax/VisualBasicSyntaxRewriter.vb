' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Represents a <see cref="VisualBasicSyntaxVisitor"/> which descends an entire <see cref="SyntaxNode"/> graph and
    ''' may replace or remove visited SyntaxNodes in depth-first order.
    ''' </summary>
    Partial Public Class VisualBasicSyntaxRewriter
        Inherits VisualBasicSyntaxVisitor(Of SyntaxNode)

        Private ReadOnly _visitIntoStructuredTrivia As Boolean

        Public Sub New(Optional visitIntoStructuredTrivia As Boolean = False)
            Me._visitIntoStructuredTrivia = visitIntoStructuredTrivia
        End Sub

        Public Overridable ReadOnly Property VisitIntoStructuredTrivia As Boolean
            Get
                Return Me._visitIntoStructuredTrivia
            End Get
        End Property

        Private _recursionDepth As Integer

        Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
            If node IsNot Nothing Then
                _recursionDepth += 1

                StackGuard.EnsureSufficientExecutionStack(_recursionDepth)

                Dim result = DirectCast(node, VisualBasicSyntaxNode).Accept(Me)

                _recursionDepth -= 1
                Return result
            Else
                Return node
            End If
        End Function

        Public Overridable Function VisitToken(token As SyntaxToken) As SyntaxToken
            Dim leading = Me.VisitList(token.LeadingTrivia)
            Dim trailing = Me.VisitList(token.TrailingTrivia)
            If leading <> token.LeadingTrivia OrElse trailing <> token.TrailingTrivia Then
                If leading <> token.LeadingTrivia Then
                    token = token.WithLeadingTrivia(leading)
                End If
                If trailing <> token.TrailingTrivia Then
                    token = token.WithTrailingTrivia(trailing)
                End If
            End If

            Return token
        End Function

        Public Overridable Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
            If Me.VisitIntoStructuredTrivia AndAlso trivia.HasStructure Then
                Dim [structure] = DirectCast(trivia.GetStructure(), VisualBasicSyntaxNode)
                Dim newStructure = DirectCast(Me.Visit([structure]), StructuredTriviaSyntax)
                If newStructure IsNot [structure] Then
                    If newStructure IsNot Nothing Then
                        Return SyntaxFactory.Trivia(newStructure)
                    Else
                        Return Nothing
                    End If
                End If
            End If
            Return trivia
        End Function

        Public Overridable Function VisitList(Of TNode As SyntaxNode)(list As SyntaxList(Of TNode)) As SyntaxList(Of TNode)
            Dim alternate As SyntaxListBuilder = Nothing
            Dim i = 0, n = list.Count
            While i < n
                Dim item = list(i)
                Dim visited = Me.VisitListElement(item)
                If item IsNot visited AndAlso alternate Is Nothing Then
                    alternate = New SyntaxListBuilder(n)
                    alternate.AddRange(list, 0, i)
                End If

                If alternate IsNot Nothing AndAlso visited IsNot Nothing AndAlso visited.Kind <> SyntaxKind.None Then
                    alternate.Add(visited)
                End If

                i = i + 1
            End While

            If alternate IsNot Nothing Then
                Return alternate.ToList(Of TNode)()
            End If

            Return list
        End Function

        Public Overridable Function VisitListElement(Of TNode As SyntaxNode)(node As TNode) As TNode
            Return DirectCast(Me.Visit(node), TNode)
        End Function

        Public Overridable Function VisitList(list As SyntaxTokenList) As SyntaxTokenList
            Dim alternate As SyntaxTokenListBuilder = Nothing
            Dim i = -1, n = list.Count
            For Each item In list
                i = i + 1

                Dim visited = Me.VisitListElement(item)
                If item <> visited AndAlso alternate Is Nothing Then
                    alternate = New SyntaxTokenListBuilder(n)
                    alternate.Add(list, 0, i)
                End If

                If alternate IsNot Nothing AndAlso visited.Kind <> SyntaxKind.None Then ' skip the null check since SyntaxToken is a value type
                    alternate.Add(visited)
                End If
            Next

            If alternate IsNot Nothing Then
                Return alternate.ToList()
            End If

            Return list
        End Function

        Public Overridable Function VisitListElement(token As SyntaxToken) As SyntaxToken
            Return Me.VisitToken(token)
        End Function

        Public Overridable Function VisitList(Of TNode As SyntaxNode)(list As SeparatedSyntaxList(Of TNode)) As SeparatedSyntaxList(Of TNode)
            Dim count As Integer = list.Count
            Dim sepCount As Integer = list.SeparatorCount
            Dim alternate As SeparatedSyntaxListBuilder(Of TNode) = Nothing
            Dim i As Integer = 0

            While i < sepCount
                Dim node As TNode = list(i)
                Dim visitedNode As TNode = Me.VisitListElement(Of TNode)(node)
                Dim separator As SyntaxToken = list.GetSeparator(i)
                Dim visitedSeparator As SyntaxToken = Me.VisitListSeparator(separator)
                If alternate.IsNull Then
                    If node IsNot visitedNode OrElse separator <> visitedSeparator Then
                        alternate = New SeparatedSyntaxListBuilder(Of TNode)(count)
                        alternate.AddRange(list, i)
                    End If
                End If
                If Not alternate.IsNull Then
                    If visitedNode IsNot Nothing Then
                        alternate.Add(visitedNode)
                        If visitedSeparator.RawKind = 0 Then
                            Throw New InvalidOperationException("separator is expected")
                        End If
                        alternate.AddSeparator(visitedSeparator)
                    Else
                        If visitedNode Is Nothing Then
                            Throw New InvalidOperationException("element is expected")
                        End If
                    End If
                End If
                i += 1
            End While

            If i < count Then
                Dim node As TNode = list(i)
                Dim visitedNode As TNode = Me.VisitListElement(Of TNode)(node)
                If alternate.IsNull Then
                    If node IsNot visitedNode Then
                        alternate = New SeparatedSyntaxListBuilder(Of TNode)(count)
                        alternate.AddRange(list, i)
                    End If
                End If
                If Not alternate.IsNull AndAlso visitedNode IsNot Nothing Then
                    alternate.Add(visitedNode)
                End If
            End If

            If Not alternate.IsNull Then
                Return alternate.ToList()
            End If

            Return list
        End Function

        Public Overridable Function VisitListSeparator(token As SyntaxToken) As SyntaxToken
            Return Me.VisitToken(token)
        End Function

        Public Overridable Function VisitList(list As SyntaxTriviaList) As SyntaxTriviaList
            Dim count = list.Count
            If count <> 0 Then
                Dim alternate As SyntaxTriviaListBuilder = Nothing
                Dim index = -1

                For Each item In list
                    index += 1
                    Dim visited = Me.VisitListElement(item)

                    'skip the null check since SyntaxTrivia Is a value type
                    If visited <> item AndAlso alternate Is Nothing Then
                        alternate = New SyntaxTriviaListBuilder(count)
                        alternate.Add(list, 0, index)
                    End If

                    If alternate IsNot Nothing AndAlso visited.Kind() <> SyntaxKind.None Then
                        alternate.Add(visited)
                    End If
                Next

                If alternate IsNot Nothing Then
                    Return alternate.ToList()
                End If
            End If

            Return list
        End Function

        Public Overridable Function VisitListElement(element As SyntaxTrivia) As SyntaxTrivia
            Return Me.VisitTrivia(element)
        End Function

    End Class
End Namespace
