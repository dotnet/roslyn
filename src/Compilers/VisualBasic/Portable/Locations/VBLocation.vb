' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Represents a VB location in source code or metadata.
    ''' </summary>
    Friend MustInherit Class VBLocation
        Inherits Location

        ' Returns the type of embedded tree, if this is an embedded syntax tree.
        Friend Overridable ReadOnly Property EmbeddedKind As EmbeddedSymbolKind
            Get
                Return EmbeddedSymbolKind.None
            End Get
        End Property

        ' Similar to SourceSpan, but also works for synthetic locations. 
        Friend Overridable ReadOnly Property PossiblyEmbeddedOrMySourceSpan As TextSpan
            Get
                Return Me.SourceSpan
            End Get
        End Property

        ' Similar to SourceTree, but also works for synthetic locations. 
        Friend Overridable ReadOnly Property PossiblyEmbeddedOrMySourceTree As SyntaxTree
            Get
                Return DirectCast(Me.SourceTree, VisualBasicSyntaxTree)
            End Get
        End Property

    End Class

End Namespace

