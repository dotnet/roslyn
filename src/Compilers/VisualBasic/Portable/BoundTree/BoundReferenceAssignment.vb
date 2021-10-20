' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundReferenceAssignment
#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(ByRefLocal.LocalSymbol.IsByRef AndAlso LValue.IsLValue AndAlso TypeSymbol.Equals(Type, LValue.Type, TypeCompareKind.ConsiderEverything))
        End Sub
#End If

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundReferenceAssignment
            If _IsLValue Then
                Return Update(ByRefLocal, LValue, False, Type)
            End If

            Return Me
        End Function
    End Class

End Namespace
