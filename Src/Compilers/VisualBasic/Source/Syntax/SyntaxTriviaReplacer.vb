Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Friend Class SyntaxTriviaReplacer

        Friend Shared Function Replace(Of TRoot As SyntaxNode)(root As TRoot, oldTrivia As SyntaxTrivia, newTrivia As SyntaxTriviaList) As TRoot
            If oldTrivia = newTrivia Then
                Return root
            End If

            Return DirectCast(New SingleTriviaReplacer(oldTrivia, newTrivia).Visit(root), TRoot)
        End Function

        Friend Shared Function Replace(token As SyntaxToken, oldTrivia As SyntaxTrivia, newTrivia As SyntaxTriviaList) As SyntaxToken
            If oldTrivia = newTrivia Then
                Return token
            End If

            Return New SingleTriviaReplacer(oldTrivia, newTrivia).VisitStartToken(token)
        End Function

        Friend Shared Function Replace(Of TRoot As SyntaxNode)(root As TRoot, oldTrivia As IEnumerable(Of SyntaxTrivia), computeReplacementTrivia As Func(Of SyntaxTrivia, SyntaxTrivia, SyntaxTriviaList)) As TRoot
            Dim oldTriviaArray = oldTrivia.ToArray()
            If oldTriviaArray.Length = 0 Then
                Return root
            End If

            Return DirectCast(New MultipleTriviaReplacer(oldTriviaArray, computeReplacementTrivia).Visit(root), TRoot)
        End Function

        Friend Shared Function Replace(token As SyntaxToken, oldTrivia As IEnumerable(Of SyntaxTrivia), computeReplacementTrivia As Func(Of SyntaxTrivia, SyntaxTrivia, SyntaxTriviaList)) As SyntaxToken
            Dim oldTriviaArray = oldTrivia.ToArray()
            If oldTriviaArray.Length = 0 Then
                Return token
            End If

            Return New MultipleTriviaReplacer(oldTriviaArray, computeReplacementTrivia).VisitStartToken(token)
        End Function

        Private Class SingleTriviaReplacer
            Inherits SyntaxRewriter

            Private ReadOnly oldTrivia As SyntaxTrivia

            Private ReadOnly newTrivia As SyntaxTriviaList

            Private ReadOnly oldTriviaFullSpan As TextSpan

            Public Sub New(oldTrivia As SyntaxTrivia, newTrivia As SyntaxTriviaList)
                MyBase.New(oldTrivia.IsPartOfStructuredTrivia())
                Me.oldTrivia = oldTrivia
                Me.newTrivia = newTrivia
                Me.oldTriviaFullSpan = oldTrivia.FullSpan
            End Sub

            Public Function VisitStartToken(token As SyntaxToken) As SyntaxToken
                Return Me.VisitToken(token)
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                If token.FullSpan.IntersectsWith(Me.oldTriviaFullSpan) Then
                    Dim leading = token.LeadingTrivia
                    Dim trailing = token.TrailingTrivia
                    If Me.oldTriviaFullSpan.Start < token.Span.Start Then
                        token = token.WithLeadingTrivia(Me.VisitList(leading))
                    Else
                        token = token.WithTrailingTrivia(Me.VisitList(trailing))
                    End If
                End If

                Return token
            End Function

            Public Overrides Function VisitListElement(trivia As SyntaxTrivia) As SyntaxTriviaList
                If trivia = Me.oldTrivia Then
                    Return Me.newTrivia
                End If

                If Me.VisitIntoStructuredTrivia AndAlso trivia.FullSpan.IntersectsWith(Me.oldTriviaFullSpan) Then
                    Return MyBase.VisitTrivia(trivia)
                End If

                Return trivia
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node IsNot Nothing Then
                    If node.FullSpan.IntersectsWith(Me.oldTriviaFullSpan) Then
                        Return MyBase.Visit(node)
                    End If
                End If

                Return node
            End Function
        End Class

        Private Class MultipleTriviaReplacer
            Inherits SyntaxRewriter

            Private ReadOnly trivia As SyntaxTrivia()

            Private ReadOnly triviaSet As HashSet(Of SyntaxTrivia)

            Private ReadOnly totalSpan As TextSpan

            Private ReadOnly computeReplacementTrivia As Func(Of SyntaxTrivia, SyntaxTrivia, SyntaxTriviaList)

            Public Sub New(trivia As SyntaxTrivia(), computeReplacementTrivia As Func(Of SyntaxTrivia, SyntaxTrivia, SyntaxTriviaList))
                MyBase.New(trivia.Any(Function(t) t.IsPartOfStructuredTrivia()))
                Me.trivia = trivia
                Me.triviaSet = New HashSet(Of SyntaxTrivia)(Me.trivia)
                Me.totalSpan = ComputeTotalSpan(Me.trivia)
                Me.computeReplacementTrivia = computeReplacementTrivia
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node IsNot Nothing Then
                    If Me.ShouldVisit(node.FullSpan) Then
                        Return MyBase.Visit(node)
                    End If
                End If

                Return node
            End Function

            Public Function VisitStartToken(token As SyntaxToken) As SyntaxToken
                Return Me.VisitToken(token)
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                If Me.ShouldVisit(token.FullSpan) Then
                    Return MyBase.VisitToken(token)
                End If

                Return token
            End Function

            Public Overrides Function VisitListElement(trivia As SyntaxTrivia) As SyntaxTriviaList
                Dim result = trivia
                If Me.VisitIntoStructuredTrivia AndAlso trivia.HasStructure AndAlso Me.ShouldVisit(trivia.FullSpan) Then
                    result = MyBase.VisitTrivia(trivia)
                End If

                If Me.triviaSet.Contains(trivia) Then
                    Return Me.computeReplacementTrivia(trivia, result)
                End If

                Return result
            End Function

            Private Shared Function ComputeTotalSpan(trivia As SyntaxTrivia()) As TextSpan
                Dim span0 = trivia(0).FullSpan
                Dim start As Integer = span0.Start
                Dim [end] As Integer = span0.End
                Dim i As Integer = 1

                While i < trivia.Length
                    Dim span = trivia(i).FullSpan
                    start = Math.Min(start, span.Start)
                    [end] = Math.Max([end], span.End)
                    i = i + 1
                End While

                Return New TextSpan(start, [end] - start)
            End Function

            Private Function ShouldVisit(span As TextSpan) As Boolean
                If Not span.IntersectsWith(Me.totalSpan) Then
                    Return False
                End If

                For Each n In Me.trivia
                    If span.IntersectsWith(n.FullSpan) Then
                        Return True
                    End If
                Next

                Return False
            End Function
        End Class
    End Class

End Namespace