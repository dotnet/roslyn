// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using System;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class NativeViewExpansion : Expansion
    {
        internal static readonly NativeViewExpansion Instance = new NativeViewExpansion();

        private NativeViewExpansion()
        {
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
                rows.Add(GetRow(resultProvider, inspectionContext, value, parent));
            }

            index++;
        }

        private EvalResultDataItem GetRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue comObject,
            EvalResultDataItem parent)
        {
            try
            {
                inspectionContext.RuntimeInstance.Process.GetNativeRuntimeInstance();
            }
            catch (DkmException)
            {
                // Native View requires native debugging to be enabled.
                return new EvalResultDataItem(Resources.NativeView, Resources.NativeViewNotNativeDebugging);
            }

            var name = "(IUnknown*)0x" + string.Format(IntPtr.Size == 4 ? "{0:x8}" : "{0:x16}", comObject.NativeComPointer);
            var fullName = "{C++}" + name;

            return new EvalResultDataItem(
                ExpansionKind.NativeView,
                name: name,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: new TypeAndCustomInfo(comObject.Type), // DkmClrValue types don't have attributes.
                parent: null,
                value: comObject,
                displayValue: null,
                expansion: this,
                childShouldParenthesize: false,
                fullName: fullName,
                childFullNamePrefixOpt: fullName,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Data,
                flags: DkmEvaluationResultFlags.ReadOnly,
                editableValue: null,
                inspectionContext: inspectionContext);
        }
    }
}
