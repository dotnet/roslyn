' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.FileHeaders

Namespace Microsoft.CodeAnalysis.VisualBasic.FileHeaders
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicFileHeaderCodeFixProvider))>
    <[Shared]>
    Friend Class VisualBasicFileHeaderCodeFixProvider
        Inherits AbstractFileHeaderCodeFixProvider

        Protected Overrides ReadOnly Property CommentTriviaKind As Integer
            Get
                Return SyntaxKind.CommentTrivia
            End Get
        End Property

        Protected Overrides ReadOnly Property WhitespaceTriviaKind As Integer
            Get
                Return SyntaxKind.WhitespaceTrivia
            End Get
        End Property

        Protected Overrides ReadOnly Property EndOfLineTriviaKind As Integer
            Get
                Return SyntaxKind.EndOfLineTrivia
            End Get
        End Property

        Protected Overrides ReadOnly Property CommentPrefix As String
            Get
                Return "'"
            End Get
        End Property

        Protected Overrides Function EndOfLine(text As String) As SyntaxTrivia
            Return SyntaxFactory.EndOfLine(text)
        End Function

        Protected Overrides Function ParseLeadingTrivia(text As String) As SyntaxTriviaList
            Return SyntaxFactory.ParseLeadingTrivia(text)
        End Function

        Protected Overrides Function ParseFileHeader(root As SyntaxNode) As FileHeader
            Return FileHeaderHelpers.ParseFileHeader(root)
        End Function
    End Class
End Namespace
