' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundRelationalCaseClause

#If DEBUG Then
        Private Sub Validate()
            ValidateValueAndCondition(ValueOpt, ConditionOpt, OperatorKind)
        End Sub
#End If

    End Class
End Namespace
