' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Friend Class EndConstructState
        Private ReadOnly _caretPosition As Integer
        Private ReadOnly _semanticModel As Lazy(Of SemanticModel)
        Private ReadOnly _tree As SyntaxTree
        Private ReadOnly _tokenToLeft As SyntaxToken
        Private ReadOnly _newLineCharacter As String

        Public Sub New(caretPosition As Integer, semanticModel As Lazy(Of SemanticModel), syntaxTree As SyntaxTree, tokenToLeft As SyntaxToken, newLineCharacter As String)
            ThrowIfNull(syntaxTree)

            _caretPosition = caretPosition
            _newLineCharacter = newLineCharacter
            _semanticModel = semanticModel
            _tree = syntaxTree
            _tokenToLeft = tokenToLeft
        End Sub

        Public ReadOnly Property CaretPosition As Integer
            Get
                Return _caretPosition
            End Get
        End Property

        Public ReadOnly Property SemanticModel As SemanticModel
            Get
                Return _semanticModel.Value
            End Get
        End Property

        Public ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _tree
            End Get
        End Property

        Public ReadOnly Property TokenToLeft As SyntaxToken
            Get
                Return _tokenToLeft
            End Get
        End Property

        ''' <summary>
        ''' The new line character that should be used when spitting lines of code.
        ''' </summary>
        Public ReadOnly Property NewLineCharacter As String
            Get
                Return _newLineCharacter
            End Get
        End Property
    End Class
End Namespace
