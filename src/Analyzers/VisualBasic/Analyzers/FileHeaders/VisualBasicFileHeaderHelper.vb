' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.FileHeaders
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.FileHeaders
    Friend NotInheritable Class VisualBasicFileHeaderHelper
        Inherits AbstractFileHeaderHelper

        Public Shared ReadOnly Instance As VisualBasicFileHeaderHelper = New VisualBasicFileHeaderHelper()

        Private Sub New()
            MyBase.New(VisualBasicSyntaxKinds.Instance)
        End Sub

        Public Overrides ReadOnly Property CommentPrefix As String
            Get
                Return "'"
            End Get
        End Property

        Protected Overrides Function GetTextContextOfComment(commentTrivia As SyntaxTrivia) As ReadOnlyMemory(Of Char)
            If Not commentTrivia.IsKind(SyntaxKind.CommentTrivia) Then
                Throw ExceptionUtilities.UnexpectedValue(commentTrivia.Kind())
            End If

            Return commentTrivia.ToFullString().AsMemory().Slice(1)
        End Function
    End Class
End Namespace
