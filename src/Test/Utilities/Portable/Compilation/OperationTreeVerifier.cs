// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Extensions;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class OperationTreeVerifier : OperationWalker
    {
        protected readonly Compilation _compilation;
        protected readonly IOperation _root;
        protected readonly StringBuilder _builder;
        private readonly Dictionary<SyntaxNode, IOperation> _explictNodeMap;
        private readonly Dictionary<ILabelSymbol, uint> _labelIdMap;

        private const string indent = "  ";
        protected string _currentIndent;
        private bool _pendingIndent;
        private uint _currentLabelId = 0;

        public OperationTreeVerifier(Compilation compilation, IOperation root, int initialIndent)
        {
            _compilation = compilation;
            _root = root;
            _builder = new StringBuilder();

            _currentIndent = new string(' ', initialIndent);
            _pendingIndent = true;

            _explictNodeMap = new Dictionary<SyntaxNode, IOperation>();
            _labelIdMap = new Dictionary<ILabelSymbol, uint>();
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
            actual = actual.Replace("\"", "\"\"");
            expectedOperationTree = expectedOperationTree.Trim(newLineChars);
            expectedOperationTree = expectedOperationTree.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            expectedOperationTree = expectedOperationTree.Replace("\"", "\"\"");

            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedOperationTree, actual);
        }

        #region Logging helpers

        private void LogPatternPropertiesAndNewLine(IPatternOperation operation)
        {
            LogPatternProperties(operation);
            LogString(")");
            LogNewLine();
        }

        private void LogPatternProperties(IPatternOperation operation)
        {
            LogCommonProperties(operation);
            LogString(" (");
            LogType(operation.InputType, $"{nameof(operation.InputType)}");
        }

        private void LogCommonPropertiesAndNewLine(IOperation operation)
        {
            LogCommonProperties(operation);
            LogNewLine();
        }

        private void LogCommonProperties(IOperation operation)
        {
            LogString(" (");

            // Kind
            LogString($"{nameof(OperationKind)}.{GetKindText(operation.Kind)}");

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
            Assert.NotNull(operation.Syntax);
            LogString($" (Syntax: {GetSnippetFromSyntax(operation.Syntax)})");

            // Some of these kinds were inconsistent in the first release, and in standardizing them the
            // string output isn't guaranteed to be one or the other. So standardize manually.
            string GetKindText(OperationKind kind)
            {
                switch (kind)
                {
                    case OperationKind.Unary:
                        return "Unary";
                    case OperationKind.Binary:
                        return "Binary";
                    case OperationKind.TupleBinary:
                        return "TupleBinary";
                    case OperationKind.MethodBody:
                        return "MethodBody";
                    case OperationKind.ConstructorBody:
                        return "ConstructorBody";
                    default:
                        return kind.ToString();
                }
            }
        }

        private static string GetSnippetFromSyntax(SyntaxNode syntax)
        {
            if (syntax == null)
            {
                return "null";
            }

            var text = syntax.ToString().Trim(Environment.NewLine.ToCharArray());
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

        protected void LogString(string str)
        {
            if (_pendingIndent)
            {
                str = _currentIndent + str;
                _pendingIndent = false;
            }

            _builder.Append(str);
        }

        protected void LogNewLine()
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

        private uint GetLabelId(ILabelSymbol symbol)
        {
            if (_labelIdMap.ContainsKey(symbol))
            {
                return _labelIdMap[symbol];
            }

            var id = _currentLabelId++;
            _labelIdMap[symbol] = id;
            return id;
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
            var declarationKind = operation.DeclarationKind != VariableDeclarationKind.Default ? $", DeclarationKind: {operation.DeclarationKind}" : string.Empty;
            LogString($"{nameof(IVariableDeclarationGroupOperation)} ({variablesCountStr}{declarationKind})");
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

            if (!operation.IgnoredDimensions.IsEmpty)
            {
                VisitArray(operation.IgnoredDimensions, "Ignored Dimensions", true);
            }
            VisitArray(operation.Declarators, "Declarators", false);
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitSwitch(ISwitchOperation operation)
        {
            var caseCountStr = $"{operation.Cases.Length} cases";
            var exitLabelStr = $", Exit Label Id: {GetLabelId(operation.ExitLabel)}";
            LogString($"{nameof(ISwitchOperation)} ({caseCountStr}{exitLabelStr})");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, header: "Switch expression");
            LogLocals(operation.Locals);

            foreach (ISwitchCaseOperation section in operation.Cases)
            {
                foreach (ICaseClauseOperation c in section.Clauses)
                {
                    if (c.Label != null)
                    {
                        GetLabelId(c.Label);
                    }
                }
            }

            VisitArray(operation.Cases, "Sections", logElementCount: false);
        }

        public override void VisitSwitchCase(ISwitchCaseOperation operation)
        {
            var caseClauseCountStr = $"{operation.Clauses.Length} case clauses";
            var statementCountStr = $"{operation.Body.Length} statements";
            LogString($"{nameof(ISwitchCaseOperation)} ({caseClauseCountStr}, {statementCountStr})");
            LogCommonPropertiesAndNewLine(operation);
            LogLocals(operation.Locals);

            Indent();
            VisitArray(operation.Clauses, "Clauses", logElementCount: false);
            VisitArray(operation.Body, "Body", logElementCount: false);
            Unindent();
            _ = ((BaseSwitchCaseOperation)operation).Condition;
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

            LogLocals(operation.ConditionLocals, header: nameof(operation.ConditionLocals));

            Visit(operation.Condition, "Condition");
            VisitArray(operation.Before, "Before", logElementCount: false);
            VisitArray(operation.AtLoopBottom, "AtLoopBottom", logElementCount: false);
            Visit(operation.Body, "Body");
        }

        public override void VisitForToLoop(IForToLoopOperation operation)
        {
            LogString(nameof(IForToLoopOperation));
            LogLoopStatementHeader(operation, operation.IsChecked);

            Visit(operation.LoopControlVariable, "LoopControlVariable");
            Visit(operation.InitialValue, "InitialValue");
            Visit(operation.LimitValue, "LimitValue");
            Visit(operation.StepValue, "StepValue");
            Visit(operation.Body, "Body");
            VisitArray(operation.NextVariables, "NextVariables", logElementCount: true);

            (ILocalSymbol loopObject, ForToLoopOperationUserDefinedInfo userDefinedInfo) = ((BaseForToLoopOperation)operation).Info;

            if (userDefinedInfo != null)
            {
                _ = userDefinedInfo.Addition.Value;
                _ = userDefinedInfo.Subtraction.Value;
                _ = userDefinedInfo.LessThanOrEqual.Value;
                _ = userDefinedInfo.GreaterThanOrEqual.Value;
            }
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

        private void LogLoopStatementHeader(ILoopOperation operation, bool? isChecked = null)
        {
            Assert.Equal(OperationKind.Loop, operation.Kind);
            var propertyStringBuilder = new StringBuilder();
            propertyStringBuilder.Append(" (");
            propertyStringBuilder.Append($"{nameof(LoopKind)}.{operation.LoopKind}");
            if (operation is IForEachLoopOperation { IsAsynchronous: true })
            {
                propertyStringBuilder.Append($", IsAsynchronous");
            }
            propertyStringBuilder.Append($", Continue Label Id: {GetLabelId(operation.ContinueLabel)}");
            propertyStringBuilder.Append($", Exit Label Id: {GetLabelId(operation.ExitLabel)}");
            if (isChecked.GetValueOrDefault())
            {
                propertyStringBuilder.Append($", Checked");
            }
            propertyStringBuilder.Append(")");
            LogString(propertyStringBuilder.ToString());
            LogCommonPropertiesAndNewLine(operation);

            LogLocals(operation.Locals);
        }

        public override void VisitForEachLoop(IForEachLoopOperation operation)
        {
            LogString(nameof(IForEachLoopOperation));
            LogLoopStatementHeader(operation);

            Assert.NotNull(operation.LoopControlVariable);
            Visit(operation.LoopControlVariable, "LoopControlVariable");
            Visit(operation.Collection, "Collection");
            Visit(operation.Body, "Body");
            VisitArray(operation.NextVariables, "NextVariables", logElementCount: true);
            ForEachLoopOperationInfo info = ((BaseForEachLoopOperation)operation).Info;
        }

        public override void VisitLabeled(ILabeledOperation operation)
        {
            LogString(nameof(ILabeledOperation));

            if (!operation.Label.IsImplicitlyDeclared)
            {
                LogString($" (Label: {operation.Label.Name})");
            }
            else
            {
                LogString($" (Label Id: {GetLabelId(operation.Label)})");
            }

            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operation, "Statement");
        }

        public override void VisitBranch(IBranchOperation operation)
        {
            LogString(nameof(IBranchOperation));
            var kindStr = $"{nameof(BranchKind)}.{operation.BranchKind}";
            // If the label is implicit, or if it has been assigned an id (such as VB Exit Do/While/Switch labels) then print the id, instead of the name.
            var labelStr = !(operation.Target.IsImplicitlyDeclared || _labelIdMap.ContainsKey(operation.Target)) ? $", Label: {operation.Target.Name}" : $", Label Id: {GetLabelId(operation.Target)}";
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
            if (operation.ExitLabel != null)
            {
                LogString($" (Exit Label Id: {GetLabelId(operation.ExitLabel)})");
            }
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
            if (operation.IsAsynchronous)
            {
                LogString($" (IsAsynchronous)");
            }
            LogCommonPropertiesAndNewLine(operation);

            LogLocals(operation.Locals);
            Visit(operation.Resources, "Resources");
            Visit(operation.Body, "Body");

            Assert.NotEqual(OperationKind.VariableDeclaration, operation.Resources.Kind);
            Assert.NotEqual(OperationKind.VariableDeclarator, operation.Resources.Kind);
        }

        // https://github.com/dotnet/roslyn/issues/21281
        internal override void VisitFixed(IFixedOperation operation)
        {
            LogString(nameof(IFixedOperation));
            LogCommonPropertiesAndNewLine(operation);

            LogLocals(operation.Locals);
            Visit(operation.Variables, "Declaration");
            Visit(operation.Body, "Body");
        }

        internal override void VisitAggregateQuery(IAggregateQueryOperation operation)
        {
            LogString(nameof(IAggregateQueryOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Group, "Group");
            Visit(operation.Aggregation, "Aggregation");
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
            var spacing = operation is { IsVirtual: false, Instance: { } } ? " " : string.Empty;
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

        public override void VisitFlowCapture(IFlowCaptureOperation operation)
        {
            LogString(nameof(IFlowCaptureOperation));
            LogString($": {operation.Id.Value}");
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, "Value");

            TestOperationVisitor.Singleton.VisitFlowCapture(operation);
        }

        public override void VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation)
        {
            LogString(nameof(IFlowCaptureReferenceOperation));
            LogString($": {operation.Id.Value}");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitIsNull(IIsNullOperation operation)
        {
            LogString(nameof(IIsNullOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operand, "Operand");
        }

        public override void VisitCaughtException(ICaughtExceptionOperation operation)
        {
            LogString(nameof(ICaughtExceptionOperation));
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
            LogString($" (ReferenceKind: {operation.ReferenceKind})");
            LogCommonPropertiesAndNewLine(operation);

            if (operation.IsImplicit)
            {
                if (operation.Parent is IMemberReferenceOperation memberReference && memberReference.Instance == operation)
                {
                    Assert.False(memberReference.Member.IsStatic);
                }
                else if (operation.Parent is IInvocationOperation invocation && invocation.Instance == operation)
                {
                    Assert.False(invocation.TargetMethod.IsStatic);
                }
            }
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

            Assert.NotNull(operation.EventReference);
            Visit(operation.EventReference, header: "Event Reference");
            Visit(operation.HandlerValue, header: "Handler");
        }

        public override void VisitConditionalAccess(IConditionalAccessOperation operation)
        {
            LogString(nameof(IConditionalAccessOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Operation, header: nameof(operation.Operation));
            Visit(operation.WhenNotNull, header: nameof(operation.WhenNotNull));
            Assert.NotNull(operation.Type);
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
            Assert.Equal(PlaceholderKind.AggregationGroup, operation.PlaceholderKind);
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
            var unaryOperatorMethod = ((BaseBinaryOperation)operation).UnaryOperatorMethod;
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.LeftOperand, "Left");
            Visit(operation.RightOperand, "Right");
        }

        public override void VisitTupleBinaryOperator(ITupleBinaryOperation operation)
        {
            LogString(nameof(ITupleBinaryOperation));
            var kindStr = $"{nameof(BinaryOperatorKind)}.{operation.OperatorKind}";

            LogString($" ({kindStr})");
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

            if (((Operation)operation).OwningSemanticModel == null)
            {
                LogNewLine();
                Indent();
                LogString($"({((BaseConversionOperation)operation).ConversionConvertible})");
                Unindent();
            }

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
            Indent();
            LogConversion(operation.ValueConversion, "ValueConversion");
            LogNewLine();
            Indent();
            LogString($"({((BaseCoalesceOperation)operation).ValueConversionConvertible})");
            Unindent();
            LogNewLine();
            Unindent();

            Visit(operation.WhenNull, "WhenNull");
        }

        public override void VisitCoalesceAssignment(ICoalesceAssignmentOperation operation)
        {
            LogString(nameof(ICoalesceAssignmentOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Target, nameof(operation.Target));
            Visit(operation.Value, nameof(operation.Value));
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

            // For C# this prints "lambda expression", which is not very helpful if we want to tell lambdas apart.
            // That is how symbol display is implemented for C#.
            // https://github.com/dotnet/roslyn/issues/22559#issuecomment-393667316 tracks improving the output.
            LogSymbol(operation.Symbol, header: " (Symbol");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitAnonymousFunction(operation);
        }

        public override void VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation)
        {
            LogString(nameof(IFlowAnonymousFunctionOperation));

            // For C# this prints "lambda expression", which is not very helpful if we want to tell lambdas apart.
            // That is how symbol display is implemented for C#.
            // https://github.com/dotnet/roslyn/issues/22559#issuecomment-393667316 tracks improving the output.
            LogSymbol(operation.Symbol, header: " (Symbol");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            base.VisitFlowAnonymousFunction(operation);
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

            LogString($" (Constructor: {operation.Constructor?.ToTestDisplayString() ?? "<null>"})");

            LogCommonPropertiesAndNewLine(operation);

            VisitArguments(operation.Arguments);
            Visit(operation.Initializer, "Initializer");
        }

        public override void VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation)
        {
            LogString(nameof(IAnonymousObjectCreationOperation));
            LogCommonPropertiesAndNewLine(operation);

            foreach (var initializer in operation.Initializers)
            {
                var simpleAssignment = (ISimpleAssignmentOperation)initializer;
                var propertyReference = (IPropertyReferenceOperation)simpleAssignment.Target;
                Assert.Empty(propertyReference.Arguments);
                Assert.Equal(OperationKind.InstanceReference, propertyReference.Instance.Kind);
                Assert.Equal(InstanceReferenceKind.ImplicitReceiver, ((IInstanceReferenceOperation)propertyReference.Instance).ReferenceKind);
            }

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

        [Obsolete("ICollectionElementInitializerOperation has been replaced with IInvocationOperation and IDynamicInvocationOperation", error: true)]
        public override void VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation)

        {
            // Kept to ensure that it's never called, as we can't override DefaultVisit in this visitor
            throw ExceptionUtilities.Unreachable;
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

            LogLocals(operation.Locals);
            base.VisitFieldInitializer(operation);
        }

        public override void VisitVariableInitializer(IVariableInitializerOperation operation)
        {
            LogString(nameof(IVariableInitializerOperation));
            LogCommonPropertiesAndNewLine(operation);
            Assert.Empty(operation.Locals);
            base.VisitVariableInitializer(operation);
        }

        public override void VisitPropertyInitializer(IPropertyInitializerOperation operation)
        {
            LogString(nameof(IPropertyInitializerOperation));

            if (operation.InitializedProperties.Length <= 1)
            {
                if (operation.InitializedProperties.Length == 1)
                {
                    LogSymbol(operation.InitializedProperties[0], header: " (Property");
                    LogString(")");
                }

                LogCommonPropertiesAndNewLine(operation);
            }
            else
            {
                LogString($" ({operation.InitializedProperties.Length} initialized properties)");
                LogCommonPropertiesAndNewLine(operation);

                Indent();

                int index = 1;
                foreach (var property in operation.InitializedProperties)
                {
                    LogSymbol(property, header: $"Property_{index++}");
                    LogNewLine();
                }

                Unindent();
            }

            LogLocals(operation.Locals);
            base.VisitPropertyInitializer(operation);
        }

        public override void VisitParameterInitializer(IParameterInitializerOperation operation)
        {
            LogString(nameof(IParameterInitializerOperation));
            LogSymbol(operation.Parameter, header: " (Parameter");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);

            LogLocals(operation.Locals);
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

            Assert.Null(operation.Type);
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
            Indent();
            LogConversion(operation.InConversion, "InConversion");
            LogNewLine();
            LogConversion(operation.OutConversion, "OutConversion");
            LogNewLine();
            Unindent();

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

            Visit(operation.Initializer, "Initializer");
        }

        internal override void VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation)
        {
            LogString(nameof(INoPiaObjectCreationOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Initializer, "Initializer");
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

            if (operation.Body != null)
            {
                if (operation.IgnoredBody != null)
                {
                    Visit(operation.Body, "Body");
                    Visit(operation.IgnoredBody, "IgnoredBody");

                }
                else
                {
                    Visit(operation.Body);
                }
            }
            else
            {
                Assert.Null(operation.IgnoredBody);
            }
        }

        private void LogCaseClauseCommon(ICaseClauseOperation operation)
        {
            Assert.Equal(OperationKind.CaseClause, operation.Kind);

            if (operation.Label != null)
            {
                LogString($" (Label Id: {GetLabelId(operation.Label)})");
            }

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
            Indent();
            LogType(operation.NaturalType, nameof(operation.NaturalType));
            LogNewLine();
            Unindent();

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

            Assert.Equal(OperationKind.Literal, operation.Text.Kind);
            Visit(operation.Text, "Text");
        }

        public override void VisitInterpolation(IInterpolationOperation operation)
        {
            LogString(nameof(IInterpolationOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Expression, "Expression");
            Visit(operation.Alignment, "Alignment");
            Visit(operation.FormatString, "FormatString");

            if (operation.FormatString != null)
            {
                Assert.Equal(OperationKind.Literal, operation.FormatString.Kind);
            }
        }

        public override void VisitConstantPattern(IConstantPatternOperation operation)
        {
            LogString(nameof(IConstantPatternOperation));
            LogPatternPropertiesAndNewLine(operation);

            Visit(operation.Value, "Value");
        }

        public override void VisitDeclarationPattern(IDeclarationPatternOperation operation)
        {
            LogString(nameof(IDeclarationPatternOperation));
            LogPatternProperties(operation);
            LogSymbol(operation.DeclaredSymbol, $", {nameof(operation.DeclaredSymbol)}");
            LogConstant((object)operation.MatchesNull, $", {nameof(operation.MatchesNull)}");
            LogString(")");
            LogNewLine();
        }

        public override void VisitRecursivePattern(IRecursivePatternOperation operation)
        {
            LogString(nameof(IRecursivePatternOperation));
            LogPatternProperties(operation);
            LogSymbol(operation.DeclaredSymbol, $", {nameof(operation.DeclaredSymbol)}");
            LogType(operation.MatchedType, $", {nameof(operation.MatchedType)}");
            LogSymbol(operation.DeconstructSymbol, $", {nameof(operation.DeconstructSymbol)}");
            LogString(")");
            LogNewLine();

            VisitArray(operation.DeconstructionSubpatterns, $"{nameof(operation.DeconstructionSubpatterns)} ", true, true);
            VisitArray(operation.PropertySubpatterns, $"{nameof(operation.PropertySubpatterns)} ", true, true);
        }

        public override void VisitPropertySubpattern(IPropertySubpatternOperation operation)
        {
            LogString(nameof(IPropertySubpatternOperation));
            LogCommonProperties(operation);
            LogNewLine();

            Visit(operation.Member, $"{nameof(operation.Member)}");
            Visit(operation.Pattern, $"{nameof(operation.Pattern)}");
        }

        public override void VisitIsPattern(IIsPatternOperation operation)
        {
            LogString(nameof(IIsPatternOperation));
            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.Value, $"{nameof(operation.Value)}");
            Visit(operation.Pattern, "Pattern");
        }

        public override void VisitPatternCaseClause(IPatternCaseClauseOperation operation)
        {
            LogString(nameof(IPatternCaseClauseOperation));
            LogCaseClauseCommon(operation);
            Assert.Same(((ICaseClauseOperation)operation).Label, operation.Label);

            Visit(operation.Pattern, "Pattern");
            if (operation.Guard != null)
                Visit(operation.Guard, nameof(operation.Guard));
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

        public override void VisitConstructorBodyOperation(IConstructorBodyOperation operation)
        {
            LogString(nameof(IConstructorBodyOperation));
            LogCommonPropertiesAndNewLine(operation);

            LogLocals(operation.Locals);
            Visit(operation.Initializer, "Initializer");
            Visit(operation.BlockBody, "BlockBody");
            Visit(operation.ExpressionBody, "ExpressionBody");
        }

        public override void VisitMethodBodyOperation(IMethodBodyOperation operation)
        {
            LogString(nameof(IMethodBodyOperation));
            LogCommonPropertiesAndNewLine(operation);
            Visit(operation.BlockBody, "BlockBody");
            Visit(operation.ExpressionBody, "ExpressionBody");
        }

        public override void VisitDiscardOperation(IDiscardOperation operation)
        {
            LogString(nameof(IDiscardOperation));
            LogString(" (");
            LogSymbol(operation.DiscardSymbol, "Symbol");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitDiscardPattern(IDiscardPatternOperation operation)
        {
            LogString(nameof(IDiscardPatternOperation));
            LogPatternPropertiesAndNewLine(operation);
        }

        public override void VisitSwitchExpression(ISwitchExpressionOperation operation)
        {
            LogString($"{nameof(ISwitchExpressionOperation)} ({operation.Arms.Length} arms)");
            LogCommonPropertiesAndNewLine(operation);
            Visit(operation.Value, nameof(operation.Value));
            VisitArray(operation.Arms, nameof(operation.Arms), logElementCount: true);
        }

        public override void VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation)
        {
            LogString($"{nameof(ISwitchExpressionArmOperation)} ({operation.Locals.Length} locals)");
            LogCommonPropertiesAndNewLine(operation);
            Visit(operation.Pattern, nameof(operation.Pattern));
            if (operation.Guard != null)
                Visit(operation.Guard, nameof(operation.Guard));
            Visit(operation.Value, nameof(operation.Value));
            LogLocals(operation.Locals);
        }

        public override void VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation)
        {
            LogString(nameof(IStaticLocalInitializationSemaphoreOperation));
            LogSymbol(operation.Local, " (Local Symbol");
            LogString(")");
            LogCommonPropertiesAndNewLine(operation);
        }

        public override void VisitRangeOperation(IRangeOperation operation)
        {
            LogString(nameof(IRangeOperation));

            if (operation.IsLifted)
            {
                LogString(" (IsLifted)");
            }

            LogCommonPropertiesAndNewLine(operation);

            Visit(operation.LeftOperand, nameof(operation.LeftOperand));
            Visit(operation.RightOperand, nameof(operation.RightOperand));
        }

        public override void VisitReDim(IReDimOperation operation)
        {
            LogString(nameof(IReDimOperation));
            if (operation.Preserve)
            {
                LogString(" (Preserve)");
            }
            LogCommonPropertiesAndNewLine(operation);
            VisitArray(operation.Clauses, "Clauses", logElementCount: true);
        }

        public override void VisitReDimClause(IReDimClauseOperation operation)
        {
            LogString(nameof(IReDimClauseOperation));
            LogCommonPropertiesAndNewLine(operation);
            Visit(operation.Operand, "Operand");
            VisitArray(operation.DimensionSizes, "DimensionSizes", logElementCount: true);
        }

        #endregion
    }
}
