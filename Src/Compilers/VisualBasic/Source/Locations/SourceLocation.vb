Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A program location in source code.
    ''' </summary>
    <Serializable()>
    Friend Class SourceLocation
        Inherits VBLocation

        Private ReadOnly _syntaxTree As SyntaxTree
        Private ReadOnly _span As TextSpan

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.SourceFile
            End Get
        End Property

        Public Sub New(syntaxTree As SyntaxTree, span As TextSpan)
            ' Should never create source location for embedded trees
            Debug.Assert(syntaxTree Is Nothing OrElse Not syntaxTree.IsEmbeddedOrMyTemplateTree())
            _syntaxTree = syntaxTree
            _span = span
        End Sub

        Public Sub New(node As VisualBasicSyntaxNode)
            Me.New(node.SyntaxTree, node.Span)
        End Sub

        Public Sub New(token As SyntaxToken)
            Me.New(DirectCast(token.SyntaxTree, VisualBasicSyntaxTree), token.Span)
        End Sub

        Public Sub New(nodeOrToken As SyntaxNodeOrToken)
            Me.New(DirectCast(nodeOrToken.SyntaxTree, VisualBasicSyntaxTree), nodeOrToken.Span)
        End Sub

        Public Sub New(trivia As SyntaxTrivia)
            Me.New(DirectCast(trivia.SyntaxTree, VisualBasicSyntaxTree), trivia.Span)
        End Sub

        Public Sub New(syntaxRef As SyntaxReference)
            Me.New(syntaxRef.SyntaxTree, syntaxRef.Span)

            ' If we're using a syntaxref, we don't have a node in hand, so we couldn't get equality
            ' on syntax node, so associatedNode shouldn't be set. We never use this constructor
            ' when binding executable code anywhere, so it has no use.
        End Sub

        Public Overrides ReadOnly Property SourceSpan As TextSpan
            Get
                Return _span
            End Get
        End Property

        Public Overrides ReadOnly Property SourceTree As SyntaxTree
            Get
                Return _syntaxTree
            End Get
        End Property

        Public Overrides Function GetLineSpan(usePreprocessorDirectives As Boolean) As FileLinePositionSpan
            Debug.Assert(_syntaxTree IsNot Nothing, "If we ever have a null SyntaxTree, handle as in C#.")
            Return _syntaxTree.GetLineSpan(_span, usePreprocessorDirectives)
        End Function

        Public Overloads Function Equals(other As SourceLocation) As Boolean
            If Me Is other Then
                Return True
            End If

            Return other IsNot Nothing AndAlso other._syntaxTree Is _syntaxTree AndAlso other._span.Equals(_span)
        End Function

        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, SourceLocation))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(_syntaxTree, _span.GetHashCode())
        End Function

        Friend Overrides ReadOnly Property DebugView As String
            Get
                Return MyBase.DebugView + """" + _syntaxTree.ToString().Substring(_span.Start, _span.Length) + """"
            End Get
        End Property
    End Class
End Namespace
