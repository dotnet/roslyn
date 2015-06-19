' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundPseudoVariable

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            If Not _IsLValue Then
                Return Me
            End If
            Return Update(_LocalSymbol, isLValue:=False, emitExpressions:=_EmitExpressions, type:=Type)
        End Function

    End Class

End Namespace
