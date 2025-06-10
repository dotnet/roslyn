' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SyntaxAnnotationTests

#Region "Boundary Tests"

        <Fact()>
        Public Sub TestEmpty()
            Dim code = ""
            Dim tree = VisualBasicSyntaxTree.ParseText(code)

            TestAnnotation(tree)
        End Sub

        <Fact()>
        Public Sub TestAddAnnotationToNullSyntaxToken()
            Dim annotation = New SyntaxAnnotation()
            Dim oldToken As SyntaxToken = Nothing
            Dim newToken = oldToken.WithAdditionalAnnotations(annotation)
            Assert.False(newToken.ContainsAnnotations)
        End Sub

        <Fact()>
        Public Sub TestAddAnnotationToNullSyntaxTrivia()
            Dim annotation = New SyntaxAnnotation()
            Dim oldTrivia As SyntaxTrivia = Nothing
            Dim newTrivia = oldTrivia.WithAdditionalAnnotations(annotation)
            Assert.False(newTrivia.ContainsAnnotations)
        End Sub

        <Fact()>
        Public Sub TestCopyAnnotationToNullSyntaxNode()
            Dim fromNode = VisualBasicSyntaxTree.ParseText(_helloWorldCode).GetRoot()
            Dim toNode As VisualBasicSyntaxNode = Nothing
            Dim annotatedNode = fromNode.CopyAnnotationsTo(toNode)
            Assert.Null(annotatedNode)
        End Sub

        <Fact()>
        Public Sub TestCopyAnnotationOfZeroLengthToSyntaxNode()
            Dim fromNode = VisualBasicSyntaxTree.ParseText(_helloWorldCode).GetRoot()
            Dim toNode = VisualBasicSyntaxTree.ParseText(_helloWorldCode).GetRoot()
            Dim annotatedNode = fromNode.CopyAnnotationsTo(toNode)
            Assert.Equal(annotatedNode, toNode)
            ' Reference Equal
        End Sub

        <Fact()>
        Public Sub TestCopyAnnotationFromNullSyntaxToken()
            Dim fromToken As SyntaxToken = Nothing
            Dim toToken = VisualBasicSyntaxTree.ParseText(_helloWorldCode).GetRoot().DescendantTokens().First()
            Dim annotatedToken = fromToken.CopyAnnotationsTo(toToken)
            Assert.True(annotatedToken.IsEquivalentTo(toToken))
        End Sub

        <Fact()>
        Public Sub TestCopyAnnotationToNullSyntaxToken()
            Dim fromToken = VisualBasicSyntaxTree.ParseText(_helloWorldCode).GetRoot().DescendantTokens().First()
            Dim toToken As SyntaxToken = Nothing
            Dim annotatedToken = fromToken.CopyAnnotationsTo(toToken)
            Assert.True(annotatedToken.IsEquivalentTo(toToken))
        End Sub

        <Fact()>
        Public Sub TestCopyAnnotationOfZeroLengthToSyntaxToken()
            Dim fromToken = VisualBasicSyntaxTree.ParseText(_helloWorldCode).GetRoot().DescendantTokens().First()
            Dim toToken = VisualBasicSyntaxTree.ParseText(_helloWorldCode).GetRoot().DescendantTokens().First()
            Dim annotatedToken = fromToken.CopyAnnotationsTo(toToken)
            Assert.Equal(annotatedToken, toToken)
            ' Reference Equal
        End Sub

        <Fact()>
        Public Sub TestCopyAnnotationFromNullSyntaxTrivia()
            Dim fromTrivia As SyntaxTrivia = Nothing
            Dim tree = VisualBasicSyntaxTree.ParseText(_helloWorldCode)
            Dim toTrivia = GetAllTrivia(tree.GetRoot()).FirstOrDefault()
            Dim annotatedTrivia = fromTrivia.CopyAnnotationsTo(toTrivia)
            Assert.True(annotatedTrivia.IsEquivalentTo(toTrivia))
        End Sub

        <Fact()>
        Public Sub TestCopyAnnotationToNullSyntaxTrivia()
            Dim toTrivia As SyntaxTrivia = Nothing
            Dim tree = VisualBasicSyntaxTree.ParseText(_helloWorldCode)
            Dim fromTrivia = GetAllTrivia(tree.GetRoot()).FirstOrDefault()
            Dim annotatedTrivia = fromTrivia.CopyAnnotationsTo(toTrivia)
            Assert.True(annotatedTrivia.IsEquivalentTo(toTrivia))
        End Sub

        <Fact()>
        Public Sub TestCopyAnnotationOfZeroLengthToSyntaxTrivia()
            Dim tree = VisualBasicSyntaxTree.ParseText(_helloWorldCode)
            Dim fromTrivia = GetAllTrivia(tree.GetRoot()).FirstOrDefault()
            Dim toTrivia = GetAllTrivia(tree.GetRoot()).FirstOrDefault()
            Dim annotatedTrivia = fromTrivia.CopyAnnotationsTo(toTrivia)
            Assert.Equal(annotatedTrivia, toTrivia)
            ' Reference Equal
        End Sub

