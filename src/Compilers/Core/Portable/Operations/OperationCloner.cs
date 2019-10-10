// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed class OperationCloner : OperationVisitor<object, IOperation>
    {
        private static readonly OperationCloner s_instance = new OperationCloner();

        /// <summary>
        /// Deep clone given IOperation
        /// </summary>
        public static T CloneOperation<T>(T operation) where T : IOperation
        {
            return s_instance.Visit(operation);
        }

        private OperationCloner()
        {
        }

        private T Visit<T>(T node) where T : IOperation
        {
            return (T)Visit(node, argument: null);
        }

        public IOperation Visit(IOperation operation)
        {
            return Visit(operation, argument: null);
        }

        public override IOperation DefaultVisit(IOperation operation, object argument)
        {
            // this should never reach, otherwise, there is missing override for IOperation type
            throw ExceptionUtilities.Unreachable;
        }

        internal override IOperation VisitNoneOperation(IOperation operation, object argument)
        {
            return new NoneOperation(VisitArray(operation.Children.ToImmutableArray()), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.ConstantValue, operation.IsImplicit);
        }

        private ImmutableArray<T> VisitArray<T>(ImmutableArray<T> nodes) where T : IOperation
        {
            // clone the array
            return nodes.SelectAsArray(n => Visit(n));
        }

        private ImmutableArray<(ISymbol, T)> VisitArray<T>(ImmutableArray<(ISymbol, T)> nodes) where T : IOperation
        {
            // clone the array
            return nodes.SelectAsArray(n => (n.Item1, Visit(n.Item2)));
        }

        public override IOperation VisitBlock(IBlockOperation operation, object argument)
        {
            return new BlockOperation(VisitArray(operation.Operations), operation.Locals, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, object argument)
        {
            return new VariableDeclarationGroupOperation(VisitArray(operation.Declarations), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitVariableDeclarator(IVariableDeclaratorOperation operation, object argument)
        {
            return new VariableDeclaratorOperation(operation.Symbol, Visit(operation.Initializer), VisitArray(operation.IgnoredArguments), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitVariableDeclaration(IVariableDeclarationOperation operation, object argument)
        {
            return new VariableDeclarationOperation(VisitArray(operation.Declarators), Visit(operation.Initializer), VisitArray(operation.IgnoredDimensions), operation.DeclarationKind, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConversion(IConversionOperation operation, object argument)
        {
            return new ConversionOperation(Visit(operation.Operand), ((BaseConversionOperation)operation).ConversionConvertible, operation.IsTryCast, operation.IsChecked, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSwitch(ISwitchOperation operation, object argument)
        {
            return new SwitchOperation(operation.Locals, Visit(operation.Value), VisitArray(operation.Cases), operation.ExitLabel, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSwitchCase(ISwitchCaseOperation operation, object argument)
        {
            return new SwitchCaseOperation(operation.Locals, ((BaseSwitchCaseOperation)operation).Condition, VisitArray(operation.Clauses), VisitArray(operation.Body), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, object argument)
        {
            return new SingleValueCaseClauseOperation(operation.Label, Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, object argument)
        {
            return new RelationalCaseClauseOperation(Visit(operation.Value), operation.Relation, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRangeCaseClause(IRangeCaseClauseOperation operation, object argument)
        {
            return new RangeCaseClauseOperation(Visit(operation.MinimumValue), Visit(operation.MaximumValue), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, object argument)
        {
            return new DefaultCaseClauseOperation(operation.Label, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitWhileLoop(IWhileLoopOperation operation, object argument)
        {
            return new WhileLoopOperation(Visit(operation.Condition), Visit(operation.Body), Visit(operation.IgnoredCondition), operation.Locals, operation.ContinueLabel, operation.ExitLabel, operation.ConditionIsTop, operation.ConditionIsUntil, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForLoop(IForLoopOperation operation, object argument)
        {
            return new ForLoopOperation(VisitArray(operation.Before), Visit(operation.Condition), VisitArray(operation.AtLoopBottom), operation.Locals, operation.ConditionLocals,
                operation.ContinueLabel, operation.ExitLabel, Visit(operation.Body), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForToLoop(IForToLoopOperation operation, object argument)
        {
            return new ForToLoopOperation(operation.Locals, operation.IsChecked, ((BaseForToLoopOperation)operation).Info, operation.ContinueLabel, operation.ExitLabel,
                                          Visit(operation.LoopControlVariable), Visit(operation.InitialValue), Visit(operation.LimitValue), Visit(operation.StepValue),
                                          Visit(operation.Body), VisitArray(operation.NextVariables), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type,
                                          operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForEachLoop(IForEachLoopOperation operation, object argument)
        {
            return new ForEachLoopOperation(operation.Locals, operation.ContinueLabel, operation.ExitLabel, Visit(operation.LoopControlVariable),
                                            Visit(operation.Collection), VisitArray(operation.NextVariables), operation.IsAsynchronous, Visit(operation.Body), ((BaseForEachLoopOperation)operation).Info,
                                            ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLabeled(ILabeledOperation operation, object argument)
        {
            return new LabeledOperation(operation.Label, Visit(operation.Operation), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitBranch(IBranchOperation operation, object argument)
        {
            return new BranchOperation(operation.Target, operation.BranchKind, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEmpty(IEmptyOperation operation, object argument)
        {
            return new EmptyOperation(((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitReturn(IReturnOperation operation, object argument)
        {
            return new ReturnOperation(Visit(operation.ReturnedValue), operation.Kind, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLock(ILockOperation operation, object argument)
        {
            var baseLockStatement = (BaseLockOperation)operation;
            return new LockOperation(Visit(operation.LockedValue), Visit(operation.Body), baseLockStatement.LockTakenSymbol, baseLockStatement.OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTry(ITryOperation operation, object argument)
        {
            return new TryOperation(Visit(operation.Body), VisitArray(operation.Catches), Visit(operation.Finally), operation.ExitLabel, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCatchClause(ICatchClauseOperation operation, object argument)
        {
            return new CatchClauseOperation(Visit(operation.ExceptionDeclarationOrExpression), operation.ExceptionType, operation.Locals, Visit(operation.Filter), Visit(operation.Handler), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitUsing(IUsingOperation operation, object argument)
        {
            return new UsingOperation(Visit(operation.Resources), Visit(operation.Body), operation.Locals, operation.IsAsynchronous, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        // https://github.com/dotnet/roslyn/issues/21281
        internal override IOperation VisitFixed(IFixedOperation operation, object argument)
        {
            return new FixedOperation(operation.Locals, Visit(operation.Variables), Visit(operation.Body), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitAggregateQuery(IAggregateQueryOperation operation, object argument)
        {
            return new AggregateQueryOperation(Visit(operation.Group), Visit(operation.Aggregation), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitExpressionStatement(IExpressionStatementOperation operation, object argument)
        {
            return new ExpressionStatementOperation(Visit(operation.Operation), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitWith(IWithOperation operation, object argument)
        {
            return new WithOperation(Visit(operation.Body), Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitStop(IStopOperation operation, object argument)
        {
            return new StopOperation(((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEnd(IEndOperation operation, object argument)
        {
            return new EndOperation(((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInvocation(IInvocationOperation operation, object argument)
        {
            return new InvocationOperation(operation.TargetMethod, Visit(operation.Instance), operation.IsVirtual, VisitArray(operation.Arguments), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArgument(IArgumentOperation operation, object argument)
        {
            var baseArgument = (BaseArgumentOperation)operation;
            return new ArgumentOperation(Visit(operation.Value), operation.ArgumentKind, operation.Parameter, baseArgument.InConversionConvertibleOpt, baseArgument.OutConversionConvertibleOpt, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.IsImplicit);
        }

        public override IOperation VisitOmittedArgument(IOmittedArgumentOperation operation, object argument)
        {
            return new OmittedArgumentOperation(((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayElementReference(IArrayElementReferenceOperation operation, object argument)
        {
            return new ArrayElementReferenceOperation(Visit(operation.ArrayReference), VisitArray(operation.Indices), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLocalReference(ILocalReferenceOperation operation, object argument)
        {
            return new LocalReferenceOperation(operation.Local, operation.IsDeclaration, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParameterReference(IParameterReferenceOperation operation, object argument)
        {
            return new ParameterReferenceOperation(operation.Parameter, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInstanceReference(IInstanceReferenceOperation operation, object argument)
        {
            return new InstanceReferenceOperation(operation.ReferenceKind, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFieldReference(IFieldReferenceOperation operation, object argument)
        {
            return new FieldReferenceOperation(operation.Field, operation.IsDeclaration, Visit(operation.Instance), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitMethodReference(IMethodReferenceOperation operation, object argument)
        {
            return new MethodReferenceOperation(operation.Method, operation.IsVirtual, Visit(operation.Instance), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPropertyReference(IPropertyReferenceOperation operation, object argument)
        {
            return new PropertyReferenceOperation(operation.Property, VisitArray(operation.Arguments), Visit(operation.Instance), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEventReference(IEventReferenceOperation operation, object argument)
        {
            return new EventReferenceOperation(operation.Event, Visit(operation.Instance), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEventAssignment(IEventAssignmentOperation operation, object argument)
        {
            return new EventAssignmentOperation(Visit(operation.EventReference), Visit(operation.HandlerValue), operation.Adds, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConditionalAccess(IConditionalAccessOperation operation, object argument)
        {
            return new ConditionalAccessOperation(Visit(operation.WhenNotNull), Visit(operation.Operation), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, object argument)
        {
            return new ConditionalAccessInstanceOperation(((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitPlaceholder(IPlaceholderOperation operation, object argument)
        {
            return new PlaceholderOperation(operation.PlaceholderKind, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitUnaryOperator(IUnaryOperation operation, object argument)
        {
            return new UnaryOperation(operation.OperatorKind, Visit(operation.Operand), operation.IsLifted, operation.IsChecked, operation.OperatorMethod, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitBinaryOperator(IBinaryOperation operation, object argument)
        {
            return new BinaryOperation(operation.OperatorKind, Visit(operation.LeftOperand), Visit(operation.RightOperand),
                                                operation.IsLifted, operation.IsChecked, operation.IsCompareText, operation.OperatorMethod,
                                                ((BaseBinaryOperation)operation).UnaryOperatorMethod, ((Operation)operation).OwningSemanticModel,
                                                operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTupleBinaryOperator(ITupleBinaryOperation operation, object argument)
        {
            return new TupleBinaryOperation(operation.OperatorKind, Visit(operation.LeftOperand), Visit(operation.RightOperand), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCompoundAssignment(ICompoundAssignmentOperation operation, object argument)
        {
            var compoundAssignment = (BaseCompoundAssignmentOperation)operation;
            return new CompoundAssignmentOperation(compoundAssignment.InConversionConvertible, compoundAssignment.OutConversionConvertible, operation.OperatorKind, operation.IsLifted, operation.IsChecked, operation.OperatorMethod, Visit(operation.Target), Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConditional(IConditionalOperation operation, object argument)
        {
            return new ConditionalOperation(Visit(operation.Condition), Visit(operation.WhenTrue), Visit(operation.WhenFalse), operation.IsRef, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCoalesce(ICoalesceOperation operation, object argument)
        {
            var coalesceOperation = (BaseCoalesceOperation)operation;
            return new CoalesceOperation(Visit(operation.Value), Visit(operation.WhenNull), coalesceOperation.ValueConversionConvertible, coalesceOperation.OwningSemanticModel,
                                          operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCoalesceAssignment(ICoalesceAssignmentOperation operation, object argument)
        {
            return new CoalesceAssignmentOperation(Visit(operation.Target), Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIsType(IIsTypeOperation operation, object argument)
        {
            return new IsTypeOperation(Visit(operation.ValueOperand), operation.TypeOperand, operation.IsNegated, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSizeOf(ISizeOfOperation operation, object argument)
        {
            return new SizeOfOperation(operation.TypeOperand, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTypeOf(ITypeOfOperation operation, object argument)
        {
            return new TypeOfOperation(operation.TypeOperand, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAnonymousFunction(IAnonymousFunctionOperation operation, object argument)
        {
            return new AnonymousFunctionOperation(operation.Symbol, Visit(operation.Body), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation, object argument)
        {
            var anonymous = (FlowAnonymousFunctionOperation)operation;
            return new FlowAnonymousFunctionOperation(in anonymous.Context, anonymous.Original, operation.IsImplicit);
        }

        public override IOperation VisitDelegateCreation(IDelegateCreationOperation operation, object argument)
        {
            return new DelegateCreationOperation(Visit(operation.Target), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLiteral(ILiteralOperation operation, object argument)
        {
            return new LiteralOperation(((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAwait(IAwaitOperation operation, object argument)
        {
            return new AwaitOperation(Visit(operation.Operation), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitNameOf(INameOfOperation operation, object argument)
        {
            return new NameOfOperation(Visit(operation.Argument), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitThrow(IThrowOperation operation, object argument)
        {
            return new ThrowOperation(Visit(operation.Exception), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAddressOf(IAddressOfOperation operation, object argument)
        {
            return new AddressOfOperation(Visit(operation.Reference), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitObjectCreation(IObjectCreationOperation operation, object argument)
        {
            return new ObjectCreationOperation(operation.Constructor, Visit(operation.Initializer), VisitArray(operation.Arguments), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, object argument)
        {
            return new AnonymousObjectCreationOperation(VisitArray(operation.Initializers), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, object argument)
        {
            return new ObjectOrCollectionInitializerOperation(VisitArray(operation.Initializers), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitMemberInitializer(IMemberInitializerOperation operation, object argument)
        {
            return new MemberInitializerOperation(Visit(operation.InitializedMember), Visit(operation.Initializer), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFieldInitializer(IFieldInitializerOperation operation, object argument)
        {
            return new FieldInitializerOperation(operation.InitializedFields, operation.Locals, Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitVariableInitializer(IVariableInitializerOperation operation, object argument)
        {
            return new VariableInitializerOperation(Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPropertyInitializer(IPropertyInitializerOperation operation, object argument)
        {
            return new PropertyInitializerOperation(operation.InitializedProperties, operation.Locals, Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParameterInitializer(IParameterInitializerOperation operation, object argument)
        {
            return new ParameterInitializerOperation(operation.Parameter, operation.Locals, Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayCreation(IArrayCreationOperation operation, object argument)
        {
            return new ArrayCreationOperation(VisitArray(operation.DimensionSizes), Visit(operation.Initializer), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayInitializer(IArrayInitializerOperation operation, object argument)
        {
            return new ArrayInitializerOperation(VisitArray(operation.ElementValues), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSimpleAssignment(ISimpleAssignmentOperation operation, object argument)
        {
            return new SimpleAssignmentOperation(operation.IsRef, Visit(operation.Target), Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, object argument)
        {
            return new DeconstructionAssignmentOperation(Visit(operation.Target), Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDeclarationExpression(IDeclarationExpressionOperation operation, object argument)
        {
            return new DeclarationExpressionOperation(Visit(operation.Expression), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, object argument)
        {
            return new IncrementOrDecrementOperation(operation.IsPostfix, operation.IsLifted, operation.IsChecked, Visit(operation.Target), operation.OperatorMethod, operation.Kind, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParenthesized(IParenthesizedOperation operation, object argument)
        {
            return new ParenthesizedOperation(Visit(operation.Operand), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, object argument)
        {
            return new DynamicMemberReferenceOperation(Visit(operation.Instance), operation.MemberName, operation.TypeArguments, operation.ContainingType, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, object argument)
        {
            return new DynamicObjectCreationOperation(VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, Visit(operation.Initializer), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicInvocation(IDynamicInvocationOperation operation, object argument)
        {
            return new DynamicInvocationOperation(Visit(operation.Operation), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, object argument)
        {
            return new DynamicIndexerAccessOperation(Visit(operation.Operation), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDefaultValue(IDefaultValueOperation operation, object argument)
        {
            return new DefaultValueOperation(((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, object argument)
        {
            return new TypeParameterObjectCreationOperation(Visit(operation.Initializer), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation, object argument)
        {
            return new NoPiaObjectCreationOperation(Visit(operation.Initializer), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInvalid(IInvalidOperation operation, object argument)
        {
            return new InvalidOperation(VisitArray(operation.Children.ToImmutableArray()), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLocalFunction(ILocalFunctionOperation operation, object argument)
        {
            return new LocalFunctionOperation(operation.Symbol, Visit(operation.Body), Visit(operation.IgnoredBody), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolatedString(IInterpolatedStringOperation operation, object argument)
        {
            return new InterpolatedStringOperation(VisitArray(operation.Parts), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, object argument)
        {
            return new InterpolatedStringTextOperation(Visit(operation.Text), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolation(IInterpolationOperation operation, object argument)
        {
            return new InterpolationOperation(Visit(operation.Expression), Visit(operation.Alignment), Visit(operation.FormatString), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIsPattern(IIsPatternOperation operation, object argument)
        {
            return new IsPatternOperation(Visit(operation.Value), Visit(operation.Pattern), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConstantPattern(IConstantPatternOperation operation, object argument)
        {
            return new ConstantPatternOperation(operation.InputType, Visit(operation.Value), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.IsImplicit);
        }

        public override IOperation VisitDeclarationPattern(IDeclarationPatternOperation operation, object argument)
        {
            return new DeclarationPatternOperation(
                operation.MatchedType,
                operation.MatchesNull,
                operation.DeclaredSymbol,
                operation.InputType,
                ((Operation)operation).OwningSemanticModel,
                operation.Syntax,
                operation.Type,
                operation.ConstantValue,
                operation.IsImplicit);
        }

        public override IOperation VisitRecursivePattern(IRecursivePatternOperation operation, object argument)
        {
            return new RecursivePatternOperation(
                operation.InputType,
                operation.MatchedType,
                operation.DeconstructSymbol,
                VisitArray(operation.DeconstructionSubpatterns),
                VisitArray(operation.PropertySubpatterns),
                operation.DeclaredSymbol,
                ((Operation)operation).OwningSemanticModel,
                operation.Syntax,
                operation.IsImplicit);
        }

        public override IOperation VisitPropertySubpattern(IPropertySubpatternOperation operation, object argument)
        {
            return new PropertySubpatternOperation(
                semanticModel: ((Operation)operation).OwningSemanticModel,
                operation.Syntax,
                operation.IsImplicit,
                Visit(operation.Member),
                Visit(operation.Pattern));
        }

        public override IOperation VisitPatternCaseClause(IPatternCaseClauseOperation operation, object argument)
        {
            return new PatternCaseClauseOperation(operation.Label, Visit(operation.Pattern), Visit(operation.Guard), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTuple(ITupleOperation operation, object argument)
        {
            return new TupleOperation(VisitArray(operation.Elements), operation.NaturalType, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTranslatedQuery(ITranslatedQueryOperation operation, object argument)
        {
            return new TranslatedQueryOperation(Visit(operation.Operation), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRaiseEvent(IRaiseEventOperation operation, object argument)
        {
            return new RaiseEventOperation(Visit(operation.EventReference), VisitArray(operation.Arguments), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConstructorBodyOperation(IConstructorBodyOperation operation, object argument)
        {
            return new ConstructorBodyOperation(operation.Locals, ((Operation)operation).OwningSemanticModel, operation.Syntax, Visit(operation.Initializer), Visit(operation.BlockBody), Visit(operation.ExpressionBody));
        }

        public override IOperation VisitMethodBodyOperation(IMethodBodyOperation operation, object argument)
        {
            return new MethodBodyOperation(((Operation)operation).OwningSemanticModel, operation.Syntax, Visit(operation.BlockBody), Visit(operation.ExpressionBody));
        }

        public override IOperation VisitDiscardOperation(IDiscardOperation operation, object argument)
        {
            return new DiscardOperation(
                operation.DiscardSymbol, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDiscardPattern(IDiscardPatternOperation operation, object argument)
        {
            return new DiscardPatternOperation(operation.InputType, operation.SemanticModel, operation.Syntax, operation.IsImplicit);
        }

        public override IOperation VisitSwitchExpression(ISwitchExpressionOperation operation, object argument)
        {
            return new SwitchExpressionOperation(operation.Type, Visit(operation.Value), VisitArray(operation.Arms), operation.SemanticModel, operation.Syntax, operation.IsImplicit);
        }

        public override IOperation VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation, object argument)
        {
            return new SwitchExpressionArmOperation(operation.Locals, Visit(operation.Pattern), Visit(operation.Guard), Visit(operation.Value), operation.SemanticModel, operation.Syntax, operation.IsImplicit);
        }

        public override IOperation VisitFlowCapture(IFlowCaptureOperation operation, object argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, object argument)
        {
            return new FlowCaptureReferenceOperation(operation.Id, operation.Syntax, operation.Type, constantValue: operation.ConstantValue);
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

        public override IOperation VisitRangeOperation(IRangeOperation operation, object argument)
        {
            return new RangeOperation(operation.IsLifted, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, Visit(operation.LeftOperand), Visit(operation.RightOperand), operation.Method, operation.IsImplicit);
        }

        public override IOperation VisitReDim(IReDimOperation operation, object argument)
        {
            return new ReDimOperation(VisitArray(operation.Clauses), operation.Preserve, ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitReDimClause(IReDimClauseOperation operation, object argument)
        {
            return new ReDimClauseOperation(Visit(operation.Operand), VisitArray(operation.DimensionSizes), ((Operation)operation).OwningSemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }
    }
}
