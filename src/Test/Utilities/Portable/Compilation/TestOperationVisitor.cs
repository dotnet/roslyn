﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class TestOperationVisitor : OperationVisitor
    {
        public static readonly TestOperationVisitor Singleton = new TestOperationVisitor();

        private TestOperationVisitor()
            : base()
        { }

        public override void DefaultVisit(IOperation operation)
        {
            throw new NotImplementedException(operation.GetType().ToString());
        }

        internal override void VisitNoneOperation(IOperation operation)
        {
#if Test_IOperation_None_Kind
            Assert.True(false, "Encountered an IOperation with `Kind == OperationKind.None` while walking the operation tree.");
#endif
            Assert.Equal(OperationKind.None, operation.Kind);
        }

        public override void Visit(IOperation operation)
        {
            if (operation != null)
            {
                var syntax = operation.Syntax;
                var type = operation.Type;
                var constantValue = operation.ConstantValue;

                Assert.NotNull(syntax);
                // operation.Language can throw due to https://github.com/dotnet/roslyn/issues/23821
                // Conditional logic below should be removed once the issue is fixed
                if (syntax is Microsoft.CodeAnalysis.Syntax.SyntaxList)
                {
                    Assert.Equal(OperationKind.None, operation.Kind);
                    Assert.Equal(LanguageNames.CSharp, operation.Parent.Language);
                }
                else
                {
                    var language = operation.Language;
                }

                var isImplicit = operation.IsImplicit;

                foreach (IOperation child in operation.Children)
                {
                    Assert.NotNull(child);
                }

                if (operation.SemanticModel != null)
                {
                    Assert.Same(operation.SemanticModel, operation.SemanticModel.ContainingModelOrSelf);
                }
            }
            base.Visit(operation);
        }

        public override void VisitBlock(IBlockOperation operation)
        {
            Assert.Equal(OperationKind.Block, operation.Kind);
            VisitLocals(operation.Locals);

            AssertEx.Equal(operation.Operations, operation.Children);
        }

        public override void VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation)
        {
            Assert.Equal(OperationKind.VariableDeclarationGroup, operation.Kind);
            AssertEx.Equal(operation.Declarations, operation.Children);
        }

        public override void VisitVariableDeclarator(IVariableDeclaratorOperation operation)
        {
            Assert.Equal(OperationKind.VariableDeclarator, operation.Kind);
            Assert.NotNull(operation.Symbol);
            IEnumerable<IOperation> children = operation.IgnoredArguments;
            var initializer = operation.Initializer;

            if (initializer != null)
            {
                children = children.Concat(new[] { initializer });
            }

            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitVariableDeclaration(IVariableDeclarationOperation operation)
        {
            Assert.Equal(OperationKind.VariableDeclaration, operation.Kind);
            IEnumerable<IOperation> children = operation.Declarators;
            var initializer = operation.Initializer;

            if (initializer != null)
            {
                children = children.Concat(new[] { initializer });
            }

            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitSwitch(ISwitchOperation operation)
        {
            Assert.Equal(OperationKind.Switch, operation.Kind);
            VisitLocals(operation.Locals);
            Assert.NotNull(operation.ExitLabel);
            AssertEx.Equal(new[] { operation.Value }.Concat(operation.Cases), operation.Children);
        }

        public override void VisitSwitchCase(ISwitchCaseOperation operation)
        {
            Assert.Equal(OperationKind.SwitchCase, operation.Kind);
            VisitLocals(operation.Locals);
            AssertEx.Equal(operation.Clauses.Concat(operation.Body), operation.Children);

            VerifySubTree(((BaseSwitchCaseOperation)operation).Condition);
        }

        internal static void VerifySubTree(IOperation root)
        {
            if (root != null)
            {
                Assert.Null(root.Parent);
                var explictNodeMap = new Dictionary<SyntaxNode, IOperation>();

                foreach (IOperation descendant in root.DescendantsAndSelf())
                {
                    if (!descendant.IsImplicit)
                    {
                        try
                        {
                            explictNodeMap.Add(descendant.Syntax, descendant);
                        }
                        catch (ArgumentException)
                        {
                            Assert.False(true, $"Duplicate explicit node for syntax ({descendant.Syntax.RawKind}): {descendant.Syntax.ToString()}");
                        }
                    }

                    Singleton.Visit(descendant);
                }
            }
        }

        public override void VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation)
        {
            VisitCaseClauseOperation(operation);
            Assert.Equal(CaseKind.SingleValue, operation.CaseKind);
            AssertEx.Equal(new[] { operation.Value }, operation.Children);
        }

        private static void VisitCaseClauseOperation(ICaseClauseOperation operation)
        {
            Assert.Equal(OperationKind.CaseClause, operation.Kind);
            _ = operation.Label;
        }

        public override void VisitRelationalCaseClause(IRelationalCaseClauseOperation operation)
        {
            VisitCaseClauseOperation(operation);
            Assert.Equal(CaseKind.Relational, operation.CaseKind);
            var relation = operation.Relation;

            AssertEx.Equal(new[] { operation.Value }, operation.Children);
        }

        public override void VisitDefaultCaseClause(IDefaultCaseClauseOperation operation)
        {
            VisitCaseClauseOperation(operation);
            Assert.Equal(CaseKind.Default, operation.CaseKind);
            Assert.Empty(operation.Children);
        }

        private static void VisitLocals(ImmutableArray<ILocalSymbol> locals)
        {
            foreach (var local in locals)
            {
                Assert.NotNull(local);
            }
        }

        public override void VisitWhileLoop(IWhileLoopOperation operation)
        {
            VisitLoop(operation);
            Assert.Equal(LoopKind.While, operation.LoopKind);

            var conditionIsTop = operation.ConditionIsTop;
            var conditionIsUntil = operation.ConditionIsUntil;

            IEnumerable<IOperation> children;

            if (conditionIsTop)
            {
                if (operation.IgnoredCondition != null)
                {
                    children = new[] { operation.Condition, operation.Body, operation.IgnoredCondition };
                }
                else
                {
                    children = new[] { operation.Condition, operation.Body };
                }
            }
            else
            {
                Assert.Null(operation.IgnoredCondition);

                if (operation.Condition != null)
                {
                    children = new[] { operation.Body, operation.Condition };
                }
                else
                {
                    children = new[] { operation.Body };
                }
            }

            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitForLoop(IForLoopOperation operation)
        {
            VisitLoop(operation);
            Assert.Equal(LoopKind.For, operation.LoopKind);
            VisitLocals(operation.Locals);
            VisitLocals(operation.ConditionLocals);

            IEnumerable<IOperation> children = operation.Before;

            if (operation.Condition != null)
            {
                children = children.Concat(new[] { operation.Condition });
            }

            children = children.Concat(new[] { operation.Body });
            children = children.Concat(operation.AtLoopBottom);

            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitForToLoop(IForToLoopOperation operation)
        {
            VisitLoop(operation);
            Assert.Equal(LoopKind.ForTo, operation.LoopKind);
            _ = operation.IsChecked;
            (ILocalSymbol loopObject, ForToLoopOperationUserDefinedInfo userDefinedInfo) = ((BaseForToLoopOperation)operation).Info;

            if (userDefinedInfo != null)
            {
                VerifySubTree(userDefinedInfo.Addition.Value);
                VerifySubTree(userDefinedInfo.Subtraction.Value);
                VerifySubTree(userDefinedInfo.LessThanOrEqual.Value);
                VerifySubTree(userDefinedInfo.GreaterThanOrEqual.Value);
            }

            IEnumerable<IOperation> children;
            children = new[] { operation.LoopControlVariable, operation.InitialValue, operation.LimitValue, operation.StepValue, operation.Body };
            children = children.Concat(operation.NextVariables);
            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitForEachLoop(IForEachLoopOperation operation)
        {
            VisitLoop(operation);
            Assert.Equal(LoopKind.ForEach, operation.LoopKind);

            IEnumerable<IOperation> children = new[] { operation.Collection, operation.LoopControlVariable, operation.Body }.Concat(operation.NextVariables);
            AssertEx.Equal(children, operation.Children);
            ForEachLoopOperationInfo info = ((BaseForEachLoopOperation)operation).Info;
            if (info != null)
            {
                visitArguments(info.GetEnumeratorArguments);
                visitArguments(info.MoveNextArguments);
                visitArguments(info.CurrentArguments);
            }

            void visitArguments(ImmutableArray<IArgumentOperation> arguments)
            {
                if (arguments != null)
                {
                    foreach (IArgumentOperation arg in arguments)
                    {
                        VerifySubTree(arg);
                    }
                }
            }
        }

        private static void VisitLoop(ILoopOperation operation)
        {
            Assert.Equal(OperationKind.Loop, operation.Kind);
            VisitLocals(operation.Locals);
            Assert.NotNull(operation.ContinueLabel);
            Assert.NotNull(operation.ExitLabel);
        }

        public override void VisitLabeled(ILabeledOperation operation)
        {
            Assert.Equal(OperationKind.Labeled, operation.Kind);
            Assert.NotNull(operation.Label);

            if (operation.Operation == null)
            {
                Assert.Empty(operation.Children);
            }
            else
            {
                Assert.Same(operation.Operation, operation.Children.Single());
            }
        }

        public override void VisitBranch(IBranchOperation operation)
        {
            Assert.Equal(OperationKind.Branch, operation.Kind);
            Assert.NotNull(operation.Target);

            switch (operation.BranchKind)
            {
                case BranchKind.Break:
                case BranchKind.Continue:
                case BranchKind.GoTo:
                    break;
                default:
                    Assert.False(true);
                    break;
            }

            Assert.Empty(operation.Children);
        }

        public override void VisitEmpty(IEmptyOperation operation)
        {
            Assert.Equal(OperationKind.Empty, operation.Kind);
            Assert.Empty(operation.Children);
        }

        public override void VisitReturn(IReturnOperation operation)
        {
            Assert.Contains(operation.Kind, new[] { OperationKind.Return, OperationKind.YieldReturn, OperationKind.YieldBreak });
            if (operation.ReturnedValue == null)
            {
                Assert.NotEqual(OperationKind.YieldReturn, operation.Kind);
                Assert.Empty(operation.Children);
            }
            else
            {
                Assert.Same(operation.ReturnedValue, operation.Children.Single());
            }
        }

        public override void VisitLock(ILockOperation operation)
        {
            Assert.Equal(OperationKind.Lock, operation.Kind);
            AssertEx.Equal(new[] { operation.LockedValue, operation.Body }, operation.Children);
        }

        public override void VisitTry(ITryOperation operation)
        {
            Assert.Equal(OperationKind.Try, operation.Kind);
            IEnumerable<IOperation> children = new[] { operation.Body };
            _ = operation.ExitLabel;
            children = children.Concat(operation.Catches);
            if (operation.Finally != null)
            {
                children = children.Concat(new[] { operation.Finally });
            }

            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitCatchClause(ICatchClauseOperation operation)
        {
            Assert.Equal(OperationKind.CatchClause, operation.Kind);
            var exceptionType = operation.ExceptionType;
            VisitLocals(operation.Locals);

            IEnumerable<IOperation> children = Array.Empty<IOperation>();
            if (operation.ExceptionDeclarationOrExpression != null)
            {
                children = children.Concat(new[] { operation.ExceptionDeclarationOrExpression });
            }

            if (operation.Filter != null)
            {
                children = children.Concat(new[] { operation.Filter });
            }

            children = children.Concat(new[] { operation.Handler });
            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitUsing(IUsingOperation operation)
        {
            Assert.Equal(OperationKind.Using, operation.Kind);
            VisitLocals(operation.Locals);
            AssertEx.Equal(new[] { operation.Resources, operation.Body }, operation.Children);
            Assert.NotEqual(OperationKind.VariableDeclaration, operation.Resources.Kind);
            Assert.NotEqual(OperationKind.VariableDeclarator, operation.Resources.Kind);
        }

        // https://github.com/dotnet/roslyn/issues/21281
        internal override void VisitFixed(IFixedOperation operation)
        {
            Assert.Equal(OperationKind.None, operation.Kind);
            VisitLocals(operation.Locals);
            AssertEx.Equal(new[] { operation.Variables, operation.Body }, operation.Children);
        }

        internal override void VisitAggregateQuery(IAggregateQueryOperation operation)
        {
            Assert.Equal(OperationKind.None, operation.Kind);
            AssertEx.Equal(new[] { operation.Group, operation.Aggregation }, operation.Children);
        }

        public override void VisitExpressionStatement(IExpressionStatementOperation operation)
        {
            Assert.Equal(OperationKind.ExpressionStatement, operation.Kind);
            Assert.Same(operation.Operation, operation.Children.Single());
        }

        internal override void VisitWith(IWithOperation operation)
        {
            Assert.Equal(OperationKind.None, operation.Kind);
            AssertEx.Equal(new[] { operation.Value, operation.Body }, operation.Children);
        }

        public override void VisitStop(IStopOperation operation)
        {
            Assert.Equal(OperationKind.Stop, operation.Kind);
            Assert.Empty(operation.Children);
        }

        public override void VisitEnd(IEndOperation operation)
        {
            Assert.Equal(OperationKind.End, operation.Kind);
            Assert.Empty(operation.Children);
        }

        public override void VisitInvocation(IInvocationOperation operation)
        {
            Assert.Equal(OperationKind.Invocation, operation.Kind);
            Assert.NotNull(operation.TargetMethod);
            var isVirtual = operation.IsVirtual;

            IEnumerable<IOperation> children;
            if (operation.Instance != null)
            {
                children = new[] { operation.Instance }.Concat(operation.Arguments);
            }
            else
            {
                children = operation.Arguments;
            }

            AssertEx.Equal(children, operation.Children);

            // Make sure that all static member references or invocations of static methods do not have implicit IInstanceReferenceOperations
            // as their receivers
            if (operation.TargetMethod.IsStatic &&
                operation.Instance is IInstanceReferenceOperation)
            {
                Assert.False(operation.Instance.IsImplicit, $"Implicit {nameof(IInstanceReferenceOperation)} on {operation.Syntax}");
            }
        }

        public override void VisitArgument(IArgumentOperation operation)
        {
            Assert.Equal(OperationKind.Argument, operation.Kind);
            Assert.Contains(operation.ArgumentKind, new[] { ArgumentKind.DefaultValue, ArgumentKind.Explicit, ArgumentKind.ParamArray });
            var parameter = operation.Parameter;

            Assert.Same(operation.Value, operation.Children.Single());
            var inConversion = operation.InConversion;
            var outConversion = operation.OutConversion;

            if (operation.ArgumentKind == ArgumentKind.DefaultValue)
            {
                Assert.True(operation.Descendants().All(n => n.IsImplicit), $"Explicit node in default argument value ({operation.Syntax.RawKind}): {operation.Syntax.ToString()}");
            }
        }

        public override void VisitOmittedArgument(IOmittedArgumentOperation operation)
        {
            Assert.Equal(OperationKind.OmittedArgument, operation.Kind);
            Assert.Empty(operation.Children);
        }

        public override void VisitArrayElementReference(IArrayElementReferenceOperation operation)
        {
            Assert.Equal(OperationKind.ArrayElementReference, operation.Kind);
            AssertEx.Equal(new[] { operation.ArrayReference }.Concat(operation.Indices), operation.Children);
        }

        internal override void VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation)
        {
            Assert.Equal(OperationKind.None, operation.Kind);
            Assert.Same(operation.Pointer, operation.Children.Single());
        }

        public override void VisitLocalReference(ILocalReferenceOperation operation)
        {
            Assert.Equal(OperationKind.LocalReference, operation.Kind);
            Assert.NotNull(operation.Local);
            var isDeclaration = operation.IsDeclaration;
            Assert.Empty(operation.Children);
        }

        public override void VisitParameterReference(IParameterReferenceOperation operation)
        {
            Assert.Equal(OperationKind.ParameterReference, operation.Kind);
            Assert.NotNull(operation.Parameter);
            Assert.Empty(operation.Children);
        }

        public override void VisitInstanceReference(IInstanceReferenceOperation operation)
        {
            Assert.Equal(OperationKind.InstanceReference, operation.Kind);
            Assert.Empty(operation.Children);
            var referenceKind = operation.ReferenceKind;
        }

        private void VisitMemberReference(IMemberReferenceOperation operation)
        {
            VisitMemberReference(operation, Array.Empty<IOperation>());
        }

        private void VisitMemberReference(IMemberReferenceOperation operation, IEnumerable<IOperation> additionalChildren)
        {
            Assert.NotNull(operation.Member);

            IEnumerable<IOperation> children;

            if (operation.Instance != null)
            {
                children = new[] { operation.Instance }.Concat(additionalChildren);

                // Make sure that all static member references or invocations of static methods do not have implicit IInstanceReferenceOperations
                // as their receivers
                if (operation.Member.IsStatic &&
                    operation.Instance is IInstanceReferenceOperation)
                {
                    Assert.False(operation.Instance.IsImplicit, $"Implicit {nameof(IInstanceReferenceOperation)} on {operation.Syntax}");
                }
            }
            else
            {
                children = additionalChildren;
            }

            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitFieldReference(IFieldReferenceOperation operation)
        {
            Assert.Equal(OperationKind.FieldReference, operation.Kind);
            VisitMemberReference(operation);

            Assert.Same(operation.Member, operation.Field);
            var isDeclaration = operation.IsDeclaration;
        }

        public override void VisitMethodReference(IMethodReferenceOperation operation)
        {
            Assert.Equal(OperationKind.MethodReference, operation.Kind);
            VisitMemberReference(operation);

            Assert.Same(operation.Member, operation.Method);
            var isVirtual = operation.IsVirtual;
        }

        public override void VisitPropertyReference(IPropertyReferenceOperation operation)
        {
            Assert.Equal(OperationKind.PropertyReference, operation.Kind);
            VisitMemberReference(operation, operation.Arguments);

            Assert.Same(operation.Member, operation.Property);
        }

        public override void VisitEventReference(IEventReferenceOperation operation)
        {
            Assert.Equal(OperationKind.EventReference, operation.Kind);
            VisitMemberReference(operation);

            Assert.Same(operation.Member, operation.Event);
        }

        public override void VisitEventAssignment(IEventAssignmentOperation operation)
        {
            Assert.Equal(OperationKind.EventAssignment, operation.Kind);
            var adds = operation.Adds;
            AssertEx.Equal(new[] { operation.EventReference, operation.HandlerValue }, operation.Children);
        }

        public override void VisitConditionalAccess(IConditionalAccessOperation operation)
        {
            Assert.Equal(OperationKind.ConditionalAccess, operation.Kind);
            Assert.NotNull(operation.Type);
            AssertEx.Equal(new[] { operation.Operation, operation.WhenNotNull }, operation.Children);
        }

        public override void VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation)
        {
            Assert.Equal(OperationKind.ConditionalAccessInstance, operation.Kind);
            Assert.Empty(operation.Children);
        }

        internal override void VisitPlaceholder(IPlaceholderOperation operation)
        {
            Assert.Equal(OperationKind.None, operation.Kind);
            Assert.Empty(operation.Children);
        }

        public override void VisitUnaryOperator(IUnaryOperation operation)
        {
            Assert.Equal(OperationKind.UnaryOperator, operation.Kind);
            Assert.Equal(OperationKind.Unary, operation.Kind);
            var operatorMethod = operation.OperatorMethod;
            var unaryOperationKind = operation.OperatorKind;
            var isLifted = operation.IsLifted;
            var isChecked = operation.IsChecked;

            Assert.Same(operation.Operand, operation.Children.Single());
        }

        public override void VisitBinaryOperator(IBinaryOperation operation)
        {
            Assert.Equal(OperationKind.BinaryOperator, operation.Kind);
            Assert.Equal(OperationKind.Binary, operation.Kind);
            var operatorMethod = operation.OperatorMethod;
            var unaryOperatorMethod = ((BaseBinaryOperation)operation).UnaryOperatorMethod;
            var binaryOperationKind = operation.OperatorKind;
            var isLifted = operation.IsLifted;
            var isChecked = operation.IsChecked;
            var isCompareText = operation.IsCompareText;

            AssertEx.Equal(new[] { operation.LeftOperand, operation.RightOperand }, operation.Children);
        }

        public override void VisitTupleBinaryOperator(ITupleBinaryOperation operation)
        {
            Assert.Equal(OperationKind.TupleBinaryOperator, operation.Kind);
            Assert.Equal(OperationKind.TupleBinary, operation.Kind);
            var binaryOperationKind = operation.OperatorKind;

            AssertEx.Equal(new[] { operation.LeftOperand, operation.RightOperand }, operation.Children);
        }

        public override void VisitConversion(IConversionOperation operation)
        {
            Assert.Equal(OperationKind.Conversion, operation.Kind);
            var operatorMethod = operation.OperatorMethod;
            var conversion = operation.Conversion;
            var isChecked = operation.IsChecked;
            var isTryCast = operation.IsTryCast;

            switch (operation.Language)
            {
                case LanguageNames.CSharp:
                    CSharp.Conversion csharpConversion = CSharp.CSharpExtensions.GetConversion(operation);
                    Assert.Throws<ArgumentException>(() => VisualBasic.VisualBasicExtensions.GetConversion(operation));
                    break;
                case LanguageNames.VisualBasic:
                    VisualBasic.Conversion visualBasicConversion = VisualBasic.VisualBasicExtensions.GetConversion(operation);
                    Assert.Throws<ArgumentException>(() => CSharp.CSharpExtensions.GetConversion(operation));
                    break;
                default:
                    Debug.Fail($"Language {operation.Language} is unknown!");
                    break;
            }

            Assert.Same(operation.Operand, operation.Children.Single());
        }

        public override void VisitConditional(IConditionalOperation operation)
        {
            Assert.Equal(OperationKind.Conditional, operation.Kind);
            bool isRef = operation.IsRef;

            if (operation.WhenFalse != null)
            {
                AssertEx.Equal(new[] { operation.Condition, operation.WhenTrue, operation.WhenFalse }, operation.Children);
            }
            else
            {
                AssertEx.Equal(new[] { operation.Condition, operation.WhenTrue }, operation.Children);
            }
        }

        public override void VisitCoalesce(ICoalesceOperation operation)
        {
            Assert.Equal(OperationKind.Coalesce, operation.Kind);
            AssertEx.Equal(new[] { operation.Value, operation.WhenNull }, operation.Children);
            var valueConversion = operation.ValueConversion;
        }

        public override void VisitCoalesceAssignment(ICoalesceAssignmentOperation operation)
        {
            Assert.Equal(OperationKind.CoalesceAssignment, operation.Kind);
            AssertEx.Equal(new[] { operation.Target, operation.Value }, operation.Children);
        }

        public override void VisitIsType(IIsTypeOperation operation)
        {
            Assert.Equal(OperationKind.IsType, operation.Kind);
            Assert.NotNull(operation.TypeOperand);
            bool isNegated = operation.IsNegated;
            Assert.Same(operation.ValueOperand, operation.Children.Single());
        }

        public override void VisitSizeOf(ISizeOfOperation operation)
        {
            Assert.Equal(OperationKind.SizeOf, operation.Kind);
            Assert.NotNull(operation.TypeOperand);
            Assert.Empty(operation.Children);
        }

        public override void VisitTypeOf(ITypeOfOperation operation)
        {
            Assert.Equal(OperationKind.TypeOf, operation.Kind);
            Assert.NotNull(operation.TypeOperand);
            Assert.Empty(operation.Children);
        }

        public override void VisitAnonymousFunction(IAnonymousFunctionOperation operation)
        {
            Assert.Equal(OperationKind.AnonymousFunction, operation.Kind);
            Assert.NotNull(operation.Symbol);
            Assert.Same(operation.Body, operation.Children.Single());
        }

        public override void VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation)
        {
            Assert.Equal(OperationKind.FlowAnonymousFunction, operation.Kind);
            Assert.NotNull(operation.Symbol);
            Assert.Empty(operation.Children);
        }

        public override void VisitLocalFunction(ILocalFunctionOperation operation)
        {
            Assert.Equal(OperationKind.LocalFunction, operation.Kind);
            Assert.NotNull(operation.Symbol);

            if (operation.Body != null)
            {
                var children = operation.Children.ToImmutableArray();
                Assert.Same(operation.Body, children[0]);
                if (operation.IgnoredBody != null)
                {
                    Assert.Same(operation.IgnoredBody, children[1]);
                    Assert.Equal(2, children.Length);
                }
                else
                {
                    Assert.Equal(1, children.Length);
                }
            }
            else
            {
                Assert.Null(operation.IgnoredBody);
                Assert.Empty(operation.Children);
            }
        }

        public override void VisitLiteral(ILiteralOperation operation)
        {
            Assert.Equal(OperationKind.Literal, operation.Kind);
            Assert.Empty(operation.Children);
        }

        public override void VisitAwait(IAwaitOperation operation)
        {
            Assert.Equal(OperationKind.Await, operation.Kind);
            Assert.Same(operation.Operation, operation.Children.Single());
        }

        public override void VisitNameOf(INameOfOperation operation)
        {
            Assert.Equal(OperationKind.NameOf, operation.Kind);
            Assert.Same(operation.Argument, operation.Children.Single());
        }

        public override void VisitThrow(IThrowOperation operation)
        {
            Assert.Equal(OperationKind.Throw, operation.Kind);
            if (operation.Exception == null)
            {
                Assert.Empty(operation.Children);
            }
            else
            {
                Assert.Same(operation.Exception, operation.Children.Single());
            }
        }

        public override void VisitAddressOf(IAddressOfOperation operation)
        {
            Assert.Equal(OperationKind.AddressOf, operation.Kind);
            Assert.Same(operation.Reference, operation.Children.Single());
        }

        public override void VisitObjectCreation(IObjectCreationOperation operation)
        {
            Assert.Equal(OperationKind.ObjectCreation, operation.Kind);
            var constructor = operation.Constructor;

            // When parameter-less struct constructor is inaccessible, the constructor symbol is null
            if (!operation.Type.IsValueType)
            {
                Assert.NotNull(constructor);

                if (constructor == null)
                {
                    Assert.Empty(operation.Arguments);
                }
            }

            IEnumerable<IOperation> children = operation.Arguments;
            if (operation.Initializer != null)
            {
                children = children.Concat(new[] { operation.Initializer });
            }

            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation)
        {
            Assert.Equal(OperationKind.AnonymousObjectCreation, operation.Kind);
            AssertEx.Equal(operation.Initializers, operation.Children);
            foreach (var initializer in operation.Initializers)
            {
                var simpleAssignment = (ISimpleAssignmentOperation)initializer;
                var propertyReference = (IPropertyReferenceOperation)simpleAssignment.Target;
                Assert.Empty(propertyReference.Arguments);
                Assert.Equal(OperationKind.InstanceReference, propertyReference.Instance.Kind);
                Assert.Equal(InstanceReferenceKind.ImplicitReceiver, ((IInstanceReferenceOperation)propertyReference.Instance).ReferenceKind);
            }
        }

        public override void VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation)
        {
            Assert.Equal(OperationKind.DynamicObjectCreation, operation.Kind);

            IEnumerable<IOperation> children = operation.Arguments;
            if (operation.Initializer != null)
            {
                children = children.Concat(new[] { operation.Initializer });
            }

            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitDynamicInvocation(IDynamicInvocationOperation operation)
        {
            Assert.Equal(OperationKind.DynamicInvocation, operation.Kind);
            AssertEx.Equal(new[] { operation.Operation }.Concat(operation.Arguments), operation.Children);
        }

        public override void VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation)
        {
            Assert.Equal(OperationKind.DynamicIndexerAccess, operation.Kind);
            AssertEx.Equal(new[] { operation.Operation }.Concat(operation.Arguments), operation.Children);
        }

        public override void VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation)
        {
            Assert.Equal(OperationKind.ObjectOrCollectionInitializer, operation.Kind);
            AssertEx.Equal(operation.Initializers, operation.Children);
        }

        public override void VisitMemberInitializer(IMemberInitializerOperation operation)
        {
            Assert.Equal(OperationKind.MemberInitializer, operation.Kind);
            AssertEx.Equal(new[] { operation.InitializedMember, operation.Initializer }, operation.Children);
        }

        private void VisitSymbolInitializer(ISymbolInitializerOperation operation)
        {
            VisitLocals(operation.Locals);
            Assert.Same(operation.Value, operation.Children.Single());
        }

        public override void VisitFieldInitializer(IFieldInitializerOperation operation)
        {
            Assert.Equal(OperationKind.FieldInitializer, operation.Kind);
            foreach (var field in operation.InitializedFields)
            {
                Assert.NotNull(field);
            }
            VisitSymbolInitializer(operation);
        }

        public override void VisitVariableInitializer(IVariableInitializerOperation operation)
        {
            Assert.Equal(OperationKind.VariableInitializer, operation.Kind);
            Assert.Empty(operation.Locals);
            VisitSymbolInitializer(operation);
        }

        public override void VisitPropertyInitializer(IPropertyInitializerOperation operation)
        {
            Assert.Equal(OperationKind.PropertyInitializer, operation.Kind);
            foreach (var property in operation.InitializedProperties)
            {
                Assert.NotNull(property);
            }
            VisitSymbolInitializer(operation);
        }

        public override void VisitParameterInitializer(IParameterInitializerOperation operation)
        {
            Assert.Equal(OperationKind.ParameterInitializer, operation.Kind);
            Assert.NotNull(operation.Parameter);
            VisitSymbolInitializer(operation);
        }

        public override void VisitArrayCreation(IArrayCreationOperation operation)
        {
            Assert.Equal(OperationKind.ArrayCreation, operation.Kind);

            IEnumerable<IOperation> children = operation.DimensionSizes;
            if (operation.Initializer != null)
            {
                children = children.Concat(new[] { operation.Initializer });
            }

            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitArrayInitializer(IArrayInitializerOperation operation)
        {
            Assert.Equal(OperationKind.ArrayInitializer, operation.Kind);
            Assert.Null(operation.Type);
            AssertEx.Equal(operation.ElementValues, operation.Children);
        }

        private void VisitAssignment(IAssignmentOperation operation)
        {
            AssertEx.Equal(new[] { operation.Target, operation.Value }, operation.Children);
        }

        public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
        {
            Assert.Equal(OperationKind.SimpleAssignment, operation.Kind);
            bool isRef = operation.IsRef;
            VisitAssignment(operation);
        }

        public override void VisitCompoundAssignment(ICompoundAssignmentOperation operation)
        {
            Assert.Equal(OperationKind.CompoundAssignment, operation.Kind);
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

            var isLifted = operation.IsLifted;
            var isChecked = operation.IsChecked;
            VisitAssignment(operation);
        }

        public override void VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation)
        {
            Assert.Contains(operation.Kind, new[] { OperationKind.Increment, OperationKind.Decrement });
            var operatorMethod = operation.OperatorMethod;
            var isPostFix = operation.IsPostfix;
            var isLifted = operation.IsLifted;
            var isChecked = operation.IsChecked;

            Assert.Same(operation.Target, operation.Children.Single());
        }

        public override void VisitParenthesized(IParenthesizedOperation operation)
        {
            Assert.Equal(OperationKind.Parenthesized, operation.Kind);
            Assert.Same(operation.Operand, operation.Children.Single());
        }

        public override void VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation)
        {
            Assert.Equal(OperationKind.DynamicMemberReference, operation.Kind);
            Assert.NotNull(operation.MemberName);

            foreach (var typeArg in operation.TypeArguments)
            {
                Assert.NotNull(typeArg);
            }

            var containingType = operation.ContainingType;

            if (operation.Instance == null)
            {
                Assert.Empty(operation.Children);
            }
            else
            {
                Assert.Same(operation.Instance, operation.Children.Single());
            }
        }

        public override void VisitDefaultValue(IDefaultValueOperation operation)
        {
            Assert.Equal(OperationKind.DefaultValue, operation.Kind);
            Assert.Empty(operation.Children);
        }

        public override void VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation)
        {
            Assert.Equal(OperationKind.TypeParameterObjectCreation, operation.Kind);
            if (operation.Initializer == null)
            {
                Assert.Empty(operation.Children);
            }
            else
            {
                Assert.Same(operation.Initializer, operation.Children.Single());
            }
        }

        internal override void VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation)
        {
            Assert.Equal(OperationKind.None, operation.Kind);
            if (operation.Initializer == null)
            {
                Assert.Empty(operation.Children);
            }
            else
            {
                Assert.Same(operation.Initializer, operation.Children.Single());
            }
        }

        public override void VisitInvalid(IInvalidOperation operation)
        {
            Assert.Equal(OperationKind.Invalid, operation.Kind);
        }

        public override void VisitTuple(ITupleOperation operation)
        {
            Assert.Equal(OperationKind.Tuple, operation.Kind);
            var naturalType = operation.NaturalType;
            AssertEx.Equal(operation.Elements, operation.Children);
        }

        public override void VisitInterpolatedString(IInterpolatedStringOperation operation)
        {
            Assert.Equal(OperationKind.InterpolatedString, operation.Kind);
            AssertEx.Equal(operation.Parts, operation.Children);
        }

        public override void VisitInterpolatedStringText(IInterpolatedStringTextOperation operation)
        {
            Assert.Equal(OperationKind.InterpolatedStringText, operation.Kind);
            Assert.Equal(OperationKind.Literal, operation.Text.Kind);
            Assert.Same(operation.Text, operation.Children.Single());
        }

        public override void VisitInterpolation(IInterpolationOperation operation)
        {
            Assert.Equal(OperationKind.Interpolation, operation.Kind);
            IEnumerable<IOperation> children = new[] { operation.Expression };
            if (operation.Alignment != null)
            {
                children = children.Concat(new[] { operation.Alignment });
            }

            if (operation.FormatString != null)
            {
                Assert.Equal(OperationKind.Literal, operation.FormatString.Kind);
                children = children.Concat(new[] { operation.FormatString });
            }

            AssertEx.Equal(children, operation.Children);
        }

        private void VisitPatternCommon(IPatternOperation pattern)
        {
            Assert.NotNull(pattern.InputType);
            Assert.Null(pattern.Type);
            Assert.False(pattern.ConstantValue.HasValue);
        }

        public override void VisitConstantPattern(IConstantPatternOperation operation)
        {
            Assert.Equal(OperationKind.ConstantPattern, operation.Kind);
            VisitPatternCommon(operation);
            Assert.Same(operation.Value, operation.Children.Single());
        }

        public override void VisitDeclarationPattern(IDeclarationPatternOperation operation)
        {
            Assert.Equal(OperationKind.DeclarationPattern, operation.Kind);
            VisitPatternCommon(operation);
            if (operation.Syntax.IsKind(CSharp.SyntaxKind.VarPattern) ||
                // in `var (x, y)`, the syntax here is the designation `x`.
                operation.Syntax.IsKind(CSharp.SyntaxKind.SingleVariableDesignation))
            {
                Assert.True(operation.MatchesNull);
                Assert.Null(operation.MatchedType);
            }
            else
            {
                Assert.False(operation.MatchesNull);
                Assert.NotNull(operation.MatchedType);
            }

            var designation =
                (operation.Syntax as CSharp.Syntax.DeclarationPatternSyntax)?.Designation ??
                (operation.Syntax as CSharp.Syntax.VarPatternSyntax)?.Designation ??
                (operation.Syntax as CSharp.Syntax.VariableDesignationSyntax);
            if (designation.IsKind(CSharp.SyntaxKind.SingleVariableDesignation))
            {
                Assert.NotNull(operation.DeclaredSymbol);
            }
            else
            {
                Assert.Null(operation.DeclaredSymbol);
            }

            Assert.Empty(operation.Children);
        }

        internal override void VisitRecursivePattern(IRecursivePatternOperation operation)
        {
            Assert.Equal(OperationKind.RecursivePattern, operation.Kind);
            VisitPatternCommon(operation);
            Assert.NotNull(operation.MatchedType);
            switch (operation.DeconstructSymbol)
            {
                case IErrorTypeSymbol error:
                case null: // OK: indicates deconstruction of a tuple, or an error case
                    break;
                case IMethodSymbol method:
                    // when we have a method, it is a `Deconstruct` method
                    Assert.Equal("Deconstruct", method.Name);
                    break;
                case ITypeSymbol type:
                    // when we have a type, it is the type "ITuple"
                    Assert.Equal("ITuple", type.Name);
                    break;
                default:
                    Assert.True(false, $"Unexpected symbol {operation.DeconstructSymbol}");
                    break;
            }

            var designation = (operation.Syntax as CSharp.Syntax.RecursivePatternSyntax)?.Designation;
            if (designation.IsKind(CSharp.SyntaxKind.SingleVariableDesignation))
            {
                Assert.NotNull(operation.DeclaredSymbol);
            }
            else
            {
                Assert.Null(operation.DeclaredSymbol);
            }

            foreach (var subpat in operation.PropertySubpatterns)
            {
                Assert.True(subpat is IPropertySubpatternOperation);
            }

            IEnumerable<IOperation> children = operation.DeconstructionSubpatterns.Cast<IOperation>();
            children = children.Concat(operation.PropertySubpatterns);

            AssertEx.Equal(children, operation.Children);
        }

        internal override void VisitPropertySubpattern(IPropertySubpatternOperation operation)
        {
            Assert.NotNull(operation.Pattern);
            var children = new IOperation[] { operation.Member, operation.Pattern };
            AssertEx.Equal(children, operation.Children);

            if (operation.Member.Kind == OperationKind.Invalid)
            {
                return;
            }

            Assert.True(operation.Member is IMemberReferenceOperation);
            var member = (IMemberReferenceOperation)operation.Member;
            switch (member.Member)
            {
                case IFieldSymbol field:
                case IPropertySymbol prop:
                case IErrorTypeSymbol error:
                    break;
                case var symbol:
                    Assert.True(false, $"Unexpected symbol {symbol}");
                    break;
            }
        }

        public override void VisitSwitchExpression(ISwitchExpressionOperation operation)
        {
            Assert.NotNull(operation.Type);
            Assert.False(operation.ConstantValue.HasValue);
            Assert.Equal(OperationKind.SwitchExpression, operation.Kind);
            Assert.NotNull(operation.Value);
            var children = operation.Arms.Cast<IOperation>().Prepend(operation.Value);
            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation)
        {
            Assert.Null(operation.Type);
            Assert.False(operation.ConstantValue.HasValue);
            Assert.NotNull(operation.Pattern);
            _ = operation.Guard;
            Assert.NotNull(operation.Value);
            VisitLocals(operation.Locals);
            var children = operation.Guard == null
                ? new[] { operation.Pattern, operation.Value }
                : new[] { operation.Pattern, operation.Guard, operation.Value };
            AssertEx.Equal(children, operation.Children);
        }

        public override void VisitIsPattern(IIsPatternOperation operation)
        {
            Assert.Equal(OperationKind.IsPattern, operation.Kind);
            AssertEx.Equal(new[] { operation.Value, operation.Pattern }, operation.Children);
        }

        public override void VisitPatternCaseClause(IPatternCaseClauseOperation operation)
        {
            VisitCaseClauseOperation(operation);
            Assert.Equal(CaseKind.Pattern, operation.CaseKind);
            Assert.Same(((ICaseClauseOperation)operation).Label, operation.Label);

            if (operation.Guard != null)
            {
                AssertEx.Equal(new[] { operation.Pattern, operation.Guard }, operation.Children);
            }
            else
            {
                Assert.Same(operation.Pattern, operation.Children.Single());
            }
        }

        public override void VisitTranslatedQuery(ITranslatedQueryOperation operation)
        {
            Assert.Equal(OperationKind.TranslatedQuery, operation.Kind);
            Assert.Same(operation.Operation, operation.Children.Single());
        }

        public override void VisitDeclarationExpression(IDeclarationExpressionOperation operation)
        {
            Assert.Equal(OperationKind.DeclarationExpression, operation.Kind);
            Assert.Same(operation.Expression, operation.Children.Single());
        }

        public override void VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation)
        {
            Assert.Equal(OperationKind.DeconstructionAssignment, operation.Kind);
            VisitAssignment(operation);
        }

        public override void VisitDelegateCreation(IDelegateCreationOperation operation)
        {
            Assert.Equal(OperationKind.DelegateCreation, operation.Kind);
            Assert.Same(operation.Target, operation.Children.Single());
        }

        public override void VisitRaiseEvent(IRaiseEventOperation operation)
        {
            Assert.Equal(OperationKind.RaiseEvent, operation.Kind);
            AssertEx.Equal(new IOperation[] { operation.EventReference }.Concat(operation.Arguments), operation.Children);
        }

        public override void VisitRangeCaseClause(IRangeCaseClauseOperation operation)
        {
            VisitCaseClauseOperation(operation);
            Assert.Equal(CaseKind.Range, operation.CaseKind);
            AssertEx.Equal(new[] { operation.MinimumValue, operation.MaximumValue }, operation.Children);
        }

        public override void VisitConstructorBodyOperation(IConstructorBodyOperation operation)
        {
            Assert.Equal(OperationKind.ConstructorBodyOperation, operation.Kind);
            Assert.Equal(OperationKind.ConstructorBody, operation.Kind);
            VisitLocals(operation.Locals);

            var builder = ArrayBuilder<IOperation>.GetInstance();

            if (operation.Initializer != null)
            {
                builder.Add(operation.Initializer);
            }

            if (operation.BlockBody != null)
            {
                builder.Add(operation.BlockBody);
            }

            if (operation.ExpressionBody != null)
            {
                builder.Add(operation.ExpressionBody);
            }

            AssertEx.Equal(builder, operation.Children);
            builder.Free();
        }

        public override void VisitMethodBodyOperation(IMethodBodyOperation operation)
        {
            Assert.Equal(OperationKind.MethodBodyOperation, operation.Kind);
            Assert.Equal(OperationKind.MethodBody, operation.Kind);

            if (operation.BlockBody != null)
            {
                if (operation.ExpressionBody != null)
                {
                    AssertEx.Equal(new[] { operation.BlockBody, operation.ExpressionBody }, operation.Children);
                }
                else
                {
                    Assert.Same(operation.BlockBody, operation.Children.Single());
                }
            }
            else if (operation.ExpressionBody != null)
            {
                Assert.Same(operation.ExpressionBody, operation.Children.Single());
            }
            else
            {
                Assert.Empty(operation.Children);
            }
        }

        public override void VisitDiscardOperation(IDiscardOperation operation)
        {
            Assert.Equal(OperationKind.Discard, operation.Kind);
            Assert.Empty(operation.Children);

            var discardSymbol = operation.DiscardSymbol;
            Assert.Equal(operation.Type, discardSymbol.Type);
        }

        public override void VisitDiscardPattern(IDiscardPatternOperation operation)
        {
            Assert.Equal(OperationKind.DiscardPattern, operation.Kind);
            VisitPatternCommon(operation);
            Assert.Empty(operation.Children);
        }

        public override void VisitFlowCapture(IFlowCaptureOperation operation)
        {
            Assert.Equal(OperationKind.FlowCapture, operation.Kind);
            Assert.True(operation.IsImplicit);
            Assert.Same(operation.Value, operation.Children.Single());

            switch (operation.Value.Kind)
            {
                case OperationKind.Invalid:
                case OperationKind.None:
                case OperationKind.AnonymousFunction:
                case OperationKind.FlowCaptureReference:
                case OperationKind.DefaultValue:
                case OperationKind.FlowAnonymousFunction: // must be an error case like in Microsoft.CodeAnalysis.CSharp.UnitTests.ConditionalOperatorTests.TestBothUntyped unit-test
                    break;

                case OperationKind.OmittedArgument:
                case OperationKind.DeclarationExpression:
                case OperationKind.Discard:
                    Assert.False(true, $"A {operation.Value.Kind} node should not be spilled or captured.");
                    break;

                default:
                    // Only values can be spilled/captured
                    if (!operation.Value.ConstantValue.HasValue || operation.Value.ConstantValue.Value != null)
                    {
                        Assert.NotNull(operation.Value.Type);
                    }
                    break;
            }
        }

        public override void VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation)
        {
            Assert.Equal(OperationKind.FlowCaptureReference, operation.Kind);
            Assert.True(operation.IsImplicit);
            Assert.Empty(operation.Children);
        }

        public override void VisitIsNull(IIsNullOperation operation)
        {
            Assert.Equal(OperationKind.IsNull, operation.Kind);
            Assert.True(operation.IsImplicit);
            Assert.Same(operation.Operand, operation.Children.Single());
        }

        public override void VisitCaughtException(ICaughtExceptionOperation operation)
        {
            Assert.Equal(OperationKind.CaughtException, operation.Kind);
            Assert.True(operation.IsImplicit);
            Assert.Empty(operation.Children);
        }

        public override void VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation)
        {
            Assert.Equal(OperationKind.StaticLocalInitializationSemaphore, operation.Kind);
            Assert.True(operation.IsImplicit);
            Assert.Empty(operation.Children);
            Assert.NotNull(operation.Local);
            Assert.True(operation.Local.IsStatic);
        }

        internal override void VisitFromEndIndexOperation(IFromEndIndexOperation operation)
        {
            Assert.Equal(OperationKind.None, operation.Kind);
            Assert.Same(operation.Operand, operation.Children.Single());
        }

        public override void VisitRangeOperation(IRangeOperation operation)
        {
            Assert.Equal(OperationKind.Range, operation.Kind);

            IOperation[] children = operation.Children.ToArray();

            int index = 0;

            if (operation.LeftOperand != null)
            {
                Assert.Same(operation.LeftOperand, children[index++]);
            }

            if (operation.RightOperand != null)
            {
                Assert.Same(operation.RightOperand, children[index++]);
            }

            Assert.Equal(index, children.Length);
        }

        public override void VisitReDim(IReDimOperation operation)
        {
            Assert.Equal(OperationKind.ReDim, operation.Kind);
            AssertEx.Equal(operation.Clauses, operation.Children);
            var preserve = operation.Preserve;
        }

        public override void VisitReDimClause(IReDimClauseOperation operation)
        {
            Assert.Equal(OperationKind.ReDimClause, operation.Kind);
            AssertEx.Equal(SpecializedCollections.SingletonEnumerable(operation.Operand).Concat(operation.DimensionSizes), operation.Children);
        }
    }
}
