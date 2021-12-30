' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Module LocationExtensions
        <Extension>
        Public Function EmbeddedKind(location As Location) As EmbeddedSymbolKind
            Dim vbloc = TryCast(location, VBLocation)
            If vbloc IsNot Nothing Then
                Return vbloc.EmbeddedKind
            Else
                Return EmbeddedSymbolKind.None
            End If
        End Function

        <Extension>
        Public Function PossiblyEmbeddedOrMySourceSpan(location As Location) As TextSpan
            Dim vbloc = TryCast(location, VBLocation)
            If vbloc IsNot Nothing Then
                Return vbloc.PossiblyEmbeddedOrMySourceSpan
            Else
                Return location.SourceSpan
            End If
        End Function

        <Extension>
        Public Function PossiblyEmbeddedOrMySourceTree(location As Location) As SyntaxTree
            Dim vbloc = TryCast(location, VBLocation)
            If vbloc IsNot Nothing Then
                Return vbloc.PossiblyEmbeddedOrMySourceTree
            Else
                Return DirectCast(location.SourceTree, VisualBasicSyntaxTree)
            End If
        End Function

    End Module
End Namespace
