// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class DynamicViewExpansion : Expansion
    {
        private const string DynamicFormatSpecifier = "dynamic";

        internal static DynamicViewExpansion CreateExpansion(DkmInspectionContext inspectionContext, DkmClrValue value)
        {
            if (value.IsError() || value.IsNull || value.HasExceptionThrown())
            {
                return null;
            }

            var type = value.Type.GetLmrType();
            if (!(type.IsComObject() || type.IsIDynamicMetaObjectProvider()))
            {
                return null;
            }

            var proxyValue = value.InstantiateDynamicViewProxy(inspectionContext);
            Debug.Assert((proxyValue == null) || (!proxyValue.IsNull && !proxyValue.IsError() && !proxyValue.HasExceptionThrown()));
            // InstantiateDynamicViewProxy may return null (if required assembly is missing, for instance).
            if (proxyValue == null)
            {
                return null;
            }

            // Expansion is based on the 'DynamicMetaObjectProviderDebugView.Items' property.
            var proxyType = proxyValue.Type;
            var itemsMemberExpansion = RootHiddenExpansion.CreateExpansion(
                proxyType.GetMemberByName("Items"),
                CustomTypeInfoTypeArgumentMap.Create(new TypeAndCustomInfo(proxyType)));
            return new DynamicViewExpansion(proxyValue, itemsMemberExpansion);
        }

        internal static EvalResult CreateMembersOnlyRow(
            DkmInspectionContext inspectionContext,
            string name,
            DkmClrValue value,
            ResultProvider resultProvider)
        {
            var expansion = CreateExpansion(inspectionContext, value);
            return (expansion != null)
                ? expansion.CreateDynamicViewRow(inspectionContext, name, parent: null, fullNameProvider: resultProvider.FullNameProvider)
                : new EvalResult(name, Resources.DynamicViewNotDynamic, inspectionContext);
        }

        private readonly DkmClrValue _proxyValue;
        private readonly Expansion _proxyMembers;

        private DynamicViewExpansion(DkmClrValue proxyValue, Expansion proxyMembers)
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
                rows.Add(CreateDynamicViewRow(inspectionContext, Resources.DynamicView, parent, resultProvider.FullNameProvider));
            }

            index++;
        }

        private EvalResult CreateDynamicViewRow(DkmInspectionContext inspectionContext, string name, EvalResultDataItem parent, IDkmClrFullNameProvider fullNameProvider)
        {
            var proxyTypeAndInfo = new TypeAndCustomInfo(_proxyValue.Type);
            var isRootExpression = parent == null;
            var fullName = isRootExpression ? name : parent.ChildFullNamePrefix;
            var childFullNamePrefix = (fullName == null)
                ? null
                : fullNameProvider.GetClrObjectCreationExpression(
                    inspectionContext,
                    proxyTypeAndInfo.ClrType,
                    proxyTypeAndInfo.Info,
                    [fullName]);
            var formatSpecifiers = isRootExpression ? Formatter.NoFormatSpecifiers : parent.FormatSpecifiers;
            return new EvalResult(
                ExpansionKind.DynamicView,
                name,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: proxyTypeAndInfo,
                useDebuggerDisplay: false,
                value: _proxyValue,
                displayValue: Resources.DynamicViewValueWarning,
                expansion: _proxyMembers,
                childShouldParenthesize: false,
                fullName: fullName,
                childFullNamePrefixOpt: childFullNamePrefix,
                formatSpecifiers: Formatter.AddFormatSpecifier(formatSpecifiers, DynamicFormatSpecifier),
                category: DkmEvaluationResultCategory.Method,
                flags: DkmEvaluationResultFlags.ReadOnly,
                editableValue: null,
                inspectionContext: inspectionContext);
        }
    }
}
