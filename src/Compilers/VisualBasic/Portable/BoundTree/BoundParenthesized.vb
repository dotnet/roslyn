' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundParenthesized

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Type Is _Expression.Type)
        End Sub
#End If

        Public Overrides ReadOnly Property ConstantValueOpt As ConstantValue
            Get
                Return Expression.ConstantValueOpt
            End Get
        End Property
    End Class

End Namespace
