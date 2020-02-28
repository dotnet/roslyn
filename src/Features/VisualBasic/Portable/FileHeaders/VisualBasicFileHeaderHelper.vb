' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.FileHeaders

Namespace Microsoft.CodeAnalysis.VisualBasic.FileHeaders
    Friend NotInheritable Class VisualBasicFileHeaderHelper
        Inherits AbstractFileHeaderHelper

        Public Shared ReadOnly Property Instance As VisualBasicFileHeaderHelper = New VisualBasicFileHeaderHelper()

        Private Sub New()
        End Sub

        Friend Overrides ReadOnly Property SingleLineCommentTriviaKind As Integer
            Get
                Return SyntaxKind.CommentTrivia
            End Get
        End Property

        Friend Overrides ReadOnly Property MultiLineCommentTriviaKind As Integer
            Get
                Return SyntaxKind.None
            End Get
        End Property

        Friend Overrides ReadOnly Property WhitespaceTriviaKind As Integer
            Get
                Return SyntaxKind.WhitespaceTrivia
            End Get
        End Property

        Friend Overrides ReadOnly Property EndOfLineTriviaKind As Integer
            Get
                Return SyntaxKind.EndOfLineTrivia
            End Get
        End Property

        Friend Overrides ReadOnly Property CommentPrefix As String
            Get
                Return "'"
            End Get
        End Property

        Protected Overrides Function GetTextContextOfComment(commentTrivia As SyntaxTrivia) As String
            If Not commentTrivia.IsKind(SyntaxKind.CommentTrivia) Then
                Throw ExceptionUtilities.UnexpectedValue(commentTrivia.Kind())
            End If

            Return commentTrivia.ToFullString().Substring(1)
        End Function
    End Class
End Namespace