#End Region

#Region "Negative Tests"

        <Fact()>
        Public Sub TestMissingAnnotationsOnNodesOrTokens()
            Dim annotation As New SyntaxAnnotation()
            Dim tree = VisualBasicSyntaxTree.ParseText(_allInOneVisualBasicCode)

            Dim matchingNodesOrTokens = tree.GetRoot().GetAnnotatedNodesAndTokens(annotation)
            Assert.Empty(matchingNodesOrTokens)
        End Sub

        <Fact()>
        Public Sub TestMissingAnnotationsOnTrivia()
            Dim annotation As New SyntaxAnnotation()
            Dim tree = VisualBasicSyntaxTree.ParseText(_allInOneVisualBasicCode)

            Dim matchingTrivia = tree.GetRoot().GetAnnotatedTrivia(annotation)
            Assert.Empty(matchingTrivia)
        End Sub

#End Region

#Region "Other Functional Tests"

        <Fact()>
        Public Sub TestSimpleMultipleAnnotationsOnNode()
            Dim tree = VisualBasicSyntaxTree.ParseText(_helloWorldCode)
            Dim annotation1 As New SyntaxAnnotation()
            Dim annotation2 As New SyntaxAnnotation()

            ' Pick the first node from tree
            Dim node = GetAllNodesAndTokens(tree.GetRoot()).First(Function(t) t.IsNode).AsNode()

            ' Annotate it
            Dim annotatedNode = node.WithAdditionalAnnotations(annotation1)
            Dim newRoot = tree.GetRoot().ReplaceNode(node, annotatedNode)

            ' Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, node)

            ' Pick the annotated node from the new tree
            Dim node2 = newRoot.GetAnnotatedNodesAndTokens(annotation1).Single().AsNode()

            ' Annotate it again
            Dim twiceAnnotatedNode = node2.WithAdditionalAnnotations(annotation2)
            Dim twiceAnnotatedRoot = newRoot.ReplaceNode(node2, twiceAnnotatedNode)

            ' Verify the recent annotation
            TestAnnotation(annotation2, twiceAnnotatedRoot, node2)

            ' Verify both annotations exist in the newTree
            TestAnnotation(annotation1, twiceAnnotatedRoot, node)
            TestAnnotation(annotation2, twiceAnnotatedRoot, node)
        End Sub

        <Fact()>
        Public Sub TestSimpleMultipleAnnotationsOnToken()
            Dim tree = VisualBasicSyntaxTree.ParseText(_helloWorldCode)
            Dim annotation1 As New SyntaxAnnotation()
            Dim annotation2 As New SyntaxAnnotation()

            ' Pick the first node from tree
            Dim token = GetAllNodesAndTokens(tree.GetRoot()).First(Function(t) t.IsToken).AsToken()

            ' Annotate it
            Dim annotatedToken = token.WithAdditionalAnnotations(annotation1)
            Dim newRoot = tree.GetRoot().ReplaceToken(token, annotatedToken)

            ' Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, token)

            ' Pick the annotated node from the new tree
            Dim token2 = newRoot.GetAnnotatedNodesAndTokens(annotation1).Single().AsToken()

            ' Annotate it again
            Dim twiceAnnotatedToken = token2.WithAdditionalAnnotations(annotation2)
            Dim twiceAnnotatedRoot = newRoot.ReplaceToken(token2, twiceAnnotatedToken)

            ' Verify the recent annotation
            TestAnnotation(annotation2, twiceAnnotatedRoot, token2)

            ' Verify both annotations exist in the newTree
            TestAnnotation(annotation1, twiceAnnotatedRoot, token)
            TestAnnotation(annotation2, twiceAnnotatedRoot, token)
        End Sub

        <Fact()>
        Public Sub TestSimpleMultipleAnnotationsOnTrivia()
            Dim tree = VisualBasicSyntaxTree.ParseText(_helloWorldCode)
            Dim annotation1 As New SyntaxAnnotation()
            Dim annotation2 As New SyntaxAnnotation()

            ' Pick the first node from tree
            Dim trivia = GetAllTrivia(tree.GetRoot()).First()

            ' Annotate it
            Dim annotatedTrivia = trivia.WithAdditionalAnnotations(annotation1)
            Dim newRoot = tree.GetRoot().ReplaceTrivia(trivia, annotatedTrivia)

            ' Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, trivia)

            ' Pick the annotated node from the new tree
            Dim trivia2 = newRoot.GetAnnotatedTrivia(annotation1).Single()

            ' Annotate it again
            Dim twiceAnnotatedTrivia = trivia2.WithAdditionalAnnotations(annotation2)
            Dim twiceAnnotatedRoot = newRoot.ReplaceTrivia(trivia2, twiceAnnotatedTrivia)

            ' Verify the recent annotation
            TestAnnotation(annotation2, twiceAnnotatedRoot, trivia2)

            ' Verify both annotations exist in the newTree
            TestAnnotation(annotation1, twiceAnnotatedRoot, trivia)
            TestAnnotation(annotation2, twiceAnnotatedRoot, trivia)
        End Sub

        <Fact()>
        Public Sub TestMultipleAnnotationsOnAllNodesTokensAndTrivia()
            Dim tree = VisualBasicSyntaxTree.ParseText(_helloWorldCode)
            Dim newRoot = tree.GetRoot()

            Dim annotations = New List(Of SyntaxAnnotation)(Enumerable.Range(0, 3).Select(Function(x)
                                                                                              Return New SyntaxAnnotation()
                                                                                          End Function))

            ' add annotation one by one to every single node, token, trivia
            For Each annotation In annotations
                Dim rewriter = New InjectAnnotationRewriter(annotation)
                newRoot = DirectCast(rewriter.Visit(newRoot), VisualBasicSyntaxNode)
            Next

            ' Verify that all annotations are present in whichever places they were added
            TestMultipleAnnotationsInTree(tree.GetRoot(), newRoot, annotations)
        End Sub

        <Fact()>
        Public Sub TestAnnotationOnEveryNodeTokenTriviaOfHelloWorld()
            Dim tree = VisualBasicSyntaxTree.ParseText(_helloWorldCode)

            TestAnnotation(tree)
            TestTriviaAnnotation(tree)
        End Sub

        <Fact()>
        Public Sub TestIfNodeHasAnnotations()
            Dim tree = VisualBasicSyntaxTree.ParseText(_helloWorldCode)
            Dim annotation1 As New SyntaxAnnotation()

            ' Pick the first node from tree
            Dim firstNode = GetAllNodesAndTokens(tree.GetRoot()).First(Function(t) t.IsNode).AsNode()

            Dim children = firstNode.ChildNodesAndTokens()
            Dim lastChildOfFirstNode = Enumerable.Last(children, Function(t) t.IsNode).AsNode()
            Dim annotatedNode = lastChildOfFirstNode.WithAdditionalAnnotations(annotation1)
            Dim newRoot = tree.GetRoot().ReplaceNode(lastChildOfFirstNode, annotatedNode)

            ' Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, lastChildOfFirstNode)

            ' Pick the first node from new tree and see if any of its children is annotated
            Dim firstNodeInNewTree = GetAllNodesAndTokens(newRoot).First(Function(t) t.IsNode).AsNode()
            Assert.True(firstNodeInNewTree.ContainsAnnotations)

            ' Pick the node which was annotated and see if it has the annotation
            Dim rightNode = Enumerable.Last(firstNodeInNewTree.ChildNodesAndTokens(), Function(t) t.IsNode).AsNode()
            Assert.NotNull(rightNode.GetAnnotations().Single())
        End Sub

        <Fact()>
        Public Sub TestVisualBasicAllInOne()
            Dim tree = VisualBasicSyntaxTree.ParseText(_allInOneVisualBasicCode)

            TestAnnotation(tree)
        End Sub

        <Fact()>
        Public Sub TestRandomAnnotations1()
            Dim tree = VisualBasicSyntaxTree.ParseText(_allInOneVisualBasicCode)

            TestRandomAnnotations(tree)
        End Sub

        <Fact()>
        Public Sub TestManyRandomAnnotations1()
            Dim tree = VisualBasicSyntaxTree.ParseText(_allInOneVisualBasicCode)

            TestManyRandomAnnotations(tree)
        End Sub

        <Fact()>
        Public Sub TestVisualBasicAllInOneTrivia()
            Dim tree = VisualBasicSyntaxTree.ParseText(_allInOneVisualBasicCode)

            TestTriviaAnnotation(tree)
        End Sub

        <Fact()>
        Public Sub TestCopyAnnotations1()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(_allInOneVisualBasicCode)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(_allInOneVisualBasicCode)

            TestCopyAnnotations(tree1, tree2)
        End Sub

