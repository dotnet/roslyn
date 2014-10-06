' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend Module BaseSyntaxExtensions
        ' Methods

        <Extension()>
        Friend Function ToGreen(node As InternalSyntax.VBSyntaxNode) As InternalSyntax.VBSyntaxNode
            Debug.Assert(False, "just use the node")
            Return node
        End Function

        <Extension()>
        Friend Function ToGreen(node As VBSyntaxNode) As InternalSyntax.VBSyntaxNode
            Return If(node Is Nothing, Nothing, node.VbGreen)
        End Function

        <Extension()>
        Friend Function ToRed(node As InternalSyntax.VBSyntaxNode) As SyntaxNode
            If node Is Nothing Then
                Return Nothing
            End If
            Dim red = node.CreateRed(Nothing, 0)
            Return red
        End Function

        <Extension()>
        Friend Function ToRed(node As VBSyntaxNode) As VBSyntaxNode
            Debug.Assert(False, "just use the node")
            Return node
        End Function
    End Module
End Namespace
