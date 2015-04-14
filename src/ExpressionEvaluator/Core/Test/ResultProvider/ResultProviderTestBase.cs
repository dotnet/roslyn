// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Xunit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    public abstract class ResultProviderTestBase
    {
        private readonly IDkmClrFormatter _formatter;
        private readonly IDkmClrResultProvider _resultProvider;

        internal readonly DkmInspectionContext DefaultInspectionContext;

        internal ResultProviderTestBase(ResultProvider resultProvider, DkmInspectionContext defaultInspectionContext)
        {
            _formatter = resultProvider.Formatter;
            _resultProvider = resultProvider;
            this.DefaultInspectionContext = defaultInspectionContext;
        }

        internal DkmClrValue CreateDkmClrValue(
            object value,
            Type type = null,
            string alias = null,
            DkmEvaluationResultFlags evalFlags = DkmEvaluationResultFlags.None,
            DkmClrValueFlags valueFlags = DkmClrValueFlags.None)
        {
            if (type == null)
            {
                type = value.GetType();
            }
            return new DkmClrValue(
                value,
                DkmClrValue.GetHostObjectValue((TypeImpl)type, value),
                new DkmClrType((TypeImpl)type),
                alias,
                _formatter,
                evalFlags,
                valueFlags);
        }

        internal DkmClrValue CreateDkmClrValue(
            object value,
            DkmClrType type,
            string alias = null,
            DkmEvaluationResultFlags evalFlags = DkmEvaluationResultFlags.None,
            DkmClrValueFlags valueFlags = DkmClrValueFlags.None,
            bool isComObject = false)
        {
            return new DkmClrValue(
                value,
                DkmClrValue.GetHostObjectValue(type.GetLmrType(), value),
                type,
                alias,
                _formatter,
                evalFlags,
                valueFlags,
                isComObject);
        }

        internal DkmClrValue CreateErrorValue(
            DkmClrType type,
            string message)
        {
            return new DkmClrValue(
                value: null,
                hostObjectValue: message,
                type: type,
                alias: null,
                formatter: _formatter,
                evalFlags: DkmEvaluationResultFlags.None,
                valueFlags: DkmClrValueFlags.Error);
        }

        #region Formatter Tests

        internal string FormatNull<T>(bool useHexadecimal = false)
        {
            return FormatValue(null, typeof(T), useHexadecimal);
        }

        internal string FormatValue(object value, bool useHexadecimal = false)
        {
            return FormatValue(value, value.GetType(), useHexadecimal);
        }

        internal string FormatValue(object value, Type type, bool useHexadecimal = false)
        {
            var clrValue = CreateDkmClrValue(value, type);
            var inspectionContext = CreateDkmInspectionContext(_formatter, DkmEvaluationFlags.None, radix: useHexadecimal ? 16u : 10u);
            return clrValue.GetValueString(inspectionContext, Formatter.NoFormatSpecifiers);
        }

        internal bool HasUnderlyingString(object value)
        {
            return HasUnderlyingString(value, value.GetType());
        }

        internal bool HasUnderlyingString(object value, Type type)
        {
            var clrValue = GetValueForUnderlyingString(value, type);
            return clrValue.HasUnderlyingString(DefaultInspectionContext);
        }

        internal string GetUnderlyingString(object value)
        {
            var clrValue = GetValueForUnderlyingString(value, value.GetType());
            return clrValue.GetUnderlyingString(DefaultInspectionContext);
        }

        internal DkmClrValue GetValueForUnderlyingString(object value, Type type)
        {
            return CreateDkmClrValue(
                value,
                type,
                evalFlags: DkmEvaluationResultFlags.RawString);
        }

        #endregion

        #region ResultProvider Tests

        internal DkmInspectionContext CreateDkmInspectionContext(
            DkmEvaluationFlags flags = DkmEvaluationFlags.None,
            uint radix = 10,
            DkmRuntimeInstance runtimeInstance = null)
        {
            return CreateDkmInspectionContext(_formatter, flags, radix, runtimeInstance);
        }

        internal static DkmInspectionContext CreateDkmInspectionContext(
            IDkmClrFormatter formatter,
            DkmEvaluationFlags flags,
            uint radix,
            DkmRuntimeInstance runtimeInstance = null)
        {
            return new DkmInspectionContext(formatter, flags, radix, runtimeInstance);
        }

        internal DkmEvaluationResult FormatResult(string name, DkmClrValue value, DkmClrType declaredType = null, DkmInspectionContext inspectionContext = null)
        {
            return FormatResult(name, name, value, declaredType, inspectionContext);
        }

        internal DkmEvaluationResult FormatResult(string name, string fullName, DkmClrValue value, DkmClrType declaredType = null, DkmInspectionContext inspectionContext = null)
        {
            DkmEvaluationResult evaluationResult = null;
            var workList = new DkmWorkList();
            _resultProvider.GetResult(
                value,
                workList,
                declaredType: declaredType ?? value.Type,
                inspectionContext: inspectionContext ?? DefaultInspectionContext,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                customTypeInfo: null,
                resultName: name,
                resultFullName: null,
                completionRoutine: asyncResult => evaluationResult = asyncResult.Result);
            workList.Execute();
            return evaluationResult;
        }

        internal DkmEvaluationResult[] GetChildren(DkmEvaluationResult evalResult, DkmInspectionContext inspectionContext = null)
        {
            DkmEvaluationResultEnumContext enumContext;
            var builder = ArrayBuilder<DkmEvaluationResult>.GetInstance();

            // Request 0-3 children.
            int size;
            DkmEvaluationResult[] items;
            for (size = 0; size < 3; size++)
            {
                items = GetChildren(evalResult, size, inspectionContext, out enumContext);
                var totalChildCount = enumContext.Count;
                Assert.InRange(totalChildCount, 0, int.MaxValue);
                var expectedSize = (size < totalChildCount) ? size : totalChildCount;
                Assert.Equal(expectedSize, items.Length);
            }

            // Request items (increasing the size of the request with each iteration).
            size = 1;
            items = GetChildren(evalResult, size, inspectionContext, out enumContext);
            while (items.Length > 0)
            {
                builder.AddRange(items);
                Assert.True(builder.Count <= enumContext.Count);

                int offset = builder.Count;
                // Request 0 items.
                items = GetItems(enumContext, offset, 0);
                Assert.Equal(items.Length, 0);
                // Request >0 items.
                size++;
                items = GetItems(enumContext, offset, size);
            }

            Assert.Equal(builder.Count, enumContext.Count);
            return builder.ToArrayAndFree();
        }

        internal DkmEvaluationResult[] GetChildren(DkmEvaluationResult evalResult, int initialRequestSize, DkmInspectionContext inspectionContext, out DkmEvaluationResultEnumContext enumContext)
        {
            DkmGetChildrenAsyncResult getChildrenResult = default(DkmGetChildrenAsyncResult);
            var workList = new DkmWorkList();
            _resultProvider.GetChildren(evalResult, workList, initialRequestSize, inspectionContext ?? DefaultInspectionContext, r => { getChildrenResult = r; });
            workList.Execute();
            var exception = getChildrenResult.Exception;
            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
            enumContext = getChildrenResult.EnumContext;
            return getChildrenResult.InitialChildren;
        }

        internal DkmEvaluationResult[] GetItems(DkmEvaluationResultEnumContext enumContext, int startIndex, int count)
        {
            DkmEvaluationEnumAsyncResult getItemsResult = default(DkmEvaluationEnumAsyncResult);
            var workList = new DkmWorkList();
            _resultProvider.GetItems(enumContext, workList, startIndex, count, r => { getItemsResult = r; });
            workList.Execute();
            var exception = getItemsResult.Exception;
            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
            return getItemsResult.Items;
        }

        internal static DkmEvaluationResult EvalResult(
            string name,
            string value,
            string type,
            string fullName,
            DkmEvaluationResultFlags flags = DkmEvaluationResultFlags.None,
            DkmEvaluationResultCategory category = DkmEvaluationResultCategory.Other,
            string editableValue = null,
            DkmCustomUIVisualizerInfo[] customUIVisualizerInfo = null)
        {
            return DkmSuccessEvaluationResult.Create(
                null,
                null,
                name,
                fullName,
                flags,
                value,
                editableValue,
                type,
                category,
                default(DkmEvaluationResultAccessType),
                default(DkmEvaluationResultStorageType),
                default(DkmEvaluationResultTypeModifierFlags),
                null,
                (customUIVisualizerInfo != null) ? new ReadOnlyCollection<DkmCustomUIVisualizerInfo>(customUIVisualizerInfo) : null,
                null,
                null);
        }

        internal static DkmIntermediateEvaluationResult EvalIntermediateResult(
            string name,
            string fullName,
            string expression,
            DkmLanguage language)
        {
            return DkmIntermediateEvaluationResult.Create(
                InspectionContext: null,
                StackFrame: null,
                Name: name,
                FullName: fullName,
                Expression: expression,
                IntermediateLanguage: language,
                TargetRuntime: null,
                DataItem: null);
        }

        internal static DkmEvaluationResult EvalFailedResult(
            string name,
            string message,
            string type = null,
            string fullName = null,
            DkmEvaluationResultFlags flags = DkmEvaluationResultFlags.None)
        {
            return DkmFailedEvaluationResult.Create(
                null,
                null,
                name,
                fullName,
                message,
                flags,
                type,
                null);
        }

        internal static void Verify(IReadOnlyList<DkmEvaluationResult> actual, params DkmEvaluationResult[] expected)
        {
            try
            {
                int n = actual.Count;
                Assert.Equal(expected.Length, n);
                for (int i = 0; i < n; i++)
                {
                    Verify(actual[i], expected[i]);
                }
            }
            catch
            {
                foreach (DkmSuccessEvaluationResult result in actual)
                {
                    var optionalArgumentsTemplate = string.Empty;
                    if (result.Flags != DkmEvaluationResultFlags.None)
                    {
                        optionalArgumentsTemplate += ", {4}";
                    }
                    if (result.Category != DkmEvaluationResultCategory.Other)
                    {
                        optionalArgumentsTemplate += ", {5}";
                    }
                    if (result.EditableValue != null)
                    {
                        optionalArgumentsTemplate += ", editableValue: {6}";
                    }
                    var evalResultTemplate = "EvalResult({0}, {1}, {2}, {3}" + optionalArgumentsTemplate + "),";
                    var resultValue = (result.Value == null) ? "null" : Quote(Escape(result.Value));
                    Console.WriteLine(evalResultTemplate,
                        Quote(result.Name),
                        resultValue, Quote(result.Type),
                        (result.FullName != null) ? Quote(Escape(result.FullName)) : "null",
                        FormatEnumValue(result.Flags),
                        FormatEnumValue(result.Category),
                        Quote(result.EditableValue));
                }

                throw;
            }
        }

        private static string Escape(string str)
        {
            return str.Replace("\"", "\\\"");
        }

        private static string Quote(string str)
        {
            return '"' + str + '"';
        }

        private static string FormatEnumValue(Enum e)
        {
            var parts = e.ToString().Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            var enumTypeName = e.GetType().Name;
            return string.Join(" | ", parts.Select(p => enumTypeName + "." + p));
        }

        internal static void Verify(DkmEvaluationResult actual, DkmEvaluationResult expected)
        {
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.FullName, actual.FullName);
            var expectedSuccess = expected as DkmSuccessEvaluationResult;
            var expectedIntermediate = expected as DkmIntermediateEvaluationResult;
            if (expectedSuccess != null)
            {
                var actualSuccess = (DkmSuccessEvaluationResult)actual;
                Assert.Equal(expectedSuccess.Value, actualSuccess.Value);
                Assert.Equal(expectedSuccess.Type, actualSuccess.Type);
                Assert.Equal(expectedSuccess.Flags, actualSuccess.Flags);
                Assert.Equal(expectedSuccess.Category, actualSuccess.Category);
                Assert.Equal(expectedSuccess.EditableValue, actualSuccess.EditableValue);
                // Verify the DebuggerVisualizerAttribute
                Assert.True(
                    (expectedSuccess.CustomUIVisualizers == actualSuccess.CustomUIVisualizers) ||
                    (expectedSuccess.CustomUIVisualizers != null && actualSuccess.CustomUIVisualizers != null &&
                    expectedSuccess.CustomUIVisualizers.SequenceEqual(actualSuccess.CustomUIVisualizers, CustomUIVisualizerInfoComparer.Instance)));
            }
            else if (expectedIntermediate != null)
            {
                var actualIntermediate = (DkmIntermediateEvaluationResult)actual;
                Assert.Equal(expectedIntermediate.Expression, actualIntermediate.Expression);
                Assert.Equal(expectedIntermediate.IntermediateLanguage.Id.LanguageId, actualIntermediate.IntermediateLanguage.Id.LanguageId);
                Assert.Equal(expectedIntermediate.IntermediateLanguage.Id.VendorId, actualIntermediate.IntermediateLanguage.Id.VendorId);
            }
            else
            {
                var actualFailed = (DkmFailedEvaluationResult)actual;
                var expectedFailed = (DkmFailedEvaluationResult)expected;
                Assert.Equal(expectedFailed.ErrorMessage, actualFailed.ErrorMessage);
                Assert.Equal(expectedFailed.Type, actualFailed.Type);
                Assert.Equal(expectedFailed.Flags, actualFailed.Flags);
            }
        }

        #endregion

        private sealed class CustomUIVisualizerInfoComparer : IEqualityComparer<DkmCustomUIVisualizerInfo>
        {
            internal static readonly CustomUIVisualizerInfoComparer Instance = new CustomUIVisualizerInfoComparer();

            bool IEqualityComparer<DkmCustomUIVisualizerInfo>.Equals(DkmCustomUIVisualizerInfo x, DkmCustomUIVisualizerInfo y)
            {
                return x == y ||
                    (x != null && y != null &&
                    x.Id == y.Id &&
                    x.MenuName == y.MenuName &&
                    x.Description == y.Description &&
                    x.Metric == y.Metric &&
                    x.UISideVisualizerTypeName == y.UISideVisualizerTypeName &&
                    x.UISideVisualizerAssemblyName == y.UISideVisualizerAssemblyName &&
                    x.UISideVisualizerAssemblyLocation == y.UISideVisualizerAssemblyLocation &&
                    x.DebuggeeSideVisualizerTypeName == y.DebuggeeSideVisualizerTypeName &&
                    x.DebuggeeSideVisualizerAssemblyName == y.DebuggeeSideVisualizerAssemblyName);
            }

            int IEqualityComparer<DkmCustomUIVisualizerInfo>.GetHashCode(DkmCustomUIVisualizerInfo obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
