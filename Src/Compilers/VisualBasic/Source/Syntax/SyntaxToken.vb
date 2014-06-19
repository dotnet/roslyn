Imports System.Collections.ObjectModel
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
    Public Structure SyntaxToken
        Implements IEquatable(Of SyntaxToken)

        Friend Shared ReadOnly Any As Func(Of SyntaxToken, Boolean) = Function(t As SyntaxToken) True
        Friend Shared ReadOnly NonZeroWidth As Func(Of SyntaxToken, Boolean) = Function(t As SyntaxToken) t.Width > 0

        Private ReadOnly _parent As VisualBasicSyntaxNode
        Private ReadOnly _node As InternalSyntax.SyntaxToken
        Private ReadOnly _position As Integer
        Private ReadOnly _index As Integer

        Friend Sub New(parent As VisualBasicSyntaxNode, node As InternalSyntax.SyntaxToken, position As Integer, index As Integer)
            Debug.Assert(parent Is Nothing OrElse Not parent.IsList, "list cannot be a parent")
            Debug.Assert(node IsNot Nothing OrElse (position = 0 AndAlso index = 0 AndAlso parent Is Nothing))
            Debug.Assert(position >= 0)

            Me._parent = parent
            Me._node = node
            Me._position = position
            Me._index = index

#If DEBUG Then
            If parent IsNot Nothing AndAlso node IsNot Nothing Then
                Dim nodeOrToken = ChildSyntaxList.ItemInternal(parent, index, fromTokenCtor:=True)

                Debug.Assert(nodeOrToken.UnderlyingNode Is node, "node was not found at given index")
                Debug.Assert(nodeOrToken.Position = position, "position mismatch")
            Else
                Debug.Assert(parent Is Nothing OrElse position >= parent.Position)
            End If
