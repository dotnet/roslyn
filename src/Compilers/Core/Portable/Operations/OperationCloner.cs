// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal abstract class OperationCloner : OperationVisitor<object, IOperation>
    {
        protected T Visit<T>(T node) where T : IOperation
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
            return Operation.CreateOperationNone(((Operation)operation).SemanticModel, operation.Syntax, operation.ConstantValue, () => VisitArray(operation.Children.ToImmutableArray()), operation.IsImplicit);
        }

        private ImmutableArray<T> VisitArray<T>(ImmutableArray<T> nodes) where T : IOperation
        {
            // clone the array
            return nodes.SelectAsArray(n => Visit(n));
        }

        public override IOperation VisitBlock(IBlockOperation operation, object argument)
        {
            return new BlockStatement(VisitArray(operation.Operations), operation.Locals, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, object argument)
        {
            return new VariableDeclarationGroupOperation(VisitArray(operation.Declarations), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitVariableDeclarator(IVariableDeclaratorOperation operation, object argument)
        {
            return new VariableDeclarator(operation.Symbol, Visit(operation.Initializer), VisitArray(operation.IgnoredArguments), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitVariableDeclaration(IVariableDeclarationOperation operation, object argument)
        {
            return new VariableDeclaration(VisitArray(operation.Declarators), Visit(operation.Initializer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSwitch(ISwitchOperation operation, object argument)
        {
            return new SwitchStatement(Visit(operation.Value), VisitArray(operation.Cases), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSwitchCase(ISwitchCaseOperation operation, object argument)
        {
            return new SwitchCase(VisitArray(operation.Clauses), VisitArray(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, object argument)
        {
            return new SingleValueCaseClause(Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, object argument)
        {
            return new RelationalCaseClause(Visit(operation.Value), operation.Relation, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRangeCaseClause(IRangeCaseClauseOperation operation, object argument)
        {
            return new RangeCaseClause(Visit(operation.MinimumValue), Visit(operation.MaximumValue), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, object argument)
        {
            return new DefaultCaseClause(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitWhileLoop(IWhileLoopOperation operation, object argument)
        {
            return new WhileLoopStatement(Visit(operation.Condition), Visit(operation.Body), Visit(operation.IgnoredCondition), operation.Locals, operation.ConditionIsTop, operation.ConditionIsUntil, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForLoop(IForLoopOperation operation, object argument)
        {
            return new ForLoopStatement(VisitArray(operation.Before), Visit(operation.Condition), VisitArray(operation.AtLoopBottom), operation.Locals, Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForToLoop(IForToLoopOperation operation, object argument)
        {
            return new ForToLoopStatement(operation.Locals, Visit(operation.LoopControlVariable), Visit(operation.InitialValue), Visit(operation.LimitValue), Visit(operation.StepValue), Visit(operation.Body), VisitArray(operation.NextVariables), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForEachLoop(IForEachLoopOperation operation, object argument)
        {
            return new ForEachLoopStatement(operation.Locals, Visit(operation.LoopControlVariable), Visit(operation.Collection), VisitArray(operation.NextVariables), Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLabeled(ILabeledOperation operation, object argument)
        {
            return new LabeledStatement(operation.Label, Visit(operation.Operation), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitBranch(IBranchOperation operation, object argument)
        {
            return new BranchStatement(operation.Target, operation.BranchKind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEmpty(IEmptyOperation operation, object argument)
        {
            return new EmptyStatement(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitReturn(IReturnOperation operation, object argument)
        {
            return new ReturnStatement(operation.Kind, Visit(operation.ReturnedValue), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLock(ILockOperation operation, object argument)
        {
            return new LockStatement(Visit(operation.LockedValue), Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTry(ITryOperation operation, object argument)
        {
            return new TryStatement(Visit(operation.Body), VisitArray(operation.Catches), Visit(operation.Finally), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCatchClause(ICatchClauseOperation operation, object argument)
        {
            return new CatchClause(Visit(operation.ExceptionDeclarationOrExpression), operation.ExceptionType, operation.Locals, Visit(operation.Filter), Visit(operation.Handler), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitUsing(IUsingOperation operation, object argument)
        {
            return new UsingStatement(Visit(operation.Resources), Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        // https://github.com/dotnet/roslyn/issues/21281
        internal override IOperation VisitFixed(IFixedOperation operation, object argument)
        {
            return new FixedStatement(Visit(operation.Variables), Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitExpressionStatement(IExpressionStatementOperation operation, object argument)
        {
            return new ExpressionStatement(Visit(operation.Operation), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitWith(IWithOperation operation, object argument)
        {
            return new WithStatement(Visit(operation.Body), Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitStop(IStopOperation operation, object argument)
        {
            return new StopStatement(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEnd(IEndOperation operation, object argument)
        {
            return new EndStatement(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInvocation(IInvocationOperation operation, object argument)
        {
            return new InvocationExpression(operation.TargetMethod, Visit(operation.Instance), operation.IsVirtual, VisitArray(operation.Arguments), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitOmittedArgument(IOmittedArgumentOperation operation, object argument)
        {
            return new OmittedArgumentExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayElementReference(IArrayElementReferenceOperation operation, object argument)
        {
            return new ArrayElementReferenceExpression(Visit(operation.ArrayReference), VisitArray(operation.Indices), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation, object argument)
        {
            return new PointerIndirectionReferenceExpression(Visit(operation.Pointer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLocalReference(ILocalReferenceOperation operation, object argument)
        {
            return new LocalReferenceExpression(operation.Local, operation.IsDeclaration, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParameterReference(IParameterReferenceOperation operation, object argument)
        {
            return new ParameterReferenceExpression(operation.Parameter, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInstanceReference(IInstanceReferenceOperation operation, object argument)
        {
            return new InstanceReferenceExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFieldReference(IFieldReferenceOperation operation, object argument)
        {
            return new FieldReferenceExpression(operation.Field, operation.IsDeclaration, Visit(operation.Instance), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitMethodReference(IMethodReferenceOperation operation, object argument)
        {
            return new MethodReferenceExpression(operation.Method, operation.IsVirtual, Visit(operation.Instance), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPropertyReference(IPropertyReferenceOperation operation, object argument)
        {
            return new PropertyReferenceExpression(operation.Property, Visit(operation.Instance), VisitArray(operation.Arguments), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEventReference(IEventReferenceOperation operation, object argument)
        {
            return new EventReferenceExpression(operation.Event, Visit(operation.Instance), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEventAssignment(IEventAssignmentOperation operation, object argument)
        {
            return new EventAssignmentOperation(Visit(operation.EventReference), Visit(operation.HandlerValue), operation.Adds, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConditionalAccess(IConditionalAccessOperation operation, object argument)
        {
            return new ConditionalAccessExpression(Visit(operation.WhenNotNull), Visit(operation.Operation), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, object argument)
        {
            return new ConditionalAccessInstanceExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitPlaceholder(IPlaceholderOperation operation, object argument)
        {
            return new PlaceholderExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitUnaryOperator(IUnaryOperation operation, object argument)
        {
            return new UnaryOperatorExpression(operation.OperatorKind, Visit(operation.Operand), operation.IsLifted, operation.IsChecked, operation.OperatorMethod, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitBinaryOperator(IBinaryOperation operation, object argument)
        {
            return new BinaryOperatorExpression(operation.OperatorKind, Visit(operation.LeftOperand), Visit(operation.RightOperand), operation.IsLifted, operation.IsChecked, operation.IsCompareText, operation.OperatorMethod, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTupleBinaryOperator(ITupleBinaryOperation operation, object argument)
        {
            return new TupleBinaryOperatorExpression(operation.OperatorKind, Visit(operation.LeftOperand), Visit(operation.RightOperand), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConditional(IConditionalOperation operation, object argument)
        {
            return new ConditionalOperation(Visit(operation.Condition), Visit(operation.WhenTrue), Visit(operation.WhenFalse), operation.IsRef, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCoalesce(ICoalesceOperation operation, object argument)
        {
            return new CoalesceExpression(Visit(operation.Value), Visit(operation.WhenNull), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIsType(IIsTypeOperation operation, object argument)
        {
            return new IsTypeExpression(Visit(operation.ValueOperand), operation.TypeOperand, operation.IsNegated, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSizeOf(ISizeOfOperation operation, object argument)
        {
            return new SizeOfExpression(operation.TypeOperand, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTypeOf(ITypeOfOperation operation, object argument)
        {
            return new TypeOfExpression(operation.TypeOperand, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAnonymousFunction(IAnonymousFunctionOperation operation, object argument)
        {
            return new AnonymousFunctionExpression(operation.Symbol, Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDelegateCreation(IDelegateCreationOperation operation, object argument)
        {
            return new DelegateCreationExpression(Visit(operation.Target), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLiteral(ILiteralOperation operation, object argument)
        {
            return new LiteralExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAwait(IAwaitOperation operation, object argument)
        {
            return new AwaitExpression(Visit(operation.Operation), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitNameOf(INameOfOperation operation, object argument)
        {
            return new NameOfExpression(Visit(operation.Argument), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitThrow(IThrowOperation operation, object argument)
        {
            return new ThrowExpression(Visit(operation.Exception), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAddressOf(IAddressOfOperation operation, object argument)
        {
            return new AddressOfExpression(Visit(operation.Reference), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitObjectCreation(IObjectCreationOperation operation, object argument)
        {
            return new ObjectCreationExpression(operation.Constructor, Visit(operation.Initializer), VisitArray(operation.Arguments), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, object argument)
        {
            return new AnonymousObjectCreationExpression(VisitArray(operation.Initializers), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, object argument)
        {
            return new ObjectOrCollectionInitializerExpression(VisitArray(operation.Initializers), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitMemberInitializer(IMemberInitializerOperation operation, object argument)
        {
            return new MemberInitializerExpression(Visit(operation.InitializedMember), Visit(operation.Initializer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation, object argument)
        {
            return new CollectionElementInitializerExpression(operation.AddMethod, operation.IsDynamic, VisitArray(operation.Arguments), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFieldInitializer(IFieldInitializerOperation operation, object argument)
        {
            return new FieldInitializer(operation.InitializedFields, Visit(operation.Value), operation.Kind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitVariableInitializer(IVariableInitializerOperation operation, object argument)
        {
            return new VariableInitializer(Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPropertyInitializer(IPropertyInitializerOperation operation, object argument)
        {
            return new PropertyInitializer(operation.InitializedProperties, Visit(operation.Value), operation.Kind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParameterInitializer(IParameterInitializerOperation operation, object argument)
        {
            return new ParameterInitializer(operation.Parameter, Visit(operation.Value), operation.Kind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayCreation(IArrayCreationOperation operation, object argument)
        {
            return new ArrayCreationExpression(VisitArray(operation.DimensionSizes), Visit(operation.Initializer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayInitializer(IArrayInitializerOperation operation, object argument)
        {
            return new ArrayInitializer(VisitArray(operation.ElementValues), ((Operation)operation).SemanticModel, operation.Syntax, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSimpleAssignment(ISimpleAssignmentOperation operation, object argument)
        {
            return new SimpleAssignmentExpression(Visit(operation.Target), operation.IsRef, Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, object argument)
        {
            return new DeconstructionAssignmentExpression(Visit(operation.Target), Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDeclarationExpression(IDeclarationExpressionOperation operation, object argument)
        {
            return new DeclarationExpression(Visit(operation.Expression), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, object argument)
        {
            bool isDecrement = operation.Kind == OperationKind.Decrement;
            return new IncrementExpression(isDecrement, operation.IsPostfix, operation.IsLifted, operation.IsChecked, Visit(operation.Target), operation.OperatorMethod, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParenthesized(IParenthesizedOperation operation, object argument)
        {
            return new ParenthesizedExpression(Visit(operation.Operand), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, object argument)
        {
            return new DynamicMemberReferenceExpression(Visit(operation.Instance), operation.MemberName, operation.TypeArguments, operation.ContainingType, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, object argument)
        {
            return new DynamicObjectCreationExpression(VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, Visit(operation.Initializer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicInvocation(IDynamicInvocationOperation operation, object argument)
        {
            return new DynamicInvocationExpression(Visit(operation.Operation), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, object argument)
        {
            return new DynamicIndexerAccessExpression(Visit(operation.Operation), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDefaultValue(IDefaultValueOperation operation, object argument)
        {
            return new DefaultValueExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, object argument)
        {
            return new TypeParameterObjectCreationExpression(Visit(operation.Initializer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInvalid(IInvalidOperation operation, object argument)
        {
            return new InvalidOperation(VisitArray(operation.Children.ToImmutableArray()), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLocalFunction(ILocalFunctionOperation operation, object argument)
        {
            return new LocalFunctionStatement(operation.Symbol, Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolatedString(IInterpolatedStringOperation operation, object argument)
        {
            return new InterpolatedStringExpression(VisitArray(operation.Parts), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, object argument)
        {
            return new InterpolatedStringText(Visit(operation.Text), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolation(IInterpolationOperation operation, object argument)
        {
            return new Interpolation(Visit(operation.Expression), Visit(operation.Alignment), Visit(operation.FormatString), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIsPattern(IIsPatternOperation operation, object argument)
        {
            return new IsPatternExpression(Visit(operation.Value), Visit(operation.Pattern), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConstantPattern(IConstantPatternOperation operation, object argument)
        {
            return new ConstantPattern(Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDeclarationPattern(IDeclarationPatternOperation operation, object argument)
        {
            return new DeclarationPattern(operation.DeclaredSymbol, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPatternCaseClause(IPatternCaseClauseOperation operation, object argument)
        {
            return new PatternCaseClause(operation.Label, Visit(operation.Pattern), Visit(operation.Guard), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTuple(ITupleOperation operation, object argument)
        {
            return new TupleExpression(VisitArray(operation.Elements), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.NaturalType, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTranslatedQuery(ITranslatedQueryOperation operation, object argument)
        {
            return new TranslatedQueryExpression(Visit(operation.Operation), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRaiseEvent(IRaiseEventOperation operation, object argument)
        {
            return new RaiseEventStatement(Visit(operation.EventReference), VisitArray(operation.Arguments), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }
    }
}
