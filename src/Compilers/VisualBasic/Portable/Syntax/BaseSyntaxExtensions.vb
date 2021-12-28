' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend Module BaseSyntaxExtensions
        ' Methods

        <Extension()>
        Friend Function ToGreen(node As InternalSyntax.VisualBasicSyntaxNode) As InternalSyntax.VisualBasicSyntaxNode
            Debug.Assert(False, "just use the node")
            Return node
        End Function

        <Extension()>
        Friend Function ToGreen(node As VisualBasicSyntaxNode) As InternalSyntax.VisualBasicSyntaxNode
            Return If(node Is Nothing, Nothing, node.VbGreen)
        End Function

        <Extension()>
        Friend Function ToRed(node As InternalSyntax.VisualBasicSyntaxNode) As SyntaxNode
            If node Is Nothing Then
                Return Nothing
            End If
            Dim red = node.CreateRed(Nothing, 0)
            Return red
        End Function

        <Extension()>
        Friend Function ToRed(node As VisualBasicSyntaxNode) As VisualBasicSyntaxNode
            Debug.Assert(False, "just use the node")
            Return node
        End Function
    End Module
End Namespace
