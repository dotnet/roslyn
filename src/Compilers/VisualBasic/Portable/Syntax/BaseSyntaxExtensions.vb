﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

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
