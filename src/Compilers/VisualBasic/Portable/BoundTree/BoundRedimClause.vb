' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundRedimClause

#If DEBUG Then
        Private Sub Validate()
            Select Case Operand.Kind
                Case BoundKind.LateInvocation
                    Dim invocation = DirectCast(Operand, BoundLateInvocation)

                    If Not invocation.ArgumentsOpt.IsDefault Then
                        For Each arg In invocation.ArgumentsOpt
                            Debug.Assert(Not arg.IsSupportingAssignment())
                        Next
                    End If
            End Select
        End Sub
#End If
    End Class

End Namespace
