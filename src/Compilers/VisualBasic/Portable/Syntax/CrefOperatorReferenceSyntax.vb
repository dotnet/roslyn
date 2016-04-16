' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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