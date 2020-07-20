' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class VisualBasicSyntaxTreeFactoryServiceFactory

        Partial Friend Class VisualBasicSyntaxTreeFactoryService

            ''' <summary>
            ''' Represents a syntax reference that was passed a null
            ''' reference to a node. In this case, we just hold onto the
            ''' weak tree reference and throw if any invalid properties
            ''' are accessed.
            ''' </summary>
            Private Class NullSyntaxReference
                Inherits SyntaxReference

                Private ReadOnly _tree As SyntaxTree

                Public Sub New(tree As SyntaxTree)
                    _tree = tree
                End Sub

                Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
                    Get
                        Return _tree
                    End Get
                End Property

                Public Overrides Function GetSyntax(Optional cancellationToken As CancellationToken = Nothing) As SyntaxNode
                    Return Nothing
                End Function

                Public Overrides ReadOnly Property Span As TextSpan
                    Get
                        Throw New NotSupportedException("Cannot retrieve the Span of a null syntax reference.")
                    End Get
                End Property
            End Class
        End Class
    End Class
End Namespace
