' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

    Partial Public Class LambdaExpressionSyntax

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use SubOrFunctionHeader instead.", True)>
        Public ReadOnly Property Begin As LambdaHeaderSyntax
            Get
                Return SubOrFunctionHeader
            End Get
        End Property

    End Class

End Namespace
