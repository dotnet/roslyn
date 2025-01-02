' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The base class for all nodes in the VB syntax tree.
    ''' </summary>
    Partial Public MustInherit Class VisualBasicSyntaxNode
        Inherits SyntaxNode

        ' Constructor. Called only by derived classes.
        Friend Sub New(green As GreenNode, parent As SyntaxNode, position As Integer)
            MyBase.New(green, parent, position)
        End Sub

        ''' <summary>
        ''' Used by structured trivia which has no parent node, so need to know syntax tree explicitly
        ''' </summary>
        Friend Sub New(green As GreenNode, position As Integer, syntaxTree As SyntaxTree)
            MyBase.New(green, Nothing, position)

            _syntaxTree = syntaxTree
        End Sub

        'TODO: may be eventually not needed
        Friend ReadOnly Property VbGreen As InternalSyntax.VisualBasicSyntaxNode
            Get
                Return DirectCast(Me.Green, InternalSyntax.VisualBasicSyntaxNode)
            End Get
        End Property

        ''' <summary>
        ''' Returns a non-null SyntaxTree that owns this node.
        ''' If this node was created with an explicit non-null SyntaxTree, returns that tree.
        ''' Otherwise, if this node has a non-null parent, then returns the parent's SyntaxTree.
        ''' Otherwise, returns a newly created SyntaxTree rooted at this node, preserving this node's reference identity.
        ''' </summary>
        Friend Shadows ReadOnly Property SyntaxTree As SyntaxTree
            Get
                If Me._syntaxTree Is Nothing Then
                    Dim stack = ArrayBuilder(Of SyntaxNode).GetInstance()
                    Dim tree As SyntaxTree = Nothing

                    Dim current As SyntaxNode = Me
                    Dim rootCandidate As SyntaxNode = Nothing

                    While current IsNot Nothing
                        tree = current._syntaxTree
                        If tree IsNot Nothing Then
                            Exit While
                        End If

                        rootCandidate = current
                        stack.Push(current)
                        current = rootCandidate.Parent
                    End While

                    If tree Is Nothing Then
                        Debug.Assert(rootCandidate IsNot Nothing)
#Disable Warning RS0030 ' Do not use banned APIs (CreateWithoutClone is intended to be used from this call site only)
                        tree = VisualBasicSyntaxTree.CreateWithoutClone(DirectCast(rootCandidate, VisualBasicSyntaxNode))
#Enable Warning RS0030
                    End If

                    Debug.Assert(tree IsNot Nothing)

                    While stack.Count > 0
                        Dim alternativeTree As SyntaxTree = Interlocked.CompareExchange(stack.Pop()._syntaxTree, tree, Nothing)
                        If alternativeTree IsNot Nothing Then
                            tree = alternativeTree
                        End If
                    End While

                    stack.Free()
                End If

                Return Me._syntaxTree
            End Get
        End Property

        Public MustOverride Function Accept(Of TResult)(visitor As VisualBasicSyntaxVisitor(Of TResult)) As TResult

        Public MustOverride Sub Accept(visitor As VisualBasicSyntaxVisitor)

        ''' <summary>
        ''' Returns the <see cref="SyntaxKind"/> of the node.
        ''' </summary>
        Public Function Kind() As SyntaxKind
            Return CType(Me.Green.RawKind, SyntaxKind)
        End Function

        ''' <summary>
        ''' The language name this node is syntax of.
        ''' </summary>
        Public Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        ''' <summary>
        ''' The parent of this node.
        ''' </summary>
        ''' <value>The parent node of this node, or Nothing if this node is the root.</value>
        Friend Shadows ReadOnly Property Parent As VisualBasicSyntaxNode
            Get
                Return DirectCast(MyBase.Parent, VisualBasicSyntaxNode)
            End Get
        End Property

