' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports Microsoft.CodeAnalysis.Operations

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Class VisualBasicOperationCloner
        Inherits OperationCloner

        Public Shared ReadOnly Property Instance As OperationCloner = New VisualBasicOperationCloner()

        Public Overrides Function VisitArgument(operation As IArgumentOperation, argument As Object) As IOperation
            Return New VisualBasicArgument(operation.ArgumentKind, operation.Parameter, Visit(operation.Value), operation.GetInConversion(), operation.GetOutConversion(), DirectCast(operation, Operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit)
        End Function

        Public Overrides Function VisitConversion(operation As IConversionOperation, argument As Object) As IOperation
            Return New VisualBasicConversionExpression(Visit(operation.Operand), operation.GetConversion(), operation.IsTryCast, operation.IsChecked, DirectCast(operation, Operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit)
        End Function

        Public Overrides Function VisitCompoundAssignment(operation As ICompoundAssignmentOperation, argument As Object) As IOperation
            Dim baseType = DirectCast(operation, BaseVisualBasicCompoundAssignmentOperation)
            Return New VisualBasicCompoundAssignmentOperation(Visit(operation.Target), Visit(operation.Value), baseType.InConversionInternal, baseType.OutConversionInternal, operation.OperatorKind, operation.IsLifted, operation.IsChecked, operation.OperatorMethod, DirectCast(operation, Operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit)
        End Function
    End Class
End Namespace
