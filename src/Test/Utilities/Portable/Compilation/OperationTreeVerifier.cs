// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Extensions;
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
        private readonly Dictionary<SyntaxNode, IOperation> _explictNodeMap;

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

            _explictNodeMap = new Dictionary<SyntaxNode, IOperation>();
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
            LogString(", ");
            LogType(operation.Type);

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

            // IsImplicit
            if (operation.IsImplicit)
            {
                LogString(", IsImplicit");
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

        private static string ConstantToString(object constant, bool quoteString = true)
        {
            switch (constant)
            {
                case null:
                    return "null";
                case string s:
                    if (quoteString)
                    {
                        return @"""" + s + @"""";
                    }
                    return s;
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return constant.ToString();
            }
        }

        private void LogConstant(object constant, string header = "Constant")
        {
            string valueStr = ConstantToString(constant);

            LogString($"{header}: {valueStr}");
        }

        private void LogConversion(CommonConversion conversion, string header = "Conversion")
        {
            var exists = FormatBoolProperty(nameof(conversion.Exists), conversion.Exists);
            var isIdentity = FormatBoolProperty(nameof(conversion.IsIdentity), conversion.IsIdentity);
            var isNumeric = FormatBoolProperty(nameof(conversion.IsNumeric), conversion.IsNumeric);
            var isReference = FormatBoolProperty(nameof(conversion.IsReference), conversion.IsReference);
            var isUserDefined = FormatBoolProperty(nameof(conversion.IsUserDefined), conversion.IsUserDefined);

            LogString($"{header}: {nameof(CommonConversion)} ({exists}, {isIdentity}, {isNumeric}, {isReference}, {isUserDefined}) (");
            LogSymbol(conversion.MethodSymbol, nameof(conversion.MethodSymbol));
            LogString(")");
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

        private static string FormatBoolProperty(string propertyName, bool value) => $"{propertyName}: {(value ? "True" : "False")}";

        #endregion

        #region Visit methods

        public override void Visit(IOperation operation)
        {
            if (operation == null)
            {
                Indent();
                LogString("null");
                LogNewLine();
                Unindent();
                return;
            }

            if (!operation.IsImplicit)
            {
                try
                {
                    _explictNodeMap.Add(operation.Syntax, operation);
                }
                catch (ArgumentException)
                {
                    Assert.False(true, $"Duplicate explicit node for syntax ({operation.Syntax.RawKind}): {operation.Syntax.ToString()}");
                }
            }

            Assert.True(operation.Type == null || !operation.MustHaveNullType(), $"Unexpected non-null type: {operation.Type}");

            if (operation != _root)
            {
                Indent();
            }

            base.Visit(operation);

            if (operation != _root)
            {
                Unindent();
            }
            Assert.True(operation.Syntax.Language == operation.Language);
        }

        private void Visit(IOperation operation, string header)
        {
            Debug.Assert(!string.IsNullOrEmpty(header));

            Indent();
            LogString($"{header}: ");
            LogNewLine();
            Visit(operation);
            Unindent();
        }

        private void VisitArrayCommon<T>(ImmutableArray<T> list, string header, bool logElementCount, bool logNullForDefault, Action<T> arrayElementVisitor)
        {
            Debug.Assert(!string.IsNullOrEmpty(header));

            Indent();
            if (!list.IsDefaultOrEmpty)
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
                var suffix = logNullForDefault && list.IsDefault ? ": null" : "(0)";
                LogString($"{header}{suffix}");
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

        private void VisitChildren(IOperation operation)
        {
            Debug.Assert(operation.Children.All(o => o != null));

            var children = operation.Children.ToImmutableArray();
            if (!children.IsEmpty || operation.Kind != OperationKind.None)
            {
                VisitArray(children, "Children", logElementCount: true);
            }
        }

        private void VisitArray<T>(ImmutableArray<T> list, string header, bool logElementCount, bool logNullForDefault = false)
            where T : IOperation
        {
            VisitArrayCommon(list, header, logElementCount, logNullForDefault, VisitOperationArrayElement);
        }

        private void VisitArray(ImmutableArray<ISymbol> list, string header, bool logElementCount, bool logNullForDefault = false)
        {
            VisitArrayCommon(list, header, logElementCount, logNullForDefault, VisitSymbolArrayElement);
        }

        private void VisitArray(ImmutableArray<string> list, string header, bool logElementCount, bool logNullForDefault = false)
        {
            VisitArrayCommon(list, header, logElementCount, logNullForDefault, VisitStringArrayElement);
        }

        private void VisitArray(ImmutableArray<RefKind> list, string header, bool logElementCount, bool logNullForDefault = false)
        {
            VisitArrayCommon(list, header, logElementCount, logNullForDefault, VisitRefKindArrayElement);
        }

        private void VisitInstance(IOperation instance)
        {
            Visit(instance, header: "Instance Receiver");
        }

        internal override void VisitNoneOperation(IOperation operation)
        {
            LogString("IOperation: ");
            LogCommonPropertiesAndNewLine(operation);

            VisitChildren(operation);
        }

        public override void VisitBlock(IBlockOperation operation)
        {
            LogString(nameof(IBlockOperation));

            var statementsStr = $"{operation.Operations.Length} statements";
            var localStr = !operation.Locals.IsEmpty ? $", {operation.Locals.Length} locals" : string.Empty;
            LogString($" ({statementsStr}{localStr})");
            LogCommonPropertiesAndNewLine(operation);

            if (operation.Operations.IsEmpty)
            {
                return;
            }

            LogLocals(operation.Locals);
            base.VisitBlock(operation);
        }

        public override void VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation)
        {
            var variablesCountStr = $"{operation.Declarations.Length} declarations";
            LogString($"{nameof(IVariableDeclarationGroupOperation)} ({variablesCountStr})");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitVariableDeclarationGroup(operation);
        }

        public override void VisitVariableDeclarator(IVariableDeclaratorOperation operation)
        {
            LogString($"{nameof(IVariableDeclaratorOperation)} (");
            LogSymbol(operation.Symbol, "Symbol");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Initializer, "Initializer");
            if (!operation.IgnoredArguments.IsEmpty)
            {
                VisitArray(operation.IgnoredArguments, "IgnoredArguments", logElementCount: true);
            }
        }

        public override void VisitVariableDeclaration(IVariableDeclarationOperation operation)
        {
            var variableCount = operation.Declarators.Length;
            LogString($"{nameof(IVariableDeclarationOperation)} ({variableCount} declarators)");
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Declarators, "Declarators", false);
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitSwitch(ISwitchOperation operation)
        {
            var caseCountStr = $"{operation.Cases.Length} cases";
            LogString($"{nameof(ISwitchOperation)} ({caseCountStr})");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, header: "Switch expression");
            VisitArray(operation.Cases, "Sections", logElementCount: false);
        }

        public override void VisitSwitchCase(ISwitchCaseOperation operation)
        {
            var caseClauseCountStr = $"{operation.Clauses.Length} case clauses";
            var statementCountStr = $"{operation.Body.Length} statements";
            LogString($"{nameof(ISwitchCaseOperation)} ({caseClauseCountStr}, {statementCountStr})");
            LogCommonPropertiesAndNewLine(operation);

            Indent();
            VisitArray(operation.Clauses, "Clauses", logElementCount: false);
            VisitArray(operation.Body, "Body", logElementCount: false);
            Unindent();
        }

        public override void VisitWhileLoop(IWhileLoopOperation operation)
        {
            LogString(nameof(IWhileLoopOperation));
            LogString($" (ConditionIsTop: {operation.ConditionIsTop}, ConditionIsUntil: {operation.ConditionIsUntil})");
            LogLoopStatementHeader(operation);

            Visit(operation.Condition, "Condition");
            Visit(operation.Body, "Body");
            Visit(operation.IgnoredCondition, "IgnoredCondition");
        }

        public override void VisitForLoop(IForLoopOperation operation)
        {
            LogString(nameof(IForLoopOperation));
            LogLoopStatementHeader(operation);

            Visit(operation.Condition, "Condition");
            VisitArray(operation.Before, "Before", logElementCount: false);
            VisitArray(operation.AtLoopBottom, "AtLoopBottom", logElementCount: false);
            Visit(operation.Body, "Body");
        }

        public override void VisitForToLoop(IForToLoopOperation operation)
        {
            LogString(nameof(IForToLoopOperation));
            LogLoopStatementHeader(operation);

            Visit(operation.LoopControlVariable, "LoopControlVariable");
            Visit(operation.InitialValue, "InitialValue");
            Visit(operation.LimitValue, "LimitValue");
            Visit(operation.StepValue, "StepValue");
            Visit(operation.Body, "Body");
            VisitArray(operation.NextVariables, "NextVariables", logElementCount: true);
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

        private void LogLoopStatementHeader(ILoopOperation operation)
        {
            var kindStr = $"{nameof(LoopKind)}.{operation.LoopKind}";
            LogString($" ({kindStr})");
            LogCommonPropertiesAndNewLine(operation);

            LogLocals(operation.Locals);
        }

        public override void VisitForEachLoop(IForEachLoopOperation operation)
        {
            LogString(nameof(IForEachLoopOperation));
            LogLoopStatementHeader(operation);

            Visit(operation.LoopControlVariable, "LoopControlVariable");
            Visit(operation.Collection, "Collection");
            Visit(operation.Body, "Body");
            VisitArray(operation.NextVariables, "NextVariables", logElementCount: true);
        }

        public override void VisitLabeled(ILabeledOperation operation)
        {
            LogString(nameof(ILabeledOperation));

            // TODO: Put a better workaround to skip compiler generated labels.
            if (!operation.Label.IsImplicitlyDeclared)
            {
                LogString($" (Label: {operation.Label.Name})");
            }

            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operation, "Statement");
        }

        public override void VisitBranch(IBranchOperation operation)
        {
            LogString(nameof(IBranchOperation));
            var kindStr = $"{nameof(BranchKind)}.{operation.BranchKind}";
            var labelStr = !operation.Target.IsImplicitlyDeclared ? $", Label: {operation.Target.Name}" : string.Empty;
            LogString($" ({kindStr}{labelStr})");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitBranch(operation);
        }

        public override void VisitEmpty(IEmptyOperation operation)
        {
            LogString(nameof(IEmptyOperation));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitReturn(IReturnOperation operation)
        {
            LogString(nameof(IReturnOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.ReturnedValue, "ReturnedValue");
        }

        public override void VisitLock(ILockOperation operation)
        {
            LogString(nameof(ILockOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.LockedValue, "Expression");
            Visit(operation.Body, "Body");
        }

        public override void VisitTry(ITryOperation operation)
        {
            LogString(nameof(ITryOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Body, "Body");
            VisitArray(operation.Catches, "Catch clauses", logElementCount: true);
            Visit(operation.Finally, "Finally");
        }

        public override void VisitCatchClause(ICatchClauseOperation operation)
        {
            LogString(nameof(ICatchClauseOperation));
            var exceptionTypeStr = operation.ExceptionType != null ? operation.ExceptionType.ToTestDisplayString() : "null";
            LogString($" (Exception type: {exceptionTypeStr})");
            LogCommonPropertiesAndNewLine(operation);

            LogLocals(operation.Locals);
            Visit(operation.ExceptionDeclarationOrExpression, "ExceptionDeclarationOrExpression");
            Visit(operation.Filter, "Filter");
            Visit(operation.Handler, "Handler");
        }

        public override void VisitUsing(IUsingOperation operation)
        {
            LogString(nameof(IUsingOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Resources, "Resources");
            Visit(operation.Body, "Body");
        }

        // https://github.com/dotnet/roslyn/issues/21281
        internal override void VisitFixed(IFixedOperation operation)
        {
            LogString(nameof(IFixedOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Variables, "Declaration");
            Visit(operation.Body, "Body");
        }

        public override void VisitExpressionStatement(IExpressionStatementOperation operation)
        {
            LogString(nameof(IExpressionStatementOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operation, "Expression");
        }

        internal override void VisitWith(IWithOperation operation)
        {
            LogString(nameof(IWithOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, "Value");
            Visit(operation.Body, "Body");
        }

        public override void VisitStop(IStopOperation operation)
        {
            LogString(nameof(IStopOperation));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitEnd(IEndOperation operation)
        {
            LogString(nameof(IEndOperation));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitInvocation(IInvocationOperation operation)
        {
            LogString(nameof(IInvocationOperation));

            var isVirtualStr = operation.IsVirtual ? "virtual " : string.Empty;
            var spacing = !operation.IsVirtual && operation.Instance != null ? " " : string.Empty;
            LogString($" ({isVirtualStr}{spacing}");
            LogSymbol(operation.TargetMethod, header: string.Empty);
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            VisitInstance(operation.Instance);
            VisitArguments(operation.Arguments);
        }

        private void VisitArguments(ImmutableArray<IArgumentOperation> arguments)
        {
            VisitArray(arguments, "Arguments", logElementCount: true);
        }

        private void VisitDynamicArguments(HasDynamicArgumentsExpression operation)
        {
            VisitArray(operation.Arguments, "Arguments", logElementCount: true);
            VisitArray(operation.ArgumentNames, "ArgumentNames", logElementCount: true);
            VisitArray(operation.ArgumentRefKinds, "ArgumentRefKinds", logElementCount: true, logNullForDefault: true);

            VerifyGetArgumentNamePublicApi(operation, operation.ArgumentNames);
            VerifyGetArgumentRefKindPublicApi(operation, operation.ArgumentRefKinds);
        }

        private static void VerifyGetArgumentNamePublicApi(HasDynamicArgumentsExpression operation, ImmutableArray<string> argumentNames)
        {
            var length = operation.Arguments.Length;
            if (argumentNames.IsDefaultOrEmpty)
            {
                for (int i = 0; i < length; i++)
                {
                    Assert.Null(operation.GetArgumentName(i));
                }
            }
            else
            {
                Assert.Equal(length, argumentNames.Length);
                for (var i = 0; i < length; i++)
                {
                    Assert.Equal(argumentNames[i], operation.GetArgumentName(i));
                }
            }
        }

        private static void VerifyGetArgumentRefKindPublicApi(HasDynamicArgumentsExpression operation, ImmutableArray<RefKind> argumentRefKinds)
        {
            var length = operation.Arguments.Length;
            if (argumentRefKinds.IsDefault)
            {
                for (int i = 0; i < length; i++)
                {
                    Assert.Null(operation.GetArgumentRefKind(i));
                }
            }
            else if (argumentRefKinds.IsEmpty)
            {
                for (int i = 0; i < length; i++)
                {
                    Assert.Equal(RefKind.None, operation.GetArgumentRefKind(i));
                }
            }
            else
            {
                Assert.Equal(length, argumentRefKinds.Length);
                for (var i = 0; i < length; i++)
                {
                    Assert.Equal(argumentRefKinds[i], operation.GetArgumentRefKind(i));
                }
            }
        }

        public override void VisitArgument(IArgumentOperation operation)
        {
            LogString($"{nameof(IArgumentOperation)} (");
            LogString($"{nameof(ArgumentKind)}.{operation.ArgumentKind}, ");
            LogSymbol(operation.Parameter, header: "Matching Parameter", logDisplayString: false);
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value);

            Indent();
            LogConversion(operation.InConversion, "InConversion");
            LogNewLine();
            LogConversion(operation.OutConversion, "OutConversion");
            LogNewLine();
            Unindent();
        }

        public override void VisitOmittedArgument(IOmittedArgumentOperation operation)
        {
            LogString(nameof(IOmittedArgumentOperation));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitArrayElementReference(IArrayElementReferenceOperation operation)
        {
            LogString(nameof(IArrayElementReferenceOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.ArrayReference, "Array reference");
            VisitArray(operation.Indices, "Indices", logElementCount: true);
        }

        internal override void VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation)
        {
            LogString(nameof(IPointerIndirectionReferenceOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Pointer, "Pointer");
        }

        public override void VisitLocalReference(ILocalReferenceOperation operation)
        {
            LogString(nameof(ILocalReferenceOperation));
            LogString($": {operation.Local.Name}");
            if (operation.IsDeclaration)
            {
                LogString($" (IsDeclaration: {operation.IsDeclaration})");
            }
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitParameterReference(IParameterReferenceOperation operation)
        {
            LogString(nameof(IParameterReferenceOperation));
            LogString($": {operation.Parameter.Name}");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitInstanceReference(IInstanceReferenceOperation operation)
        {
            LogString(nameof(IInstanceReferenceOperation));
            LogCommonPropertiesAndNewLine(operation);
        }

        private void VisitMemberReferenceExpressionCommon(IMemberReferenceOperation operation)
        {
            if (operation.Member.IsStatic)
            {
                LogString(" (Static)");
            }

            LogCommonPropertiesAndNewLine(operation);
            VisitInstance(operation.Instance);
        }

        public override void VisitFieldReference(IFieldReferenceOperation operation)
        {
            LogString(nameof(IFieldReferenceOperation));
            LogString($": {operation.Field.ToTestDisplayString()}");
            if (operation.IsDeclaration)
            {
                LogString($" (IsDeclaration: {operation.IsDeclaration})");
            }

            VisitMemberReferenceExpressionCommon(operation);
        }

        public override void VisitMethodReference(IMethodReferenceOperation operation)
        {
            LogString(nameof(IMethodReferenceOperation));
            LogString($": {operation.Method.ToTestDisplayString()}");

            if (operation.IsVirtual)
            {
                LogString(" (IsVirtual)");
            }

            Assert.Null(operation.Type);

            VisitMemberReferenceExpressionCommon(operation);
        }

        public override void VisitPropertyReference(IPropertyReferenceOperation operation)
        {
            LogString(nameof(IPropertyReferenceOperation));
            LogString($": {operation.Property.ToTestDisplayString()}");

            VisitMemberReferenceExpressionCommon(operation);

            if (operation.Arguments.Length > 0)
            {
                VisitArguments(operation.Arguments);
            }
        }

        public override void VisitEventReference(IEventReferenceOperation operation)
        {
            LogString(nameof(IEventReferenceOperation));
            LogString($": {operation.Event.ToTestDisplayString()}");

            VisitMemberReferenceExpressionCommon(operation);
        }

        public override void VisitEventAssignment(IEventAssignmentOperation operation)
        {
            var kindStr = operation.Adds ? "EventAdd" : "EventRemove";
            LogString($"{nameof(IEventAssignmentOperation)} ({kindStr})");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.EventReference, header: "Event Reference");
            Visit(operation.HandlerValue, header: "Handler");
        }

        public override void VisitConditionalAccess(IConditionalAccessOperation operation)
        {
            LogString(nameof(IConditionalAccessOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operation, header: nameof(operation.Operation));
            Visit(operation.WhenNotNull, header: nameof(operation.WhenNotNull));
        }

        public override void VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation)
        {
            LogString(nameof(IConditionalAccessInstanceOperation));
            LogCommonPropertiesAndNewLine(operation);
        }

        internal override void VisitPlaceholder(IPlaceholderOperation operation)
        {
            LogString(nameof(IPlaceholderOperation));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitUnaryOperator(IUnaryOperation operation)
        {
            LogString(nameof(IUnaryOperation));

            var kindStr = $"{nameof(UnaryOperatorKind)}.{operation.OperatorKind}";
            if (operation.IsLifted)
            {
                kindStr += ", IsLifted";
            }

            if (operation.IsChecked)
            {
                kindStr += ", Checked";
            }

            LogString($" ({kindStr})");
            LogHasOperatorMethodExpressionCommon(operation.OperatorMethod);
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operand, "Operand");
        }

        public override void VisitBinaryOperator(IBinaryOperation operation)
        {
            LogString(nameof(IBinaryOperation));

            var kindStr = $"{nameof(BinaryOperatorKind)}.{operation.OperatorKind}";
            if (operation.IsLifted)
            {
                kindStr += ", IsLifted";
            }

            if (operation.IsChecked)
            {
                kindStr += ", Checked";
            }

            if (operation.IsCompareText)
            {
                kindStr += ", CompareText";
            }

            LogString($" ({kindStr})");
            LogHasOperatorMethodExpressionCommon(operation.OperatorMethod);
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.LeftOperand, "Left");
            Visit(operation.RightOperand, "Right");
        }

        private void LogHasOperatorMethodExpressionCommon(IMethodSymbol operatorMethodOpt)
        {
            if (operatorMethodOpt != null)
            {
                LogSymbol(operatorMethodOpt, header: " (OperatorMethod");
                LogString(")");
            }
        }

        public override void VisitConversion(IConversionOperation operation)
        {
            LogString(nameof(IConversionOperation));

            var isTryCast = $"TryCast: {(operation.IsTryCast ? "True" : "False")}";
            var isChecked = operation.IsChecked ? "Checked" : "Unchecked";
            LogString($" ({isTryCast}, {isChecked})");

            LogHasOperatorMethodExpressionCommon(operation.OperatorMethod);
            LogCommonPropertiesAndNewLine(operation);
            Indent();
            LogConversion(operation.Conversion);
            Unindent();
            LogNewLine();

            Visit(operation.Operand, "Operand");
        }

        public override void VisitConditional(IConditionalOperation operation)
        {
            LogString(nameof(IConditionalOperation));

            if (operation.IsRef)
            {
                LogString(" (IsRef)");
            }

            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Condition, "Condition");
            Visit(operation.WhenTrue, "WhenTrue");
            Visit(operation.WhenFalse, "WhenFalse");
        }

        public override void VisitCoalesce(ICoalesceOperation operation)
        {
            LogString(nameof(ICoalesceOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, "Expression");
            Visit(operation.WhenNull, "WhenNull");
        }

        public override void VisitIsType(IIsTypeOperation operation)
        {
            LogString(nameof(IIsTypeOperation));
            if (operation.IsNegated)
            {
                LogString(" (IsNotExpression)");
            }

            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.ValueOperand, "Operand");

            Indent();
            LogType(operation.TypeOperand, "IsType");
            LogNewLine();
            Unindent();
        }

        public override void VisitSizeOf(ISizeOfOperation operation)
        {
            LogString(nameof(ISizeOfOperation));
            LogCommonPropertiesAndNewLine(operation);

            Indent();
            LogType(operation.TypeOperand, "TypeOperand");
            LogNewLine();
            Unindent();
        }

        public override void VisitTypeOf(ITypeOfOperation operation)
        {
            LogString(nameof(ITypeOfOperation));
            LogCommonPropertiesAndNewLine(operation);

            Indent();
            LogType(operation.TypeOperand, "TypeOperand");
            LogNewLine();
            Unindent();
        }

        public override void VisitAnonymousFunction(IAnonymousFunctionOperation operation)
        {
            LogString(nameof(IAnonymousFunctionOperation));

            LogSymbol(operation.Symbol, header: " (Symbol");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitAnonymousFunction(operation);
        }

        public override void VisitDelegateCreation(IDelegateCreationOperation operation)
        {
            LogString(nameof(IDelegateCreationOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, nameof(operation.Target));
        }

        public override void VisitLiteral(ILiteralOperation operation)
        {
            LogString(nameof(ILiteralOperation));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitAwait(IAwaitOperation operation)
        {
            LogString(nameof(IAwaitOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operation, "Expression");
        }

        public override void VisitNameOf(INameOfOperation operation)
        {
            LogString(nameof(INameOfOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Argument);
        }

        public override void VisitThrow(IThrowOperation operation)
        {
            LogString(nameof(IThrowOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Exception);
        }

        public override void VisitAddressOf(IAddressOfOperation operation)
        {
            LogString(nameof(IAddressOfOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Reference, "Reference");
        }

        public override void VisitObjectCreation(IObjectCreationOperation operation)
        {
            LogString(nameof(IObjectCreationOperation));
            LogString($" (Constructor: {operation.Constructor.ToTestDisplayString()})");
            LogCommonPropertiesAndNewLine(operation);

            VisitArguments(operation.Arguments);
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation)
        {
            LogString(nameof(IAnonymousObjectCreationOperation));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Initializers, "Initializers", logElementCount: true);
        }

        public override void VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation)
        {
            LogString(nameof(IDynamicObjectCreationOperation));
            LogCommonPropertiesAndNewLine(operation);

            VisitDynamicArguments((HasDynamicArgumentsExpression)operation);
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitDynamicInvocation(IDynamicInvocationOperation operation)
        {
            LogString(nameof(IDynamicInvocationOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operation, "Expression");
            VisitDynamicArguments((HasDynamicArgumentsExpression)operation);
        }

        public override void VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation)
        {
            LogString(nameof(IDynamicIndexerAccessOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operation, "Expression");
            VisitDynamicArguments((HasDynamicArgumentsExpression)operation);
        }

        public override void VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation)
        {
            LogString(nameof(IObjectOrCollectionInitializerOperation));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Initializers, "Initializers", logElementCount: true);
        }

        public override void VisitMemberInitializer(IMemberInitializerOperation operation)
        {
            LogString(nameof(IMemberInitializerOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.InitializedMember, "InitializedMember");
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation)
        {
            LogString(nameof(ICollectionElementInitializerOperation));
            if (operation.AddMethod != null)
            {
                LogString($" (AddMethod: {operation.AddMethod.ToTestDisplayString()})");
            }
            LogString($" (IsDynamic: {operation.IsDynamic})");
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Arguments, "Arguments", logElementCount: true);
        }

        public override void VisitFieldInitializer(IFieldInitializerOperation operation)
        {
            LogString(nameof(IFieldInitializerOperation));

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

        public override void VisitVariableInitializer(IVariableInitializerOperation operation)
        {
            LogString(nameof(IVariableInitializerOperation));
            LogCommonPropertiesAndNewLine(operation);

            base.VisitVariableInitializer(operation);
        }

        public override void VisitPropertyInitializer(IPropertyInitializerOperation operation)
        {
            LogString(nameof(IPropertyInitializerOperation));
            LogSymbol(operation.InitializedProperty, header: " (Property");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitPropertyInitializer(operation);
        }

        public override void VisitParameterInitializer(IParameterInitializerOperation operation)
        {
            LogString(nameof(IParameterInitializerOperation));
            LogSymbol(operation.Parameter, header: " (Parameter");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitParameterInitializer(operation);
        }

        public override void VisitArrayCreation(IArrayCreationOperation operation)
        {
            LogString(nameof(IArrayCreationOperation));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.DimensionSizes, "Dimension Sizes", logElementCount: true);
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitArrayInitializer(IArrayInitializerOperation operation)
        {
            LogString(nameof(IArrayInitializerOperation));
            LogString($" ({operation.ElementValues.Length} elements)");
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.ElementValues, "Element Values", logElementCount: true);
        }

        public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
        {
            LogString(nameof(ISimpleAssignmentOperation));

            if (operation.IsRef)
            {
                LogString(" (IsRef)");
            }

            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, "Left");
            Visit(operation.Value, "Right");
        }

        public override void VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation)
        {
            LogString(nameof(IDeconstructionAssignmentOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, "Left");
            Visit(operation.Value, "Right");
        }

        public override void VisitDeclarationExpression(IDeclarationExpressionOperation operation)
        {
            LogString(nameof(IDeclarationExpressionOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Expression);
        }

        public override void VisitCompoundAssignment(ICompoundAssignmentOperation operation)
        {
            LogString(nameof(ICompoundAssignmentOperation));

            var kindStr = $"{nameof(BinaryOperatorKind)}.{operation.OperatorKind}";
            if (operation.IsLifted)
            {
                kindStr += ", IsLifted";
            }

            if (operation.IsChecked)
            {
                kindStr += ", Checked";
            }

            LogString($" ({kindStr})");
            LogHasOperatorMethodExpressionCommon(operation.OperatorMethod);
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, "Left");
            Visit(operation.Value, "Right");
        }

        public override void VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation)
        {
            LogString(nameof(IIncrementOrDecrementOperation));

            var kindStr = operation.IsPostfix ? "Postfix" : "Prefix";
            if (operation.IsLifted)
            {
                kindStr += ", IsLifted";
            }

            if (operation.IsChecked)
            {
                kindStr += ", Checked";
            }

            LogString($" ({kindStr})");
            LogHasOperatorMethodExpressionCommon(operation.OperatorMethod);
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, "Target");
        }

        public override void VisitParenthesized(IParenthesizedOperation operation)
        {
            LogString(nameof(IParenthesizedOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operand, "Operand");
        }

        public override void VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation)
        {
            LogString(nameof(IDynamicMemberReferenceOperation));
            // (Member Name: "quoted name", Containing Type: type)
            LogString(" (");
            LogConstant((object)operation.MemberName, "Member Name");
            LogString(", ");
            LogType(operation.ContainingType, "Containing Type");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            VisitArrayCommon(operation.TypeArguments, "Type Arguments", logElementCount: true, logNullForDefault: false, arrayElementVisitor: VisitSymbolArrayElement);

            VisitInstance(operation.Instance);
        }

        public override void VisitDefaultValue(IDefaultValueOperation operation)
        {
            LogString(nameof(IDefaultValueOperation));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation)
        {
            LogString(nameof(ITypeParameterObjectCreationOperation));
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitInvalid(IInvalidOperation operation)
        {
            LogString(nameof(IInvalidOperation));
            LogCommonPropertiesAndNewLine(operation);

            VisitChildren(operation);
        }

        public override void VisitLocalFunction(ILocalFunctionOperation operation)
        {
            LogString(nameof(ILocalFunctionOperation));

            LogSymbol(operation.Symbol, header: " (Symbol");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Body);
        }

        private void LogCaseClauseCommon(ICaseClauseOperation operation)
        {
            var kindStr = $"{nameof(CaseKind)}.{operation.CaseKind}";
            LogString($" ({kindStr})");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation)
        {
            LogString(nameof(ISingleValueCaseClauseOperation));
            LogCaseClauseCommon(operation);

            Visit(operation.Value, "Value");
        }

        public override void VisitRelationalCaseClause(IRelationalCaseClauseOperation operation)
        {
            LogString(nameof(IRelationalCaseClauseOperation));
            var kindStr = $"{nameof(BinaryOperatorKind)}.{operation.Relation}";
            LogString($" (Relational operator kind: {kindStr})");
            LogCaseClauseCommon(operation);

            Visit(operation.Value, "Value");
        }

        public override void VisitRangeCaseClause(IRangeCaseClauseOperation operation)
        {
            LogString(nameof(IRangeCaseClauseOperation));
            LogCaseClauseCommon(operation);

            Visit(operation.MinimumValue, "Min");
            Visit(operation.MaximumValue, "Max");
        }

        public override void VisitDefaultCaseClause(IDefaultCaseClauseOperation operation)
        {
            LogString(nameof(IDefaultCaseClauseOperation));
            LogCaseClauseCommon(operation);
        }

        public override void VisitTuple(ITupleOperation operation)
        {
            LogString(nameof(ITupleOperation));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Elements, "Elements", logElementCount: true);
        }

        public override void VisitInterpolatedString(IInterpolatedStringOperation operation)
        {
            LogString(nameof(IInterpolatedStringOperation));
            LogCommonPropertiesAndNewLine(operation);

            VisitArray(operation.Parts, "Parts", logElementCount: true);
        }

        public override void VisitInterpolatedStringText(IInterpolatedStringTextOperation operation)
        {
            LogString(nameof(IInterpolatedStringTextOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Text, "Text");
        }

        public override void VisitInterpolation(IInterpolationOperation operation)
        {
            LogString(nameof(IInterpolationOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Expression, "Expression");
            Visit(operation.Alignment, "Alignment");
            Visit(operation.FormatString, "FormatString");
        }

        public override void VisitConstantPattern(IConstantPatternOperation operation)
        {
            LogString(nameof(IConstantPatternOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, "Value");
        }

        public override void VisitDeclarationPattern(IDeclarationPatternOperation operation)
        {
            LogString(nameof(IDeclarationPatternOperation));
            LogSymbol(operation.DeclaredSymbol, " (Declared Symbol");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitIsPattern(IIsPatternOperation operation)
        {
            LogString(nameof(IIsPatternOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, "Expression");
            Visit(operation.Pattern, "Pattern");
        }

        public override void VisitPatternCaseClause(IPatternCaseClauseOperation operation)
        {
            LogString(nameof(IPatternCaseClauseOperation));
            LogSymbol(operation.Label, " (Label Symbol");
            LogString(")");
            LogCaseClauseCommon(operation);

            Visit(operation.Pattern, "Pattern");
            Visit(operation.Guard, "Guard Expression");
        }

        public override void VisitTranslatedQuery(ITranslatedQueryOperation operation)
        {
            LogString(nameof(ITranslatedQueryOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operation, "Expression");
        }

        public override void VisitRaiseEvent(IRaiseEventOperation operation)
        {
            LogString(nameof(IRaiseEventOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.EventReference, header: "Event Reference");
            VisitArguments(operation.Arguments);
        }

        #endregion
    }
}
