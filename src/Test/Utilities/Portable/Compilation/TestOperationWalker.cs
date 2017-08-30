// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Semantics;
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

        public override void VisitBlockStatement(IBlockStatement operation)
        {
            foreach (var local in operation.Locals)
            {
                // empty loop body, just want to make sure it won't crash.
            }

            base.VisitBlockStatement(operation);
        }

        public override void VisitVariableDeclarationStatement(IVariableDeclarationStatement operation)
        {
            base.VisitVariableDeclarationStatement(operation);
        }

        public override void VisitVariableDeclaration(IVariableDeclaration operation)
        {
            foreach (var symbol in operation.Variables)
            {
                // empty loop body, just want to make sure it won't crash.
            }

            base.VisitVariableDeclaration(operation);
        }

        public override void VisitSwitchStatement(ISwitchStatement operation)
        {
            base.VisitSwitchStatement(operation);
        }

        public override void VisitSwitchCase(ISwitchCase operation)
        {
            base.VisitSwitchCase(operation);
        }

        public override void VisitSingleValueCaseClause(ISingleValueCaseClause operation)
        {
            var caseKind = operation.CaseKind;
            var equality = operation.Equality;

            base.VisitSingleValueCaseClause(operation);
        }

        public override void VisitRelationalCaseClause(IRelationalCaseClause operation)
        {
            var caseKind = operation.CaseKind;
            var relation = operation.Relation;

            base.VisitRelationalCaseClause(operation);
        }

        public override void VisitDefaultCaseClause(IDefaultCaseClause operation)
        {
            base.VisitDefaultCaseClause(operation);
        }

        private void WalkLoopStatement(ILoopStatement operation)
        {
            var loopKind = operation.LoopKind;
            foreach (var local in operation.Locals)
            {
                // empty loop body, just want to make sure it won't crash.
            }
        }

        public override void VisitDoLoopStatement(IDoLoopStatement operation)
        {
            var doLoopKind = operation.DoLoopKind;
            WalkLoopStatement(operation);

            base.VisitDoLoopStatement(operation);
        }

        public override void VisitWhileLoopStatement(IWhileLoopStatement operation)
        {
            WalkLoopStatement(operation);

            base.VisitWhileLoopStatement(operation);
        }

        public override void VisitForLoopStatement(IForLoopStatement operation)
        {
            WalkLoopStatement(operation);

            base.VisitForLoopStatement(operation);
        }

        public override void VisitForToLoopStatement(IForToLoopStatement operation)
        {
            WalkLoopStatement(operation);

            base.VisitForToLoopStatement(operation);
        }

        public override void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            WalkLoopStatement(operation);

            base.VisitForEachLoopStatement(operation);
        }

        public override void VisitLabelStatement(ILabelStatement operation)
        {
            var label = operation.Label;

            base.VisitLabelStatement(operation);
        }

        public override void VisitBranchStatement(IBranchStatement operation)
        {
            var target = operation.Target;
            var branchKind = operation.BranchKind;

            base.VisitBranchStatement(operation);
        }

        public override void VisitYieldBreakStatement(IReturnStatement operation)
        {
            base.VisitYieldBreakStatement(operation);
        }

        public override void VisitEmptyStatement(IEmptyStatement operation)
        {
            base.VisitEmptyStatement(operation);
        }

        public override void VisitReturnStatement(IReturnStatement operation)
        {
            base.VisitReturnStatement(operation);
        }

        public override void VisitLockStatement(ILockStatement operation)
        {
            base.VisitLockStatement(operation);
        }

        public override void VisitTryStatement(ITryStatement operation)
        {
            base.VisitTryStatement(operation);
        }

        public override void VisitCatchClause(ICatchClause operation)
        {
            var caughtType = operation.CaughtType;
            var exceptionLocal = operation.ExceptionLocal;

            base.VisitCatchClause(operation);
        }

        public override void VisitUsingStatement(IUsingStatement operation)
        {
            base.VisitUsingStatement(operation);
        }

        public override void VisitFixedStatement(IFixedStatement operation)
        {
            base.VisitFixedStatement(operation);
        }

        public override void VisitExpressionStatement(IExpressionStatement operation)
        {
            base.VisitExpressionStatement(operation);
        }

        public override void VisitWithStatement(IWithStatement operation)
        {
            base.VisitWithStatement(operation);
        }

        public override void VisitStopStatement(IStopStatement operation)
        {
            base.VisitStopStatement(operation);
        }

        public override void VisitEndStatement(IEndStatement operation)
        {
            base.VisitEndStatement(operation);
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            var targetMethod = operation.TargetMethod;
            var isVirtual = operation.IsVirtual;

            base.VisitInvocationExpression(operation);
        }

        public override void VisitArgument(IArgument operation)
        {
            var argumentKind = operation.ArgumentKind;
            var parameter = operation.Parameter;

            base.VisitArgument(operation);
        }

        public override void VisitOmittedArgumentExpression(IOmittedArgumentExpression operation)
        {
            base.VisitOmittedArgumentExpression(operation);
        }

        public override void VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation)
        {
            base.VisitArrayElementReferenceExpression(operation);
        }

        public override void VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation)
        {
            base.VisitPointerIndirectionReferenceExpression(operation);
        }

        public override void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        {
            var local = operation.Local;

            base.VisitLocalReferenceExpression(operation);
        }

        public override void VisitParameterReferenceExpression(IParameterReferenceExpression operation)
        {
            var parameter = operation.Parameter;

            base.VisitParameterReferenceExpression(operation);
        }

        public override void VisitInstanceReferenceExpression(IInstanceReferenceExpression operation)
        {
            var instanceReferenceKind = operation.InstanceReferenceKind;

            base.VisitInstanceReferenceExpression(operation);
        }

        public override void VisitFieldReferenceExpression(IFieldReferenceExpression operation)
        {
            var member = operation.Member;
            var field = operation.Field;

            base.VisitFieldReferenceExpression(operation);
        }

        public override void VisitMethodBindingExpression(IMethodBindingExpression operation)
        {
            var member = operation.Member;
            var method = operation.Method;

            base.VisitMethodBindingExpression(operation);
        }

        public override void VisitPropertyReferenceExpression(IPropertyReferenceExpression operation)
        {
            var member = operation.Member;
            var property = operation.Property;

            base.VisitPropertyReferenceExpression(operation);
        }

        public override void VisitEventReferenceExpression(IEventReferenceExpression operation)
        {
            var member = operation.Member;
            var eventSymbol = operation.Event;

            base.VisitEventReferenceExpression(operation);
        }

        public override void VisitEventAssignmentExpression(IEventAssignmentExpression operation)
        {
            var adds = operation.Adds;

            base.VisitEventAssignmentExpression(operation);
        }

        public override void VisitConditionalAccessExpression(IConditionalAccessExpression operation)
        {
            base.VisitConditionalAccessExpression(operation);
        }

        public override void VisitConditionalAccessInstanceExpression(IConditionalAccessInstanceExpression operation)
        {
            base.VisitConditionalAccessInstanceExpression(operation);
        }

        public override void VisitPlaceholderExpression(IPlaceholderExpression operation)
        {
            base.VisitPlaceholderExpression(operation);
        }

        public override void VisitUnaryOperatorExpression(IUnaryOperatorExpression operation)
        {
            var usesOperatorMethod = operation.UsesOperatorMethod;
            var operatorMethod = operation.OperatorMethod;
            var unaryOperationKind = operation.UnaryOperationKind;

            base.VisitUnaryOperatorExpression(operation);
        }

        public override void VisitBinaryOperatorExpression(IBinaryOperatorExpression operation)
        {
            var usesOperatorMethod = operation.UsesOperatorMethod;
            var operatorMethod = operation.OperatorMethod;
            var binaryOperationKind = operation.BinaryOperationKind;

            base.VisitBinaryOperatorExpression(operation);
        }

        public override void VisitConversionExpression(IConversionExpression operation)
        {
            var usesOperatorMethod = operation.UsesOperatorMethod;
            var operatorMethod = operation.OperatorMethod;
            var conversion = operation.Conversion;
            var isExplicitInCode = operation.IsExplicitInCode;
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

            base.VisitConversionExpression(operation);
        }

        public override void VisitConditionalExpression(IConditionalExpression operation)
        {
            base.VisitConditionalExpression(operation);
        }

        public override void VisitCoalesceExpression(ICoalesceExpression operation)
        {
            base.VisitCoalesceExpression(operation);
        }

        public override void VisitIsTypeExpression(IIsTypeExpression operation)
        {
            var isType = operation.IsType;

            base.VisitIsTypeExpression(operation);
        }

        public override void VisitSizeOfExpression(ISizeOfExpression operation)
        {
            var typeOperand = operation.TypeOperand;

            base.VisitSizeOfExpression(operation);
        }

        public override void VisitTypeOfExpression(ITypeOfExpression operation)
        {
            var typeOperand = operation.TypeOperand;

            base.VisitTypeOfExpression(operation);
        }

        public override void VisitAnonymousFunctionExpression(IAnonymousFunctionExpression operation)
        {
            var signature = operation.Symbol;

            base.VisitAnonymousFunctionExpression(operation);
        }

        public override void VisitLocalFunctionStatement(ILocalFunctionStatement operation)
        {
            var localFunction = operation.LocalFunctionSymbol;

            base.VisitLocalFunctionStatement(operation);
        }

        public override void VisitLiteralExpression(ILiteralExpression operation)
        {
            base.VisitLiteralExpression(operation);
        }

        public override void VisitAwaitExpression(IAwaitExpression operation)
        {
            base.VisitAwaitExpression(operation);
        }

        public override void VisitNameOfExpression(INameOfExpression operation)
        {
            base.VisitNameOfExpression(operation);
        }

        public override void VisitThrowExpression(IThrowExpression operation)
        {
            base.VisitThrowExpression(operation);
        }

        public override void VisitAddressOfExpression(IAddressOfExpression operation)
        {
            base.VisitAddressOfExpression(operation);
        }

        public override void VisitObjectCreationExpression(IObjectCreationExpression operation)
        {
            var ctor = operation.Constructor;

            base.VisitObjectCreationExpression(operation);
        }

        public override void VisitAnonymousObjectCreationExpression(IAnonymousObjectCreationExpression operation)
        {
            base.VisitAnonymousObjectCreationExpression(operation);
        }

        public override void VisitDynamicObjectCreationExpression(IDynamicObjectCreationExpression operation)
        {
            var name = operation.Name;
            var applicableSymbols = operation.ApplicableSymbols;
            var names = operation.ArgumentNames;
            var refKinds = operation.ArgumentRefKinds;

            base.VisitDynamicObjectCreationExpression(operation);
        }

        public override void VisitObjectOrCollectionInitializerExpression(IObjectOrCollectionInitializerExpression operation)
        {
            base.VisitObjectOrCollectionInitializerExpression(operation);
        }

        public override void VisitMemberInitializerExpression(IMemberInitializerExpression operation)
        {
            base.VisitMemberInitializerExpression(operation);
        }

        public override void VisitCollectionElementInitializerExpression(ICollectionElementInitializerExpression operation)
        {
            var addMethod = operation.AddMethod;
            var isDynamic = operation.IsDynamic;

            base.VisitCollectionElementInitializerExpression(operation);
        }

        public override void VisitFieldInitializer(IFieldInitializer operation)
        {
            foreach (var field in operation.InitializedFields)
            {
                // empty loop body, just want to make sure it won't crash.
            }
            base.VisitFieldInitializer(operation);
        }

        public override void VisitPropertyInitializer(IPropertyInitializer operation)
        {
            var initializedProperty = operation.InitializedProperty;

            base.VisitPropertyInitializer(operation);
        }

        public override void VisitParameterInitializer(IParameterInitializer operation)
        {
            var parameter = operation.Parameter;

            base.VisitParameterInitializer(operation);
        }

        public override void VisitArrayCreationExpression(IArrayCreationExpression operation)
        {
            var elementType = operation.ElementType;

            base.VisitArrayCreationExpression(operation);
        }

        public override void VisitArrayInitializer(IArrayInitializer operation)
        {
            base.VisitArrayInitializer(operation);
        }

        public override void VisitSimpleAssignmentExpression(ISimpleAssignmentExpression operation)
        {
            base.VisitSimpleAssignmentExpression(operation);
        }

        public override void VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation)
        {
            var usesOperatorMethod = operation.UsesOperatorMethod;
            var operatorMethod = operation.OperatorMethod;
            var binaryOperationKind = operation.BinaryOperationKind;

            base.VisitCompoundAssignmentExpression(operation);
        }

        public override void VisitIncrementExpression(IIncrementExpression operation)
        {
            var usesOperatorMethod = operation.UsesOperatorMethod;
            var operatorMethod = operation.OperatorMethod;
            var incrementOperationKind = operation.IncrementOperationKind;

            base.VisitIncrementExpression(operation);
        }

        public override void VisitParenthesizedExpression(IParenthesizedExpression operation)
        {
            base.VisitParenthesizedExpression(operation);
        }

        public override void VisitDynamicMemberReferenceExpression(IDynamicMemberReferenceExpression operation)
        {
            var memberName = operation.MemberName;
            var typeArgs = operation.TypeArguments;
            var containingType = operation.ContainingType;

            base.VisitDynamicMemberReferenceExpression(operation);
        }

        public override void VisitDefaultValueExpression(IDefaultValueExpression operation)
        {
            base.VisitDefaultValueExpression(operation);
        }

        public override void VisitTypeParameterObjectCreationExpression(ITypeParameterObjectCreationExpression operation)
        {
            base.VisitTypeParameterObjectCreationExpression(operation);
        }

        public override void VisitInvalidStatement(IInvalidStatement operation)
        {
            base.VisitInvalidStatement(operation);
        }

        public override void VisitInvalidExpression(IInvalidExpression operation)
        {
            base.VisitInvalidExpression(operation);
        }

        public override void VisitTupleExpression(ITupleExpression operation)
        {
            base.VisitTupleExpression(operation);
        }

        public override void VisitInterpolatedStringExpression(IInterpolatedStringExpression operation)
        {
            base.VisitInterpolatedStringExpression(operation);
        }

        public override void VisitInterpolatedStringText(IInterpolatedStringText operation)
        {
            base.VisitInterpolatedStringText(operation);
        }

        public override void VisitInterpolation(IInterpolation operation)
        {
            base.VisitInterpolation(operation);
        }

        public override void VisitConstantPattern(IConstantPattern operation)
        {
            base.VisitConstantPattern(operation);
        }

        public override void VisitDeclarationPattern(IDeclarationPattern operation)
        {
            var declaredSymbol = operation.DeclaredSymbol;

            base.VisitDeclarationPattern(operation);
        }

        public override void VisitIsPatternExpression(IIsPatternExpression operation)
        {
            base.VisitIsPatternExpression(operation);
        }

        public override void VisitPatternCaseClause(IPatternCaseClause operation)
        {
            var label = operation.Label;

            base.VisitPatternCaseClause(operation);
        }
    }
}
