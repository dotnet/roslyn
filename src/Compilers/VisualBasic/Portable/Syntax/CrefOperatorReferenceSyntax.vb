' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class CrefOperatorReferenceSyntax

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use OperatorKeyword or a more specific property (e.g. OperatorKeyword) instead.", True)>
        Public ReadOnly Property Keyword As SyntaxToken
            Get
                Return OperatorKeyword
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use OperatorKeyword or a more specific property (e.g. WithOperatorKeyword) instead.", True)>
        Public Function WithKeyword(keyword As SyntaxToken) As CrefOperatorReferenceSyntax
            Return WithOperatorKeyword(keyword)
        End Function

    End Class

End Namespace
