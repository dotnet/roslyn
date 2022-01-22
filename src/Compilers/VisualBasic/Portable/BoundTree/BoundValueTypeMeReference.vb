' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundValueTypeMeReference

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Me.Type.IsValueType)
            Debug.Assert(Not Me.Type.IsTypeParameter)
        End Sub
#End If

        Public Overrides ReadOnly Property IsLValue As Boolean
            Get
                Return True
            End Get
        End Property

    End Class

End Namespace

