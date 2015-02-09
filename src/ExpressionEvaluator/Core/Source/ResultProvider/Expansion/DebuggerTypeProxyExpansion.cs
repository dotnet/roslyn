// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// Debugger type proxy expansion.
    /// </summary>
    /// <remarks>
    /// May include <see cref="System.Collections.IEnumerable"/> and
    /// <see cref="System.Collections.Generic.IEnumerable{T}"/> as special cases.
    /// (The proxy is not declared by an attribute, but is known to debugger.)
    /// </remarks>
    internal sealed class DebuggerTypeProxyExpansion : Expansion
    {
        internal static Expansion CreateExpansion(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            string name,
            Type typeDeclaringMemberOpt,
            Type declaredType,
            DkmClrValue value,
            bool childShouldParenthesize,
            string fullName,
            string childFullNamePrefix,
            ReadOnlyCollection<string> formatSpecifiers,
            DkmEvaluationResultFlags flags,
            string editableValue)
        {
            Debug.Assert((inspectionContext.EvaluationFlags & DkmEvaluationFlags.NoExpansion) == 0);

            // Note: The native EE uses the proxy type, even for
            // null instances, so statics on the proxy type are
            // displayed. That case is not supported currently.
            if (!value.IsNull)
            {
                var proxyType = value.Type.GetProxyType();
                if (proxyType != null)
                {
                    if ((inspectionContext.EvaluationFlags & DkmEvaluationFlags.ShowValueRaw) != 0)
                    {
                        var rawView = CreateRawView(resultProvider, inspectionContext, declaredType, value);
                        Debug.Assert(rawView != null);
                        return rawView;
                    }

                    DkmClrValue proxyValue;
                    try
                    {
                        proxyValue = value.InstantiateProxyType(proxyType);
                    }
                    catch
                    {
                        proxyValue = null;
                    }

                    if (proxyValue != null)
                    {
                        return new DebuggerTypeProxyExpansion(
                            proxyValue,
                            name,
                            typeDeclaringMemberOpt,
                            declaredType,
                            value,
                            childShouldParenthesize,
                            fullName,
                            childFullNamePrefix,
                            formatSpecifiers,
                            flags,
                            editableValue,
                            resultProvider.Formatter);
                    }
                }
            }

            return null;
        }

        private readonly EvalResultDataItem proxyItem;
        private readonly string name;
        private readonly Type typeDeclaringMemberOpt;
        private readonly Type _declaredType;
        private readonly DkmClrValue value;
        private readonly bool childShouldParenthesize;
        private readonly string fullName;
        private readonly string childFullNamePrefix;
        private readonly ReadOnlyCollection<string> formatSpecifiers;
        private readonly DkmEvaluationResultFlags flags;
        private readonly string editableValue;

        private DebuggerTypeProxyExpansion(
            DkmClrValue proxyValue,
            string name,
            Type typeDeclaringMemberOpt,
            Type declaredType,
            DkmClrValue value,
            bool childShouldParenthesize,
            string fullName,
            string childFullNamePrefix,
            ReadOnlyCollection<string> formatSpecifiers,
            DkmEvaluationResultFlags flags,
            string editableValue,
            Formatter formatter)
        {
            Debug.Assert(proxyValue != null);
            var proxyType = proxyValue.Type.GetLmrType();
            var proxyMembers = MemberExpansion.CreateExpansion(
                proxyType,
                proxyValue,
                ExpansionFlags.IncludeBaseMembers,
                TypeHelpers.IsPublic,
                formatter);
            if (proxyMembers != null)
            {
                var proxyMemberFullNamePrefix = (childFullNamePrefix == null) ?
                    null :
                    formatter.GetObjectCreationExpression(formatter.GetTypeName(proxyType, escapeKeywordIdentifiers: true), childFullNamePrefix);
                this.proxyItem = new EvalResultDataItem(
                    name: null,
                    typeDeclaringMember: null,
                    declaredType: proxyType,
                    value: proxyValue,
                    expansion: proxyMembers,
                    childShouldParenthesize: false,
                    fullName: null,
                    childFullNamePrefixOpt: proxyMemberFullNamePrefix,
                    formatSpecifiers: Formatter.NoFormatSpecifiers,
                    category: default(DkmEvaluationResultCategory),
                    flags: default(DkmEvaluationResultFlags),
                    editableValue: null);
            }

            this.name = name;
            this.typeDeclaringMemberOpt = typeDeclaringMemberOpt;
            _declaredType = declaredType;
            this.value = value;
            this.childShouldParenthesize = childShouldParenthesize;
            this.fullName = fullName;
            this.childFullNamePrefix = childFullNamePrefix;
            this.formatSpecifiers = formatSpecifiers;
            this.flags = flags;
            this.editableValue = editableValue;
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
            if (this.proxyItem != null)
            {
                this.proxyItem.Expansion.GetRows(resultProvider, rows, inspectionContext, this.proxyItem, this.proxyItem.Value, startIndex, count, visitAll, ref index);
            }

            if (InRange(startIndex, count, index))
            {
                rows.Add(this.CreateRawViewRow(resultProvider, inspectionContext));
            }

            index++;
        }

        private DkmEvaluationResult CreateRawViewRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext)
        {
            var dataItem = new EvalResultDataItem(
                this.name,
                this.typeDeclaringMemberOpt,
                _declaredType,
                this.value,
                CreateRawView(resultProvider, inspectionContext, _declaredType, this.value),
                this.childShouldParenthesize,
                this.fullName,
                this.childFullNamePrefix,
                Formatter.AddFormatSpecifier(this.formatSpecifiers, "raw"),
                DkmEvaluationResultCategory.Data,
                this.flags | DkmEvaluationResultFlags.ReadOnly,
                this.editableValue);
            return ResultProvider.CreateEvaluationResult(
                value,
                Resources.RawView,
                typeName: "",
                display: null,
                dataItem: dataItem);
        }

        private static Expansion CreateRawView(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            Type declaredType,
            DkmClrValue value)
        {
            return resultProvider.GetTypeExpansion(inspectionContext, declaredType, value, ExpansionFlags.IncludeBaseMembers);
        }
    }
}
