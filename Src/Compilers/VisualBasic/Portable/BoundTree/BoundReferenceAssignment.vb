' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundReferenceAssignment
#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(ByRefLocal.LocalSymbol.IsByRef AndAlso Type = Target.Type)
        End Sub
#End If

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundReferenceAssignment
            If _IsLValue Then
                Return Update(ByRefLocal, Target, False, Type)
            End If

            Return Me
        End Function
    End Class

End Namespace
