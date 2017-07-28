﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Test.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class OperationTreeVerifier : OperationWalker
    {
        private readonly Compilation _compilation;
        private readonly IOperation _root;
        private readonly StringBuilder _builder;

        private const string indent = "  ";
        private string _currentIndent;
        private bool _pendingIndent;

        public OperationTreeVerifier(Compilation compilation, IOperation root, int initialIndent)
        {
            _compilation = compilation;
            _root = root;
            _builder = new StringBuilder();

            _currentIndent = new string(' ', initialIndent);
            _pendingIndent = true;
        }

        public static void Verify(Compilation compilation, IOperation operation, string expectedOperationTree, int initialIndent = 0)
        {
            var actual = GetOperationTree(compilation, operation, initialIndent);
            Assert.Equal(expectedOperationTree, actual);
        }

        public static string GetOperationTree(Compilation compilation, IOperation operation, int initialIndent = 0)
        {
            var walker = new OperationTreeVerifier(compilation, operation, initialIndent);
            walker.Visit(operation);
            return walker._builder.ToString();
        }

        public static void Verify(string expectedOperationTree, string actualOperationTree)
        {
            char[] newLineChars = Environment.NewLine.ToCharArray();
            string actual = actualOperationTree.Trim(newLineChars);
            expectedOperationTree = expectedOperationTree.Trim(newLineChars);
            expectedOperationTree = Regex.Replace(expectedOperationTree, "([^\r])\n", "$1" + Environment.NewLine);

            AssertEx.AreEqual(expectedOperationTree, actual);
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
            if (operation.HasErrors(_compilation))
            {
                LogString(", IsInvalid");
            }

            LogString(")");

            // Syntax
            LogString($" (Syntax: {GetSnippetFromSyntax(operation.Syntax)})");

            LogNewLine();
        }

        private static string GetSnippetFromSyntax(SyntaxNode syntax)
        {
            if (syntax == null)
            {
                return "null";
            }

            var text = syntax.ToString();
            var lines = text.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToArray();
            if (lines.Length <= 1 && text.Length < 25)
            {
                return $"'{text}'";
            }

            const int maxTokenLength = 11;
            var firstLine = lines[0];
            var lastLine = lines[lines.Length - 1];
            var prefix = firstLine.Length <= maxTokenLength ? firstLine : firstLine.Substring(0, maxTokenLength);
            var suffix = lastLine.Length <= maxTokenLength ? lastLine : lastLine.Substring(lastLine.Length - maxTokenLength, maxTokenLength);
            return $"'{prefix} ... {suffix}'";
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
            if (constant is string)
            {
                valueStr = @"""" + valueStr + @"""";
            }

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

        private void LogType(ITypeSymbol type, string header = "Type")
        {
            var typeStr = type != null ? type.ToTestDisplayString() : "null";
            LogString($"{header}: {typeStr}");
        }

        #endregion

        #region Visit methods

        public override void Visit(IOperation operation)
        {
            if (operation == null)
            {
                LogString("null");
                LogNewLine();
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

            Indent();
            LogString($"{header}: ");
            Visit(operation);
            Unindent();
        }

        private void VisitArrayCommon<T>(IEnumerable<T> list, string header, bool logElementCount, Action<T> arrayElementVisitor)
        {
            Debug.Assert(!string.IsNullOrEmpty(header));

            Indent();
            if (!list.IsEmpty())
            {
                var elementCount = logElementCount ? $"({list.Count()})" : string.Empty;
                LogString($"{header}{elementCount}:");
                LogNewLine();
                Indent();
                foreach (var element in list)
                {
                    arrayElementVisitor(element);
                }
                Unindent();
            }
            else
            {
                LogString($"{header}(0)");
                LogNewLine();
            }

            Unindent();
        }

        internal void VisitSymbolArrayElement(ISymbol element)
        {
            LogSymbol(element, header: "Symbol");
            LogNewLine();
        }

        internal void VisitStringArrayElement(string element)
        {
            var valueStr = element != null ? element.ToString() : "null";
            valueStr = @"""" + valueStr + @"""";
            LogString(valueStr);
            LogNewLine();
        }

        internal void VisitRefKindArrayElement(RefKind element)
        {
            LogString(element.ToString());
            LogNewLine();
        }

        private void VisitArray<T>(IEnumerable<T> list, string header, bool logElementCount)
            where T: IOperation
        {
            VisitArrayCommon(list, header, logElementCount, VisitOperationArrayElement);
        }

        private void VisitArray(IEnumerable<ISymbol> list, string header, bool logElementCount)
        {
            VisitArrayCommon(list, header, logElementCount, VisitSymbolArrayElement);
        }

        private void VisitArray(IEnumerable<string> list, string header, bool logElementCount)
        {
            VisitArrayCommon(list, header, logElementCount, VisitStringArrayElement);
        }

        private void VisitArray(IEnumerable<RefKind> list, string header, bool logElementCount)
        {
            VisitArrayCommon(list, header, logElementCount, VisitRefKindArrayElement);
        }

        private void VisitInstanceExpression(IOperation instance)
        {
            Visit(instance, header: "Instance Receiver");
        }

        internal override void VisitNoneOperation(IOperation operation)
        {
            LogString("IOperation: ");
            LogCommonPropertiesAndNewLine(operation);

            var children = operation.Children.WhereNotNull();
            if (children.Any())
            {
                VisitArray(children, "Children", logElementCount: true);
            }
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
            var variablesCountStr = $"{operation.Declarations.Length} declarations";
            LogString($"{nameof(IVariableDeclarationStatement)} ({variablesCountStr})");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitVariableDeclarationStatement(operation);
        }

        public override void VisitVariableDeclaration(IVariableDeclaration operation)
        {
            var symbolsCountStr = $"{operation.Variables.Length} variables";
            LogString($"{nameof(IVariableDeclaration)} ({symbolsCountStr})");
            LogCommonPropertiesAndNewLine(operation);

            LogLocals(operation.Variables, header: "Variables");

            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitSwitchStatement(ISwitchStatement operation)
        {
            var caseCountStr = $"{operation.Cases.Length} cases";
            LogString($"{nameof(ISwitchStatement)} ({caseCountStr})");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, header: "Switch expression");
            VisitArray(operation.Cases, "Sections", logElementCount: false);
        }

        public override void VisitSwitchCase(ISwitchCase operation)
        {
            var caseClauseCountStr = $"{operation.Clauses.Length} case clauses";
            var statementCountStr = $"{operation.Body.Length} statements";
            LogString($"{nameof(ISwitchCase)} ({caseClauseCountStr}, {statementCountStr})");
            LogCommonPropertiesAndNewLine(operation);

            Indent();
            VisitArray(operation.Clauses, "Clauses", logElementCount: false);
            VisitArray(operation.Body, "Body", logElementCount: false);
            Unindent();
        }

        public override void VisitWhileUntilLoopStatement(IWhileUntilLoopStatement operation)
        {
            LogString(nameof(IWhileUntilLoopStatement));

            LogString($" (IsTopTest: {operation.IsTopTest}, IsWhile: {operation.IsWhile})");
            LogLoopStatementHeader(operation);

            Visit(operation.Condition, "Condition");
            Visit(operation.Body, "Body");
        }

        public override void VisitForLoopStatement(IForLoopStatement operation)
        {
            LogString(nameof(IForLoopStatement));
            LogLoopStatementHeader(operation);

            Visit(operation.Condition, "Condition");
            LogLocals(operation.Locals);
            VisitArray(operation.Before, "Before", logElementCount: false);
            VisitArray(operation.AtLoopBottom, "AtLoopBottom", logElementCount: false);
            Visit(operation.Body, "Body");
        }

        private void LogLocals(IEnumerable<ILocalSymbol> locals, string header = "Locals")
        {
            if (!locals.Any())
            {
                return;
            }

            Indent();

            LogString($"{header}: ");
            Indent();

            int localIndex = 1;
            foreach (var local in locals)
            {
                LogSymbol(local, header: $"Local_{localIndex++}");
                LogNewLine();
            }

            Unindent();
            Unindent();
        }

        private void LogLoopStatementHeader(ILoopStatement operation)
        {
            var kindStr = $"{nameof(LoopKind)}.{operation.LoopKind}";
            LogString($" ({kindStr})");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            LogString(nameof(IForEachLoopStatement));
            LogSymbol(operation.IterationVariable, " (Iteration variable");
            LogString(")");

            LogLoopStatementHeader(operation);
            Visit(operation.Collection, "Collection");
            Visit(operation.Body, "Body");
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

            Visit(operation.LabeledStatement, "LabeledStatement");
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
            LogString(nameof(IReturnStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.ReturnedValue, "ReturnedValue");
        }

        public override void VisitEmptyStatement(IEmptyStatement operation)
        {
            LogString(nameof(IEmptyStatement));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitThrowStatement(IThrowStatement operation)
        {
            LogString(nameof(IThrowStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.ThrownObject, "ThrownObject");
        }

        public override void VisitReturnStatement(IReturnStatement operation)
        {
            LogString(nameof(IReturnStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.ReturnedValue, "ReturnedValue");
        }

        public override void VisitLockStatement(ILockStatement operation)
        {
            LogString(nameof(ILockStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.LockedObject, "LockedObject");
            Visit(operation.Body, "Body");
        }

        public override void VisitTryStatement(ITryStatement operation)
        {
            LogString(nameof(ITryStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Body, "Body");
            VisitArray(operation.Catches, "Catch clauses", logElementCount: true);
            Visit(operation.FinallyHandler, "Finally");
        }

        public override void VisitCatchClause(ICatchClause operation)
        {
            LogString(nameof(ICatchClause));
            LogString($" (Exception type: {operation.CaughtType?.ToTestDisplayString()}, Exception local: {operation.ExceptionLocal?.ToTestDisplayString()})");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Filter, "Filter");
            Visit(operation.Handler, "Handler");
        }

        public override void VisitUsingStatement(IUsingStatement operation)
        {
            LogString(nameof(IUsingStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Declaration, "Declaration");
            Visit(operation.Value, "Value");
            Visit(operation.Body, "Body");
        }

        public override void VisitFixedStatement(IFixedStatement operation)
        {
            LogString(nameof(IFixedStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Variables, "Declaration");
            Visit(operation.Body, "Body");
        }

        public override void VisitExpressionStatement(IExpressionStatement operation)
        {
            LogString(nameof(IExpressionStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Expression, "Expression");
        }

        public override void VisitWithStatement(IWithStatement operation)
        {
            LogString(nameof(IWithStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, "Value");
            Visit(operation.Body, "Body");
        }

        public override void VisitStopStatement(IStopStatement operation)
        {
            LogString(nameof(IStopStatement));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitEndStatement(IEndStatement operation)
        {
            LogString(nameof(IEndStatement));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            LogString(nameof(IInvocationExpression));

            var isVirtualStr = operation.IsVirtual ? "virtual " : string.Empty;
            var spacing = !operation.IsVirtual && operation.Instance != null ? " " : string.Empty;
            LogString($" ({isVirtualStr}{spacing}");
            LogSymbol(operation.TargetMethod, header: string.Empty);
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            VisitInstanceExpression(operation.Instance);
            VisitArguments(operation);
        }

        private void VisitArguments(IHasArgumentsExpression operation)
        {
            VisitArray(operation.ArgumentsInEvaluationOrder, "Arguments", logElementCount: true);
        }

        private void VisitDynamicArguments(IHasDynamicArgumentsExpression operation)
        {
            VisitArray(operation.ApplicableSymbols, "ApplicableSymbols", logElementCount: true);
            VisitArray(operation.Arguments, "Arguments", logElementCount: true);
            VisitArray(operation.ArgumentNames, "ArgumentNames", logElementCount: true);
            VisitArray(operation.ArgumentRefKinds, "ArgumentRefKinds", logElementCount: true);
        }

        public override void VisitArgument(IArgument operation)
        {
            LogString($"{nameof(IArgument)} (");
            LogString($"{nameof(ArgumentKind)}.{operation.ArgumentKind}, ");
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
        }

        public override void VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation)
        {
            LogString(nameof(IArrayElementReferenceExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.ArrayReference, "Array reference");
            VisitArray(operation.Indices, "Indices", logElementCount: true);
        }

        public override void VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation)
        {
            LogString(nameof(IPointerIndirectionReferenceExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Pointer, "Pointer");
        }

        public override void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        {
            LogString(nameof(ILocalReferenceExpression));
            LogString($": {operation.Local.Name}");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitParameterReferenceExpression(IParameterReferenceExpression operation)
        {
            LogString(nameof(IParameterReferenceExpression));
            LogString($": {operation.Parameter.Name}");
            LogCommonPropertiesAndNewLine(operation);
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
                LogString(" (IsVirtual)");
            }

            VisitMemberReferenceExpressionCommon(operation);
        }

        public override void VisitPropertyReferenceExpression(IPropertyReferenceExpression operation)
        {
            LogString(nameof(IPropertyReferenceExpression));
            LogString($": {operation.Property.ToTestDisplayString()}");

            VisitMemberReferenceExpressionCommon(operation);

            if (operation.ArgumentsInEvaluationOrder.Length > 0)
            {
                VisitArguments(operation);
            }
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
        }

        public override void VisitPlaceholderExpression(IPlaceholderExpression operation)
        {
            LogString(nameof(IPlaceholderExpression));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitUnaryOperatorExpression(IUnaryOperatorExpression operation)
        {
            LogString(nameof(IUnaryOperatorExpression));

            var kindStr = $"{nameof(UnaryOperationKind)}.{operation.UnaryOperationKind}";
            if (operation.IsLifted)
            {
                LogString($" ({kindStr}-IsLifted)");
            }
            else
            {
                LogString($" ({kindStr})");
            }
            LogHasOperatorMethodExpressionCommon(operation);
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operand, "Operand");
        }

        public override void VisitBinaryOperatorExpression(IBinaryOperatorExpression operation)
        {
            LogString(nameof(IBinaryOperatorExpression));

            var kindStr = $"{nameof(BinaryOperationKind)}.{operation.BinaryOperationKind}";
            if (operation.IsLifted)
            {
                LogString($" ({kindStr}-IsLifted)");
            }
            else
            {
                LogString($" ({kindStr})");
            }
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

            Visit(operation.Operand, "Operand");
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

            Visit(operation.Operand, "Operand");

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
        }

        public override void VisitTypeOfExpression(ITypeOfExpression operation)
        {
            LogString(nameof(ITypeOfExpression));
            LogTypeOperationExpressionCommon(operation);
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

            object value;
            if (operation.ConstantValue.HasValue && ((value = operation.ConstantValue.Value) == null ? "null" : value.ToString()) == operation.Text)
            {
                LogString($" (Text: {operation.Text})");
            }

            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitAwaitExpression(IAwaitExpression operation)
        {
            LogString(nameof(IAwaitExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.AwaitedValue, "AwaitedValue");
        }

        public override void VisitNameOfExpression(INameOfExpression operation)
        {
            LogString(nameof(INameOfExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Argument);
        }

        public override void VisitThrowExpression(IThrowExpression operation)
        {
            LogString(nameof(IThrowExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Expression);
        }

        public override void VisitAddressOfExpression(IAddressOfExpression operation)
        {
            LogString(nameof(IAddressOfExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Reference, "Reference");
        }

        public override void VisitObjectCreationExpression(IObjectCreationExpression operation)
        {
            LogString(nameof(IObjectCreationExpression));
            LogString($" (Constructor: {operation.Constructor.ToTestDisplayString()})");
            LogCommonPropertiesAndNewLine(operation);

            VisitArguments(operation);
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitAnonymousObjectCreationExpression(IAnonymousObjectCreationExpression operation)
        {
            LogString(nameof(IAnonymousObjectCreationExpression));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Initializers, "Initializers", logElementCount: true);
        }

        public override void VisitDynamicObjectCreationExpression(IDynamicObjectCreationExpression operation)
        {
            LogString(nameof(IDynamicObjectCreationExpression));

            var name = operation.Name;
            LogString($" (Name: {operation.Name})");
            LogCommonPropertiesAndNewLine(operation);

            VisitDynamicArguments(operation);
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitObjectOrCollectionInitializerExpression(IObjectOrCollectionInitializerExpression operation)
        {
            LogString(nameof(IObjectOrCollectionInitializerExpression));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Initializers, "Initializers", logElementCount: true);
        }

        public override void VisitMemberInitializerExpression(IMemberInitializerExpression operation)
        {
            LogString(nameof(IMemberInitializerExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.InitializedMember, "InitializedMember");
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitCollectionElementInitializerExpression(ICollectionElementInitializerExpression operation)
        {
            LogString(nameof(ICollectionElementInitializerExpression));
            if (operation.AddMethod != null)
            {
                LogString($" (AddMethod: {operation.AddMethod.ToTestDisplayString()})");
            }
            LogString($" (IsDynamic: {operation.IsDynamic})");
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Arguments, "Arguments", logElementCount: true);
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
            LogString($" (Element Type: {operation.ElementType?.ToTestDisplayString()})");
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.DimensionSizes, "Dimension Sizes", logElementCount: true);
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitArrayInitializer(IArrayInitializer operation)
        {
            LogString(nameof(IArrayInitializer));
            LogString($" ({operation.ElementValues.Length} elements)");
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.ElementValues, "Element Values", logElementCount: true);
        }

        public override void VisitSimpleAssignmentExpression(ISimpleAssignmentExpression operation)
        {
            LogString(nameof(ISimpleAssignmentExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, "Left");
            Visit(operation.Value, "Right");
        }

        public override void VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation)
        {
            LogString(nameof(ICompoundAssignmentExpression));

            var kindStr = $"{nameof(BinaryOperationKind)}.{operation.BinaryOperationKind}";
            if (operation.IsLifted)
            {
                LogString($" ({kindStr}-IsLifted)");
            }
            else
            {
                LogString($" ({kindStr})");
            }
            LogHasOperatorMethodExpressionCommon(operation);
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, "Left");
            Visit(operation.Value, "Right");
        }

        public override void VisitIncrementExpression(IIncrementExpression operation)
        {
            LogString(nameof(IIncrementExpression));

            var unaryKindStr = $"{nameof(UnaryOperandKind)}.{operation.IncrementOperationKind}";
            LogString($" ({unaryKindStr})");
            LogHasOperatorMethodExpressionCommon(operation);
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, "Left");
        }

        public override void VisitParenthesizedExpression(IParenthesizedExpression operation)
        {
            LogString(nameof(IParenthesizedExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operand, "Operand");
        }

        public override void VisitDynamicMemberReferenceExpression(IDynamicMemberReferenceExpression operation)
        {
            LogString(nameof(IDynamicMemberReferenceExpression));
            // (Member Name: "quoted name", Containing Type: type)
            LogString(" (");
            LogConstant((object)operation.MemberName, "Member Name");
            LogString(", ");
            LogType(operation.ContainingType, "Containing Type");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            VisitArrayCommon(operation.TypeArguments, "Type Arguments", true, VisitSymbolArrayElement);

            VisitInstanceExpression(operation.Instance);
        }

        public override void VisitDefaultValueExpression(IDefaultValueExpression operation)
        {
            LogString(nameof(IDefaultValueExpression));
            LogCommonPropertiesAndNewLine(operation);            
        }

        public override void VisitTypeParameterObjectCreationExpression(ITypeParameterObjectCreationExpression operation)
        {
            LogString(nameof(ITypeParameterObjectCreationExpression));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitInvalidStatement(IInvalidStatement operation)
        {
            LogString(nameof(IInvalidStatement));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Children, "Children", logElementCount: true);
        }

        public override void VisitInvalidExpression(IInvalidExpression operation)
        {
            LogString(nameof(IInvalidExpression));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Children, "Children", logElementCount: true);
        }

        public override void VisitIfStatement(IIfStatement operation)
        {
            LogString(nameof(IIfStatement));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Condition, "Condition");
            Visit(operation.IfTrueStatement, "IfTrue");
            Visit(operation.IfFalseStatement, "IfFalse");
        }

        public override void VisitLocalFunctionStatement(ILocalFunctionStatement operation)
        {
            LogString(nameof(ILocalFunctionStatement));

            LogSymbol(operation.LocalFunctionSymbol, header: " (Local Function");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Body);
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

            Visit(operation.Value, "Value");
        }

        public override void VisitRelationalCaseClause(IRelationalCaseClause operation)
        {
            LogString(nameof(IRelationalCaseClause));
            var kindStr = $"{nameof(BinaryOperationKind)}.{operation.Relation}";
            LogString($" (Relational operator kind: {kindStr})");
            LogCaseClauseCommon(operation);

            Visit(operation.Value, "Value");
        }

        public override void VisitRangeCaseClause(IRangeCaseClause operation)
        {
            LogString(nameof(IRangeCaseClause));
            LogCaseClauseCommon(operation);

            Visit(operation.MinimumValue, "Min");
            Visit(operation.MaximumValue, "Max");
        }

        public override void VisitDefaultCaseClause(IDefaultCaseClause operation)
        {
            LogString(nameof(IDefaultCaseClause));
            LogCaseClauseCommon(operation);
        }

        public override void VisitTupleExpression(ITupleExpression operation)
        {
            LogString(nameof(ITupleExpression));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Elements, "Elements", logElementCount: true);
        }

        public override void VisitInterpolatedStringExpression(IInterpolatedStringExpression operation)
        {
            LogString(nameof(IInterpolatedStringExpression));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Parts, "Parts", logElementCount: true);
        }

        public override void VisitInterpolatedStringText(IInterpolatedStringText operation)
        {
            LogString(nameof(IInterpolatedStringText));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Text, "Text");
        }

        public override void VisitInterpolation(IInterpolation operation)
        {
            LogString(nameof(IInterpolation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Expression, "Expression");
            Visit(operation.Alignment, "Alignment");
            Visit(operation.FormatString, "FormatString");
        }

        public override void VisitConstantPattern(IConstantPattern operation)
        {
            LogString(nameof(IConstantPattern));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, "Value");
        }

        public override void VisitDeclarationPattern(IDeclarationPattern operation)
        {
            LogString(nameof(IDeclarationPattern));
            LogSymbol(operation.DeclaredSymbol, " (Declared Symbol");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitIsPatternExpression(IIsPatternExpression operation)
        {
            LogString(nameof(IIsPatternExpression));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Expression, "Expression");
            Visit(operation.Pattern, "Pattern");
        }

        public override void VisitPatternCaseClause(IPatternCaseClause operation)
        {
            LogString(nameof(IPatternCaseClause));
            LogSymbol(operation.Label, " (Label Symbol");
            LogString(")");
            LogCaseClauseCommon(operation);

            Visit(operation.Pattern, "Pattern");
            Visit(operation.GuardExpression, "Guard Expression");
        }

        #endregion
    }
}
