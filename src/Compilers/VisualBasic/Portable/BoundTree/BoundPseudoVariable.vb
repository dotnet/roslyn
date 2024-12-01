' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundPseudoVariable

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Not Type.IsTypeParameter())
        End Sub
#End If
        Protected Overrides Function MakeRValueImpl() As BoundExpression
            If Not _IsLValue Then
                Return Me
            End If
            Return Update(_LocalSymbol, isLValue:=False, emitExpressions:=_EmitExpressions, type:=Type)
        End Function

    End Class

End Namespace
