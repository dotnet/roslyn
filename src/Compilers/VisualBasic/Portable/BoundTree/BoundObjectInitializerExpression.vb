﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundObjectInitializerExpression

#If DEBUG Then
        Private Sub Validate()
            If Not Me.HasErrors Then
                For Each initializer In Me.Initializers
                    Debug.Assert(initializer.Kind = BoundKind.AssignmentOperator)
                    Debug.Assert(DirectCast(initializer, BoundAssignmentOperator).Left.Kind = BoundKind.BadExpression OrElse
                                 DirectCast(initializer, BoundAssignmentOperator).Left.Kind = BoundKind.FieldAccess OrElse
                                 DirectCast(initializer, BoundAssignmentOperator).Left.Kind = BoundKind.PropertyAccess)
                Next
            End If
        End Sub
#End If

    End Class

End Namespace