#End Region

        Private Sub TestMultipleAnnotationsInTree(oldRoot As SyntaxNode, newRoot As SyntaxNode, annotations As List(Of SyntaxAnnotation))
            For Each annotation In annotations
                ' Verify annotations in Nodes or Tokens
                Dim annotatedNodesOrTokens = newRoot.GetAnnotatedNodesAndTokens(annotation).OrderBy(Function(x) x.SpanStart)
                Dim actualNodesOrTokens = GetAllNodesAndTokens(oldRoot).OrderBy(Function(x) x.SpanStart)

                Assert.Equal(annotatedNodesOrTokens.Count(), actualNodesOrTokens.Count())

                For index = 0 To actualNodesOrTokens.Count() - 1
                    Dim oldNode = actualNodesOrTokens.ElementAt(index)
                    Dim annotatedNode = annotatedNodesOrTokens.ElementAt(index)
                    Assert.Equal(oldNode.FullSpan, annotatedNode.FullSpan)
                    Assert.Equal(oldNode.Span, annotatedNode.Span)
                    Assert.True(oldNode.IsEquivalentTo(annotatedNode))
                Next

                ' Verify annotations in Trivia
                Dim annotatedTrivia = newRoot.GetAnnotatedTrivia(annotation).OrderBy(Function(x) x.SpanStart)
                Dim actualTrivia = GetAllTrivia(oldRoot).OrderBy(Function(x) x.SpanStart)

                Assert.Equal(annotatedTrivia.Count(), actualTrivia.Count())

                For index = 0 To actualTrivia.Count - 1
                    Dim oldTrivia = actualTrivia.ElementAt(index)
                    Dim newTrivia = annotatedTrivia.ElementAt(index)
                    Assert.Equal(oldTrivia.FullSpan, newTrivia.FullSpan)
                    Assert.Equal(oldTrivia.Span, newTrivia.Span)
                    Assert.True(oldTrivia.IsEquivalentTo(newTrivia))
                Next
            Next
        End Sub

        Private Sub TestCopyAnnotations(tree1 As SyntaxTree, tree2 As SyntaxTree)
            ' create 10 annotations
            Dim annotations = New List(Of SyntaxAnnotation)(Enumerable.Range(0, 10).Select(Function(s) New SyntaxAnnotation()))

            ' add a random annotation to every single node, token, trivia
            Dim rewriter = New InjectRandomAnnotationRewriter(annotations)
            Dim sourceTreeRoot = DirectCast(rewriter.Visit(tree1.GetRoot()), VisualBasicSyntaxNode)

            Dim destTreeRoot = CopyAnnotationsTo(sourceTreeRoot, tree2.GetRoot())

            ' now we have two tree with same annotation everywhere
            ' verify that
            For Each annotation In annotations
                ' verify annotation at nodes and tokens
                Dim sourceNodeOrTokens = sourceTreeRoot.GetAnnotatedNodesAndTokens(annotation)
                Dim sourceNodeOrTokenEnumerator = sourceNodeOrTokens.GetEnumerator()

                Dim destNodeOrTokens = destTreeRoot.GetAnnotatedNodesAndTokens(annotation)
                Dim destNodeOrTokenEnumerator = destNodeOrTokens.GetEnumerator()

                Assert.Equal(sourceNodeOrTokens.Count(), destNodeOrTokens.Count())

                Do While sourceNodeOrTokenEnumerator.MoveNext() AndAlso destNodeOrTokenEnumerator.MoveNext()
                    Assert.True(sourceNodeOrTokenEnumerator.Current.IsEquivalentTo(destNodeOrTokenEnumerator.Current))
                Loop

                ' verify annotation at trivia
                Dim sourceTrivia = sourceTreeRoot.GetAnnotatedTrivia(annotation)
                Dim destTrivia = destTreeRoot.GetAnnotatedTrivia(annotation)

                Dim sourceTriviaEnumerator = sourceTrivia.GetEnumerator()
                Dim destTriviaEnumerator = destTrivia.GetEnumerator()

                Assert.Equal(sourceTrivia.Count(), destTrivia.Count())

                Do While sourceTriviaEnumerator.MoveNext() AndAlso destTriviaEnumerator.MoveNext()
                    Assert.True(sourceTriviaEnumerator.Current.IsEquivalentTo(destTriviaEnumerator.Current))
                Loop
            Next annotation
        End Sub

        Private Function CopyAnnotationsTo(sourceTreeRoot As SyntaxNode, destTreeRoot As SyntaxNode) As SyntaxNode
            ' now I have a tree that has annotation at every node/token/trivia
            ' copy all those annotation to tree2 and create map from old one to new one

            Dim sourceTreeNodeOrTokenEnumerator = GetAllNodesAndTokens(sourceTreeRoot).GetEnumerator()
            Dim destTreeNodeOrTokenEnumerator = GetAllNodesAndTokens(destTreeRoot).GetEnumerator()

            Dim nodeOrTokenMap = New Dictionary(Of SyntaxNodeOrToken, SyntaxNodeOrToken)()
            Do While sourceTreeNodeOrTokenEnumerator.MoveNext() AndAlso destTreeNodeOrTokenEnumerator.MoveNext()
                If sourceTreeNodeOrTokenEnumerator.Current.IsNode Then
                    Dim oldNode = destTreeNodeOrTokenEnumerator.Current.AsNode()
                    Dim newNode = sourceTreeNodeOrTokenEnumerator.Current.AsNode().CopyAnnotationsTo(oldNode)
                    nodeOrTokenMap.Add(oldNode, newNode)
                ElseIf sourceTreeNodeOrTokenEnumerator.Current.IsToken Then
                    Dim oldToken = destTreeNodeOrTokenEnumerator.Current.AsToken()
                    Dim newToken = sourceTreeNodeOrTokenEnumerator.Current.AsToken().CopyAnnotationsTo(oldToken)
                    nodeOrTokenMap.Add(oldToken, newToken)
                End If
            Loop

            ' copy annotations at trivia
            Dim sourceTreeTriviaEnumerator = GetAllTrivia(sourceTreeRoot).GetEnumerator()
            Dim destTreeTriviaEnumerator = GetAllTrivia(destTreeRoot).GetEnumerator()

            Dim triviaMap = New Dictionary(Of SyntaxTrivia, SyntaxTrivia)()
            Do While sourceTreeTriviaEnumerator.MoveNext() AndAlso destTreeTriviaEnumerator.MoveNext()
                Dim oldTrivia = destTreeTriviaEnumerator.Current
                Dim newTrivia = sourceTreeTriviaEnumerator.Current.CopyAnnotationsTo(oldTrivia)
                triviaMap.Add(oldTrivia, newTrivia)
            Loop

            Dim copier = New CopyAnnotationRewriter(nodeOrTokenMap, triviaMap)
            Return copier.Visit(destTreeRoot)
        End Function

        Private Sub TestManyRandomAnnotations(tree As SyntaxTree)
            ' inject annotations in random places and see whether it is preserved after tree transformation
            Dim annotations = New List(Of Tuple(Of SyntaxAnnotation, SyntaxNodeOrToken))()

            ' we give constant seed so that we get exact same sequence every time.
            Dim randomGenerator = New Random(0)

            Dim currentRoot = tree.GetRoot()
            Dim count = GetAllNodesAndTokens(currentRoot).Count

            ' add one in root
            Dim rootAnnotation = New SyntaxAnnotation()
            annotations.Add(Tuple.Create(rootAnnotation, New SyntaxNodeOrToken(currentRoot)))

            Dim rootAnnotated = AddAnnotationTo(rootAnnotation, currentRoot)
            currentRoot = Replace(currentRoot, currentRoot, rootAnnotated)

            For i As Integer = 0 To 19
                Dim annotation = New SyntaxAnnotation()
                Dim item = GetAllNodesAndTokens(currentRoot)(randomGenerator.Next(count - 1))

                ' save it
                annotations.Add(Tuple.Create(annotation, item))

                Dim annotated = AddAnnotationTo(annotation, item)
                currentRoot = Replace(currentRoot, item, annotated)

                TestAnnotations(annotations, currentRoot)
            Next i
        End Sub

        Private Sub TestRandomAnnotations(tree As SyntaxTree)
            ' inject annotations in random places and see whether it is preserved after tree transformation
            Dim firstAnnotation = New SyntaxAnnotation()
            Dim secondAnnotation = New SyntaxAnnotation()

            Dim candidatePool = GetAllNodesAndTokens(tree.GetRoot())
            Dim numberOfCandidates = candidatePool.Count

            ' we give constant seed so that we get exact same sequence every time.
            Dim randomGenerator = New Random(100)

            For i As Integer = 0 To 19
                Dim firstItem = candidatePool(randomGenerator.Next(numberOfCandidates - 1))
                Dim firstAnnotated = AddAnnotationTo(firstAnnotation, firstItem)

                Dim newRoot = Replace(tree.GetRoot(), firstItem, firstAnnotated)

                ' check the first annotation
                TestAnnotation(firstAnnotation, newRoot, firstItem)

                Dim secondItem = GetAllNodesAndTokens(newRoot)(randomGenerator.Next(numberOfCandidates - 1))
                Dim secondAnnotated = AddAnnotationTo(secondAnnotation, secondItem)

                ' transform the tree again
                newRoot = Replace(newRoot, secondItem, secondAnnotated)

                ' make sure both annotation are in the tree
                TestAnnotation(firstAnnotation, newRoot, firstItem)
                TestAnnotation(secondAnnotation, newRoot, secondItem)
            Next i
        End Sub

        Public Function Replace(Of TRoot As SyntaxNode)(root As TRoot, oldNodeOrToken As SyntaxNodeOrToken, newNodeOrToken As SyntaxNodeOrToken) As TRoot
            If oldNodeOrToken.IsToken Then
                Return root.ReplaceToken(oldNodeOrToken.AsToken(), newNodeOrToken.AsToken())
            End If

            Return root.ReplaceNode(oldNodeOrToken.AsNode(), newNodeOrToken.AsNode())
        End Function

        Public Function AddAnnotationTo(annotation As SyntaxAnnotation, nodeOrToken As SyntaxNodeOrToken) As SyntaxNodeOrToken
            Return nodeOrToken.WithAdditionalAnnotations(annotation)
        End Function

        Private Sub TestAnnotations(annotations As List(Of Tuple(Of SyntaxAnnotation, SyntaxNodeOrToken)), currentRoot As SyntaxNode)
            ' check every annotations
            For Each pair In annotations
                Dim annotation = pair.Item1
                Dim nodeOrToken = pair.Item2

                TestAnnotation(annotation, currentRoot, nodeOrToken)
            Next pair
        End Sub

        Private Sub TestTriviaAnnotation(tree As SyntaxTree)
            Dim annotation = New SyntaxAnnotation()

            For Each trivia In GetAllTrivia(tree.GetRoot())
                ' add one annotation and test its existence
                Dim newTrivia = trivia.WithAdditionalAnnotations(annotation)
                Dim newRoot = tree.GetRoot().ReplaceTrivia(trivia, newTrivia)

                TestAnnotation(annotation, newRoot, trivia)
            Next trivia
        End Sub

        Private Sub TestAnnotation(tree As SyntaxTree)
            Dim annotation = New SyntaxAnnotation()

            For Each nodeOrToken In GetAllNodesAndTokens(tree.GetRoot())
                Dim newRoot As SyntaxNode

                ' add one annotation and test its existence
                If nodeOrToken.IsToken Then
                    Dim newToken = nodeOrToken.AsToken().WithAdditionalAnnotations(annotation)
                    newRoot = tree.GetRoot().ReplaceToken(nodeOrToken.AsToken(), newToken)
                Else
                    Dim newNode = nodeOrToken.AsNode().WithAdditionalAnnotations(annotation)
                    newRoot = tree.GetRoot().ReplaceNode(nodeOrToken.AsNode(), newNode)
                End If

                TestAnnotation(annotation, newRoot, nodeOrToken)
            Next nodeOrToken
        End Sub

        Private Sub TestAnnotation(annotation As SyntaxAnnotation, root As SyntaxNode, oldNodeOrToken As SyntaxNodeOrToken)
            ' add one annotation and test its existence
            If oldNodeOrToken.IsToken Then
                TestAnnotation(annotation, root, oldNodeOrToken.AsToken())
                Return
            End If

            TestAnnotation(annotation, root, oldNodeOrToken.AsNode())
        End Sub

        Private Sub TestAnnotation(annotation As SyntaxAnnotation, root As SyntaxNode, oldNode As SyntaxNode)
            Dim results = root.GetAnnotatedNodesAndTokens(annotation)

            Assert.Equal(1, results.Count())

            Dim annotatedNode = results.Single().AsNode()

            ' try to check whether it is same node as old node.
            Assert.Equal(oldNode.FullSpan, annotatedNode.FullSpan)
            Assert.Equal(oldNode.Span, annotatedNode.Span)
            Assert.True(oldNode.IsEquivalentTo(annotatedNode))
        End Sub

        Private Sub TestAnnotation(annotation As SyntaxAnnotation, root As SyntaxNode, oldToken As SyntaxToken)
            Dim results = root.GetAnnotatedNodesAndTokens(annotation)

            Assert.Equal(1, results.Count())

            Dim annotatedToken = results.Single().AsToken()

            ' try to check whether it is same token as old token.
            Assert.Equal(oldToken.FullSpan, annotatedToken.FullSpan)
            Assert.Equal(oldToken.Span, annotatedToken.Span)
            Assert.True(oldToken.IsEquivalentTo(annotatedToken))
        End Sub

        Private Sub TestAnnotation(annotation As SyntaxAnnotation, root As SyntaxNode, oldTrivia As SyntaxTrivia)
            Dim results = root.GetAnnotatedTrivia(annotation)

            Assert.Equal(1, results.Count())

            Dim annotatedTrivia = results.Single()

            ' try to check whether it is same token as old token.
            Assert.Equal(oldTrivia.FullSpan, annotatedTrivia.FullSpan)
            Assert.Equal(oldTrivia.Span, annotatedTrivia.Span)
            Assert.True(oldTrivia.IsEquivalentTo(annotatedTrivia))
        End Sub

        Private Function GetAllTrivia(root As SyntaxNode) As List(Of SyntaxTrivia)
            Dim myCollector = New Collector()
            myCollector.Visit(root)

            Return myCollector.Trivia
        End Function

        Private Function GetAllNodesAndTokens(root As SyntaxNode) As List(Of SyntaxNodeOrToken)
            Dim myCollector = New Collector()
            myCollector.Visit(root)

            Return myCollector.NodeOrTokens
        End Function

        Private Class Collector
            Inherits VisualBasicSyntaxWalker
            Private _privateNodeOrTokens As List(Of SyntaxNodeOrToken)
            Public Property NodeOrTokens() As List(Of SyntaxNodeOrToken)
                Get
                    Return _privateNodeOrTokens
                End Get
                Private Set(value As List(Of SyntaxNodeOrToken))
                    _privateNodeOrTokens = value
                End Set
            End Property

            Private _privateTrivia As List(Of SyntaxTrivia)
            Public Property Trivia() As List(Of SyntaxTrivia)
                Get
                    Return _privateTrivia
                End Get
                Private Set(value As List(Of SyntaxTrivia))
                    _privateTrivia = value
                End Set
            End Property

            Public Sub New()
                MyBase.New(SyntaxWalkerDepth.StructuredTrivia)
                Me.NodeOrTokens = New List(Of SyntaxNodeOrToken)()
                Me.Trivia = New List(Of SyntaxTrivia)()
            End Sub

            Public Overrides Sub Visit(node As SyntaxNode)
                If node IsNot Nothing Then
                    Me.NodeOrTokens.Add(node)
                End If

                MyBase.Visit(node)
            End Sub

            Public Overrides Sub VisitToken(token As SyntaxToken)
                If token.Kind <> SyntaxKind.None Then
                    Me.NodeOrTokens.Add(token)
                End If

                MyBase.VisitToken(token)
            End Sub

            Public Overrides Sub VisitTrivia(trivia As SyntaxTrivia)
                If trivia.Kind <> SyntaxKind.None Then
                    Me.Trivia.Add(trivia)
                End If

                MyBase.VisitTrivia(trivia)
            End Sub
        End Class

        Private Class InjectAnnotationRewriter
            Inherits VisualBasicSyntaxRewriter
            Private ReadOnly _annotation As SyntaxAnnotation

            Public Sub New(annotation As SyntaxAnnotation)
                MyBase.New(visitIntoStructuredTrivia:=True)
                Me._annotation = annotation
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node Is Nothing Then
                    Return node
                End If

                Return MyBase.Visit(node).WithAdditionalAnnotations(_annotation)
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                If token.Kind = SyntaxKind.None Then
                    Return token
                End If

                Return MyBase.VisitToken(token).WithAdditionalAnnotations(_annotation)
            End Function

            Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                If trivia.Kind = SyntaxKind.None Then
                    Return trivia
                End If

                If trivia.HasStructure Then
                    Return MyBase.VisitTrivia(trivia)
                End If

                Return MyBase.VisitTrivia(trivia).WithAdditionalAnnotations(_annotation)
            End Function
        End Class

        Private Class InjectRandomAnnotationRewriter
            Inherits VisualBasicSyntaxRewriter
            Private ReadOnly _annotations As List(Of SyntaxAnnotation)
            Private ReadOnly _myRandom As Random

            Public Sub New(annotations As List(Of SyntaxAnnotation))
                MyBase.New(visitIntoStructuredTrivia:=True)
                Me._annotations = annotations
                Me._myRandom = New Random(10)
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node Is Nothing Then
                    Return node
                End If

                Dim annotation = Me._annotations(Me._myRandom.Next(0, _annotations.Count - 1))
                Return MyBase.Visit(node).WithAdditionalAnnotations(annotation)
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                If token.Kind = SyntaxKind.None Then
                    Return token
                End If

                Dim annotation = Me._annotations(Me._myRandom.Next(0, _annotations.Count - 1))
                Return MyBase.VisitToken(token).WithAdditionalAnnotations(annotation)
            End Function

            Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                If trivia.Kind = SyntaxKind.None Then
                    Return trivia
                End If

                ' annotation will be set by actual structure trivia
                If trivia.HasStructure Then
                    Return MyBase.VisitTrivia(trivia)
                End If

                Dim annotation = Me._annotations(Me._myRandom.Next(0, _annotations.Count - 1))
                Return MyBase.VisitTrivia(trivia).WithAdditionalAnnotations(annotation)
            End Function
        End Class

        Private Class CopyAnnotationRewriter
            Inherits VisualBasicSyntaxRewriter
            Private ReadOnly _nodeOrTokenMap As Dictionary(Of SyntaxNodeOrToken, SyntaxNodeOrToken)
            Private ReadOnly _triviaMap As Dictionary(Of SyntaxTrivia, SyntaxTrivia)

            Public Sub New(nodeOrTokenMap As Dictionary(Of SyntaxNodeOrToken, SyntaxNodeOrToken), triviaMap As Dictionary(Of SyntaxTrivia, SyntaxTrivia))
                MyBase.New(visitIntoStructuredTrivia:=True)
                Me._nodeOrTokenMap = nodeOrTokenMap
                Me._triviaMap = triviaMap
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node Is Nothing Then
                    Return node
                End If

                Return Me._nodeOrTokenMap(node).AsNode().CopyAnnotationsTo(MyBase.Visit(node))
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                If token.Kind = SyntaxKind.None Then
                    Return token
                End If

                Return Me._nodeOrTokenMap(token).AsToken().CopyAnnotationsTo(MyBase.VisitToken(token))
            End Function

            Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                If trivia.Kind = SyntaxKind.None Then
                    Return trivia
                End If

                ' annotation will be set by actual structure trivia
                If trivia.HasStructure Then
                    Return MyBase.VisitTrivia(trivia)
                End If

                Return Me._triviaMap(trivia).CopyAnnotationsTo(MyBase.VisitTrivia(trivia))
            End Function
        End Class

        Private ReadOnly _allInOneVisualBasicCode As String = TestResource.AllInOneVisualBasicCode
        Private ReadOnly _helloWorldCode As String = TestResource.HelloWorldVisualBasicCode
    End Class
End Namespace
