// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class DynamicViewExpansion : Expansion
    {
        private const string DynamicFormatSpecifier = "dynamic";

        internal static DynamicViewExpansion CreateExpansion(DkmInspectionContext inspectionContext, DkmClrValue value, Formatter formatter)
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
                DynamicFlagsMap.Create(new TypeAndCustomInfo(proxyType)));
            return new DynamicViewExpansion(proxyValue, itemsMemberExpansion);
        }

        internal static EvalResultDataItem CreateMembersOnlyRow(
            DkmInspectionContext inspectionContext,
            string name,
            DkmClrValue value,
            Formatter formatter)
        {
            var expansion = CreateExpansion(inspectionContext, value, formatter);
            return (expansion != null) ?
                expansion.CreateDynamicViewRow(inspectionContext, name, parent: null, formatter: formatter) :
                new EvalResultDataItem(name, Resources.DynamicViewNotDynamic);
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
                rows.Add(CreateDynamicViewRow(inspectionContext, Resources.DynamicView, parent, resultProvider.Formatter));
            }

            index++;
        }

        private EvalResultDataItem CreateDynamicViewRow(DkmInspectionContext inspectionContext, string name, EvalResultDataItem parent, Formatter formatter)
        {
            var proxyTypeAndInfo = new TypeAndCustomInfo(_proxyValue.Type);
            var isRootExpression = parent == null;
            Debug.Assert(isRootExpression != (name == Resources.DynamicView));
            var fullName = isRootExpression ? name : parent.ChildFullNamePrefix;
            var sawInvalidIdentifier = false;
            var childFullNamePrefix = (fullName == null) ?
                null :
                formatter.GetObjectCreationExpression(formatter.GetTypeName(proxyTypeAndInfo, escapeKeywordIdentifiers: true, sawInvalidIdentifier: out sawInvalidIdentifier), fullName);
            Debug.Assert(!sawInvalidIdentifier); // Expected type name is "Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView".
            var formatSpecifiers = isRootExpression ? Formatter.NoFormatSpecifiers : parent.FormatSpecifiers;
            return new EvalResultDataItem(
                ExpansionKind.DynamicView,
                name,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: proxyTypeAndInfo,
                parent: null,
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
