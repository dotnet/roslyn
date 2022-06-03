' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class ArrayRankSpecifierSyntax

        ''' <summary>
        ''' Returns the ranks of this array rank specifier.
        ''' </summary>
        Public ReadOnly Property Rank() As Integer
            Get
                Return Me.CommaTokens.Count + 1
            End Get
        End Property

    End Class
End Namespace
