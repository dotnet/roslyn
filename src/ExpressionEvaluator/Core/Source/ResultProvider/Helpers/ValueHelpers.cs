// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

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
            return value.GetMemberValue(member.MetadataName, (int)member.MemberType, member.DeclaringType.FullName, inspectionContext);
        }

        internal static string Parenthesize(this string expr)
        {
            return $"({expr})";
        }

        internal static string ToCommaSeparatedString(this string[] values, char openParen, char closeParen)
        {
            Debug.Assert(values != null);

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            builder.Append(openParen);
            builder.AppendCommaSeparatedList(values);
            builder.Append(closeParen);

            return pooled.ToStringAndFree();
        }

        internal static void AppendCommaSeparatedList(this StringBuilder builder, string[] values)
        {
            bool any = false;
            foreach (var value in values)
            {
                if (any)
                {
                    builder.Append(", ");
                }
                builder.Append(value);
                any = true;
            }
        }
    }
}
