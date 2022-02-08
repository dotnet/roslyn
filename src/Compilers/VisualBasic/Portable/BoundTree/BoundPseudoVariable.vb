' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundPseudoVariable

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            If Not _IsLValue Then
                Return Me
            End If
            Return Update(_LocalSymbol, isLValue:=False, emitExpressions:=_EmitExpressions, type:=Type)
        End Function

    End Class

End Namespace
