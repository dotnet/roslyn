' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------------------------------------
' Contains hand-written Partial class extensions to certain of the syntax nodes (other that the 
' base node SyntaxNode, which is in a different file.)
'-----------------------------------------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Public Partial Class StructuredTriviaSyntax
        Inherits VisualBasicSyntaxNode
        Implements IStructuredTriviaSyntax

        Private _parentTrivia As SyntaxTrivia

        Friend Sub New(green As GreenNode, parent As SyntaxNode, startLocation As Integer)
            MyBase.New(green, startLocation, If(parent IsNot Nothing, parent.SyntaxTree, Nothing))
        End Sub

        Friend Shared Function Create(trivia As SyntaxTrivia) As StructuredTriviaSyntax
            Dim parent = DirectCast(trivia.Token.Parent, VisualBasicSyntaxNode)
            Dim position = trivia.Position
            Dim red = DirectCast(trivia.UnderlyingNode.CreateRed(parent, position), StructuredTriviaSyntax)
            red._parentTrivia = trivia
            Return red
        End Function

        Public Overrides ReadOnly Property ParentTrivia As SyntaxTrivia Implements IStructuredTriviaSyntax.ParentTrivia
            Get
                Return _parentTrivia
            End Get
        End Property
    End Class
End Namespace
