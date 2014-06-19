Imports Roslyn.Compilers.Internal
Imports System.Collections.ObjectModel

Namespace Roslyn.Compilers.VisualBasic
    Public Structure SyntaxTokenStruct
        Implements IEquatable(Of SyntaxTokenStruct)
        Private ReadOnly _parent As SyntaxNode
        Private ReadOnly _node As InternalSyntax.SyntaxToken
        Private ReadOnly _position As Integer

        Friend Sub New(ByVal parent As SyntaxNode, ByVal node As InternalSyntax.SyntaxToken, ByVal position As Integer)
            Debug.Assert(parent Is Nothing OrElse Not parent.IsList, "list cannot be a parent")
            Debug.Assert(position >= 0)

            Me._parent = parent
            Me._node = node
            Me._position = position
        End Sub

        Friend ReadOnly Property Node As InternalSyntax.SyntaxToken
            Get
                Return Me._node
            End Get
        End Property

        Friend ReadOnly Property Offset As Integer
            Get
                If Parent Is Nothing Then
                    Return Me._position
                Else
                    Return _position - Parent.StartLocation
                End If
            End Get
        End Property

        Friend ReadOnly Property Position As Integer
            Get
                Return _position
            End Get
        End Property

        Friend ReadOnly Property [End] As Integer
            Get
                Return _position + FullWidth
            End Get
        End Property

        Public ReadOnly Property Kind As SyntaxKind
            Get
                Return If(Me._node IsNot Nothing, Me._node.Kind, SyntaxKind.None)
            End Get
        End Property

        Public ReadOnly Property ContextualKind As SyntaxKind
            Get
                If Me._node IsNot Nothing Then
                    Dim id = TryCast(Me._node, InternalSyntax.IdentifierSyntax)
                    If id IsNot Nothing Then
                        Return id.PossibleKeywordKind
                    End If
                    Return Me._node.Kind
                End If
                Return SyntaxKind.None
            End Get
        End Property

        Public ReadOnly Property IsMissing As Boolean
            Get
                Return If(Me._node IsNot Nothing, Me._node.IsMissing, False)
            End Get
        End Property

        Public ReadOnly Property [Text] As String
            Get
                Return If(Me._node IsNot Nothing, Me._node.Text, String.Empty)
            End Get
        End Property

        Public ReadOnly Property IdentifierText As String
            Get
                Return If(Me._node IsNot Nothing,
                            If(Me._node.Kind = SyntaxKind.Identifier,
                                DirectCast(Me._node, InternalSyntax.IdentifierSyntax).IdentifierText,
                                Me.Text),
                            String.Empty)
            End Get
        End Property

        Public ReadOnly Property FullText As String
            Get
                Return If(Me._node IsNot Nothing, Me._node.GetFullText, String.Empty)
            End Get
        End Property

        Public ReadOnly Property Value As Object
            Get
                Return If(Me._node IsNot Nothing, Me._node.ObjectValue, Nothing)
            End Get
        End Property

        Public ReadOnly Property ValueText As String
            Get
                Return If(Me._node IsNot Nothing, Me._node.ValueText, String.Empty)
            End Get
        End Property

        Public ReadOnly Property Parent As SyntaxNode
            Get
                Return Me._parent
            End Get
        End Property

        Public ReadOnly Property FullSpan As TextSpan
            Get
                Return TextSpan.FromBounds(Position, [End])
            End Get
        End Property

        Public ReadOnly Property Span As TextSpan
            Get
                If Me._node IsNot Nothing Then
                    Return New TextSpan(Me._position + Me._node.GetLeadingTriviaWidth, Me._node.Width)
                End If
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property FullWidth As Integer
            Get
                Return If(Me._node IsNot Nothing, Me._node.FullWidth, 0)
            End Get
        End Property

        Public ReadOnly Property Width As Integer
            Get
                Return If(Me._node IsNot Nothing, Me._node.Width, 0)
            End Get
        End Property

        Public ReadOnly Property LeadingWidth As Integer
            Get
                Return If(Me._node IsNot Nothing, Me._node.GetLeadingTriviaWidth, 0)
            End Get
        End Property

        Public ReadOnly Property TrailingWidth As Integer
            Get
                Return If(Me._node IsNot Nothing, Me._node.GetTrailingTriviaWidth, 0)
            End Get
        End Property

        Public ReadOnly Property HasDiagnostics As Boolean
            Get
                Return If(Me._node IsNot Nothing, Me._node.HasDiagnostics, False)
            End Get
        End Property

        Public ReadOnly Property HasDirectives As Boolean
            Get
                Return If(Me._node IsNot Nothing, Me._node.HasDirectives, False)
            End Get
        End Property

        Public Function GetFullText() As String
            Return If(Me._node IsNot Nothing, Me._node.GetFullText, String.Empty)
        End Function

        Public Function GetText() As String
            Return If(Me._node IsNot Nothing, Me._node.GetText, String.Empty)
        End Function

        Friend ReadOnly Property Errors As InternalSyntax.SyntaxDiagnosticInfoList
            Get
                Return New InternalSyntax.SyntaxDiagnosticInfoList(Me._node)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return Me.GetFullText
        End Function

        Public ReadOnly Property HasLeadingTrivia As Boolean
            Get
                Return (Me._node IsNot Nothing AndAlso (Not Me._node.GetLeadingTriviaNode Is Nothing))
            End Get
        End Property

        Public ReadOnly Property LeadingTrivia As SyntaxTriviaList
            Get
                If Me._node IsNot Nothing Then
                    Return New SyntaxTriviaList(Me, Me._node.GetLeadingTriviaNode, Me.Position)
                End If
                Return New SyntaxTriviaList
            End Get
        End Property

        Public ReadOnly Property HasTrailingTrivia As Boolean
            Get
                Return (Me._node IsNot Nothing AndAlso (Not Me._node.GetTrailingTriviaNode Is Nothing))
            End Get
        End Property

        Public ReadOnly Property TrailingTrivia As SyntaxTriviaList
            Get
                If Me._node IsNot Nothing Then
                    Return New SyntaxTriviaList(Me, Me._node.GetTrailingTriviaNode, Me._position + Me._node.FullWidth - Me._node.GetTrailingTriviaWidth)
                End If
                Return New SyntaxTriviaList
            End Get
        End Property

        Public ReadOnly Property TypeCharacter As TypeCharacter
            Get
                Select Case _node.Kind
                    Case SyntaxKind.Identifier
                        Dim id = DirectCast(_node, InternalSyntax.IdentifierSyntax)
                        Return id.TypeCharacter

                    Case SyntaxKind.IntegerLiteralToken
                        Dim literal = DirectCast(_node, InternalSyntax.IntegerLiteralTokenSyntax)
                        Return literal.TypeSuffix

                    Case SyntaxKind.FloatingLiteralToken
                        Dim literal = DirectCast(_node, InternalSyntax.FloatingLiteralTokenSyntax)
                        Return literal.TypeSuffix

                    Case SyntaxKind.DecimalLiteralToken
                        Dim literal = DirectCast(_node, InternalSyntax.DecimalLiteralTokenSyntax)
                        Return literal.TypeSuffix

                    Case SyntaxKind.ElseDirective
                        Return VisualBasic.TypeCharacter.None
                End Select
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property Base As LiteralBase
            Get
                If _node.Kind = SyntaxKind.IntegerLiteralToken Then
                    Dim tk = DirectCast(_node, InternalSyntax.IntegerLiteralTokenSyntax)
                    Return tk.Base
                End If
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property IsBracketed As Boolean
            Get
                If _node.Kind = SyntaxKind.Identifier Then
                    Dim tk = DirectCast(_node, InternalSyntax.IdentifierSyntax)
                    Return tk.IsBracketed
                End If

                Return Nothing
            End Get
        End Property

        Public Function WithLeadingTrivia(ByVal trivia As SyntaxTriviaList) As SyntaxTokenStruct
            If Me._node IsNot Nothing Then
                Return New SyntaxTokenStruct(Nothing, Me._node.WithLeadingTrivia(trivia.Node), 0)
            End If
            Return New SyntaxTokenStruct
        End Function

        Public Function WithTrailingTrivia(ByVal trivia As SyntaxTriviaList) As SyntaxTokenStruct
            If Me._node IsNot Nothing Then
                Return New SyntaxTokenStruct(Nothing, Me._node.WithTrailingTrivia(trivia.Node), 0)
            End If
            Return New SyntaxTokenStruct
        End Function

        Public Shared Operator =(ByVal a As SyntaxTokenStruct, ByVal b As SyntaxTokenStruct) As Boolean
            Return a.Equals(b)
        End Operator

        Public Shared Operator <>(ByVal a As SyntaxTokenStruct, ByVal b As SyntaxTokenStruct) As Boolean
            Return Not a.Equals(b)
        End Operator

        Public Overloads Function Equals(ByVal other As SyntaxTokenStruct) As Boolean Implements IEquatable(Of Roslyn.Compilers.VisualBasic.SyntaxTokenStruct).Equals
            Return (((Me._parent Is other._parent) AndAlso (Me._node Is other._node)) AndAlso (Me._position = other._position))
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return (TypeOf obj Is SyntaxTokenStruct AndAlso Me.Equals(DirectCast(obj, SyntaxTokenStruct)))
        End Function

        Public Function IsEquivalentTo(ByVal other As SyntaxTokenStruct) As Boolean
            Return InternalSyntax.SyntaxNode.AreEquivalent(Me._node, other._node)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ((If(Me._parent IsNot Nothing, Me._parent.GetHashCode, 0) + If(Me._node IsNot Nothing, Me._node.GetHashCode, 0)) + Me._position)
        End Function

        Public Shared Widening Operator CType(ByVal token As SyntaxTokenStruct) As CommonSyntaxToken
            Return New CommonSyntaxToken(token.Parent, token.Node, token.Offset)
        End Operator

        Public Shared Narrowing Operator CType(ByVal token As CommonSyntaxToken) As SyntaxTokenStruct
            Return New SyntaxTokenStruct(DirectCast(token.Parent, SyntaxNode), DirectCast(token.Node, InternalSyntax.SyntaxToken), token.FullSpan.Start)
        End Operator

        ''' <summary>
        ''' Get all syntax errors associated with this node, or any child nodes, grand-child nodes, etc. The errors
        ''' are not in order.
        ''' </summary>
        Public Function GetSyntaxErrors() As ReadOnlyCollection(Of Diagnostic)
            Return SyntaxNode.DoGetSyntaxErrors(Me)
        End Function

        Public Function GetPreviousToken(Optional ByVal includZeroWidthTokens As Boolean = False) As SyntaxTokenStruct
            If includZeroWidthTokens Then
                Return GetPreviousPossiblyZeroWidthToken()
            Else
                Return GetPreviousNonZeroWidthToken()
            End If
        End Function

        ' TODO: this is very inefficient. Is it called often enough to rewrite?
        Private Function GetPreviousPossiblyZeroWidthToken() As SyntaxTokenStruct
            If Me.Parent IsNot Nothing Then
                ' walk forward in parent's child list until we find ourself 
                ' and then return the next token
                Dim returnNext = False
                For Each child In Me.Parent.Children.Reverse()
                    If returnNext Then
                        If child.IsToken Then
                            Return child.AsToken()
                        Else
                            Return child.AsNode().GetLastToken(includeZeroWidthTokens:=True)
                        End If
                    End If
                    If child.IsToken AndAlso child.AsToken() = Me Then
                        returnNext = True
                    End If
                Next

                ' otherwise get next token from the parent's parent, and so on
                Dim node = Me.Parent
                While node IsNot Nothing
                    Dim nodesParent = node.Parent
                    If nodesParent IsNot Nothing Then
                        returnNext = False
                        For Each child In nodesParent.Children.Reverse()
                            If returnNext Then
                                If child.IsToken Then
                                    Return child.AsToken()
                                Else
                                    Return child.AsNode().GetLastToken(includeZeroWidthTokens:=True)
                                End If
                            End If
                            If child.IsNode AndAlso child.AsNode() Is node Then
                                returnNext = True
                            End If
                        Next
                    End If
                    node = nodesParent
                End While
            End If
            Return Nothing
        End Function

        Private Function GetPreviousNonZeroWidthToken() As SyntaxTokenStruct
            Dim position = Me.Position - 1
            Dim parent = Me.Parent

            While parent IsNot Nothing AndAlso parent.StartLocation > position
                parent = parent.Parent
            End While

            If parent Is Nothing Then
                Return Nothing
            End If
            Return parent.FindToken(position)
        End Function
    End Structure
End Namespace

