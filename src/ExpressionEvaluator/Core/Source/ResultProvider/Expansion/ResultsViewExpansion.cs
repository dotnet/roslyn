// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class ResultsViewExpansion : Expansion
    {
        private const string ResultsFormatSpecifier = "results";

        internal static ResultsViewExpansion CreateExpansion(DkmInspectionContext inspectionContext, DkmClrValue value, ResultProvider resultProvider)
        {
            var enumerableType = GetEnumerableType(value);
            if (enumerableType == null)
            {
                return null;
            }
            return CreateExpansion(inspectionContext, value, enumerableType, resultProvider);
        }

        internal static EvalResult CreateResultsOnlyRow(
            DkmInspectionContext inspectionContext,
            string name,
            string fullName,
            ReadOnlyCollection<string> formatSpecifiers,
            DkmClrType declaredType,
            DkmClrCustomTypeInfo declaredTypeInfo,
            DkmClrValue value,
            ResultProvider resultProvider)
        {
            string errorMessage;
            if (value.IsError())
            {
                errorMessage = (string)value.HostObjectValue;
            }
            else if (value.HasExceptionThrown())
            {
                errorMessage = value.GetExceptionMessage(inspectionContext, name);
            }
            else
            {
                var enumerableType = GetEnumerableType(value);
                if (enumerableType != null)
                {
                    var expansion = CreateExpansion(inspectionContext, value, enumerableType, resultProvider);
                    if (expansion != null)
                    {
                        return expansion.CreateResultsViewRow(
                            inspectionContext,
                            name,
                            fullName,
                            formatSpecifiers,
                            new TypeAndCustomInfo(declaredType, declaredTypeInfo),
                            value,
                            includeResultsFormatSpecifier: true,
                            fullNameProvider: resultProvider.FullNameProvider);
                    }
                    errorMessage = Resources.ResultsViewNoSystemCore;
                }
                else
                {
                    errorMessage = Resources.ResultsViewNotEnumerable;
                }
            }

            Debug.Assert(errorMessage != null);
            return new EvalResult(name, errorMessage, inspectionContext);
        }

        /// <summary>
        /// Generate a Results Only row if the value is a synthesized
        /// value declared as IEnumerable or IEnumerable&lt;T&gt;.
        /// </summary>
        internal static EvalResult CreateResultsOnlyRowIfSynthesizedEnumerable(
            DkmInspectionContext inspectionContext,
            string name,
            string fullName,
            ReadOnlyCollection<string> formatSpecifiers,
            DkmClrType declaredType,
            DkmClrCustomTypeInfo declaredTypeInfo,
            DkmClrValue value,
            ResultProvider resultProvider)
        {
            if ((value.ValueFlags & DkmClrValueFlags.Synthetic) == 0)
            {
                return null;
            }

            // Must be declared as IEnumerable or IEnumerable<T>, not a derived type.
            var enumerableType = GetEnumerableType(value, declaredType, requireExactInterface: true);
            if (enumerableType == null)
            {
                return null;
            }

            var expansion = CreateExpansion(inspectionContext, value, enumerableType, resultProvider);
            if (expansion == null)
            {
                return null;
            }

            return expansion.CreateResultsViewRow(
                inspectionContext,
                name,
                fullName,
                formatSpecifiers,
                new TypeAndCustomInfo(declaredType, declaredTypeInfo),
                value,
                includeResultsFormatSpecifier: false,
                fullNameProvider: resultProvider.FullNameProvider);
        }

        private static DkmClrType GetEnumerableType(DkmClrValue value)
        {
            return GetEnumerableType(value, value.Type, requireExactInterface: false);
        }

        private static bool IsEnumerableCandidate(DkmClrValue value)
        {
            Debug.Assert(!value.IsError());

            if (value.IsNull || value.HasExceptionThrown())
            {
                return false;
            }

            // Do not support Results View for strings
            // or arrays. (Matches legacy EE.)
            var type = value.Type.GetLmrType();
            return !type.IsString() && !type.IsArray;
        }

        private static DkmClrType GetEnumerableType(DkmClrValue value, DkmClrType valueType, bool requireExactInterface)
        {
            if (!IsEnumerableCandidate(value))
            {
                return null;
            }

            var type = valueType.GetLmrType();
            Type enumerableType;
            if (requireExactInterface)
            {
                if (!type.IsIEnumerable() && !type.IsIEnumerableOfT())
                {
                    return null;
                }
                enumerableType = type;
            }
            else
            {
                enumerableType = type.GetIEnumerableImplementationIfAny();
                if (enumerableType == null)
                {
                    return null;
                }
            }

            return DkmClrType.Create(valueType.AppDomain, enumerableType);
        }

        private static ResultsViewExpansion CreateExpansion(DkmInspectionContext inspectionContext, DkmClrValue value, DkmClrType enumerableType, ResultProvider resultProvider)
        {
            var proxyValue = value.InstantiateResultsViewProxy(inspectionContext, enumerableType);
            // InstantiateResultsViewProxy may return null (if required assembly is missing, for instance).
            if (proxyValue == null)
            {
                return null;
            }

            var proxyMembers = MemberExpansion.CreateExpansion(
                inspectionContext,
                new TypeAndCustomInfo(proxyValue.Type),
                proxyValue,
                flags: ExpansionFlags.None,
                predicate: TypeHelpers.IsPublic,
                resultProvider: resultProvider,
                isProxyType: false,
                supportsFavorites: false);
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
            ArrayBuilder<EvalResult> rows,
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
                rows.Add(CreateResultsViewRow(inspectionContext, parent, resultProvider.FullNameProvider));
            }

            index++;
        }

        private EvalResult CreateResultsViewRow(
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent,
            IDkmClrFullNameProvider fullNameProvider)
        {
            Debug.Assert(parent != null);
            var proxyTypeAndInfo = new TypeAndCustomInfo(_proxyValue.Type);
            var fullName = parent.ChildFullNamePrefix;
            var childFullNamePrefix = (fullName == null)
                ? null
                : fullNameProvider.GetClrObjectCreationExpression(
                    inspectionContext,
                    proxyTypeAndInfo.ClrType,
                    proxyTypeAndInfo.Info,
                    [fullName]);
            return new EvalResult(
                ExpansionKind.ResultsView,
                Resources.ResultsView,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: proxyTypeAndInfo,
                useDebuggerDisplay: false,
                value: _proxyValue,
                displayValue: Resources.ResultsViewValueWarning,
                expansion: _proxyMembers,
                childShouldParenthesize: false,
                fullName: fullName,
                childFullNamePrefixOpt: childFullNamePrefix,
                formatSpecifiers: Formatter.AddFormatSpecifier(parent.FormatSpecifiers, ResultsFormatSpecifier),
                category: DkmEvaluationResultCategory.Method,
                flags: DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExpansionHasSideEffects,
                editableValue: null,
                inspectionContext: inspectionContext);
        }

        private EvalResult CreateResultsViewRow(
            DkmInspectionContext inspectionContext,
            string name,
            string fullName,
            ReadOnlyCollection<string> formatSpecifiers,
            TypeAndCustomInfo declaredTypeAndInfo,
            DkmClrValue value,
            bool includeResultsFormatSpecifier,
            IDkmClrFullNameProvider fullNameProvider)
        {
            if (includeResultsFormatSpecifier)
            {
                formatSpecifiers = Formatter.AddFormatSpecifier(formatSpecifiers, ResultsFormatSpecifier);
            }
            var childFullNamePrefix = fullNameProvider.GetClrObjectCreationExpression(
                inspectionContext,
                _proxyValue.Type,
                customTypeInfo: null,
                arguments: [fullName]);
            return new EvalResult(
                ExpansionKind.Default,
                name,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: declaredTypeAndInfo,
                useDebuggerDisplay: false,
                value: value,
                displayValue: null,
                expansion: new IndirectExpansion(_proxyValue, _proxyMembers),
                childShouldParenthesize: false,
                fullName: fullName,
                childFullNamePrefixOpt: childFullNamePrefix,
                formatSpecifiers: formatSpecifiers,
                category: DkmEvaluationResultCategory.Method,
                flags: DkmEvaluationResultFlags.ReadOnly,
                editableValue: null,
                inspectionContext: inspectionContext);
        }

        private sealed class IndirectExpansion : Expansion
        {
            private readonly DkmClrValue _proxyValue;
            private readonly Expansion _expansion;

            internal IndirectExpansion(DkmClrValue proxyValue, Expansion expansion)
            {
                _proxyValue = proxyValue;
                _expansion = expansion;
            }

            internal override void GetRows(
                ResultProvider resultProvider,
                ArrayBuilder<EvalResult> rows,
                DkmInspectionContext inspectionContext,
                EvalResultDataItem parent,
                DkmClrValue value,
                int startIndex,
                int count,
                bool visitAll,
                ref int index)
            {
                _expansion.GetRows(
                    resultProvider,
                    rows,
                    inspectionContext.With(ResultProvider.NoResults),
                    parent,
                    _proxyValue,
                    startIndex: startIndex,
                    count: count,
                    visitAll: visitAll,
                    index: ref index);
            }
        }
    }
}
