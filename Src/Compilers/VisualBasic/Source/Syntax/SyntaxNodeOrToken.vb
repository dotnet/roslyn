Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Common.Semantics
Imports Microsoft.CodeAnalysis.Common.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax


Namespace Microsoft.CodeAnalysis.VisualBasic

#If REMOVE Then
    <DebuggerDisplay("{DebuggerDisplay,nq}")>
    Public Structure SyntaxNodeOrToken
        Implements IEquatable(Of SyntaxNodeOrToken)

        ' The node if this is a node else it is the token's parent
        Private ReadOnly _nodeOrParent As VisualBasicSyntaxNode
        ' The internal token.  This is nothing if this structure returns a node.
        Private ReadOnly _token As InternalSyntax.SyntaxToken
        ' The token's position
        Private ReadOnly _position As Integer
        Private ReadOnly _index As Integer

        Friend Sub New(node As VisualBasicSyntaxNode)
            If node IsNot Nothing Then
                Me._nodeOrParent = node
                Me._position = node.Position
            End If
        End Sub

        Friend Sub New(parent As VisualBasicSyntaxNode, token As InternalSyntax.SyntaxToken, position As Integer, index As Integer, Optional fromTokenCtor As Boolean = False)
            Debug.Assert(parent Is Nothing OrElse Not parent.IsList, "list cannot be a parent")

            Me._nodeOrParent = parent
            Me._token = token
            Me._position = position
            Me._index = index

#If DEBUG Then
            If Not fromTokenCtor AndAlso token IsNot Nothing Then
                ' create a token just for the purpose of argument validation.
                Dim dummy = New SyntaxToken(parent, token, position, index)
            End If
