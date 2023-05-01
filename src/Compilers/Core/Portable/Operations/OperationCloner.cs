// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class OperationCloner : OperationVisitor<object?, IOperation>
    {
        [return: NotNullIfNotNull(nameof(operation))]
        public IOperation? Visit(IOperation? operation)
        {
            return Visit(operation, argument: null);
        }

        internal override IOperation VisitNoneOperation(IOperation operation, object? argument)
        {
            return new NoneOperation(VisitArray(((Operation)operation).ChildOperations.ToImmutableArray()), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation, object? argument)
        {
            var anonymous = (FlowAnonymousFunctionOperation)operation;
            return new FlowAnonymousFunctionOperation(in anonymous.Context, anonymous.Original, operation.IsImplicit);
        }

        public override IOperation VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, object? argument)
        {
            return new DynamicObjectCreationOperation(Visit(operation.Initializer), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.IsImplicit);
        }

        public override IOperation VisitDynamicInvocation(IDynamicInvocationOperation operation, object? argument)
        {
            return new DynamicInvocationOperation(Visit(operation.Operation), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.IsImplicit);
        }

        public override IOperation VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, object? argument)
        {
            return new DynamicIndexerAccessOperation(Visit(operation.Operation), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.IsImplicit);
        }

        public override IOperation VisitInvalid(IInvalidOperation operation, object? argument)
        {
            return new InvalidOperation(VisitArray(((InvalidOperation)operation).Children), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitFlowCapture(IFlowCaptureOperation operation, object? argument)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitIsNull(IIsNullOperation operation, object? argument)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitCaughtException(ICaughtExceptionOperation operation, object? argument)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation, object? argument)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
