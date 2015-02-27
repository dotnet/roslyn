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
        internal static ResultsViewExpansion CreateExpansion(DkmInspectionContext inspectionContext, DkmClrValue value, Formatter formatter)
        {
            var enumerableType = GetEnumerableType(value);
            if (enumerableType == null)
            {
                return null;
            }
            return CreateExpansion(inspectionContext, value, enumerableType, formatter);
        }

        internal static EvalResultDataItem CreateResultsOnlyRow(
            DkmInspectionContext inspectionContext,
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
                    var expansion = CreateExpansion(inspectionContext, value, enumerableType, formatter);
                    if (expansion != null)
                    {
                        return expansion.CreateResultsViewRow(inspectionContext, name, parent, formatter);
                    }
                    errorMessage = Resources.ResultsViewNoSystemCore;
                }
                else
                {
                    errorMessage = Resources.ResultsViewNotEnumerable;
                }
            }

            Debug.Assert(errorMessage != null);
            return new EvalResultDataItem(name, errorMessage);
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

        private static ResultsViewExpansion CreateExpansion(DkmInspectionContext inspectionContext, DkmClrValue value, DkmClrType enumerableType, Formatter formatter)
        {
            var proxyValue = value.InstantiateResultsViewProxy(inspectionContext, enumerableType);
            // InstantiateResultsViewProxy may return null
            // (if assembly is missing for instance).
            if (proxyValue == null)
            {
                return null;
            }

            var proxyMembers = MemberExpansion.CreateExpansion(
                inspectionContext,
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
            ArrayBuilder<EvalResultDataItem> rows,
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
                rows.Add(CreateResultsViewRow(inspectionContext, Resources.ResultsView, parent, resultProvider.Formatter));
            }

            index++;
        }

        private EvalResultDataItem CreateResultsViewRow(DkmInspectionContext inspectionContext, string name, EvalResultDataItem parent, Formatter formatter)
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
            return new EvalResultDataItem(
                ExpansionKind.ResultsView,
                name,
                typeDeclaringMember: null,
                declaredType: proxyType,
                parent: null,
                value: _proxyValue,
                displayValue: Resources.ResultsViewValueWarning,
                expansion: _proxyMembers,
                childShouldParenthesize: childShouldParenthesize,
                fullName: fullName,
                childFullNamePrefixOpt: childFullNamePrefix,
                formatSpecifiers: Formatter.AddFormatSpecifier(formatSpecifiers, "results"),
                category: DkmEvaluationResultCategory.Method,
                flags: DkmEvaluationResultFlags.ReadOnly,
                editableValue: null,
                inspectionContext: inspectionContext);
        }
    }
}
