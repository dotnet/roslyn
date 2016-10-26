// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Test.Extensions;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class OperationTreeVerifier : OperationWalker
    {
        private readonly IOperation _root;
        private readonly StringBuilder _builder;

        private const string indent = "  ";
        private string _currentIndent;
        private bool _pendingIndent;

        public OperationTreeVerifier(IOperation root, int initialIndent)
        {
            _root = root;
            _builder = new StringBuilder();

            _currentIndent = new string(' ', initialIndent);
            _pendingIndent = true;
        }

        public static void Verify(IOperation operation, string expectedOperationTree, int initialIndent = 0)
        {
            var actual = GetOperationTree(operation, initialIndent);
            Assert.Equal(expectedOperationTree, actual);
        }

        public static string GetOperationTree(IOperation operation, int initialIndent = 0)
        {
            var walker = new OperationTreeVerifier(operation, initialIndent);
            walker.Visit(operation);
            return walker._builder.ToString();
        }

        #region Logging helpers

        private void LogCommonPropertiesAndNewLine(IOperation operation)
        {
            LogString(" (");

            // Kind
            LogString($"{nameof(OperationKind)}.{operation.Kind}");

            // Type
            if (ShouldLogType(operation))
            {
                LogString(", ");
                LogType(operation.Type);
            }

            // ConstantValue
            if (operation.ConstantValue.HasValue)
            {
                LogString(", ");
                LogConstant(operation.ConstantValue);
            }

            // IsInvalid
            if (operation.IsInvalid)
            {
                LogString(", IsInvalid");
            }

            LogString(")");
            LogNewLine();
        }

        private static bool ShouldLogType(IOperation operation)
        {
            var operationKind = (int)operation.Kind;

            // Expressions
            if (operationKind >= 0x100 && operationKind < 0x400)
            {
                return true;
            }

            return false;
        }

        private void LogString(string str)
        {
            if (_pendingIndent)
            {
                str = _currentIndent + str;
                _pendingIndent = false;
            }

            _builder.Append(str);
        }

        private void LogNewLine()
        {
            LogString(Environment.NewLine);
            _pendingIndent = true;
        }

        private void Indent()
        {
            _currentIndent += indent;
        }

        private void Unindent()
        {
            _currentIndent = _currentIndent.Substring(indent.Length);
        }

        private void LogConstant(Optional<object> constant, string header = "Constant")
        {
            if (constant.HasValue)
            {
                LogConstant(constant.Value, header);
            }
        }

        private void LogConstant(object constant, string header = "Constant")
        {
            var valueStr = constant != null ? constant.ToString() : "null";
            LogString($"{header}: {valueStr}");
        }

        private void LogSymbol(ISymbol symbol, string header, bool logDisplayString = true)
        {
            if (!string.IsNullOrEmpty(header))
            {
                LogString($"{header}: ");
            }

            var symbolStr = symbol != null ? (logDisplayString ? symbol.ToTestDisplayString() : symbol.Name) : "null";
            LogString($"{symbolStr}");
        }

        private void LogType(ITypeSymbol type)
        {
            var typeStr = type != null ? type.ToTestDisplayString() : "null";
            LogString($"Type: {typeStr}");
        }

        #endregion

        #region Visit methods

        public override void Visit(IOperation operation)
        {
            if (operation == null)
            {
                return;
            }

            if (operation != _root)
            {
                Indent();
            }

            base.Visit(operation);

            if (operation != _root)
            {
                Unindent();
            }
        }

        private void Visit(IOperation operation, string header)
        {
            Debug.Assert(!string.IsNullOrEmpty(header));

            if (operation == null)
            {
                return;
            }

            Indent();
            LogString($"{header}: ");
            Visit(operation);
            Unindent();
        }

        private void VisitArray<T>(ImmutableArray<T> list, string header)
            where T : IOperation
        {
            Debug.Assert(!string.IsNullOrEmpty(header));

            if (list.IsDefaultOrEmpty)
            {
                return;
            }

            Indent();
            LogString($"{header}: ");
            VisitArray(list);
            Unindent();
        }

        private void VisitInstanceExpression(IOperation instance)
        {
            Visit(instance, header: "Instance Receiver");
        }

        internal override void VisitNoneOperation(IOperation operation)
        {
            Assert.True(false, "Encountered an IOperation with `Kind == OperationKind.None` while walking the operation tree.");
        }

        public override void VisitBlockStatement(IBlockStatement operation)
        {
            LogString(nameof(IBlockStatement));

            var statementsStr = $"{operation.Statements.Length} statements";
            var localStr = !operation.Locals.IsEmpty ? $", {operation.Locals.Length} locals" : string.Empty;
            LogString($" ({statementsStr}{localStr})");
            LogCommonPropertiesAndNewLine(operation);

            if (operation.Statements.IsEmpty)
            {
                return;
            }

            LogLocals(operation.Locals);

            base.VisitBlockStatement(operation);
        }

        public override void VisitVariableDeclarationStatement(IVariableDeclarationStatement operation)
        {
            var variablesCountStr = $"{operation.Variables.Length} variables";
            LogString($"{nameof(IVariableDeclarationStatement)} ({variablesCountStr})");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitVariableDeclarationStatement(operation);
        }

        public override void VisitVariableDeclaration(IVariableDeclaration operation)
        {
            LogSymbol(operation.Variable, header: nameof(IVariableDeclaration));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.InitialValue, "Initializer");
        }

        public override void VisitSwitchStatement(ISwitchStatement operation)
        {
            var caseCountStr = $"{operation.Cases.Length} cases";
            LogString($"{nameof(ISwitchStatement)} ({caseCountStr})");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, header: "Switch expression");
            VisitArray(operation.Cases);
        }

        public override void VisitSwitchCase(ISwitchCase operation)
        {
            var caseClauseCountStr = $"{operation.Clauses.Length} case clauses";
            var statementCountStr = $"{operation.Body.Length} statements";
            LogString($"{nameof(ISwitchCase)} ({caseClauseCountStr}, {statementCountStr})");
            LogCommonPropertiesAndNewLine(operation);

            Indent();
            LogString("Case clauses: ");
            VisitArray(operation.Clauses);
            LogString("Body: ");
            VisitArray(operation.Body);
            Unindent();
        }

        public override void VisitWhileUntilLoopStatement(IWhileUntilLoopStatement operation)
        {
            LogString(nameof(IWhileUntilLoopStatement));

            LogString($" (IsTopTest: {operation.IsTopTest}, IsWhile: {operation.IsWhile})");
            LogLoopStatementHeader(operation);

            Visit(operation.Condition, "Condition");
            VisitLoopStatementBody(operation);
        }

        public override void VisitForLoopStatement(IForLoopStatement operation)
        {
            LogString(nameof(IForLoopStatement));
            LogLoopStatementHeader(operation);

            Visit(operation.Condition, "Condition");
            LogLocals(operation.Locals);
            VisitArray(operation.Before, "Before");
            VisitArray(operation.AtLoopBottom, "AtLoopBottom");
            VisitLoopStatementBody(operation);
        }

        private void LogLocals(IEnumerable<ILocalSymbol> locals)
        {
            Indent();

            int localIndex = 1;
            foreach (var local in locals)
            {
                LogSymbol(local, header: $"Local_{localIndex++}");
                LogNewLine();
            }

            Unindent();
        }

        private void LogLoopStatementHeader(ILoopStatement operation)
        {
            var kindStr = $"{nameof(LoopKind)}.{operation.LoopKind}";
            LogString($" ({kindStr})");
            LogCommonPropertiesAndNewLine(operation);
        }

        private void VisitLoopStatementBody(ILoopStatement operation, string header = null)
        {
            if (header != null)
            {
                Visit(operation.Body, header);
            }
            else
            {
                Visit(operation.Body);
            }
        }

        public override void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            LogString(nameof(IForEachLoopStatement));
            LogSymbol(operation.IterationVariable, " (Iteration variable");
            LogString(")");

            LogLoopStatementHeader(operation);
            Visit(operation.Collection, "Collection");
            VisitLoopStatementBody(operation);
        }

        public override void VisitLabelStatement(ILabelStatement operation)
        {
            LogString(nameof(ILabelStatement));

            // TODO: Put a better workaround to skip compiler generated labels.
            if (!operation.Label.IsImplicitlyDeclared)
            {
                LogString($" (Label: {operation.Label.Name})");
            }

            LogCommonPropertiesAndNewLine(operation);

            base.VisitLabelStatement(operation);
        }

        public override void VisitBranchStatement(IBranchStatement operation)
        {
            LogString(nameof(IBranchStatement));
            var kindStr = $"{nameof(BranchKind)}.{operation.BranchKind}";
            var labelStr = !operation.Target.IsImplicitlyDeclared ? $", Label: {operation.Target.Name}" : string.Empty;
            LogString($" ({kindStr}{labelStr})");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitBranchStatement(operation);
        }

        public override void VisitYieldBreakStatement(IReturnStatement operation)
        {
            LogString("YieldBreakStatement");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitYieldBreakStatement(operation);
        }

        public override void VisitEmptyStatement(IEmptyStatement operation)
        {
            LogString(nameof(IEmptyStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitEmptyStatement(operation);
        }

        public override void VisitThrowStatement(IThrowStatement operation)
        {
            LogString(nameof(IThrowStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitThrowStatement(operation);
        }

        public override void VisitReturnStatement(IReturnStatement operation)
        {
            LogString(nameof(IReturnStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitReturnStatement(operation);
        }

        public override void VisitLockStatement(ILockStatement operation)
        {
            LogString(nameof(ILockStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitLockStatement(operation);
        }

        public override void VisitTryStatement(ITryStatement operation)
        {
            LogString(nameof(ITryStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitTryStatement(operation);
        }

        public override void VisitCatch(ICatchClause operation)
        {
            LogString(nameof(ICatchClause));
            LogString($" (Exception type: {operation.Type?.ToTestDisplayString()}, Exception local: {operation.ExceptionLocal?.ToTestDisplayString()})");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitCatch(operation);
        }

        public override void VisitUsingStatement(IUsingStatement operation)
        {
            LogString(nameof(IUsingStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitUsingStatement(operation);
        }

        public override void VisitFixedStatement(IFixedStatement operation)
        {
            LogString(nameof(IFixedStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitFixedStatement(operation);
        }

        public override void VisitExpressionStatement(IExpressionStatement operation)
        {
            LogString(nameof(IExpressionStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitExpressionStatement(operation);
        }

        public override void VisitWithStatement(IWithStatement operation)
        {
            LogString(nameof(IWithStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitWithStatement(operation);
        }

        public override void VisitStopStatement(IStopStatement operation)
        {
            LogString(nameof(IStopStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitStopStatement(operation);
        }

        public override void VisitEndStatement(IEndStatement operation)
        {
            LogString(nameof(IEndStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitEndStatement(operation);
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            LogString(nameof(IInvocationExpression));

            var isVirtualStr = operation.IsVirtual ? "virtual " : string.Empty;
            var isStaticStr = operation.Instance == null ? "static " : string.Empty;
            var spacing = !operation.IsVirtual && operation.Instance != null ? " " : string.Empty;
            LogString($" ({isVirtualStr}{isStaticStr}{spacing}");
            LogSymbol(operation.TargetMethod, header: string.Empty);
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            VisitInstanceExpression(operation.Instance);
            VisitArguments(operation);
        }

        private void VisitArguments(IHasArgumentsExpression operation, string header = null)
        {
            if (header != null)
            {
                VisitArray(operation.ArgumentsInParameterOrder, header);
            }
            else
            {
                VisitArray(operation.ArgumentsInParameterOrder);
            }
        }

        public override void VisitArgument(IArgument operation)
        {
            LogString($"{nameof(IArgument)} (");
            LogSymbol(operation.Parameter, header: "Matching Parameter", logDisplayString: false);
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value);
            Visit(operation.InConversion, "InConversion");
            Visit(operation.OutConversion, "OutConversion");
        }

        public override void VisitOmittedArgumentExpression(IOmittedArgumentExpression operation)
        {
            LogString(nameof(IOmittedArgumentExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitOmittedArgumentExpression(operation);
        }

        public override void VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation)
        {
            LogString(nameof(IArrayElementReferenceExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.ArrayReference);
            VisitArray(operation.Indices, "Indices");
        }

        public override void VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation)
        {
            LogString(nameof(IPointerIndirectionReferenceExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitPointerIndirectionReferenceExpression(operation);
        }

        public override void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        {
            LogString(nameof(ILocalReferenceExpression));
            LogString($": {operation.Local.Name}");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitLocalReferenceExpression(operation);
        }

        public override void VisitParameterReferenceExpression(IParameterReferenceExpression operation)
        {
            LogString(nameof(IParameterReferenceExpression));
            LogString($": {operation.Parameter.Name}");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitParameterReferenceExpression(operation);
        }

        public override void VisitSyntheticLocalReferenceExpression(ISyntheticLocalReferenceExpression operation)
        {
            LogString(nameof(ISyntheticLocalReferenceExpression));
            var kindStr = $"{nameof(SynthesizedLocalKind)}.{operation.SyntheticLocalKind}";
            LogString($" ({kindStr})");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitSyntheticLocalReferenceExpression(operation);
        }

        public override void VisitInstanceReferenceExpression(IInstanceReferenceExpression operation)
        {
            LogString(nameof(IInstanceReferenceExpression));
            var kindStr = $"{nameof(InstanceReferenceKind)}.{operation.InstanceReferenceKind}";
            LogString($" ({kindStr})");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitInstanceReferenceExpression(operation);
        }

        private void VisitMemberReferenceExpressionCommon(IMemberReferenceExpression operation)
        {
            if (operation.Instance == null)
            {
                LogString(" (Static)");
            }

            LogCommonPropertiesAndNewLine(operation);
            VisitInstanceExpression(operation.Instance);
        }

        public override void VisitFieldReferenceExpression(IFieldReferenceExpression operation)
        {
            LogString(nameof(IFieldReferenceExpression));
            LogString($": {operation.Field.ToTestDisplayString()}");

            VisitMemberReferenceExpressionCommon(operation);
        }

        public override void VisitMethodBindingExpression(IMethodBindingExpression operation)
        {
            LogString(nameof(IMethodBindingExpression));
            LogString($": {operation.Method.ToTestDisplayString()}");

            if (operation.IsVirtual)
            {
                LogString(" (UsesVirtualSemantics)");
            }

            VisitMemberReferenceExpressionCommon(operation);
        }

        public override void VisitPropertyReferenceExpression(IPropertyReferenceExpression operation)
        {
            LogString(nameof(IPropertyReferenceExpression));
            LogString($": {operation.Property.ToTestDisplayString()}");

            VisitMemberReferenceExpressionCommon(operation);
        }

        public override void VisitEventReferenceExpression(IEventReferenceExpression operation)
        {
            LogString(nameof(IEventReferenceExpression));
            LogString($": {operation.Event.ToTestDisplayString()}");

            VisitMemberReferenceExpressionCommon(operation);
        }

        public override void VisitEventAssignmentExpression(IEventAssignmentExpression operation)
        {
            var kindStr = operation.Adds ? "EventAdd" : "EventRemove";
            LogString($"{nameof(IEventAssignmentExpression)} ({kindStr})");
            LogSymbol(operation.Event, header: " (Event: ");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.EventInstance, header: "Event Instance");
            Visit(operation.HandlerValue, header: "Handler");
        }

        public override void VisitConditionalAccessExpression(IConditionalAccessExpression operation)
        {
            LogString(nameof(IConditionalAccessExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.ConditionalInstance, header: "Left");
            Visit(operation.ConditionalValue, header: "Right");
        }

        public override void VisitConditionalAccessInstanceExpression(IConditionalAccessInstanceExpression operation)
        {
            LogString(nameof(IConditionalAccessInstanceExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitConditionalAccessInstanceExpression(operation);
        }

        public override void VisitPlaceholderExpression(IPlaceholderExpression operation)
        {
            LogString(nameof(IPlaceholderExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitPlaceholderExpression(operation);
        }

        public override void VisitIndexedPropertyReferenceExpression(IIndexedPropertyReferenceExpression operation)
        {
            LogString(nameof(IIndexedPropertyReferenceExpression));

            LogString($": {operation.Property.ToTestDisplayString()}");
            LogCommonPropertiesAndNewLine(operation);

            VisitMemberReferenceExpressionCommon(operation);
        }

        public override void VisitUnaryOperatorExpression(IUnaryOperatorExpression operation)
        {
            LogString(nameof(IUnaryOperatorExpression));

            var kindStr = $"{nameof(UnaryOperationKind)}.{operation.UnaryOperationKind}";
            LogString($" ({kindStr})");
            LogHasOperatorMethodExpressionCommon(operation);
            LogCommonPropertiesAndNewLine(operation);

            base.VisitUnaryOperatorExpression(operation);
        }

        public override void VisitBinaryOperatorExpression(IBinaryOperatorExpression operation)
        {
            LogString(nameof(IBinaryOperatorExpression));

            var kindStr = $"{nameof(BinaryOperationKind)}.{operation.BinaryOperationKind}";
            LogString($" ({kindStr})");
            LogHasOperatorMethodExpressionCommon(operation);
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.LeftOperand, "Left");
            Visit(operation.RightOperand, "Right");
        }

        private void LogHasOperatorMethodExpressionCommon(IHasOperatorMethodExpression operation)
        {
            Assert.Equal(operation.UsesOperatorMethod, operation.OperatorMethod != null);

            if (!operation.UsesOperatorMethod)
            {
                return;
            }

            LogSymbol(operation.OperatorMethod, header: " (OperatorMethod");
            LogString(")");
        }

        public override void VisitConversionExpression(IConversionExpression operation)
        {
            LogString(nameof(IConversionExpression));

            var kindStr = $"{nameof(ConversionKind)}.{operation.ConversionKind}";
            var isExplicitStr = operation.IsExplicit ? "Explicit" : "Implicit";
            LogString($" ({kindStr}, {isExplicitStr})");

            LogHasOperatorMethodExpressionCommon(operation);
            LogCommonPropertiesAndNewLine(operation);

            base.VisitConversionExpression(operation);
        }

        public override void VisitConditionalChoiceExpression(IConditionalChoiceExpression operation)
        {
            LogString(nameof(IConditionalChoiceExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Condition, "Condition");
            Visit(operation.IfTrueValue, "IfTrue");
            Visit(operation.IfFalseValue, "IfFalse");
        }

        public override void VisitNullCoalescingExpression(INullCoalescingExpression operation)
        {
            LogString(nameof(INullCoalescingExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.PrimaryOperand, "Left");
            Visit(operation.SecondaryOperand, "Right");
        }

        public override void VisitIsTypeExpression(IIsTypeExpression operation)
        {
            LogString(nameof(IIsTypeExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitIsTypeExpression(operation);

            Indent();
            LogType(operation.Type);
            Unindent();
        }

        private void LogTypeOperationExpressionCommon(ITypeOperationExpression operation)
        {
            LogString(" (");
            LogType(operation.TypeOperand);
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitSizeOfExpression(ISizeOfExpression operation)
        {
            LogString(nameof(ISizeOfExpression));
            LogTypeOperationExpressionCommon(operation);

            base.VisitSizeOfExpression(operation);
        }

        public override void VisitTypeOfExpression(ITypeOfExpression operation)
        {
            LogString(nameof(ITypeOfExpression));
            LogTypeOperationExpressionCommon(operation);

            base.VisitTypeOfExpression(operation);
        }

        public override void VisitLambdaExpression(ILambdaExpression operation)
        {
            LogString(nameof(ILambdaExpression));

            LogSymbol(operation.Signature, header: " (Signature");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitLambdaExpression(operation);
        }

        public override void VisitLiteralExpression(ILiteralExpression operation)
        {
            LogString(nameof(ILiteralExpression));

            if (operation.ConstantValue.HasValue && operation.ConstantValue.Value.ToString() == operation.Text)
            {
                LogString($" (Text: {operation.Text})");
            }

            LogCommonPropertiesAndNewLine(operation);

            base.VisitLiteralExpression(operation);
        }

        public override void VisitAwaitExpression(IAwaitExpression operation)
        {
            LogString(nameof(IAwaitExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitAwaitExpression(operation);
        }

        public override void VisitAddressOfExpression(IAddressOfExpression operation)
        {
            LogString(nameof(IAddressOfExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitAddressOfExpression(operation);
        }

        public override void VisitObjectCreationExpression(IObjectCreationExpression operation)
        {
            LogString(nameof(IObjectCreationExpression));
            LogString($" (Constructor: {operation.Constructor.ToTestDisplayString()})");
            LogCommonPropertiesAndNewLine(operation);

            VisitArguments(operation, "Arguments");
            VisitArray(operation.MemberInitializers, "Member Initializers");
        }

        public override void VisitFieldInitializer(IFieldInitializer operation)
        {
            LogString(nameof(IFieldInitializer));

            if (operation.InitializedFields.Length <= 1)
            {
                if (operation.InitializedFields.Length == 1)
                {
                    LogSymbol(operation.InitializedFields[0], header: " (Field");
                    LogString(")");
                }

                LogCommonPropertiesAndNewLine(operation);
            }
            else
            {
                LogString($" ({operation.InitializedFields.Length} initialized fields)");
                LogCommonPropertiesAndNewLine(operation);

                Indent();

                int index = 1;
                foreach (var local in operation.InitializedFields)
                {
                    LogSymbol(local, header: $"Field_{index++}");
                    LogNewLine();
                }

                Unindent();
            }

            base.VisitFieldInitializer(operation);
        }

        public override void VisitPropertyInitializer(IPropertyInitializer operation)
        {
            LogString(nameof(IPropertyInitializer));
            LogSymbol(operation.InitializedProperty, header: " (Property");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitPropertyInitializer(operation);
        }

        public override void VisitParameterInitializer(IParameterInitializer operation)
        {
            LogString(nameof(IParameterInitializer));
            LogSymbol(operation.Parameter, header: " (Parameter");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitParameterInitializer(operation);
        }

        public override void VisitArrayCreationExpression(IArrayCreationExpression operation)
        {
            LogString(nameof(IArrayCreationExpression));
            LogString($" (Dimension sizes: {operation.DimensionSizes.Length}, Element Type: {operation.ElementType?.ToTestDisplayString()})");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitArrayCreationExpression(operation);
        }

        public override void VisitArrayInitializer(IArrayInitializer operation)
        {
            LogString(nameof(IArrayInitializer));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitArrayInitializer(operation);
        }

        public override void VisitAssignmentExpression(IAssignmentExpression operation)
        {
            LogString(nameof(IAssignmentExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, "Left");
            Visit(operation.Value, "Right");
        }

        public override void VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation)
        {
            LogString(nameof(ICompoundAssignmentExpression));

            var kindStr = $"{nameof(BinaryOperationKind)}.{operation.BinaryOperationKind}";
            LogString($" ({kindStr})");
            LogHasOperatorMethodExpressionCommon(operation);
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, "Left");
            Visit(operation.Value, "Right");
        }

        public override void VisitIncrementExpression(IIncrementExpression operation)
        {
            LogString(nameof(IIncrementExpression));

            var unaryKindStr = $"{nameof(UnaryOperandKind)}.{operation.IncrementOperationKind}";
            var binaryKindStr = $"{nameof(BinaryOperationKind)}.{operation.BinaryOperationKind}";
            LogString($" ({unaryKindStr}) ({binaryKindStr})");
            LogHasOperatorMethodExpressionCommon(operation);
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, "Left");
            Visit(operation.Value, "Right");
        }

        public override void VisitParenthesizedExpression(IParenthesizedExpression operation)
        {
            LogString(nameof(IParenthesizedExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitParenthesizedExpression(operation);
        }

        public override void VisitLateBoundMemberReferenceExpression(ILateBoundMemberReferenceExpression operation)
        {
            LogString(nameof(ILateBoundMemberReferenceExpression));
            LogString($" (Member name: {operation.MemberName})");
            LogCommonPropertiesAndNewLine(operation);

            VisitInstanceExpression(operation.Instance);
        }

        public override void VisitUnboundLambdaExpression(IUnboundLambdaExpression operation)
        {
            LogString(nameof(IUnboundLambdaExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitUnboundLambdaExpression(operation);
        }

        public override void VisitDefaultValueExpression(IDefaultValueExpression operation)
        {
            LogString(nameof(IDefaultValueExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitDefaultValueExpression(operation);
        }

        public override void VisitTypeParameterObjectCreationExpression(ITypeParameterObjectCreationExpression operation)
        {
            LogString(nameof(ITypeParameterObjectCreationExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitTypeParameterObjectCreationExpression(operation);
        }

        public override void VisitInvalidStatement(IInvalidStatement operation)
        {
            LogString(nameof(IInvalidStatement));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitInvalidStatement(operation);
        }

        public override void VisitInvalidExpression(IInvalidExpression operation)
        {
            LogString(nameof(IInvalidExpression));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitInvalidExpression(operation);
        }

        public override void VisitIfStatement(IIfStatement operation)
        {
            LogString(nameof(IIfStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Condition, "Condition");
            Visit(operation.IfTrueStatement);
            Visit(operation.IfFalseStatement);
        }

        public override void VisitLocalFunctionStatement(IOperation operation)
        {
            LogString(nameof(VisitLocalFunctionStatement));
            LogCommonPropertiesAndNewLine(operation);
        }

        private void LogCaseClauseCommon(ICaseClause operation)
        {
            var kindStr = $"{nameof(CaseKind)}.{operation.CaseKind}";
            LogString($" ({kindStr})");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitSingleValueCaseClause(ISingleValueCaseClause operation)
        {
            LogString(nameof(ISingleValueCaseClause));
            var kindStr = $"{nameof(BinaryOperationKind)}.{operation.Equality}";
            LogString($" (Equality operator kind: {kindStr})");
            LogCaseClauseCommon(operation);

            base.VisitSingleValueCaseClause(operation);
        }

        public override void VisitRelationalCaseClause(IRelationalCaseClause operation)
        {
            LogString(nameof(IRelationalCaseClause));
            var kindStr = $"{nameof(BinaryOperationKind)}.{operation.Relation}";
            LogString($" (Relational operator kind: {kindStr})");
            LogCaseClauseCommon(operation);

            base.VisitRelationalCaseClause(operation);
        }

        public override void VisitRangeCaseClause(IRangeCaseClause operation)
        {
            LogString(nameof(IRangeCaseClause));
            LogCaseClauseCommon(operation);

            Visit(operation.MinimumValue, "Min");
            Visit(operation.MaximumValue, "Max");
        }

        #endregion
    }
}