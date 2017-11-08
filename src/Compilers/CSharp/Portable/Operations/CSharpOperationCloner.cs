// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpOperationCloner : OperationCloner
    {
        public static OperationCloner Instance { get; } = new CSharpOperationCloner();

        public override IOperation VisitArgument(IArgumentOperation operation, object argument)
        {
            return new CSharpArgument(operation.ArgumentKind, operation.Parameter, Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConversion(IConversionOperation operation, object argument)
        {
            return new CSharpConversionExpression(Visit(operation.Operand), operation.GetConversion(), operation.IsTryCast, operation.IsChecked, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCompoundAssignment(ICompoundAssignmentOperation operation, object argument)
        {
            var compoundAssignment = (BaseCSharpCompoundAssignmentOperation)operation;
            return new CSharpCompoundAssignmentOperation(Visit(operation.Target), Visit(operation.Value), compoundAssignment.InConversionInternal, compoundAssignment.OutConversionInternal, operation.OperatorKind, operation.IsLifted, operation.IsChecked, operation.OperatorMethod, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }
    }
}
