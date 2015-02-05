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
                    return string.Format("{0} {{${1}}}", valueStr, alias);
                }
            }
            return valueStr;
        }

        // Some evaluation results, like base type, reuse their parent's value.  They should
        // not, however, report that their evaluation triggered an exception.
        internal static bool HasExceptionThrown(this DkmClrValue value, EvalResultDataItem parent)
        {
            return value.EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown) &&
                ((parent == null) || (value != parent.Value));
        }

        internal static string GetExceptionMessage(this DkmClrValue value, string fullNameWithoutFormatSpecifiers, Formatter formatter)
        {
            return string.Format(
                Resources.ExceptionThrown,
                fullNameWithoutFormatSpecifiers,
                formatter.GetTypeName(value.Type.GetLmrType()));
        }
    }
}
