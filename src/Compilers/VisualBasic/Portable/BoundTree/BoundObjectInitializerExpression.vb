' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
