Imports System
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic

Namespace Roslyn.Services.Editor.VisualBasic.Utilities

    <ExcludeFromCodeCoverage()>
    Friend Class TrivialSyntaxTree
        Inherits SyntaxTree

        Private ReadOnly _path As String
        Private ReadOnly _rootNode As SyntaxNode
        Private ReadOnly _text As CancellableFuture(Of IText)
        Private ReadOnly _options As ParseOptions

        Public Sub New(syntaxTree As SyntaxTree, rootNode As SyntaxNode)
            Me.New(syntaxTree.FilePath, rootNode, syntaxTree.Options)
        End Sub

        Public Sub New(path As String, rootNode As SyntaxNode, options As ParseOptions)
            Me._path = path
            Me._rootNode = Me.CloneNodeAsRoot(rootNode)
            Me._options = options
            Me._text = New CancellableFuture(Of IText)(Function(c) New StringText(rootNode.GetFullText()))
        End Sub

        Public Overrides Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As SyntaxNode
            Return _rootNode
        End Function

        Public Overrides Function TryGetRoot(ByRef root As Compilers.VisualBasic.SyntaxNode) As Boolean
            root = _rootNode
            Return True
        End Function

        Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As IText
            Return _text.GetValue(cancellationToken)
        End Function

        Public Overrides ReadOnly Property Options As ParseOptions
            Get
                Return _options
            End Get

        End Property

        Public Overrides ReadOnly Property FilePath As String
            Get
                Return _path
            End Get

        End Property

        Public Overrides Function WithChange(newText As IText, ParamArray changes As TextChangeRange()) As SyntaxTree
            Throw New NotSupportedException()
        End Function

        Public Overrides Function GetReference(node As SyntaxNode) As SyntaxReference
            Return New TrivialReference(Me, node)
        End Function

        Friend Class TrivialReference
            Inherits SyntaxReference

            Private _syntaxTree As SyntaxTree

            Private _node As SyntaxNode

            Friend Sub New(syntaxTree As SyntaxTree, node As SyntaxNode)
                Me._syntaxTree = syntaxTree
                Me._node = node
            End Sub

            Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
                Get
                    Return _syntaxTree
                End Get

            End Property

            Public Overrides ReadOnly Property Span As TextSpan
                Get
                    Return _node.Span
                End Get

            End Property

            Public Overrides Function GetSyntax() As SyntaxNode
                Return _node
            End Function
        End Class
    End Class
End Namespace