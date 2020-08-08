' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class ForOrForEachBlockSyntax

        ''' <summary>
        ''' The For or For Each statement that begins the block.
        ''' </summary>
        Public MustOverride ReadOnly Property ForOrForEachStatement As ForOrForEachStatementSyntax

    End Class

    Partial Public Class ForBlockSyntax

        <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
        Public Overrides ReadOnly Property ForOrForEachStatement As ForOrForEachStatementSyntax
            Get
                Return ForStatement
            End Get
        End Property

    End Class

    Partial Public Class ForEachBlockSyntax

        <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
        Public Overrides ReadOnly Property ForOrForEachStatement As ForOrForEachStatementSyntax
            Get
                Return ForEachStatement
            End Get
        End Property

    End Class

End Namespace
