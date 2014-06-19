Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Friend Class SyntaxTokenReplacer

        Friend Shared Function Replace(Of TRoot As SyntaxNode)(root As TRoot, oldToken As SyntaxToken, newToken As SyntaxToken) As TRoot
            If oldToken = newToken Then
                Return root
            End If

            Return DirectCast(New SingleTokenReplacer(oldToken, newToken).Visit(root), TRoot)
        End Function

        Friend Shared Function Replace(Of TRoot As SyntaxNode)(root As TRoot, oldTokens As IEnumerable(Of SyntaxToken), computeReplacementToken As Func(Of SyntaxToken, SyntaxToken, SyntaxToken)) As TRoot
            Dim oldTokensArray = oldTokens.ToArray()
            If oldTokensArray.Length = 0 Then
                Return root
            End If

            Return DirectCast(New MultipleTokenReplacer(oldTokensArray, computeReplacementToken).Visit(root), TRoot)
        End Function

        Private Class SingleTokenReplacer
            Inherits SyntaxRewriter

            Private ReadOnly oldToken As SyntaxToken

            Private ReadOnly newToken As SyntaxToken

            Private ReadOnly oldTokenFullSpan As TextSpan

            Public Sub New(oldToken As SyntaxToken, newToken As SyntaxToken)
                MyBase.New(oldToken.IsPartOfStructuredTrivia())
                Me.oldToken = oldToken
                Me.newToken = newToken
                Me.oldTokenFullSpan = oldToken.FullSpan
            End Sub

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                If token = Me.oldToken Then
                    Return Me.newToken
                End If

                If Me.VisitIntoStructuredTrivia AndAlso token.HasStructuredTrivia AndAlso token.FullSpan.IntersectsWith(Me.oldTokenFullSpan) Then
                    Return MyBase.VisitToken(token)
                End If

                Return token
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node IsNot Nothing Then
                    If node.FullSpan.IntersectsWith(Me.oldTokenFullSpan) Then
                        Return MyBase.Visit(node)
                    End If
                End If

                Return node
            End Function
        End Class

        Private Class MultipleTokenReplacer
            Inherits SyntaxRewriter

            Private ReadOnly tokens As SyntaxToken()

            Private ReadOnly tokenSet As HashSet(Of SyntaxToken)

            Private ReadOnly totalSpan As TextSpan

            Private ReadOnly computeReplacementToken As Func(Of SyntaxToken, SyntaxToken, SyntaxToken)

            Public Sub New(tokens As SyntaxToken(), computeReplacementToken As Func(Of SyntaxToken, SyntaxToken, SyntaxToken))
                MyBase.New(tokens.Any(Function(t) t.IsPartOfStructuredTrivia()))
                Me.tokens = tokens
                Me.tokenSet = New HashSet(Of SyntaxToken)(Me.tokens)
                Me.totalSpan = ComputeTotalSpan(Me.tokens)
                Me.computeReplacementToken = computeReplacementToken
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node IsNot Nothing Then
                    If Me.ShouldVisit(node.FullSpan) Then
                        Return MyBase.Visit(node)
                    End If
                End If

                Return node
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Dim result = token

                If Me.VisitIntoStructuredTrivia AndAlso token.HasStructuredTrivia AndAlso Me.ShouldVisit(token.FullSpan) Then
                    result = MyBase.VisitToken(token)
                End If

                If Me.tokenSet.Contains(token) Then
                    result = Me.computeReplacementToken(token, result)
                End If

                Return result
            End Function

            Private Shared Function ComputeTotalSpan(tokens As SyntaxToken()) As TextSpan
                Dim span0 = tokens(0).FullSpan
                Dim start As Integer = span0.Start
                Dim [end] As Integer = span0.End
                Dim i As Integer = 1

                While i < tokens.Length
                    Dim span = tokens(i).FullSpan
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

                For Each n In Me.tokens
                    If span.IntersectsWith(n.FullSpan) Then
                        Return True
                    End If
                Next

                Return False
            End Function
        End Class
    End Class
End Namespace
