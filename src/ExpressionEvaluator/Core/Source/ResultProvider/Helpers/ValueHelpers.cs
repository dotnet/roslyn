// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class ValueHelpers
    {
        internal static string IncludeObjectId(this DkmClrValue value, string valueStr)
        {
            Debug.Assert(valueStr != null);
            if (value.EvalFlags.Includes(DkmEvaluationResultFlags.HasObjectId))
            {
                string alias = value.Alias;
                if (!string.IsNullOrEmpty(alias))
                {
                    return $"{valueStr} {{{alias}}}";
                }
            }
            return valueStr;
        }

        internal static bool HasExceptionThrown(this DkmClrValue value)
        {
            return value.EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown);
        }

        internal static string GetExceptionMessage(this DkmClrValue value, DkmInspectionContext inspectionContext, string fullNameWithoutFormatSpecifiers)
        {
            var typeName = inspectionContext.GetTypeName(value.Type, null, Formatter.NoFormatSpecifiers);
            return string.Format(Resources.ExceptionThrown, fullNameWithoutFormatSpecifiers, typeName);
        }

        internal static DkmClrValue GetMemberValue(this DkmClrValue value, MemberAndDeclarationInfo member, DkmInspectionContext inspectionContext)
        {
            // Note: GetMemberValue() may return special value when func-eval of properties is disabled.
            return value.GetMemberValue(member.Name, (int)member.MemberType, member.DeclaringType.FullName, inspectionContext);
        }
    }
}
