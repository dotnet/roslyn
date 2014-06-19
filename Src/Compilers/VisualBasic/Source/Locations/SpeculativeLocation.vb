Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Common.Semantics
Imports Microsoft.CodeAnalysis.Common.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A program location in speculative bound code. Right now, speculatively bound code
    ''' doesn't have a syntax tree associated with it. This probably will change in the future, but
    ''' for now this works.
    ''' </summary>
    Friend Class SpeculativeLocation
        Inherits Location

        Private ReadOnly _span As TextSpan

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.None  ' TODO: should be better kind than this?
            End Get
        End Property

        Public Sub New(span As TextSpan)
            _span = span
        End Sub

        Public Sub New(node As SyntaxNode)
            Me.New(node.Span)
            '_associatedNode = New WeakReference(Of SyntaxNode)(node)
            '_associateInParent = associateInParent
        End Sub

        Public Sub New(token As SyntaxToken)
            Me.New(token.Span)
            '_associatedNode = New WeakReference(Of SyntaxNode)(token.Parent)
            '_associateInParent = False
        End Sub

        Public Sub New(nodeOrToken As SyntaxNodeOrToken)
            Me.New(nodeOrToken.Span)
            'If nodeOrToken.IsNode Then
            '    _associatedNode = New WeakReference(Of SyntaxNode)(nodeOrToken.AsNode())
            'Else
            '    _associatedNode = New WeakReference(Of SyntaxNode)(nodeOrToken.AsToken().Parent)
            'End If

            '_associateInParent = False
        End Sub

        Public Sub New(trivia As SyntaxTrivia)
            Me.New(trivia.Span)
            '_associatedNode = New WeakReference(Of SyntaxNode)(trivia.Token.Parent)
            '_associateInParent = False
        End Sub

        Public Overrides ReadOnly Property SourceSpan As TextSpan
            Get
                Return _span
            End Get
        End Property

        Public Overloads Function Equals(other As SpeculativeLocation) As Boolean
            Return other IsNot Nothing AndAlso other._span.Equals(_span)
        End Function

        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, SpeculativeLocation))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _span.GetHashCode()
        End Function
    End Class
End Namespace
