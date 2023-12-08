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
    internal sealed class PointerDereferenceExpansion : Expansion
    {
        private readonly TypeAndCustomInfo _elementTypeAndInfo;

        public PointerDereferenceExpansion(TypeAndCustomInfo elementTypeAndInfo)
        {
            Debug.Assert(elementTypeAndInfo.Type != null);
            _elementTypeAndInfo = elementTypeAndInfo;
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
                rows.Add(GetRow(resultProvider, inspectionContext, value, _elementTypeAndInfo, parent: parent));
            }

            index++;
        }

        private static EvalResult GetRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue pointer,
            TypeAndCustomInfo elementTypeAndInfo,
            EvalResultDataItem parent)
        {
            var value = pointer.Dereference(inspectionContext);
            var wasExceptionThrown = value.EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown);

            var expansion = wasExceptionThrown
                ? null
                : resultProvider.GetTypeExpansion(inspectionContext, elementTypeAndInfo, value, ExpansionFlags.None, supportsFavorites: false);
            var parentFullName = parent.ChildFullNamePrefix;
            var fullName = parentFullName == null ? null : $"*{parentFullName}";
            var editableValue = resultProvider.Formatter2.GetEditableValueString(value, inspectionContext, elementTypeAndInfo.Info);

            // NB: Full name is based on the real (i.e. not DebuggerDisplay) name.  This is a change from dev12, 
            // which used the DebuggerDisplay name, causing surprising results in "Add Watch" scenarios.
            return new EvalResult(
                ExpansionKind.PointerDereference,
                name: fullName ?? $"*{parent.Name}",
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: elementTypeAndInfo,
                useDebuggerDisplay: false,
                value: value,
                displayValue: wasExceptionThrown ? string.Format(Resources.InvalidPointerDereference, fullName ?? parent.Name) : null,
                expansion: expansion,
                childShouldParenthesize: true,
                fullName: fullName,
                childFullNamePrefixOpt: fullName,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: DkmEvaluationResultFlags.None,
                editableValue: editableValue,
                inspectionContext: inspectionContext);
        }
    }
}
