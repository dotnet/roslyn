﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class MultiLineLambdaExpressionSyntax

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use SubOrFunctionHeader instead.", True)>
        Public Shadows ReadOnly Property Begin As LambdaHeaderSyntax
            Get
                Return SubOrFunctionHeader
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithBlockStatement or a more specific property (e.g. WithClassStatement) instead.", True)>
        Public Function WithBegin(begin As LambdaHeaderSyntax) As MultiLineLambdaExpressionSyntax
            Return WithSubOrFunctionHeader(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use EndBlockStatement or a more specific property (e.g. EndClassStatement) instead.", True)>
        Public ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndSubOrFunctionStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithEndBlockStatement or a more specific property (e.g. WithEndClassStatement) instead.", True)>
        Public Function WithEnd([end] As EndBlockStatementSyntax) As MultiLineLambdaExpressionSyntax
            Return WithEndSubOrFunctionStatement([end])
        End Function

    End Class

End Namespace
