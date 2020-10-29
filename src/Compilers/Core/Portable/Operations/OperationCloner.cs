// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class OperationCloner : OperationVisitor<object?, IOperation>
#nullable disable
    {
        public IOperation Visit(IOperation operation)
        {
            return Visit(operation, argument: null);
        }

        internal override IOperation VisitNoneOperation(IOperation operation, object argument)
        {
            return new NoneOperation(VisitArray(operation.Children.ToImmutableArray()), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.GetConstantValue(), operation.IsImplicit, operation.Type);
        }

        public override IOperation VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, object argument)
        {
            return new VariableDeclarationGroupOperation(VisitArray(operation.Declarations), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitVariableDeclarator(IVariableDeclaratorOperation operation, object argument)
        {
            return new VariableDeclaratorOperation(operation.Symbol, Visit(operation.Initializer), VisitArray(operation.IgnoredArguments), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitVariableDeclaration(IVariableDeclarationOperation operation, object argument)
        {
            return new VariableDeclarationOperation(VisitArray(operation.Declarators), Visit(operation.Initializer), VisitArray(operation.IgnoredDimensions), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation, object argument)
        {
            var anonymous = (FlowAnonymousFunctionOperation)operation;
            return new FlowAnonymousFunctionOperation(in anonymous.Context, anonymous.Original, operation.IsImplicit);
        }

        public override IOperation VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, object argument)
        {
            return new DynamicMemberReferenceOperation(Visit(operation.Instance), operation.MemberName, operation.TypeArguments, operation.ContainingType, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, object argument)
        {
            return new DynamicObjectCreationOperation(VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, Visit(operation.Initializer), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitDynamicInvocation(IDynamicInvocationOperation operation, object argument)
        {
            return new DynamicInvocationOperation(Visit(operation.Operation), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, object argument)
        {
            return new DynamicIndexerAccessOperation(Visit(operation.Operation), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitInvalid(IInvalidOperation operation, object argument)
        {
            return new InvalidOperation(VisitArray(operation.Children.ToImmutableArray()), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, object argument)
        {
            return new InterpolatedStringTextOperation(Visit(operation.Text), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitInterpolation(IInterpolationOperation operation, object argument)
        {
            return new InterpolationOperation(Visit(operation.Expression), Visit(operation.Alignment), Visit(operation.FormatString), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.GetConstantValue(), operation.IsImplicit);
        }

        public override IOperation VisitConstantPattern(IConstantPatternOperation operation, object argument)
        {
            return new ConstantPatternOperation(operation.InputType, operation.NarrowedType, Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.IsImplicit);
        }

        public override IOperation VisitDeclarationPattern(IDeclarationPatternOperation operation, object argument)
        {
            return new DeclarationPatternOperation(
                operation.MatchedType,
                operation.MatchesNull,
                operation.DeclaredSymbol,
                operation.InputType,
                operation.NarrowedType,
                ((Operation)operation).OwningSemanticModel,
                operation.Syntax,
                operation.Type,
                operation.GetConstantValue(),
                operation.IsImplicit);
        }

        public override IOperation VisitRecursivePattern(IRecursivePatternOperation operation, object argument)
        {
            return new RecursivePatternOperation(
                operation.InputType,
                operation.NarrowedType,
                operation.MatchedType,
                operation.DeconstructSymbol,
                VisitArray(operation.DeconstructionSubpatterns),
                VisitArray(operation.PropertySubpatterns),
                operation.DeclaredSymbol,
                ((Operation)operation).OwningSemanticModel,
                operation.Syntax,
                operation.IsImplicit);
        }

        public override IOperation VisitConstructorBodyOperation(IConstructorBodyOperation operation, object argument)
        {
            return new ConstructorBodyOperation(operation.Locals, ((Operation)operation).OwningSemanticModel, operation.Syntax, Visit(operation.Initializer), Visit(operation.BlockBody), Visit(operation.ExpressionBody));
        }

        public override IOperation VisitMethodBodyOperation(IMethodBodyOperation operation, object argument)
        {
            return new MethodBodyOperation(((Operation)operation).OwningSemanticModel, operation.Syntax, Visit(operation.BlockBody), Visit(operation.ExpressionBody));
        }

        public override IOperation VisitDiscardPattern(IDiscardPatternOperation operation, object argument)
        {
            return new DiscardPatternOperation(operation.InputType, operation.NarrowedType, operation.SemanticModel, operation.Syntax, operation.IsImplicit);
        }

        public override IOperation VisitFlowCapture(IFlowCaptureOperation operation, object argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, object argument)
        {
            return new FlowCaptureReferenceOperation(operation.Id, operation.Syntax, operation.Type, constantValue: operation.GetConstantValue());
        }

        public override IOperation VisitIsNull(IIsNullOperation operation, object argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitCaughtException(ICaughtExceptionOperation operation, object argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation, object argument)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