#Region "Serialization"

        ''' <summary>
        ''' Deserialize a syntax node from a byte stream.
        ''' </summary>
        <Obsolete(SerializationDeprecationException.Text, True)>
        Public Shared Function DeserializeFrom(stream As IO.Stream, Optional cancellationToken As CancellationToken = Nothing) As SyntaxNode
            Throw New SerializationDeprecationException()
        End Function

#End Region

        ''' <summary>
        ''' Returns True if this node represents a directive.
        ''' </summary>
        Public ReadOnly Property IsDirective As Boolean
            Get
                Return Me.Green.IsDirective
            End Get
        End Property

        ''' <summary>
        ''' Same as accessing <see cref="TextSpan.Start"/> on <see cref="Span"/>.
        ''' </summary>
        ''' <remarks>
        ''' Slight performance improvement.
        ''' </remarks>
        Public Shadows ReadOnly Property SpanStart As Integer
            Get
                Return Position + Me.Green.GetLeadingTriviaWidth()
            End Get
        End Property

        ''' <summary>
        ''' Get the preceding trivia nodes of this node. If this node is a token, returns the preceding trivia
        ''' associated with this node. If this is a non-terminal, returns the preceding trivia of the first token
        ''' of this node. 
        ''' </summary>
        ''' <returns>A list of the preceding trivia.</returns>
        ''' <remarks>If this node is a non-terminal, the parents of the trivia will be the first token of this 
        ''' non-terminal; NOT this node.</remarks>
        Public Shadows Function GetLeadingTrivia() As SyntaxTriviaList
            Return GetFirstToken(includeZeroWidth:=True).LeadingTrivia
        End Function

        ''' <summary>
        ''' Get the following trivia nodes of this node. If this node is a token, returns the following trivia
        ''' associated with this node. If this is a non-terminal, returns the following trivia of the last token
        ''' of this node. 
        ''' </summary>
        ''' <returns>A list of the following trivia.</returns>
        ''' <remarks>If this node is a non-terminal, the parents of the trivia will be the first token of this 
        ''' non-terminal; NOT this node.</remarks>
        Public Shadows Function GetTrailingTrivia() As SyntaxTriviaList
            Return GetLastToken(includeZeroWidth:=True).TrailingTrivia
        End Function

        ' an empty collection of syntax errors.
        Friend Shared EmptyErrorCollection As New ReadOnlyCollection(Of Diagnostic)(Array.Empty(Of Diagnostic))

        ''' <summary>
        ''' Get all syntax errors associated with this node, or any child nodes, grand-child nodes, etc. The errors
        ''' are not in order.
        ''' </summary>
        Friend Function GetSyntaxErrors(tree As SyntaxTree) As ReadOnlyCollection(Of Diagnostic)
            Return DoGetSyntaxErrors(tree, Me)
        End Function

        Friend Shared Function DoGetSyntaxErrors(tree As SyntaxTree, nodeOrToken As SyntaxNodeOrToken) As ReadOnlyCollection(Of Diagnostic)
            If Not nodeOrToken.ContainsDiagnostics Then
                Return EmptyErrorCollection
            Else
                ' Accumulated a stack of nodes with errors to process.

                Dim nodesToProcess As New Stack(Of SyntaxNodeOrToken)
                Dim errorList As New List(Of Diagnostic)
                nodesToProcess.Push(nodeOrToken)

                While nodesToProcess.Count > 0
                    ' Add errors from current node being processed to the list
                    nodeOrToken = nodesToProcess.Pop()
                    Dim node = nodeOrToken.UnderlyingNode
                    If node.ContainsDiagnostics Then
                        Dim errors = DirectCast(node, Syntax.InternalSyntax.VisualBasicSyntaxNode).GetDiagnostics
                        If errors IsNot Nothing Then
                            For i = 0 To errors.Length - 1
                                Dim greenError = errors(i)
                                Debug.Assert(greenError IsNot Nothing)
                                errorList.Add(CreateSyntaxError(tree, nodeOrToken, greenError))
                            Next
                        End If
                    End If

                    ' Children or trivia must have errors too, based on the count. Add them.
                    If Not nodeOrToken.IsToken Then
                        PushNodesWithErrors(nodesToProcess, nodeOrToken.ChildNodesAndTokens())
                    ElseIf nodeOrToken.IsToken Then
                        ProcessTrivia(tree, errorList, nodesToProcess, nodeOrToken.GetLeadingTrivia())
                        ProcessTrivia(tree, errorList, nodesToProcess, nodeOrToken.GetTrailingTrivia())
                    End If
                End While

                Return New ReadOnlyCollection(Of Diagnostic)(errorList)
            End If
        End Function

        ''' <summary>
        ''' Push any nodes that have errors in the given collection onto a stack
        ''' </summary>
        Private Shared Sub PushNodesWithErrors(stack As Stack(Of SyntaxNodeOrToken), nodes As ChildSyntaxList)
            Debug.Assert(stack IsNot Nothing)

            For Each n In nodes
                Debug.Assert(Not n.IsKind(SyntaxKind.None))
                If n.ContainsDiagnostics Then
                    stack.Push(n)
                End If
            Next
        End Sub

        Private Shared Sub ProcessTrivia(tree As SyntaxTree,
                                         errorList As List(Of Diagnostic),
                                         stack As Stack(Of SyntaxNodeOrToken),
                                         nodes As SyntaxTriviaList)
            Debug.Assert(stack IsNot Nothing)

            For Each n In nodes
                Debug.Assert(n.Kind <> SyntaxKind.None)
                If n.UnderlyingNode.ContainsDiagnostics Then
                    If n.HasStructure Then
                        stack.Push(DirectCast(n.GetStructure, VisualBasicSyntaxNode))
                    Else
                        Dim errors = DirectCast(n.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode).GetDiagnostics
                        If errors IsNot Nothing Then
                            For i = 0 To errors.Length - 1
                                Dim e = errors(i)
                                errorList.Add(CreateSyntaxError(tree, n, e))
                            Next
                        End If
                    End If
                End If
            Next
        End Sub

        ''' <summary>
        ''' Given a error info from this node, create the corresponding syntax error with the right span.
        ''' </summary>
        Private Shared Function CreateSyntaxError(tree As SyntaxTree, nodeOrToken As SyntaxNodeOrToken, errorInfo As DiagnosticInfo) As Diagnostic
            Debug.Assert(errorInfo IsNot Nothing)

            ' Translate the green error offset/width relative to my location.
            Return New VBDiagnostic(errorInfo, If(tree Is Nothing, New SourceLocation(tree, nodeOrToken.Span), tree.GetLocation(nodeOrToken.Span)))
        End Function

        Private Shared Function CreateSyntaxError(tree As SyntaxTree, nodeOrToken As SyntaxTrivia, errorInfo As DiagnosticInfo) As Diagnostic
            Debug.Assert(errorInfo IsNot Nothing)

            ' Translate the green error offset/width relative to my location.
            Return New VBDiagnostic(errorInfo, If(tree Is Nothing, New SourceLocation(tree, nodeOrToken.Span), tree.GetLocation(nodeOrToken.Span)))
        End Function

        ''' <summary>
        ''' Add an error to the given node, creating a new node that is the same except it has no parent,
        ''' and has the given error attached to it. The error span is the entire span of this node.
        ''' </summary>
        ''' <param name="err">The error to attach to this node</param>
        ''' <returns>A new node, with no parent, that has this error added to it.</returns>
        ''' <remarks>Since nodes are immutable, the only way to create nodes with errors attached is to create a node without an error,
        ''' then add an error with this method to create another node.</remarks>
        Friend Function AddError(err As DiagnosticInfo) As VisualBasicSyntaxNode
            Dim errorInfos() As DiagnosticInfo

            ' If the green node already has errors, add those on.
            If Me.Green.GetDiagnostics Is Nothing Then
                errorInfos = {err}
            Else
                ' Add the error to the error list.
                errorInfos = Me.Green.GetDiagnostics
                Dim length As Integer = errorInfos.Length
                ReDim Preserve errorInfos(length)
                errorInfos(length) = err
            End If

            ' Get a new green node with the errors added on.
            Dim greenWithDiagnostics = Me.Green.SetDiagnostics(errorInfos)

            ' convert to red node with no parent.
            Dim result = greenWithDiagnostics.CreateRed(Nothing, 0)
            Debug.Assert(result IsNot Nothing)
            Return DirectCast(result, VisualBasicSyntaxNode)
        End Function

        Public Shadows Function GetFirstToken(Optional includeZeroWidth As Boolean = False,
                                              Optional includeSkipped As Boolean = False,
                                              Optional includeDirectives As Boolean = False,
                                              Optional includeDocumentationComments As Boolean = False) As SyntaxToken
            Return CType(MyBase.GetFirstToken(includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments), SyntaxToken)
        End Function

        Public Shadows Function GetLastToken(Optional includeZeroWidth As Boolean = False,
                                             Optional includeSkipped As Boolean = False,
                                             Optional includeDirectives As Boolean = False,
                                             Optional includeDocumentationComments As Boolean = False) As SyntaxToken
            Return CType(MyBase.GetLastToken(includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments), SyntaxToken)
        End Function

        Public Function GetDirectives(Optional filter As Func(Of DirectiveTriviaSyntax, Boolean) = Nothing) As IList(Of DirectiveTriviaSyntax)
            Return (CType(Me, SyntaxNodeOrToken)).GetDirectives(Of DirectiveTriviaSyntax)(filter)
        End Function

        Public Function GetFirstDirective(Optional predicate As Func(Of DirectiveTriviaSyntax, Boolean) = Nothing) As DirectiveTriviaSyntax
            Dim child As SyntaxNodeOrToken
            For Each child In Me.ChildNodesAndTokens()
                If child.ContainsDirectives Then
                    If child.IsNode Then
                        Dim d As DirectiveTriviaSyntax = DirectCast(child.AsNode, VisualBasicSyntaxNode).GetFirstDirective(predicate)
                        If d IsNot Nothing Then
                            Return d
                        End If
                    Else
                        Dim tr As SyntaxTrivia
                        For Each tr In child.AsToken.LeadingTrivia
                            If tr.IsDirective Then
                                Dim d As DirectiveTriviaSyntax = DirectCast(tr.GetStructure, DirectiveTriviaSyntax)
                                If ((predicate Is Nothing) OrElse predicate(d)) Then
                                    Return d
                                End If
                            End If
                        Next
                        Continue For
                    End If
                End If
            Next
            Return Nothing
        End Function

        Public Function GetLastDirective(Optional predicate As Func(Of DirectiveTriviaSyntax, Boolean) = Nothing) As DirectiveTriviaSyntax
            Dim child As SyntaxNodeOrToken
            For Each child In Me.ChildNodesAndTokens().Reverse
                If child.ContainsDirectives Then
                    If child.IsNode Then
                        Dim d As DirectiveTriviaSyntax = DirectCast(child.AsNode, VisualBasicSyntaxNode).GetLastDirective(predicate)
                        If d IsNot Nothing Then
                            Return d
                        End If
                    Else
                        Dim token As SyntaxToken = child.AsToken
                        For Each tr In token.LeadingTrivia.Reverse()
                            If tr.IsDirective Then
                                Dim d As DirectiveTriviaSyntax = DirectCast(tr.GetStructure, DirectiveTriviaSyntax)
                                If ((predicate Is Nothing) OrElse predicate(d)) Then
                                    Return d
                                End If
                            End If
                        Next
                    End If
                End If
            Next
            Return Nothing
        End Function

