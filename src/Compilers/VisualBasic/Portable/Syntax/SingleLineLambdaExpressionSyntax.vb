' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class SingleLineLambdaExpressionSyntax

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use SubOrFunctionHeader instead.", True)>
        Public Shadows ReadOnly Property Begin As LambdaHeaderSyntax
            Get
                Return SubOrFunctionHeader
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithSubOrFunctionHeader instead.", True)>
        Public Function WithBegin(begin As LambdaHeaderSyntax) As SingleLineLambdaExpressionSyntax
            Return WithSubOrFunctionHeader(begin)
        End Function

    End Class

    Public Partial Class LambdaExpressionSyntax

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use SubOrFunctionHeader instead.", True)>
        Public ReadOnly Property Begin As LambdaHeaderSyntax
            Get
                Return SubOrFunctionHeader
            End Get
        End Property

    End Class

End Namespace