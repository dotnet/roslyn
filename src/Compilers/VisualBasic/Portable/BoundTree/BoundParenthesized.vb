' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundParenthesized

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
