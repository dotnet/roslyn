// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpOperationCloner : OperationCloner
    {
        public static OperationCloner Instance { get; } = new CSharpOperationCloner();

        public override IOperation VisitConversionExpression(IConversionExpression operation, object argument)
        {
            return new CSharpConversionExpression(Visit(operation.Operand), operation.GetConversion(), operation.IsExplicitInCode, operation.IsTryCast, operation.IsChecked, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue);
        }
    }
}
