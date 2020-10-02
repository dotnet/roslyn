' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SplitComment
    <ExportLanguageService(GetType(ISplitCommentService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicSplitCommentService
        Implements ISplitCommentService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public ReadOnly Property CommentStart As String Implements ISplitCommentService.CommentStart
            Get
                Return "'"
            End Get
        End Property

        Public Function IsAllowed(root As SyntaxNode, trivia As SyntaxTrivia) As Boolean Implements ISplitCommentService.IsAllowed
            ' We don't currently allow splitting the comment if there is a preceding line continuation character.  This is
            ' primarily because this is not a common enough scenario to warrant the extra complexity in this fixer.

            Dim currentTrivia = trivia
            While currentTrivia <> Nothing AndAlso currentTrivia.SpanStart > 0
                Dim previousTrivia = root.FindTrivia(currentTrivia.SpanStart - 1)
                If previousTrivia.IsKind(SyntaxKind.LineContinuationTrivia) Then
                    Return False
                End If

                If previousTrivia.IsKind(SyntaxKind.WhitespaceTrivia) Then
                    currentTrivia = previousTrivia
                    Continue While
                End If

                Return True
            End While

            Return True
        End Function
    End Class
End Namespace
