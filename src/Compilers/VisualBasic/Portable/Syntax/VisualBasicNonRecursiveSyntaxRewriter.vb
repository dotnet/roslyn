' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Public MustInherit Class VisualBasicNonRecursiveSyntaxRewriter
        Inherits VisualBasicSyntaxVisitor(Of SyntaxNode)

        Private ReadOnly _undeconstructedStack As Stack(Of SyntaxNodeOrToken)
        Private ReadOnly _untransformedStack As Stack(Of UntransformedNode)
        Private ReadOnly _transformedStack As Stack(Of SyntaxNodeOrToken)
        Private ReadOnly _deconstructor As NodeDeconstructor
        Private ReadOnly _reassembler As NodeReassembler
        Private ReadOnly _trivializer As TriviaRewriter
        Private ReadOnly _children As List(Of SyntaxNodeOrToken)

        Protected Sub New(Optional visitIntoStructuredTrivia As Boolean = False)
            _undeconstructedStack = New Stack(Of SyntaxNodeOrToken)()
            _transformedStack = New Stack(Of SyntaxNodeOrToken)()
            _untransformedStack = New Stack(Of UntransformedNode)()
            _deconstructor = New NodeDeconstructor()
            _reassembler = New NodeReassembler()
            _trivializer = New TriviaRewriter(Me, visitIntoStructuredTrivia)
            _children = New List(Of SyntaxNodeOrToken)()
        End Sub

        Private _original As SyntaxNode
        Protected Property Original() As SyntaxNode
            Get
                Return _original
            End Get
            Private Set(value As SyntaxNode)
                _original = value
            End Set
        End Property

        Protected Overridable Function ShouldRewriteChildren(nodeOrToken As SyntaxNodeOrToken, ByRef rewritten As SyntaxNodeOrToken) As Boolean
            Return True
        End Function

        Protected Overridable Function ShouldRewriteTrivia(trivia As SyntaxTrivia, ByRef rewritten As SyntaxTrivia) As Boolean
            Return True
        End Function

        Public Overridable Function VisitNode(Original As SyntaxNode, rewritten As SyntaxNode) As SyntaxNode
            Return DirectCast(rewritten, VisualBasicSyntaxNode).Accept(Me)
        End Function

        Public Overridable Function VisitToken(original As SyntaxToken, rewritten As SyntaxToken) As SyntaxToken
            Return rewritten
        End Function

        Public Overridable Function VisitTrivia(original As SyntaxTrivia, rewritten As SyntaxTrivia) As SyntaxTrivia
            Return rewritten
        End Function

        Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
            Return Me.Rewrite(node)
        End Function

        Public Overrides Function DefaultVisit(node As SyntaxNode) As SyntaxNode
            Return node
        End Function

        Protected Function Rewrite(node As SyntaxNode) As SyntaxNode
            Dim undeconstructedStart = _undeconstructedStack.Count
            Dim untransformedStart = _untransformedStack.Count
            Dim transformedStart = _transformedStack.Count

            ' add initial node so we have something to work on
            _undeconstructedStack.Push(node)

            ' as long as there Is more to deconstruct, there Is more work to do
            While _undeconstructedStack.Count > undeconstructedStart
                Dim nodeOrToken = _undeconstructedStack.Pop()
                If nodeOrToken.IsNode Then
                    node = nodeOrToken.AsNode()

                    If node Is Nothing Then
                        ' nulls just stay nulls, they don't get transformed
                        _transformedStack.Push(nodeOrToken)
                    Else
                        Dim rewritten As SyntaxNodeOrToken
                        If Me.ShouldRewriteChildren(node, rewritten) Then
                            ' deconstruct node into child elements
                            _children.Clear()
                            _deconstructor.Deconstruct(node, _children)

                            ' add child elements to undeconstructed stack in reverse order so
                            ' the first child gets operated on next
                            For i = _children.Count - 1 To 0 Step -1
                                _undeconstructedStack.Push(_children(i))
                            Next

                            ' remember the node that will be tranformed later after the children are transformed
                            _untransformedStack.Push(New UntransformedNode(node, _children.Count, _transformedStack.Count))
                        Else
                            _transformedStack.Push(rewritten)
                        End If
                    End If
                ElseIf nodeOrToken.IsToken Then
                    ' we can transform tokens immediately
                    Dim original = nodeOrToken.AsToken()
                    Dim rewritten As SyntaxNodeOrToken
                    If Me.ShouldRewriteChildren(original, rewritten) Then
                        Dim rewrittenToken = _trivializer.VisitToken(original) ' rewrite trivia
                        Dim transformed = Me.VisitToken(original, rewrittenToken)
                        _transformedStack.Push(transformed)
                    Else
                        _transformedStack.Push(rewritten)
                    End If
                End If

                ' transform any nodes that can be transformed now
                While _untransformedStack.Count > untransformedStart _
                    AndAlso _untransformedStack.Peek().HasAllChildrenOnStack(_transformedStack)
                    Dim untransformed = _untransformedStack.Pop()

                    ' gather transformed children for this node
                    _children.Clear()
                    For i = 0 To untransformed.ChildCount - 1
                        _children.Add(_transformedStack.Pop())
                    Next

                    _children.Reverse()

                    ' reassemble original node with tranformed children
                    Dim rewritten = _reassembler.Reassemble(untransformed.Node, _children)

                    ' now tranform the node
                    Dim save = Me.Original
                    Me.Original = untransformed.Node
                    Dim transformed = Me.VisitNode(untransformed.Node, rewritten.AsNode())
                    Me.Original = save

                    ' add newly transformed node to the transformed stack
                    _transformedStack.Push(transformed)
                End While
            End While

            Debug.Assert(_untransformedStack.Count = untransformedStart)
            Debug.Assert(_transformedStack.Count = transformedStart + 1)

            Return _transformedStack.Pop().AsNode()
        End Function

        Private Structure UntransformedNode
            Public ReadOnly Property Node As SyntaxNode
            Public ReadOnly Property ChildCount As Integer ' the number Of children the original had, that we need To have 
            Public ReadOnly Property TransformedStackStart As Integer ' the transformed stack top When the untransformed node was created

            Public Sub New(node As SyntaxNode, childCount As Integer, transformedStackStart As Integer)
                Me.Node = node
                Me.ChildCount = childCount
                Me.TransformedStackStart = transformedStackStart
            End Sub

            Public Function HasAllChildrenOnStack(transformedStack As Stack(Of SyntaxNodeOrToken)) As Boolean
                Return Me.TransformedStackStart + Me.ChildCount = transformedStack.Count
            End Function
        End Structure

        Private Class NodeDeconstructor
            Inherits VisualBasicSyntaxRewriter

            Private _elements As List(Of SyntaxNodeOrToken)

            Public Sub Deconstruct(node As SyntaxNode, elements As List(Of SyntaxNodeOrToken))
                _elements = elements
                DirectCast(node, VisualBasicSyntaxNode).Accept(Me)
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                _elements.Add(node)
                Return node
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                _elements.Add(token)
                Return token
            End Function
        End Class

        Private Class NodeReassembler
            Inherits VisualBasicSyntaxRewriter

            Private _elements As List(Of SyntaxNodeOrToken)
            Private _index As Integer

            Public Function Reassemble(original As SyntaxNodeOrToken, rewrittenElements As List(Of SyntaxNodeOrToken)) As SyntaxNodeOrToken
                _elements = rewrittenElements
                _index = 0
                Return DirectCast(original.AsNode(), VisualBasicSyntaxNode).Accept(Me)
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                Dim value = _elements(_index).AsNode()
                _index += 1
                Return value
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Dim value = _elements(_index).AsToken()
                _index += 1
                Return value
            End Function
        End Class

        Private Class TriviaRewriter
            Inherits VisualBasicSyntaxRewriter

            Private _parent As VisualBasicNonRecursiveSyntaxRewriter

            Public Sub New(parent As VisualBasicNonRecursiveSyntaxRewriter, visitIntoStructuredTrivia As Boolean)
                MyBase.New(visitIntoStructuredTrivia)

                _parent = parent
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                ' allow recursion for structured trivia nodes (this Is only happens once, maybe twice..)
                Return _parent.Rewrite(node)
            End Function

            Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                Dim rewritten As SyntaxTrivia
                If _parent.ShouldRewriteTrivia(trivia, rewritten) Then
                    rewritten = MyBase.VisitTrivia(trivia)
                    Return _parent.VisitTrivia(trivia, rewritten)
                End If

                Return rewritten
            End Function
        End Class
    End Class
End Namespace