#End If
        End Sub

        Friend ReadOnly Property DebuggerDisplay As String
            Get
                Return "SyntaxToken " & Kind.ToString() & " " & ToString()
            End Get
        End Property

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
                    Return _position - Parent.Position
                End If
            End Get
        End Property

        Friend ReadOnly Property Index As Integer
            Get
                Return Me._index
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

        ''' <summary>
        ''' The language name this token is syntax of.
        ''' </summary>
        Public ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Friend ReadOnly Property ContextualKind As SyntaxKind
            Get
                If Me._node IsNot Nothing Then
                    Dim id = TryCast(Me._node, InternalSyntax.IdentifierTokenSyntax)
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

        ''' <summary>
        ''' Determines if the token is a descendant of a structured trivia.
        ''' </summary>
        Public ReadOnly Property IsPartOfStructuredTrivia() As Boolean
            Get
                Return Me.Parent IsNot Nothing AndAlso Me.Parent.IsPartOfStructuredTrivia()
            End Get
        End Property

        Public Function GetIdentifierText() As String
            Return If(Me._node IsNot Nothing,
                        If(Me._node.Kind = SyntaxKind.IdentifierToken,
                            DirectCast(Me._node, InternalSyntax.IdentifierTokenSyntax).IdentifierText,
                            Me.ToString()),
                        String.Empty)
        End Function

        Public ReadOnly Property Value As Object
            Get
                Return If(Me._node IsNot Nothing, Me._node.ObjectValue, Nothing)
            End Get
        End Property

        Public ReadOnly Property ValueText() As String
            Get
                Return If(Me._node IsNot Nothing, Me._node.ValueText, String.Empty)
            End Get
        End Property

        Public Function NormalizeWhitespace(Optional indentation As String = SyntaxExtensions.DefaultIndentation, Optional elasticTrivia As Boolean = False, Optional useDefaultCasing As Boolean = False) As SyntaxToken
            Return SyntaxFormatter.Format(Me, indentation, elasticTrivia, useDefaultCasing)
        End Function

        Public ReadOnly Property Parent As VisualBasicSyntaxNode
            Get
                Return Me._parent
            End Get
        End Property

        Public ReadOnly Property SyntaxTree As VisualBasicSyntaxTree
            Get
                Dim parent = Me.Parent
                If parent IsNot Nothing Then
                    Return parent.SyntaxTree
                End If

                Return Nothing
            End Get
        End Property

        Public ReadOnly Property FullSpan As TextSpan
            Get
                Return New TextSpan(Position, FullWidth)
            End Get
        End Property

        Public ReadOnly Property Span As TextSpan
            Get
                If Me._node IsNot Nothing Then
                    Return New TextSpan(Me._position + Me._node.GetLeadingTriviaWidth(), Me._node.Width)
                End If

                Return New TextSpan(Me._position, 0)
            End Get
        End Property

        Public ReadOnly Property SpanStart As Integer
            Get
                If Me._node IsNot Nothing Then
                    Return Me._position + Me._node.GetLeadingTriviaWidth()
                End If

                Return Me._position
            End Get
        End Property

        Friend ReadOnly Property FullWidth As Integer
            Get
                Return If(Me._node IsNot Nothing, Me._node.FullWidth, 0)
            End Get
        End Property

        Friend ReadOnly Property Width As Integer
            Get
                Return If(Me._node IsNot Nothing, Me._node.Width, 0)
            End Get
        End Property

        Friend ReadOnly Property LeadingWidth As Integer
            Get
                Return If(Me._node IsNot Nothing, Me._node.GetLeadingTriviaWidth, 0)
            End Get
        End Property

        Friend ReadOnly Property TrailingWidth As Integer
            Get
                Return If(Me._node IsNot Nothing, Me._node.GetTrailingTriviaWidth, 0)
            End Get
        End Property

        Public ReadOnly Property ContainsDiagnostics As Boolean
            Get
                Return If(Me._node IsNot Nothing, Me._node.ContainsDiagnostics, False)
            End Get
        End Property

        ''' <summary>
        ''' Determines whether this token or any of its trivia has annotations.
        ''' </summary>
        Public ReadOnly Property ContainsAnnotations As Boolean
            Get
                Return Me._node IsNot Nothing AndAlso Me._node.ContainsAnnotations
            End Get
        End Property

        ''' <summary>
        ''' Determines whether this token has an annotation of the specified type.
        ''' The type must be a strict sub type of SyntaxAnnotation.
        ''' </summary>
        Public Function HasAnnotations(annotationType As Type) As Boolean
            SyntaxAnnotation.CheckTypeIsSubclassOfSyntaxAnnotation(annotationType)
            Return Me._node IsNot Nothing AndAlso Me._node.HasAnnotations(annotationType)
        End Function

        ''' <summary>
        ''' Determines whether this token as the specific annotation.
        ''' </summary>
        Public Function HasAnnotation(annotation As SyntaxAnnotation) As Boolean
            Return Me._node IsNot Nothing AndAlso Me._node.HasAnnotation(annotation)
        End Function

        ''' <summary>
        ''' Gets all annotations on this token of the specified type.
        ''' The type must be a strict sub type of SyntaxAnnotation.
        ''' </summary>
        Public Function GetAnnotations(annotationType As Type) As IEnumerable(Of SyntaxAnnotation)
            SyntaxAnnotation.CheckTypeIsSubclassOfSyntaxAnnotation(annotationType)
            Return If(Me._node Is Nothing,
                      SpecializedCollections.EmptyEnumerable(Of SyntaxAnnotation),
                      Me._node.GetAnnotations(annotationType))
        End Function

        ''' <summary>
        ''' True if any trivia of this token is structured.
        ''' </summary>
        Friend ReadOnly Property HasStructuredTrivia As Boolean
            Get
                If Me.Node IsNot Nothing Then
                    Return Me.Node.HasStructuredTrivia
                Else
                    Return False
                End If
            End Get
        End Property

        Public ReadOnly Property ContainsDirectives As Boolean
            Get
                Return If(Me._node IsNot Nothing, Me._node.ContainsDirectives, False)
            End Get
        End Property

        ''' <summary>
        ''' Returns the string representation of this token, not including its leading and trailing trivia.
        ''' </summary>
        ''' <returns>The string representation of this token, not including its leading and trailing trivia.</returns>
        ''' <remarks>The length of the returned string is always the same as Span.Length</remarks>
        Public Overrides Function ToString() As String
            Return If(Me._node IsNot Nothing, Me._node.ToString(), String.Empty)
        End Function

        ''' <summary>
        ''' Returns the full string representation of this token including its leading and trailing trivia.
        ''' </summary>
        ''' <returns>The full string representation of this token including its leading and trailing trivia.</returns>
        ''' <remarks>The length of the returned string is always the same as FullSpan.Length</remarks>
        Public Function ToFullString() As String
            Return If(Me._node IsNot Nothing, Me._node.ToFullString(), String.Empty)
        End Function

        ''' <summary>
        ''' Writes the full text of this token to the specified TextWriter
        ''' </summary>
        Public Sub WriteTo(writer As IO.TextWriter)
            If Me.Node IsNot Nothing Then
                Me.Node.WriteTo(writer)
            End If
        End Sub

        Friend ReadOnly Property Errors As InternalSyntax.SyntaxDiagnosticInfoList
            Get
                Return New InternalSyntax.SyntaxDiagnosticInfoList(Me._node)
            End Get
        End Property

        Public ReadOnly Property HasLeadingTrivia As Boolean
            Get
                Return (Me._node IsNot Nothing AndAlso Me._node.HasLeadingTrivia)
            End Get
        End Property

        Public ReadOnly Property LeadingTrivia As SyntaxTriviaList
            Get
                If Me._node IsNot Nothing Then
                    Return New SyntaxTriviaList(Me, Me._node.GetLeadingTrivia, Me.Position, 0)
                End If
                Return New SyntaxTriviaList
            End Get
        End Property

        Public ReadOnly Property HasTrailingTrivia As Boolean
            Get
                Return (Me._node IsNot Nothing AndAlso Me._node.HasTrailingTrivia)
            End Get
        End Property

        Public ReadOnly Property TrailingTrivia As SyntaxTriviaList
            Get
                If Me._node IsNot Nothing Then
                    Return New SyntaxTriviaList(Me, Me._node.GetTrailingTrivia, Me._position + Me._node.FullWidth - Me._node.GetTrailingTriviaWidth, Me.LeadingTrivia.Count)
                End If
                Return New SyntaxTriviaList
            End Get
        End Property

        ''' <summary>
        ''' Gets a list of both leading and trailing trivia for the token.
        ''' </summary>
        Public Function GetAllTrivia() As IEnumerable(Of SyntaxTrivia)
            If Me.HasLeadingTrivia Then
                If Me.HasTrailingTrivia Then
                    Return Me.LeadingTrivia.Concat(Me.TrailingTrivia)
                End If

                Return Me.LeadingTrivia
            ElseIf Me.HasTrailingTrivia Then
                Return Me.TrailingTrivia
            Else
                Return SpecializedCollections.EmptyEnumerable(Of SyntaxTrivia)()
            End If
        End Function

        Public ReadOnly Property TypeCharacter As TypeCharacter
            Get
                Select Case _node.Kind
                    Case SyntaxKind.IdentifierToken
                        Dim id = DirectCast(_node, InternalSyntax.IdentifierTokenSyntax)
                        Return id.GetTypeCharacter()

                    Case SyntaxKind.IntegerLiteralToken
                        Dim literal = DirectCast(_node, InternalSyntax.IntegerLiteralTokenSyntax)
                        Return literal.TypeSuffix

                    Case SyntaxKind.FloatingLiteralToken
                        Dim literal = DirectCast(_node, InternalSyntax.FloatingLiteralTokenSyntax)
                        Return literal.TypeSuffix

                    Case SyntaxKind.DecimalLiteralToken
                        Dim literal = DirectCast(_node, InternalSyntax.DecimalLiteralTokenSyntax)
                        Return literal.TypeSuffix
                End Select

                Return Nothing
            End Get
        End Property

        Public ReadOnly Property Base As LiteralBase?
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
                If _node.Kind = SyntaxKind.IdentifierToken Then
                    Dim tk = DirectCast(_node, InternalSyntax.IdentifierTokenSyntax)
                    Return tk.IsBracketed
                End If

                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Determine if the token instance represents a reserved or contextual keyword
        ''' </summary>
        Public ReadOnly Property IsKeyword() As Boolean
            Get
                Return SyntaxFacts.IsKeywordKind(Me.Kind)
            End Get
        End Property

        ''' <summary>
        ''' Determine if the token instance represents a reserved keyword
        ''' </summary>
        Public ReadOnly Property IsReservedKeyword() As Boolean
            Get
                Return SyntaxFacts.IsReservedKeyword(Me.Kind)
            End Get
        End Property

        ''' <summary>
        ''' Determine if the token instance represents a contextual keyword
        ''' </summary>
        Public ReadOnly Property IsContextualKeyword() As Boolean
            Get
                Return SyntaxFacts.IsContextualKeyword(Me.Kind)
            End Get
        End Property

        ''' <summary>
        ''' Determine if the token instance represents a preprocessor keyword
        ''' </summary>
        Public ReadOnly Property IsPreprocessorKeyword() As Boolean
            Get
                Return SyntaxFacts.IsPreprocessorKeyword(Me.Kind)
            End Get
        End Property

        Public Function WithLeadingTrivia(trivia As SyntaxTriviaList) As SyntaxToken
            If Me._node IsNot Nothing Then
                Return New SyntaxToken(Nothing, Me._node.WithLeadingTrivia(DirectCast(trivia.Node, InternalSyntax.VisualBasicSyntaxNode)), 0, 0)
            End If
            Return New SyntaxToken
        End Function

        Public Function WithTrailingTrivia(trivia As SyntaxTriviaList) As SyntaxToken
            If Me._node IsNot Nothing Then
                Return New SyntaxToken(Nothing, Me._node.WithTrailingTrivia(DirectCast(trivia.Node, InternalSyntax.VisualBasicSyntaxNode)), 0, 0)
            End If
            Return New SyntaxToken
        End Function

        Public Function WithLeadingTrivia(ParamArray trivia As SyntaxTrivia()) As SyntaxToken
            Return Me.WithLeadingTrivia(trivia.ToSyntaxTriviaList())
        End Function

        Public Function WithLeadingTrivia(trivia As IEnumerable(Of SyntaxTrivia)) As SyntaxToken
            Return Me.WithLeadingTrivia(trivia.ToSyntaxTriviaList())
        End Function

        Public Function WithTrailingTrivia(ParamArray trivia As SyntaxTrivia()) As SyntaxToken
            Return Me.WithTrailingTrivia(trivia.ToSyntaxTriviaList())
        End Function

        Public Function WithTrailingTrivia(trivia As IEnumerable(Of SyntaxTrivia)) As SyntaxToken
            Return Me.WithTrailingTrivia(trivia.ToSyntaxTriviaList())
        End Function

        Public Shared Operator =(left As SyntaxToken, right As SyntaxToken) As Boolean
            Return left.Equals(right)
        End Operator

        Public Shared Operator <>(left As SyntaxToken, right As SyntaxToken) As Boolean
            Return Not left.Equals(right)
        End Operator

        Public Overloads Function Equals(other As SyntaxToken) As Boolean Implements IEquatable(Of SyntaxToken).Equals
            ' Index replaces position to ensure equality.  Assert if position affects equality.
            Debug.Assert(
                (Me._parent Is other._parent AndAlso Me._node Is other._node AndAlso Me._position = other._position AndAlso Me._index = other._index) =
                (Me._parent Is other._parent AndAlso Me._node Is other._node AndAlso Me._index = other._index)
            )

            Return Me._parent Is other._parent AndAlso
                   Me._node Is other._node AndAlso
                   Me._index = other._index
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return (TypeOf obj Is SyntaxToken AndAlso Me.Equals(DirectCast(obj, SyntaxToken)))
        End Function

        Public Function IsEquivalentTo(other As SyntaxToken) As Boolean
            Return InternalSyntax.VisualBasicSyntaxNode.IsEquivalentTo(Me._node, other._node)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return If(Me._parent IsNot Nothing, Me._parent.GetHashCode(), 0) +
                   If(Me._node IsNot Nothing, Me._node.GetHashCode(), 0) +
                   Me._index
        End Function

        Public Shared Widening Operator CType(token As SyntaxToken) As SyntaxToken
            Return New SyntaxToken(token.Parent, token.Node, token.Position, token.Index)
        End Operator

        Public Shared Narrowing Operator CType(token As SyntaxToken) As SyntaxToken
            Return New SyntaxToken(DirectCast(token.Parent, VisualBasicSyntaxNode), DirectCast(token.Node, InternalSyntax.SyntaxToken), token.Position, token.Index)
        End Operator

        Public Shared Widening Operator CType(token As SyntaxToken) As SyntaxNodeOrToken
            Return CType(CType(token, SyntaxToken), SyntaxNodeOrToken)
        End Operator

        Public Shared Narrowing Operator CType(nodeOrToken As SyntaxNodeOrToken) As SyntaxToken
            Return CType(CType(nodeOrToken, SyntaxToken), SyntaxToken)
        End Operator

        ''' <summary>
        ''' Get all syntax errors associated with this node, or any child nodes, grand-child nodes, etc. The errors
        ''' are not in order.
        ''' </summary>
        Friend Function GetSyntaxErrors(tree As VisualBasicSyntaxTree) As ReadOnlyCollection(Of Diagnostic)
            Return VisualBasicSyntaxNode.DoGetSyntaxErrors(tree, Me)
        End Function

        Private Function GetPreviousNonZeroWidthToken() As SyntaxToken
            Dim position = Me.Position - 1
            Dim parent = Me.Parent

            While parent IsNot Nothing AndAlso parent.Position > position
                parent = parent.Parent
            End While

            If parent Is Nothing Then
                Return Nothing
            End If
            Return parent.FindTokenInternal(position)
        End Function

        Private Function GetPreviousPossiblyZeroWidthToken() As SyntaxToken
            Dim position = Me.Position - 1
            Dim parent = Me.Parent

            While parent IsNot Nothing AndAlso parent.Position > position
                parent = parent.Parent
            End While

            If parent Is Nothing Then
                Return Nothing
            End If

            Dim this = Me
            '.GetAllTokens(New TextSpan(position, 1))
            Dim q = From tk In parent.DescendantTokens(New TextSpan(position, 1))
                    Take While tk <> this

            Return q.LastOrDefault
        End Function

        Private Function GetNextNonZeroWidthToken() As SyntaxToken
            Dim position = Me.End
            Dim parent = Me.Parent

            While parent IsNot Nothing AndAlso parent.EndLocation <= position
                parent = parent.Parent
            End While

            If parent Is Nothing Then
                Return Nothing
            End If

            Return parent.FindTokenInternal(position)
        End Function

        Private Function GetNextPossiblyZeroWidthToken() As SyntaxToken
            Dim position = Me.End
            Dim parent = Me.Parent

            While parent IsNot Nothing AndAlso parent.EndLocation <= position
                parent = parent.Parent
            End While

            If parent Is Nothing Then
                Return Nothing
            End If

            Dim this = Me
            'GetAllTokens(New TextSpan(position, 1))
            Dim q = From tk In parent.DescendantTokens(New TextSpan(position, 1))
                    Skip While tk <> this
                    Skip 1

            Return q.FirstOrDefault

        End Function

        Friend Function GetNextToken(predicate As Func(Of SyntaxToken, Boolean), Optional stepInto As Func(Of SyntaxTrivia, Boolean) = Nothing) As SyntaxToken
            Return CType(SyntaxNavigator.Instance.GetNextToken(Me, SyntaxNavigator.ToCommon(predicate), SyntaxNavigator.ToCommon(stepInto)), SyntaxToken)
        End Function

        Public Function GetNextToken(Optional includeZeroWidth As Boolean = False,
                                     Optional includeSkipped As Boolean = False,
                                     Optional includeDirectives As Boolean = False,
                                     Optional includeDocumentationComments As Boolean = False) As SyntaxToken
            Return CType(SyntaxNavigator.Instance.GetNextToken(Me, includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments), SyntaxToken)
        End Function

        Friend Function GetPreviousToken(predicate As Func(Of SyntaxToken, Boolean), Optional stepInto As Func(Of SyntaxTrivia, Boolean) = Nothing) As SyntaxToken
            Return CType(SyntaxNavigator.Instance.GetPreviousToken(Me, SyntaxNavigator.ToCommon(predicate), SyntaxNavigator.ToCommon(stepInto)), SyntaxToken)
        End Function

        Public Function GetPreviousToken(Optional includeZeroWidth As Boolean = False,
                                         Optional includeSkipped As Boolean = False,
                                         Optional includeDirectives As Boolean = False,
                                         Optional includeDocumentationComments As Boolean = False) As SyntaxToken
            Return CType(SyntaxNavigator.Instance.GetPreviousToken(Me, includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments), SyntaxToken)
        End Function

        Public Function ReplaceTrivia(oldTrivia As SyntaxTrivia, newTrivia As SyntaxTriviaList) As SyntaxToken
            Return SyntaxReplacer.Replace(Me, trivia:={oldTrivia}, computeReplacementTrivia:=Function(o, r) newTrivia)
        End Function

        Public Function ReplaceTrivia(trivia As IEnumerable(Of SyntaxTrivia), computeReplacementTrivia As Func(Of SyntaxTrivia, SyntaxTrivia, SyntaxTriviaList)) As SyntaxToken
            Return SyntaxReplacer.Replace(Me, trivia:=trivia, computeReplacementTrivia:=computeReplacementTrivia)
        End Function

        ''' <summary>
        ''' Adds this annotation to a given syntax token, creating a new syntax token of the same type with the
        ''' annotation on it.
        ''' </summary>
        Public Function WithAdditionalAnnotations(ParamArray annotations As SyntaxAnnotation()) As SyntaxToken
            Return CType(CType(Me, SyntaxToken).WithAdditionalAnnotations(annotations), SyntaxToken)
        End Function

        ''' <summary>
        ''' Removes the annotations from the given syntax token, creating a new syntax token of the same type without
        ''' the annotations on it
        ''' </summary>
        ''' <param name="annotations"></param>
        Public Function WithoutAnnotations(ParamArray annotations As SyntaxAnnotation()) As SyntaxToken
            Return CType(CType(Me, SyntaxToken).WithoutAnnotations(annotations), SyntaxToken)
        End Function

        ''' <summary>
        ''' Copies all SyntaxAnnotations, if any, from this SyntaxToken instance and attaches them to a new instance based on <paramref name="token" />.
        ''' </summary>
        ''' <remarks>
        ''' If no annotations are copied, just returns <paramref name="token" />.
        ''' </remarks>
        Public Function CopyAnnotationsTo(token As SyntaxToken) As SyntaxToken
            Return CType(CType(Me, SyntaxToken).CopyAnnotationsTo(token), SyntaxToken)
        End Function

        ''' <summary>
        ''' Gets the location of this token.
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
        ''' Gets a list of all the diagnostics associated with this token and any related trivia.
        ''' This method does not filter diagnostics based on compiler options like nowarn, warnaserror etc.
        ''' </summary>
        Public Shadows Function GetDiagnostics() As IEnumerable(Of Diagnostic)
            Return SyntaxTree.GetDiagnostics(Me)
        End Function

    End Structure
#End If
End Namespace