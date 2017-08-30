// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Semantics
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

        public override IOperation VisitBlockStatement(IBlockStatement operation, object argument)
        {
            return new BlockStatement(VisitArray(operation.Statements), operation.Locals, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitVariableDeclarationStatement(IVariableDeclarationStatement operation, object argument)
        {
            return new VariableDeclarationStatement(VisitArray(operation.Declarations), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitVariableDeclaration(IVariableDeclaration operation, object argument)
        {
            return new VariableDeclaration(operation.Variables, Visit(operation.Initializer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSwitchStatement(ISwitchStatement operation, object argument)
        {
            return new SwitchStatement(Visit(operation.Value), VisitArray(operation.Cases), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSwitchCase(ISwitchCase operation, object argument)
        {
            return new SwitchCase(VisitArray(operation.Clauses), VisitArray(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSingleValueCaseClause(ISingleValueCaseClause operation, object argument)
        {
            return new SingleValueCaseClause(Visit(operation.Value), operation.CaseKind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRelationalCaseClause(IRelationalCaseClause operation, object argument)
        {
            return new RelationalCaseClause(Visit(operation.Value), operation.Relation, operation.CaseKind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRangeCaseClause(IRangeCaseClause operation, object argument)
        {
            return new RangeCaseClause(Visit(operation.MinimumValue), Visit(operation.MaximumValue), operation.CaseKind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDefaultCaseClause(IDefaultCaseClause operation, object argument)
        {
            return new DefaultCaseClause(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIfStatement(IIfStatement operation, object argument)
        {
            return new IfStatement(Visit(operation.Condition), Visit(operation.IfTrueStatement), Visit(operation.IfFalseStatement), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitWhileUntilLoopStatement(IWhileUntilLoopStatement operation, object argument)
        {
            return new WhileUntilLoopStatement(operation.IsTopTest, operation.IsWhile, Visit(operation.Condition), operation.LoopKind, Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForLoopStatement(IForLoopStatement operation, object argument)
        {
            return new ForLoopStatement(VisitArray(operation.Before), VisitArray(operation.AtLoopBottom), operation.Locals, Visit(operation.Condition), operation.LoopKind, Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForEachLoopStatement(IForEachLoopStatement operation, object argument)
        {
            return new ForEachLoopStatement(operation.IterationVariable, Visit(operation.Collection), operation.LoopKind, Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLabeledStatement(ILabeledStatement operation, object argument)
        {
            return new LabeledStatement(operation.Label, Visit(operation.Statement), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitBranchStatement(IBranchStatement operation, object argument)
        {
            return new BranchStatement(operation.Target, operation.BranchKind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitYieldBreakStatement(IReturnStatement operation, object argument)
        {
            return new ReturnStatement(operation.Kind, Visit(operation.ReturnedValue), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEmptyStatement(IEmptyStatement operation, object argument)
        {
            return new EmptyStatement(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitReturnStatement(IReturnStatement operation, object argument)
        {
            return new ReturnStatement(operation.Kind, Visit(operation.ReturnedValue), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLockStatement(ILockStatement operation, object argument)
        {
            return new LockStatement(Visit(operation.Expression), Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTryStatement(ITryStatement operation, object argument)
        {
            return new TryStatement(Visit(operation.Body), VisitArray(operation.Catches), Visit(operation.FinallyHandler), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCatchClause(ICatchClause operation, object argument)
        {
            return new CatchClause(Visit(operation.Handler), operation.CaughtType, Visit(operation.Filter), operation.ExceptionLocal, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitUsingStatement(IUsingStatement operation, object argument)
        {
            return new UsingStatement(Visit(operation.Body), Visit(operation.Declaration), Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFixedStatement(IFixedStatement operation, object argument)
        {
            return new FixedStatement(Visit(operation.Variables), Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitExpressionStatement(IExpressionStatement operation, object argument)
        {
            return new ExpressionStatement(Visit(operation.Expression), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitWithStatement(IWithStatement operation, object argument)
        {
            return new WithStatement(Visit(operation.Body), Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitStopStatement(IStopStatement operation, object argument)
        {
            return new StopStatement(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEndStatement(IEndStatement operation, object argument)
        {
            return new EndStatement(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInvocationExpression(IInvocationExpression operation, object argument)
        {
            return new InvocationExpression(operation.TargetMethod, Visit(operation.Instance), operation.IsVirtual, VisitArray(operation.ArgumentsInEvaluationOrder), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitOmittedArgumentExpression(IOmittedArgumentExpression operation, object argument)
        {
            return new OmittedArgumentExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation, object argument)
        {
            return new ArrayElementReferenceExpression(Visit(operation.ArrayReference), VisitArray(operation.Indices), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation, object argument)
        {
            return new PointerIndirectionReferenceExpression(Visit(operation.Pointer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLocalReferenceExpression(ILocalReferenceExpression operation, object argument)
        {
            return new LocalReferenceExpression(operation.Local, operation.IsDeclaration, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParameterReferenceExpression(IParameterReferenceExpression operation, object argument)
        {
            return new ParameterReferenceExpression(operation.Parameter, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSyntheticLocalReferenceExpression(ISyntheticLocalReferenceExpression operation, object argument)
        {
            return new SyntheticLocalReferenceExpression(operation.SyntheticLocalKind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInstanceReferenceExpression(IInstanceReferenceExpression operation, object argument)
        {
            return new InstanceReferenceExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFieldReferenceExpression(IFieldReferenceExpression operation, object argument)
        {
            return new FieldReferenceExpression(operation.Field, operation.IsDeclaration, Visit(operation.Instance), operation.Member, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitMethodBindingExpression(IMethodBindingExpression operation, object argument)
        {
            return new MethodBindingExpression(operation.Method, operation.IsVirtual, Visit(operation.Instance), operation.Member, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPropertyReferenceExpression(IPropertyReferenceExpression operation, object argument)
        {
            return new PropertyReferenceExpression(operation.Property, Visit(operation.Instance), operation.Member, VisitArray(operation.ArgumentsInEvaluationOrder), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEventReferenceExpression(IEventReferenceExpression operation, object argument)
        {
            return new EventReferenceExpression(operation.Event, Visit(operation.Instance), operation.Member, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEventAssignmentExpression(IEventAssignmentExpression operation, object argument)
        {
            return new EventAssignmentExpression(Visit(operation.EventReference), Visit(operation.HandlerValue), operation.Adds, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConditionalAccessExpression(IConditionalAccessExpression operation, object argument)
        {
            return new ConditionalAccessExpression(Visit(operation.WhenNotNull), Visit(operation.Expression), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConditionalAccessInstanceExpression(IConditionalAccessInstanceExpression operation, object argument)
        {
            return new ConditionalAccessInstanceExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPlaceholderExpression(IPlaceholderExpression operation, object argument)
        {
            return new PlaceholderExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitUnaryOperatorExpression(IUnaryOperatorExpression operation, object argument)
        {
            return new UnaryOperatorExpression(operation.OperatorKind, Visit(operation.Operand), operation.IsLifted, operation.IsChecked, operation.UsesOperatorMethod, operation.OperatorMethod, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitBinaryOperatorExpression(IBinaryOperatorExpression operation, object argument)
        {
            return new BinaryOperatorExpression(operation.OperatorKind, Visit(operation.LeftOperand), Visit(operation.RightOperand), operation.IsLifted, operation.IsChecked, operation.IsCompareText, operation.UsesOperatorMethod, operation.OperatorMethod, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConditionalExpression(IConditionalExpression operation, object argument)
        {
            return new ConditionalExpression(Visit(operation.Condition), Visit(operation.WhenTrue), Visit(operation.WhenFalse), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCoalesceExpression(ICoalesceExpression operation, object argument)
        {
            return new CoalesceExpression(Visit(operation.Expression), Visit(operation.WhenNull), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIsTypeExpression(IIsTypeExpression operation, object argument)
        {
            return new IsTypeExpression(Visit(operation.Operand), operation.IsType, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSizeOfExpression(ISizeOfExpression operation, object argument)
        {
            return new SizeOfExpression(operation.TypeOperand, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTypeOfExpression(ITypeOfExpression operation, object argument)
        {
            return new TypeOfExpression(operation.TypeOperand, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAnonymousFunctionExpression(IAnonymousFunctionExpression operation, object argument)
        {
            return new AnonymousFunctionExpression(operation.Symbol, Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLiteralExpression(ILiteralExpression operation, object argument)
        {
            return new LiteralExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAwaitExpression(IAwaitExpression operation, object argument)
        {
            return new AwaitExpression(Visit(operation.AwaitedValue), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitNameOfExpression(INameOfExpression operation, object argument)
        {
            return new NameOfExpression(Visit(operation.Argument), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitThrowExpression(IThrowExpression operation, object argument)
        {
            return new ThrowExpression(Visit(operation.Expression), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAddressOfExpression(IAddressOfExpression operation, object argument)
        {
            return new AddressOfExpression(Visit(operation.Reference), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitObjectCreationExpression(IObjectCreationExpression operation, object argument)
        {
            return new ObjectCreationExpression(operation.Constructor, Visit(operation.Initializer), VisitArray(operation.ArgumentsInEvaluationOrder), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAnonymousObjectCreationExpression(IAnonymousObjectCreationExpression operation, object argument)
        {
            return new AnonymousObjectCreationExpression(VisitArray(operation.Initializers), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitObjectOrCollectionInitializerExpression(IObjectOrCollectionInitializerExpression operation, object argument)
        {
            return new ObjectOrCollectionInitializerExpression(VisitArray(operation.Initializers), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitMemberInitializerExpression(IMemberInitializerExpression operation, object argument)
        {
            return new MemberInitializerExpression(Visit(operation.InitializedMember), Visit(operation.Initializer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCollectionElementInitializerExpression(ICollectionElementInitializerExpression operation, object argument)
        {
            return new CollectionElementInitializerExpression(operation.AddMethod, VisitArray(operation.Arguments), operation.IsDynamic, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFieldInitializer(IFieldInitializer operation, object argument)
        {
            return new FieldInitializer(operation.InitializedFields, Visit(operation.Value), operation.Kind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPropertyInitializer(IPropertyInitializer operation, object argument)
        {
            return new PropertyInitializer(operation.InitializedProperty, Visit(operation.Value), operation.Kind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParameterInitializer(IParameterInitializer operation, object argument)
        {
            return new ParameterInitializer(operation.Parameter, Visit(operation.Value), operation.Kind, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayCreationExpression(IArrayCreationExpression operation, object argument)
        {
            return new ArrayCreationExpression(operation.ElementType, VisitArray(operation.DimensionSizes), Visit(operation.Initializer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayInitializer(IArrayInitializer operation, object argument)
        {
            return new ArrayInitializer(VisitArray(operation.ElementValues), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSimpleAssignmentExpression(ISimpleAssignmentExpression operation, object argument)
        {
            return new SimpleAssignmentExpression(Visit(operation.Target), Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation, object argument)
        {
            return new CompoundAssignmentExpression(operation.OperatorKind, operation.IsLifted, operation.IsChecked, Visit(operation.Target), Visit(operation.Value), operation.UsesOperatorMethod, operation.OperatorMethod, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIncrementExpression(IIncrementExpression operation, object argument)
        {
            return new IncrementExpression(operation.IsDecrement, operation.IsPostfix, operation.IsLifted, operation.IsChecked, Visit(operation.Target), operation.UsesOperatorMethod, operation.OperatorMethod, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParenthesizedExpression(IParenthesizedExpression operation, object argument)
        {
            return new ParenthesizedExpression(Visit(operation.Operand), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicMemberReferenceExpression(IDynamicMemberReferenceExpression operation, object argument)
        {
            return new DynamicMemberReferenceExpression(Visit(operation.Instance), operation.MemberName, operation.TypeArguments, operation.ContainingType, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicObjectCreationExpression(IDynamicObjectCreationExpression operation, object argument)
        {
            return new DynamicObjectCreationExpression(operation.Name, operation.ApplicableSymbols, VisitArray(operation.Arguments), operation.ArgumentNames, operation.ArgumentRefKinds, Visit(operation.Initializer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDefaultValueExpression(IDefaultValueExpression operation, object argument)
        {
            return new DefaultValueExpression(((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTypeParameterObjectCreationExpression(ITypeParameterObjectCreationExpression operation, object argument)
        {
            return new TypeParameterObjectCreationExpression(Visit(operation.Initializer), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInvalidStatement(IInvalidStatement operation, object argument)
        {
            return new InvalidStatement(VisitArray(operation.Children.ToImmutableArray()), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInvalidExpression(IInvalidExpression operation, object argument)
        {
            return new InvalidExpression(VisitArray(operation.Children.ToImmutableArray()), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLocalFunctionStatement(ILocalFunctionStatement operation, object argument)
        {
            return new LocalFunctionStatement(operation.Symbol, Visit(operation.Body), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolatedStringExpression(IInterpolatedStringExpression operation, object argument)
        {
            return new InterpolatedStringExpression(VisitArray(operation.Parts), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolatedStringText(IInterpolatedStringText operation, object argument)
        {
            return new InterpolatedStringText(Visit(operation.Text), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolation(IInterpolation operation, object argument)
        {
            return new Interpolation(Visit(operation.Expression), Visit(operation.Alignment), Visit(operation.FormatString), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIsPatternExpression(IIsPatternExpression operation, object argument)
        {
            return new IsPatternExpression(Visit(operation.Expression), Visit(operation.Pattern), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConstantPattern(IConstantPattern operation, object argument)
        {
            return new ConstantPattern(Visit(operation.Value), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDeclarationPattern(IDeclarationPattern operation, object argument)
        {
            return new DeclarationPattern(operation.DeclaredSymbol, ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPatternCaseClause(IPatternCaseClause operation, object argument)
        {
            return new PatternCaseClause(operation.Label, Visit(operation.Pattern), Visit(operation.GuardExpression), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTupleExpression(ITupleExpression operation, object argument)
        {
            return new TupleExpression(VisitArray(operation.Elements), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }
    }
}
