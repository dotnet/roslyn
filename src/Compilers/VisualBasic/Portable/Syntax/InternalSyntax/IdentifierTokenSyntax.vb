' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    ''' <summary>
    ''' Represents an identifier token. This might include brackets around the name,
    ''' and a type character.
    ''' </summary>
    Friend MustInherit Class IdentifierTokenSyntax
        Inherits SyntaxToken

        Friend Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), text As String, precedingTrivia As GreenNode, followingTrivia As GreenNode)
            MyBase.New(kind, errors, annotations, text, precedingTrivia, followingTrivia)
        End Sub

        ''' <summary>
        ''' Contextual Nodekind
        ''' </summary>
        Friend MustOverride ReadOnly Property PossibleKeywordKind As SyntaxKind

        ''' <summary>
        ''' If true, the identifier was enclosed in brackets, such as "[End]".
        ''' </summary>
        Friend MustOverride ReadOnly Property IsBracketed As Boolean

        ''' <summary>
        ''' The text of the identifier, not including the brackets or type character.
        ''' </summary>
        Friend MustOverride ReadOnly Property IdentifierText As String

        ' TODO: do we need IdentifierText?
        Friend Overrides ReadOnly Property ValueText As String
            Get
                Return IdentifierText
            End Get
        End Property

        Public Overrides ReadOnly Property RawContextualKind As Integer
            Get
                Return Me.PossibleKeywordKind
            End Get
        End Property

        ''' <summary>
        ''' The type character suffix, if present. Returns TypeCharacter.None if no type
        ''' character was present. The only allowed values are None, Integer, Long,
        ''' Decimal, Single, Double, and String.
        ''' </summary>
        Friend MustOverride ReadOnly Property TypeCharacter As TypeCharacter
    End Class
End Namespace