#End If
        End Sub

        Friend ReadOnly Property DebuggerDisplay As String
            Get
                If _token IsNot Nothing OrElse _nodeOrParent IsNot Nothing Then
                    Return "SyntaxNodeOrToken " & Kind.ToString() & " " & ToString()
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Friend ReadOnly Property UnderlyingNode As InternalSyntax.VisualBasicSyntaxNode
            Get
                If IsNode Then
                    Return Me._nodeOrParent.Green
                End If
                Return Me._token
            End Get
        End Property

        Public ReadOnly Property IsNode As Boolean
            Get
                Return Not IsToken
            End Get
        End Property

        Public ReadOnly Property IsToken As Boolean
            Get
                Return Me._token IsNot Nothing
            End Get
        End Property

        Public Function AsNode() As VisualBasicSyntaxNode
            If Not Me.IsNode Then
                Return Nothing
            End If
            Return Me._nodeOrParent
        End Function

        Public Function AsToken() As SyntaxToken
            If Not Me.IsToken Then
                Return Nothing
            End If
            Return New SyntaxToken(Me._nodeOrParent, Me._token, Me._position, Me._index)
        End Function

        Public ReadOnly Property Kind As SyntaxKind
            Get
                If IsToken Then
                    Return Me._token.Kind
                End If
                If _nodeOrParent IsNot Nothing Then
                    Return _nodeOrParent.Kind
                End If
                Return SyntaxKind.None
            End Get
        End Property

        ''' <summary>
        ''' The language name this node or token is syntax of.
        ''' </summary>
        Public ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public ReadOnly Property IsMissing As Boolean
            Get
                If IsToken Then
                    Return Me._token.IsMissing
                End If
                If _nodeOrParent IsNot Nothing Then
                    Return _nodeOrParent.IsMissing
                End If
                Return False
            End Get
        End Property

        Friend ReadOnly Property Offset As Integer
            Get
                If Parent Is Nothing Then
                    Return _position
                Else
                    Return _position - Parent.Position
                End If
            End Get
        End Property

        Friend ReadOnly Property Position As Integer
            Get
                Return Me._position
            End Get
        End Property

        Friend ReadOnly Property [End] As Integer
            Get
                Return Me._position + Me.FullWidth
            End Get
        End Property

        Public Function ChildNodesAndTokens() As ChildSyntaxList
            If Me.IsToken Then
                Return Nothing
            End If
            Return Me._nodeOrParent.ChildNodesAndTokens()
        End Function

        ''' <summary>
        ''' binary search of nodes to find the slot.  Consider unifying this with that
        ''' implementation.
        ''' </summary>
        Public Shared Function GetFirstChildIndexSpanningPosition(node As VisualBasicSyntaxNode, position As Integer) As Integer
            Return SyntaxNodeOrToken.GetFirstChildIndexSpanningPosition(node, position)
        End Function

        Public Function GetNextSibling() As SyntaxNodeOrToken
            Return CType(CType(Me, SyntaxNodeOrToken).GetNextSibling(), SyntaxNodeOrToken)
        End Function

        Public Function GetPreviousSibling() As SyntaxNodeOrToken
            Return CType(CType(Me, SyntaxNodeOrToken).GetPreviousSibling(), SyntaxNodeOrToken)
        End Function

        Public ReadOnly Property ContainsDiagnostics As Boolean
            Get
                If IsToken Then
                    Return Me._token.ContainsDiagnostics
                End If
                Return Me._nodeOrParent.ContainsDiagnostics
            End Get
        End Property

        Friend ReadOnly Property Errors As InternalSyntax.SyntaxDiagnosticInfoList
            Get
                Return New InternalSyntax.SyntaxDiagnosticInfoList(Me.UnderlyingNode)
            End Get
        End Property

        Public ReadOnly Property ContainsDirectives As Boolean
            Get
                If IsToken Then
                    Return Me._token.ContainsDirectives
                End If
                Return Me._nodeOrParent.ContainsDirectives
            End Get
        End Property

        Friend ReadOnly Property HasStructuredTrivia As Boolean
            Get
                If Me.IsToken Then
                    Return Me._token.HasStructuredTrivia
                End If

                Return Me._nodeOrParent.HasStructuredTrivia
            End Get
        End Property

        ''' <summary>
        ''' Determines if the node or token is a descendant of a structured trivia.
        ''' </summary>
        Friend ReadOnly Property IsPartOfStructuredTrivia() As Boolean
            Get
                If Me.IsNode Then
                    Return Me.AsNode().IsPartOfStructuredTrivia()
                ElseIf Me.IsToken Then
                    Return Me.AsToken().IsPartOfStructuredTrivia()
                Else
                    Return False
                End If
            End Get
        End Property

        Friend ReadOnly Property HasSkippedText As Boolean
            Get
                Dim node = UnderlyingNode
                If node IsNot Nothing Then
                    Return node.HasSkippedText
                End If
                Return False
            End Get
        End Property

        Public ReadOnly Property Parent As VisualBasicSyntaxNode
            Get
                Return If(Me.IsToken, Me._nodeOrParent, Me._nodeOrParent.Parent)
            End Get
        End Property

        Public ReadOnly Property SyntaxTree As VisualBasicSyntaxTree
            Get
                Dim nodeOrParent = Me._nodeOrParent
                If nodeOrParent IsNot Nothing Then
                    Return nodeOrParent.SyntaxTree
                End If

                Return Nothing
            End Get
        End Property

        Public ReadOnly Property Span As TextSpan
            Get
                If Me.IsNode Then
                    Return Me._nodeOrParent.Span
                End If
                Return Me.AsToken.Span
            End Get
        End Property

        Public ReadOnly Property SpanStart As Integer
            Get
                If Me.IsNode Then
                    Return Me._nodeOrParent.SpanStart
                End If

                ' PERF : Inlined "Me.AsToken.SpanStart"
                Return Me._position + Me._token.GetLeadingTriviaWidth()
            End Get
        End Property

        Public ReadOnly Property FullSpan As TextSpan
            Get
                If Me.IsNode Then
                    Return Me._nodeOrParent.FullSpan
                End If

                Return New TextSpan(Position, Me._token.FullWidth)
            End Get
        End Property

        Friend ReadOnly Property Width As Integer
            Get
                If IsToken Then
                    Return Me._token.Width
                End If
                Return Me._nodeOrParent.Width
            End Get
        End Property

        Friend ReadOnly Property FullWidth As Integer
            Get
                If IsToken Then
                    Return Me._token.FullWidth
                End If
                Return Me._nodeOrParent.FullWidth
            End Get
        End Property

        ''' <summary>
        ''' Returns the string representation of this node or token, not including its leading and trailing
        ''' trivia.
        ''' </summary>
        ''' <returns>The string representation of this node or token, not including its leading and trailing
        ''' trivia.</returns>
        ''' <remarks>The length of the returned string is always the same as Span.Length</remarks>
        Public Overrides Function ToString() As String
            Return If(Me.IsToken, Me._token.ToString(), Me._nodeOrParent.ToString())
        End Function

        ''' <summary>
        ''' Returns the full string representation of this node or token including its leading and trailing trivia.
        ''' </summary>
        ''' <returns>The full string representation of this node or token including its leading and trailing
        ''' trivia.</returns>
        ''' <remarks>The length of the returned string is always the same as FullSpan.Length</remarks>
        Public Function ToFullString() As String
            Return If(Me.IsToken, Me._token.ToFullString(), Me._nodeOrParent.ToFullString())
        End Function

        Public ReadOnly Property HasLeadingTrivia As Boolean
            Get
                Return If(Me.IsToken, Me._token.HasLeadingTrivia, Me._nodeOrParent.HasLeadingTrivia)
            End Get
        End Property

        Public Function GetLeadingTrivia() As SyntaxTriviaList
            Return If(Me.IsToken, Me.AsToken.LeadingTrivia, Me.AsNode.GetLeadingTrivia())
        End Function

        Public ReadOnly Property HasTrailingTrivia As Boolean
            Get
                Return If(Me.IsToken, Me._token.HasTrailingTrivia, Me.AsNode.HasTrailingTrivia)
            End Get
        End Property

        Public Function GetTrailingTrivia() As SyntaxTriviaList
            Return If(Me.IsToken, Me.AsToken.TrailingTrivia, Me.AsNode.GetTrailingTrivia())
        End Function

        Public Shared Widening Operator CType(node As VisualBasicSyntaxNode) As SyntaxNodeOrToken
            Return New SyntaxNodeOrToken(node)
        End Operator

        Public Shared Narrowing Operator CType(nodeOrToken As SyntaxNodeOrToken) As VisualBasicSyntaxNode
            Return nodeOrToken.AsNode()
        End Operator

        Public Shared Widening Operator CType(token As SyntaxToken) As SyntaxNodeOrToken
            Return New SyntaxNodeOrToken(DirectCast(token.Parent, VisualBasicSyntaxNode), DirectCast(token.Node, InternalSyntax.SyntaxToken), token.Position, token.Index)
        End Operator

        Public Shared Narrowing Operator CType(nodeOrToken As SyntaxNodeOrToken) As SyntaxToken
            Return nodeOrToken.AsToken()
        End Operator

        Public Overloads Function Equals(other As SyntaxNodeOrToken) As Boolean Implements IEquatable(Of SyntaxNodeOrToken).Equals
            ' index replaces posiiton to ensure equality.  Assert if position affects equality.
            Debug.Assert(
                (Me._nodeOrParent Is other._nodeOrParent AndAlso Me._token Is other._token AndAlso Me._position = other._position AndAlso Me._index = other._index) =
                (Me._nodeOrParent Is other._nodeOrParent AndAlso Me._token Is other._token AndAlso Me._index = other._index)
            )

            Return Me._nodeOrParent Is other._nodeOrParent AndAlso
                   Me._token Is other._token AndAlso
                   Me._index = other._index
        End Function

        Public Shared Operator =(left As SyntaxNodeOrToken, right As SyntaxNodeOrToken) As Boolean
            Return left.Equals(right)
        End Operator

        Public Shared Operator <>(left As SyntaxNodeOrToken, right As SyntaxNodeOrToken) As Boolean
            Return Not left.Equals(right)
        End Operator

        Public Overrides Function Equals(obj As Object) As Boolean
            Return TypeOf obj Is SyntaxNodeOrToken AndAlso
                   Me.Equals(DirectCast(obj, SyntaxNodeOrToken))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return If(Me._nodeOrParent IsNot Nothing, Me._nodeOrParent.GetHashCode(), 0) +
                   If(Me._token IsNot Nothing, Me._token.GetHashCode(), 0) +
                   Me._position +
                   Me._index
        End Function

        Public Function IsEquivalentTo(other As SyntaxNodeOrToken) As Boolean
            If Me.IsNode AndAlso other.IsNode Then
                Return InternalSyntax.VisualBasicSyntaxNode.IsEquivalentTo(Me._nodeOrParent.Green, other._nodeOrParent.Green)
            ElseIf Me.IsToken AndAlso other.IsToken Then
                Return InternalSyntax.VisualBasicSyntaxNode.IsEquivalentTo(Me._token, other._token)
            End If
            Return False
        End Function

        Public Shared Widening Operator CType(nodeOrToken As SyntaxNodeOrToken) As SyntaxNodeOrToken
            Return New SyntaxNodeOrToken(nodeOrToken._nodeOrParent, nodeOrToken._token, nodeOrToken.Position, nodeOrToken._index)
        End Operator

        Public Shared Narrowing Operator CType(nodeOrToken As SyntaxNodeOrToken) As SyntaxNodeOrToken
            If nodeOrToken.IsNode Then
                Return New SyntaxNodeOrToken(DirectCast(nodeOrToken.UnderlyingNode, VisualBasicSyntaxNode))
            Else
                Return New SyntaxNodeOrToken(DirectCast(nodeOrToken.Parent, VisualBasicSyntaxNode), DirectCast(nodeOrToken.UnderlyingNode, InternalSyntax.SyntaxToken), nodeOrToken.Position, nodeOrToken.Index)
            End If
        End Operator

#If False Then
        Public Shared Narrowing Operator CType(token As SyntaxToken) As SyntaxNodeOrToken
            Return CType(token, SyntaxToken)
        End Operator
#End If

        Public Shared Widening Operator CType(node As SyntaxNode) As SyntaxNodeOrToken
            Return New SyntaxNodeOrToken(DirectCast(node, VisualBasicSyntaxNode))
        End Operator

        ''' <summary>
        ''' Determines whether this node or token (or any sub node, token or trivia) has annotations
        ''' </summary>
        Public ReadOnly Property ContainsAnnotations As Boolean
            Get
                Return If(Me.IsToken, Me._token.ContainsAnnotations, Me._nodeOrParent.ContainsAnnotations)
            End Get
        End Property

        ''' <summary>
        ''' Determines if this node or token has any annotation of the specified type attached.
        ''' </summary>
        Public Function HasAnnotations(annotationType As Type) As Boolean
            SyntaxAnnotation.CheckTypeIsSubclassOfSyntaxAnnotation(annotationType)
            Return If(Me.IsToken, Me._token.HasAnnotations(annotationType), Me._nodeOrParent.HasAnnotations(annotationType))
        End Function

        ''' <summary>
        ''' Determines if this node or token has any annotation of the specified type attached.
        ''' </summary>
        Public Function HasAnnotations(Of TSyntaxAnnotation)() As Boolean
            Return HasAnnotations(GetType(TSyntaxAnnotation))
        End Function

        ''' <summary>
        ''' Determines whether this node has the specific annotation.
        ''' </summary>
        Public Function HasAnnotation(annotation As SyntaxAnnotation) As Boolean
            Return If(Me.IsToken, Me._token.HasAnnotation(annotation), Me._nodeOrParent.HasAnnotation(annotation))
        End Function

        ''' <summary>
        ''' Gets all annotations of the specified type attached to this node or token.
        ''' The type must be a strict sub type of SyntaxAnnotation.
        ''' </summary>
        Public Function GetAnnotations(annotationType As Type) As IEnumerable(Of SyntaxAnnotation)
            SyntaxAnnotation.CheckTypeIsSubclassOfSyntaxAnnotation(annotationType)
            Return If(Me.IsToken, Me._token.GetAnnotations(annotationType), Me._nodeOrParent.GetAnnotations(annotationType))
        End Function

        ''' <summary>
        ''' Gets all the annotations of the specified type attached to this node or token (or any sub  node).
        ''' </summary>
        Public Function GetAnnotations(Of TSyntaxAnnotation)() As IEnumerable(Of TSyntaxAnnotation)
            Return GetAnnotations(GetType(TSyntaxAnnotation)).Cast(Of TSyntaxAnnotation)()
        End Function

        ''' <summary>
        ''' Adds this annotation to a given syntax node Or token, creating a New syntax node Or token of the same type with the
        ''' annotation on it.
        ''' </summary>
        Public Function WithAdditionalAnnotations(ParamArray annotations As SyntaxAnnotation()) As SyntaxNodeOrToken
            If annotations Is Nothing Then
                Throw New ArgumentNullException("annotations")
            End If

            If Me.IsNode Then
                Return Me.AsNode().WithAdditionalAnnotations(annotations)
            ElseIf Me.IsToken Then
                Return Me.AsToken().WithAdditionalAnnotations(annotations)
            Else
                Return Me
            End If
        End Function

        ''' <summary>
        ''' Gets the location of this node or token.
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
        ''' Gets a list of all the diagnostics in either the sub tree that has this node as its root or
        ''' associated with this token and its related trivia. 
        ''' This method does not filter diagnostics based on compiler options like nowarn, warnaserror etc.
        ''' </summary>
        Public Shadows Function GetDiagnostics() As IEnumerable(Of Diagnostic)
            Return SyntaxTree.GetDiagnostics(Me)
        End Function

    End Structure
#End If
End Namespace