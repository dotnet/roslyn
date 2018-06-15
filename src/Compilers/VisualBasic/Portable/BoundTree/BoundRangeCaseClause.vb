' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundRangeCaseClause

#If DEBUG Then
        Private Sub Validate()
            ValidateValueAndCondition(LowerBoundOpt, LowerBoundConditionOpt, BinaryOperatorKind.GreaterThanOrEqual)
            ValidateValueAndCondition(UpperBoundOpt, UpperBoundConditionOpt, BinaryOperatorKind.LessThanOrEqual)
        End Sub
#End If

    End Class
End Namespace
