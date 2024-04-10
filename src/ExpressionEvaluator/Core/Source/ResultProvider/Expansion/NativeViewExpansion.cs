// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                rows.Add(GetRow(inspectionContext, value));
            }

            index++;
        }

        private EvalResult GetRow(
            DkmInspectionContext inspectionContext,
            DkmClrValue comObject)
        {
            try
            {
                inspectionContext.RuntimeInstance.Process.GetNativeRuntimeInstance();
            }
            catch (DkmException)
            {
                // Native View requires native debugging to be enabled.
                return new EvalResult(Resources.NativeView, Resources.NativeViewNotNativeDebugging, inspectionContext);
            }

            var name = "(IUnknown*)0x" + string.Format(IntPtr.Size == 4 ? "{0:x8}" : "{0:x16}", comObject.NativeComPointer);
            var fullName = "{C++}" + name;

            return new EvalResult(
                ExpansionKind.NativeView,
                name: name,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: new TypeAndCustomInfo(comObject.Type), // DkmClrValue types don't have attributes.
                useDebuggerDisplay: false,
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
