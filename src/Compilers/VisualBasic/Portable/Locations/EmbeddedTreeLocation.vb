' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A program location in source code.
    ''' </summary>
    Friend NotInheritable Class EmbeddedTreeLocation
        Inherits VBLocation

        Friend ReadOnly _embeddedKind As EmbeddedSymbolKind
        Friend ReadOnly _span As TextSpan

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.None
            End Get
        End Property

        Friend Overrides ReadOnly Property EmbeddedKind As EmbeddedSymbolKind
            Get
                Return _embeddedKind
            End Get
        End Property

        Friend Overrides ReadOnly Property PossiblyEmbeddedOrMySourceSpan As TextSpan
            Get
                Return _span
            End Get
        End Property

        Friend Overrides ReadOnly Property PossiblyEmbeddedOrMySourceTree As SyntaxTree
            Get
                Return EmbeddedSymbolManager.GetEmbeddedTree(Me._embeddedKind)
            End Get
        End Property

        Public Sub New(embeddedKind As EmbeddedSymbolKind, span As TextSpan)
            Debug.Assert(embeddedKind = EmbeddedSymbolKind.VbCore OrElse
                         embeddedKind = EmbeddedSymbolKind.XmlHelper OrElse
                         embeddedKind = EmbeddedSymbolKind.EmbeddedAttribute)

            _embeddedKind = embeddedKind
            _span = span
        End Sub

        Public Overloads Function Equals(other As EmbeddedTreeLocation) As Boolean
            If Me Is other Then
                Return True
            End If

            Return other IsNot Nothing AndAlso other.EmbeddedKind = Me._embeddedKind AndAlso other._span.Equals(Me._span)
        End Function

        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, EmbeddedTreeLocation))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(CInt(_embeddedKind).GetHashCode(), _span.GetHashCode())
        End Function
    End Class
End Namespace
