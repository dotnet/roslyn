Imports System
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.VisualBasic

Namespace Roslyn.Services.VisualBasic.LanguageServices
    Partial Friend Class VisualBasicSyntaxTreeFactoryService

        <ExcludeFromCodeCoverage()>
        Private Class TrivialSyntaxTree
            Inherits SyntaxTree

            Private ReadOnly _fileName As String
            Private ReadOnly _rootNode As SyntaxNode
            Private ReadOnly _text As IText
            Private ReadOnly _options As ParseOptions

            Public Sub New(fileName As String, rootNode As SyntaxNode, options As ParseOptions)
                Me._fileName = fileName
                Me._rootNode = rootNode
                Me._options = options
                Me._text = New StringText(rootNode.GetFullText())
            End Sub

            Protected Overrides Function GetRoot(cancellationToken As CancellationToken) As SyntaxNode
                Return _rootNode
            End Function

            Public Overrides ReadOnly Property Text As IText
                Get
                    Return _text
                End Get

            End Property

            Public Overrides ReadOnly Property Options As ParseOptions
                Get
                    Return _options
                End Get

            End Property

            Public Overrides ReadOnly Property FileName As String
                Get
                    Return _fileName
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
    End Class
End Namespace