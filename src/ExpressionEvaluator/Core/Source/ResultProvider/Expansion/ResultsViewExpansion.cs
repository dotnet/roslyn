// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class ResultsViewExpansion : Expansion
    {
        internal static ResultsViewExpansion CreateExpansion(DkmClrValue value, Formatter formatter)
        {
            var enumerableType = GetEnumerableType(value);
            if (enumerableType == null)
            {
                return null;
            }
            return CreateExpansion(value, enumerableType, formatter);
        }

        internal static DkmEvaluationResult CreateResultsOnly(
            string name,
            DkmClrType declaredType,
            DkmClrValue value,
            EvalResultDataItem parent,
            Formatter formatter)
        {
            string errorMessage;
            if (value.IsError())
            {
                errorMessage = (string)value.HostObjectValue;
            }
            else if (value.HasExceptionThrown(parent))
            {
                errorMessage = value.GetExceptionMessage(name, formatter);
            }
            else
            {
                var enumerableType = GetEnumerableType(value);
                if (enumerableType != null)
                {
                    var expansion = CreateExpansion(value, enumerableType, formatter);
                    if (expansion != null)
                    {
                        return expansion.CreateEvaluationResult(name, parent, formatter);
                    }
                    errorMessage = Resources.ResultsViewNoSystemCore;
                }
                else
                {
                    errorMessage = Resources.ResultsViewNotEnumerable;
                }
            }

            Debug.Assert(errorMessage != null);
            return DkmFailedEvaluationResult.Create(
                InspectionContext: value.InspectionContext,
                StackFrame: value.StackFrame,
                Name: name,
                FullName: null,
                ErrorMessage: errorMessage,
                Flags: DkmEvaluationResultFlags.None,
                Type: null,
                DataItem: null);
        }

        private static DkmClrType GetEnumerableType(DkmClrValue value)
        {
            Debug.Assert(!value.IsError());

            if (value.IsNull)
            {
                return null;
            }

            var valueType = value.Type.GetLmrType();
            // Do not support Results View for strings
            // or arrays. (Matches legacy EE.)
            if (valueType.IsString() || valueType.IsArray)
            {
                return null;
            }

            var enumerableType = valueType.GetIEnumerableImplementationIfAny();
            if (enumerableType == null)
            {
                return null;
            }

            return DkmClrType.Create(value.Type.AppDomain, enumerableType);
        }

        private static ResultsViewExpansion CreateExpansion(DkmClrValue value, DkmClrType enumerableType, Formatter formatter)
        {
            var proxyValue = value.InstantiateResultsViewProxy(enumerableType);
            // InstantiateResultsViewProxy may return null
            // (if assembly is missing for instance).
            if (proxyValue == null)
            {
                return null;
            }

            var proxyMembers = MemberExpansion.CreateExpansion(
                proxyValue.Type.GetLmrType(),
                proxyValue,
                ExpansionFlags.None,
                TypeHelpers.IsPublic,
                formatter);
            return new ResultsViewExpansion(proxyValue, proxyMembers);
        }

        private readonly DkmClrValue _proxyValue;
        private readonly Expansion _proxyMembers;

        private ResultsViewExpansion(DkmClrValue proxyValue, Expansion proxyMembers)
        {
            Debug.Assert(proxyValue != null);
            Debug.Assert(proxyMembers != null);

            _proxyValue = proxyValue;
            _proxyMembers = proxyMembers;
        }

        internal override void GetRows(
            ResultProvider resultProvider,
            ArrayBuilder<DkmEvaluationResult> rows,
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent,
            DkmClrValue value,
            int startIndex,
            int count,
            bool visitAll,
            ref int index)
        {
            if (InRange(startIndex, count, index))
            {
                var evalResult = CreateEvaluationResult(Resources.ResultsView, parent, resultProvider.Formatter);
                rows.Add(evalResult);
            }

            index++;
        }

        private DkmEvaluationResult CreateEvaluationResult(string name, EvalResultDataItem parent, Formatter formatter)
        {
            var proxyType = _proxyValue.Type.GetLmrType();
            string fullName;
            ReadOnlyCollection<string> formatSpecifiers;
            bool childShouldParenthesize;
            if (parent == null)
            {
                Debug.Assert(name != null);
                fullName = formatter.TrimAndGetFormatSpecifiers(name, out formatSpecifiers);
                childShouldParenthesize = formatter.NeedsParentheses(fullName);
            }
            else
            {
                fullName = parent.ChildFullNamePrefix;
                formatSpecifiers = parent.FormatSpecifiers;
                childShouldParenthesize = false;
            }

            var childFullNamePrefix = (fullName == null) ?
                null :
                formatter.GetObjectCreationExpression(formatter.GetTypeName(proxyType, escapeKeywordIdentifiers: true), fullName);
            var dataItem = new EvalResultDataItem(
                name: name,
                typeDeclaringMember: null,
                declaredType: proxyType,
                value: _proxyValue,
                expansion: _proxyMembers,
                childShouldParenthesize: childShouldParenthesize,
                fullName: fullName,
                childFullNamePrefixOpt: childFullNamePrefix,
                formatSpecifiers: Formatter.AddFormatSpecifier(formatSpecifiers, "results"),
                category: DkmEvaluationResultCategory.Method,
                flags: DkmEvaluationResultFlags.ReadOnly,
                editableValue: null);
            return ResultProvider.CreateEvaluationResult(
                value: _proxyValue,
                name: name,
                typeName: string.Empty,
                display: Resources.ResultsViewValueWarning,
                dataItem: dataItem);
        }
    }
}
