// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class TestOperationWalker : OperationWalker
    {
        private static TestOperationWalker s_instance;

        private TestOperationWalker()
            : base()
        { }

        public static TestOperationWalker GetInstance()
        {
            if (s_instance == null)
            {
                s_instance = new TestOperationWalker();
            }
            return s_instance;
        }

#if Test_IOperation_None_Kind
        internal override void VisitNoneOperation(IOperation operation)
        {
            Assert.True(false, "Encountered an IOperation with `Kind == OperationKind.None` while walking the operation tree.");
        }
#endif

        public override void Visit(IOperation operation)
        {
            if (operation != null)
            {
                var syntax = operation.Syntax;
                var type = operation.Type;
                var constantValue = operation.ConstantValue;
                var language = operation.Language;
            }
            base.Visit(operation);
        }

        public override void VisitBlock(IBlockOperation operation)
        {
            foreach (var local in operation.Locals)
            {
                // empty loop body, just want to make sure it won't crash.
            }

            base.VisitBlock(operation);
        }

        public override void VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation)
        {
            base.VisitVariableDeclarationGroup(operation);
        }

        public override void VisitVariableDeclarator(IVariableDeclaratorOperation operation)
        {
            var symbol = operation.Symbol;

            base.VisitVariableDeclarator(operation);
        }

        public override void VisitVariableDeclaration(IVariableDeclarationOperation operation)
        {
            base.VisitVariableDeclaration(operation);
        }

        public override void VisitSwitch(ISwitchOperation operation)
        {
            base.VisitSwitch(operation);
        }

        public override void VisitSwitchCase(ISwitchCaseOperation operation)
        {
            base.VisitSwitchCase(operation);
        }

        public override void VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation)
        {
            var caseKind = operation.CaseKind;

            base.VisitSingleValueCaseClause(operation);
        }

        public override void VisitRelationalCaseClause(IRelationalCaseClauseOperation operation)
        {
            var caseKind = operation.CaseKind;
            var relation = operation.Relation;

            base.VisitRelationalCaseClause(operation);
        }

        public override void VisitDefaultCaseClause(IDefaultCaseClauseOperation operation)
        {
            base.VisitDefaultCaseClause(operation);
        }

        private void WalkLoop(ILoopOperation operation)
        {
            var loopKind = operation.LoopKind;
            foreach (var local in operation.Locals)
            {
                // empty loop body, just want to make sure it won't crash.
            }
        }

        public override void VisitDoLoop(IDoLoopOperation operation)
        {
            var doLoopKind = operation.DoLoopKind;
            WalkLoop(operation);

            base.VisitDoLoop(operation);
        }

        public override void VisitWhileLoop(IWhileLoopOperation operation)
        {
            WalkLoop(operation);

            base.VisitWhileLoop(operation);
        }

        public override void VisitForLoop(IForLoopOperation operation)
        {
            WalkLoop(operation);

            base.VisitForLoop(operation);
        }

        public override void VisitForToLoop(IForToLoopOperation operation)
        {
            WalkLoop(operation);

            base.VisitForToLoop(operation);
        }

        public override void VisitForEachLoop(IForEachLoopOperation operation)
        {
            WalkLoop(operation);

            base.VisitForEachLoop(operation);
        }

        public override void VisitLabeled(ILabeledOperation operation)
        {
            var label = operation.Label;

            base.VisitLabeled(operation);
        }

        public override void VisitBranch(IBranchOperation operation)
        {
            var target = operation.Target;
            var branchKind = operation.BranchKind;

            base.VisitBranch(operation);
        }

        public override void VisitEmpty(IEmptyOperation operation)
        {
            base.VisitEmpty(operation);
        }

        public override void VisitReturn(IReturnOperation operation)
        {
            base.VisitReturn(operation);
        }

        public override void VisitLock(ILockOperation operation)
        {
            base.VisitLock(operation);
        }

        public override void VisitTry(ITryOperation operation)
        {
            base.VisitTry(operation);
        }

        public override void VisitCatchClause(ICatchClauseOperation operation)
        {
            var exceptionType = operation.ExceptionType;
            var locals = operation.Locals;

            base.VisitCatchClause(operation);
        }

        public override void VisitUsing(IUsingOperation operation)
        {
            base.VisitUsing(operation);
        }

        // https://github.com/dotnet/roslyn/issues/21281
        internal override void VisitFixed(IFixedOperation operation)
        {
            base.VisitFixed(operation);
        }

        public override void VisitExpressionStatement(IExpressionStatementOperation operation)
        {
            base.VisitExpressionStatement(operation);
        }

        internal override void VisitWith(IWithOperation operation)
        {
            base.VisitWith(operation);
        }

        public override void VisitStop(IStopOperation operation)
        {
            base.VisitStop(operation);
        }

        public override void VisitEnd(IEndOperation operation)
        {
            base.VisitEnd(operation);
        }

        public override void VisitInvocation(IInvocationOperation operation)
        {
            var targetMethod = operation.TargetMethod;
            var isVirtual = operation.IsVirtual;

            base.VisitInvocation(operation);
        }

        public override void VisitArgument(IArgumentOperation operation)
        {
            var argumentKind = operation.ArgumentKind;
            var parameter = operation.Parameter;

            base.VisitArgument(operation);
        }

        public override void VisitOmittedArgument(IOmittedArgumentOperation operation)
        {
            base.VisitOmittedArgument(operation);
        }

        public override void VisitArrayElementReference(IArrayElementReferenceOperation operation)
        {
            base.VisitArrayElementReference(operation);
        }

        internal override void VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation)
        {
            base.VisitPointerIndirectionReference(operation);
        }

        public override void VisitLocalReference(ILocalReferenceOperation operation)
        {
            var local = operation.Local;
            var isDeclaration = operation.IsDeclaration;

            base.VisitLocalReference(operation);
        }

        public override void VisitParameterReference(IParameterReferenceOperation operation)
        {
            var parameter = operation.Parameter;

            base.VisitParameterReference(operation);
        }

        public override void VisitInstanceReference(IInstanceReferenceOperation operation)
        {
            base.VisitInstanceReference(operation);
        }

        public override void VisitFieldReference(IFieldReferenceOperation operation)
        {
            var member = operation.Member;
            var field = operation.Field;

            base.VisitFieldReference(operation);
        }

        public override void VisitMethodReference(IMethodReferenceOperation operation)
        {
            var member = operation.Member;
            var method = operation.Method;

            base.VisitMethodReference(operation);
        }

        public override void VisitPropertyReference(IPropertyReferenceOperation operation)
        {
            var member = operation.Member;
            var property = operation.Property;

            base.VisitPropertyReference(operation);
        }

        public override void VisitEventReference(IEventReferenceOperation operation)
        {
            var member = operation.Member;
            var eventSymbol = operation.Event;

            base.VisitEventReference(operation);
        }

        public override void VisitEventAssignment(IEventAssignmentOperation operation)
        {
            var adds = operation.Adds;

            base.VisitEventAssignment(operation);
        }

        public override void VisitConditionalAccess(IConditionalAccessOperation operation)
        {
            base.VisitConditionalAccess(operation);
        }

        public override void VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation)
        {
            base.VisitConditionalAccessInstance(operation);
        }

        internal override void VisitPlaceholder(IPlaceholderOperation operation)
        {
            base.VisitPlaceholder(operation);
        }

        public override void VisitUnaryOperator(IUnaryOperation operation)
        {
            var operatorMethod = operation.OperatorMethod;
            var unaryOperationKind = operation.OperatorKind;
            var isLifted = operation.IsLifted;
            var isChecked = operation.IsChecked;

            base.VisitUnaryOperator(operation);
        }

        public override void VisitBinaryOperator(IBinaryOperation operation)
        {
            var operatorMethod = operation.OperatorMethod;
            var binaryOperationKind = operation.OperatorKind;
            var isLifted = operation.IsLifted;
            var isChecked = operation.IsChecked;
            var isCompareText = operation.IsCompareText;

            base.VisitBinaryOperator(operation);
        }

        public override void VisitConversion(IConversionOperation operation)
        {
            var operatorMethod = operation.OperatorMethod;
            var conversion = operation.Conversion;
            var isChecked = operation.IsChecked;
            var isTryCast = operation.IsTryCast;
            switch (operation.Language)
            {
                case LanguageNames.CSharp:
                    CSharp.Conversion csharpConversion = CSharp.CSharpExtensions.GetConversion(operation);
                    break;
                case LanguageNames.VisualBasic:
                    VisualBasic.Conversion visualBasicConversion = VisualBasic.VisualBasicExtensions.GetConversion(operation);
                    break;
                default:
                    Debug.Fail($"Language {operation.Language} is unknown!");
                    break;
            }

            base.VisitConversion(operation);
        }

        public override void VisitConditional(IConditionalOperation operation)
        {
            bool isRef = operation.IsRef;
            base.VisitConditional(operation);
        }

        public override void VisitCoalesce(ICoalesceOperation operation)
        {
            base.VisitCoalesce(operation);
        }

        public override void VisitIsType(IIsTypeOperation operation)
        {
            var isType = operation.TypeOperand;

            base.VisitIsType(operation);
        }

        public override void VisitSizeOf(ISizeOfOperation operation)
        {
            var typeOperand = operation.TypeOperand;

            base.VisitSizeOf(operation);
        }

        public override void VisitTypeOf(ITypeOfOperation operation)
        {
            var typeOperand = operation.TypeOperand;

            base.VisitTypeOf(operation);
        }

        public override void VisitAnonymousFunction(IAnonymousFunctionOperation operation)
        {
            var signature = operation.Symbol;

            base.VisitAnonymousFunction(operation);
        }

        public override void VisitLocalFunction(ILocalFunctionOperation operation)
        {
            var localFunction = operation.Symbol;

            base.VisitLocalFunction(operation);
        }

        public override void VisitLiteral(ILiteralOperation operation)
        {
            base.VisitLiteral(operation);
        }

        public override void VisitAwait(IAwaitOperation operation)
        {
            base.VisitAwait(operation);
        }

        public override void VisitNameOf(INameOfOperation operation)
        {
            base.VisitNameOf(operation);
        }

        public override void VisitThrow(IThrowOperation operation)
        {
            base.VisitThrow(operation);
        }

        public override void VisitAddressOf(IAddressOfOperation operation)
        {
            base.VisitAddressOf(operation);
        }

        public override void VisitObjectCreation(IObjectCreationOperation operation)
        {
            var ctor = operation.Constructor;

            base.VisitObjectCreation(operation);
        }

        public override void VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation)
        {
            base.VisitAnonymousObjectCreation(operation);
        }

        private void VisitDynamicArguments(HasDynamicArgumentsExpression operation)
        {
            var names = operation.ArgumentNames;
            var refKinds = operation.ArgumentRefKinds;
        }

        public override void VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation)
        {
            VisitDynamicArguments((HasDynamicArgumentsExpression)operation);

            base.VisitDynamicObjectCreation(operation);
        }

        public override void VisitDynamicInvocation(IDynamicInvocationOperation operation)
        {
            VisitDynamicArguments((HasDynamicArgumentsExpression)operation);

            base.VisitDynamicInvocation(operation);
        }

        public override void VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation)
        {
            VisitDynamicArguments((HasDynamicArgumentsExpression)operation);

            base.VisitDynamicIndexerAccess(operation);
        }

        public override void VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation)
        {
            base.VisitObjectOrCollectionInitializer(operation);
        }

        public override void VisitMemberInitializer(IMemberInitializerOperation operation)
        {
            base.VisitMemberInitializer(operation);
        }

        public override void VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation)
        {
            var addMethod = operation.AddMethod;
            var isDynamic = operation.IsDynamic;

            base.VisitCollectionElementInitializer(operation);
        }

        public override void VisitFieldInitializer(IFieldInitializerOperation operation)
        {
            foreach (var field in operation.InitializedFields)
            {
                // empty loop body, just want to make sure it won't crash.
            }
            base.VisitFieldInitializer(operation);
        }

        public override void VisitVariableInitializer(IVariableInitializerOperation operation)
        {
            base.VisitVariableInitializer(operation);
        }

        public override void VisitPropertyInitializer(IPropertyInitializerOperation operation)
        {
            var initializedProperty = operation.InitializedProperty;

            base.VisitPropertyInitializer(operation);
        }

        public override void VisitParameterInitializer(IParameterInitializerOperation operation)
        {
            var parameter = operation.Parameter;

            base.VisitParameterInitializer(operation);
        }

        public override void VisitArrayCreation(IArrayCreationOperation operation)
        {
            base.VisitArrayCreation(operation);
        }

        public override void VisitArrayInitializer(IArrayInitializerOperation operation)
        {
            base.VisitArrayInitializer(operation);
        }

        public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
        {
            bool isRef = operation.IsRef;
            base.VisitSimpleAssignment(operation);
        }

        public override void VisitCompoundAssignment(ICompoundAssignmentOperation operation)
        {
            var operatorMethod = operation.OperatorMethod;
            var binaryOperationKind = operation.OperatorKind;
            var inConversion = operation.InConversion;
            var outConversion = operation.OutConversion;

            if (operation.Syntax.Language == LanguageNames.CSharp)
            {
                Assert.Throws<ArgumentException>("compoundAssignment", () => VisualBasic.VisualBasicExtensions.GetInConversion(operation));
                Assert.Throws<ArgumentException>("compoundAssignment", () => VisualBasic.VisualBasicExtensions.GetOutConversion(operation));
                var inConversionInteranl = CSharp.CSharpExtensions.GetInConversion(operation);
                var outConversionInteranl = CSharp.CSharpExtensions.GetOutConversion(operation);
            }
            else
            {
                Assert.Throws<ArgumentException>("compoundAssignment", () => CSharp.CSharpExtensions.GetInConversion(operation));
                Assert.Throws<ArgumentException>("compoundAssignment", () => CSharp.CSharpExtensions.GetOutConversion(operation));
                var inConversionInternal = VisualBasic.VisualBasicExtensions.GetInConversion(operation);
                var outConversionInternal = VisualBasic.VisualBasicExtensions.GetOutConversion(operation);
            }

            base.VisitCompoundAssignment(operation);
        }

        public override void VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation)
        {
            var operatorMethod = operation.OperatorMethod;
            var isPostFix = operation.IsPostfix;

            base.VisitIncrementOrDecrement(operation);
        }

        public override void VisitParenthesized(IParenthesizedOperation operation)
        {
            base.VisitParenthesized(operation);
        }

        public override void VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation)
        {
            var memberName = operation.MemberName;
            var typeArgs = operation.TypeArguments;
            var containingType = operation.ContainingType;

            base.VisitDynamicMemberReference(operation);
        }

        public override void VisitDefaultValue(IDefaultValueOperation operation)
        {
            base.VisitDefaultValue(operation);
        }

        public override void VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation)
        {
            base.VisitTypeParameterObjectCreation(operation);
        }

        public override void VisitInvalid(IInvalidOperation operation)
        {
            base.VisitInvalid(operation);
        }

        public override void VisitTuple(ITupleOperation operation)
        {
            base.VisitTuple(operation);
        }

        public override void VisitInterpolatedString(IInterpolatedStringOperation operation)
        {
            base.VisitInterpolatedString(operation);
        }

        public override void VisitInterpolatedStringText(IInterpolatedStringTextOperation operation)
        {
            base.VisitInterpolatedStringText(operation);
        }

        public override void VisitInterpolation(IInterpolationOperation operation)
        {
            base.VisitInterpolation(operation);
        }

        public override void VisitConstantPattern(IConstantPatternOperation operation)
        {
            base.VisitConstantPattern(operation);
        }

        public override void VisitDeclarationPattern(IDeclarationPatternOperation operation)
        {
            var declaredSymbol = operation.DeclaredSymbol;

            base.VisitDeclarationPattern(operation);
        }

        public override void VisitIsPattern(IIsPatternOperation operation)
        {
            base.VisitIsPattern(operation);
        }

        public override void VisitPatternCaseClause(IPatternCaseClauseOperation operation)
        {
            var label = operation.Label;

            base.VisitPatternCaseClause(operation);
        }

        public override void VisitTranslatedQuery(ITranslatedQueryOperation operation)
        {
            base.VisitTranslatedQuery(operation);
        }
    }
}