#Region "Core Overloads"

        Protected Overrides ReadOnly Property SyntaxTreeCore As SyntaxTree
            Get
                Return Me.SyntaxTree
            End Get
        End Property

        Protected Overrides Function ReplaceCore(Of TNode As SyntaxNode)(
            Optional nodes As IEnumerable(Of TNode) = Nothing,
            Optional computeReplacementNode As Func(Of TNode, TNode, SyntaxNode) = Nothing,
            Optional tokens As IEnumerable(Of SyntaxToken) = Nothing,
            Optional computeReplacementToken As Func(Of SyntaxToken, SyntaxToken, SyntaxToken) = Nothing,
            Optional trivia As IEnumerable(Of SyntaxTrivia) = Nothing,
            Optional computeReplacementTrivia As Func(Of SyntaxTrivia, SyntaxTrivia, SyntaxTrivia) = Nothing) As SyntaxNode

            Return SyntaxReplacer.Replace(Me, nodes, computeReplacementNode, tokens, computeReplacementToken, trivia, computeReplacementTrivia).AsRootOfNewTreeWithOptionsFrom(Me.SyntaxTree)
        End Function

        Protected Overrides Function RemoveNodesCore(nodes As IEnumerable(Of SyntaxNode), options As SyntaxRemoveOptions) As SyntaxNode
            Return SyntaxNodeRemover.RemoveNodes(Me, nodes, options).AsRootOfNewTreeWithOptionsFrom(Me.SyntaxTree)
        End Function

        Protected Overrides Function ReplaceNodeInListCore(originalNode As SyntaxNode, replacementNodes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxReplacer.ReplaceNodeInList(Me, originalNode, replacementNodes).AsRootOfNewTreeWithOptionsFrom(Me.SyntaxTree)
        End Function

        Protected Overrides Function InsertNodesInListCore(nodeInList As SyntaxNode, nodesToInsert As IEnumerable(Of SyntaxNode), insertBefore As Boolean) As SyntaxNode
            Return SyntaxReplacer.InsertNodeInList(Me, nodeInList, nodesToInsert, insertBefore).AsRootOfNewTreeWithOptionsFrom(Me.SyntaxTree)
        End Function

        Protected Overrides Function ReplaceTokenInListCore(originalToken As SyntaxToken, newTokens As IEnumerable(Of SyntaxToken)) As SyntaxNode
            Return SyntaxReplacer.ReplaceTokenInList(Me, originalToken, newTokens).AsRootOfNewTreeWithOptionsFrom(Me.SyntaxTree)
        End Function

        Protected Overrides Function InsertTokensInListCore(originalToken As SyntaxToken, newTokens As IEnumerable(Of SyntaxToken), insertBefore As Boolean) As SyntaxNode
            Return SyntaxReplacer.InsertTokenInList(Me, originalToken, newTokens, insertBefore).AsRootOfNewTreeWithOptionsFrom(Me.SyntaxTree)
        End Function

        Protected Overrides Function ReplaceTriviaInListCore(originalTrivia As SyntaxTrivia, newTrivia As IEnumerable(Of SyntaxTrivia)) As SyntaxNode
            Return SyntaxReplacer.ReplaceTriviaInList(Me, originalTrivia, newTrivia).AsRootOfNewTreeWithOptionsFrom(Me.SyntaxTree)
        End Function

        Protected Overrides Function InsertTriviaInListCore(originalTrivia As SyntaxTrivia, newTrivia As IEnumerable(Of SyntaxTrivia), insertBefore As Boolean) As SyntaxNode
            Return SyntaxReplacer.InsertTriviaInList(Me, originalTrivia, newTrivia, insertBefore).AsRootOfNewTreeWithOptionsFrom(Me.SyntaxTree)
        End Function

        Protected Overrides Function NormalizeWhitespaceCore(indentation As String, eol As String, elasticTrivia As Boolean) As SyntaxNode
            Return SyntaxNormalizer.Normalize(Me, indentation, eol, elasticTrivia, useDefaultCasing:=False).AsRootOfNewTreeWithOptionsFrom(Me.SyntaxTree)
        End Function
#End Region

        ''' <summary>
        ''' Gets the location of this node.
        ''' </summary>
        Public Shadows Function GetLocation() As Location
            ' Note that we want to return 'no location' for all nodes from embedded syntax trees
            If Me.SyntaxTree IsNot Nothing Then
                Dim tree = Me.SyntaxTree
                If tree.IsEmbeddedSyntaxTree Then
                    Return New EmbeddedTreeLocation(tree.GetEmbeddedKind, Me.Span)
                ElseIf tree.IsMyTemplate Then
                    Return New MyTemplateLocation(tree, Me.Span)
                End If
            End If
            Return New SourceLocation(Me)
        End Function

        ''' <summary>
        ''' Gets a SyntaxReference for this syntax node. SyntaxReferences can be used to regain access to a
        ''' syntax node without keeping the entire tree and source text in memory.
        ''' </summary>
        Friend Shadows Function GetReference() As SyntaxReference
            Return SyntaxTree.GetReference(Me)
        End Function

        ''' <summary>
        ''' Gets a list of all the diagnostics in the sub tree that has this node as its root.
        ''' This method does not filter diagnostics based on compiler options like nowarn, warnaserror etc.
        ''' </summary>
        Public Shadows Function GetDiagnostics() As IEnumerable(Of Diagnostic)
            Return SyntaxTree.GetDiagnostics(Me)
        End Function

        Protected Overrides Function IsEquivalentToCore(node As SyntaxNode, Optional topLevel As Boolean = False) As Boolean
            Return SyntaxFactory.AreEquivalent(Me, DirectCast(node, VisualBasicSyntaxNode), topLevel)
        End Function

        Friend Overrides Function ShouldCreateWeakList() As Boolean
            Return TypeOf Me Is MethodBlockBaseSyntax
        End Function
    End Class
End Namespace
