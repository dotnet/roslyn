' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports Microsoft.CodeAnalysis.Semantics

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Class VisualBasicOperationCloner
        Inherits OperationCloner

        Public Shared ReadOnly Property Instance As OperationCloner = New VisualBasicOperationCloner()

        Public Overrides Function VisitConversionExpression(operation As IConversionExpression, argument As Object) As IOperation
            Return New VisualBasicConversionExpression(Visit(operation.Operand), operation.GetConversion(), operation.IsExplicitInCode, operation.IsTryCast, operation.IsChecked, DirectCast(operation, Operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue)
        End Function
    End Class
End Namespace
