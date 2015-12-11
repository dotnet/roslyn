// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Object pretty printer.
    /// </summary>
    public abstract partial class CommonObjectFormatter : ObjectFormatter
    {
        public override string FormatObject(object obj, PrintOptions options)
        {
            var formatter = new Visitor(this, GetInternalBuilderOptions(options), GetPrimitiveOptions(options), GetTypeNameOptions(options), options.MemberDisplayFormat);
            return formatter.FormatObject(obj);
        }

        protected virtual StackTraceRewriter StackTraceRewriter { get; } = new CommonStackTraceRewriter();

        protected abstract CommonTypeNameFormatter TypeNameFormatter { get; }
        protected abstract CommonPrimitiveFormatter PrimitiveFormatter { get; }

        protected abstract bool TryFormatCompositeObject(object obj, out string value, out bool suppressMembers);

        internal virtual BuilderOptions GetInternalBuilderOptions(PrintOptions printOptions) =>
            new BuilderOptions(
                indentation: "  ",
                newLine: Environment.NewLine,
                ellipsis: "...",
                maximumLineLength: int.MaxValue,
                maximumOutputLength: printOptions.MaximumOutputLength);

        protected virtual CommonPrimitiveFormatter.Options GetPrimitiveOptions(PrintOptions printOptions) =>
            new CommonPrimitiveFormatter.Options(
                useHexadecimalNumbers: printOptions.NumberRadix == NumberRadix.Hexadecimal,
                includeCodePoints: printOptions.EscapeNonPrintableCharacters, // TODO (acasey): not quite the same
                omitStringQuotes: false);

        protected virtual CommonTypeNameFormatter.Options GetTypeNameOptions(PrintOptions printOptions) =>
            new CommonTypeNameFormatter.Options(
                useHexadecimalArrayBounds: printOptions.NumberRadix == NumberRadix.Hexadecimal,
                showNamespaces: false);

        public override string FormatRaisedException(Exception e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            builder.AppendLine(e.Message);

            var trace = new StackTrace(e, needFileInfo: true);
            foreach (var frame in StackTraceRewriter.Rewrite(trace.GetFrames().Where(f => f.HasMethod()).Select(f => new StackFrame(f))))
            {
                var method = frame.Method;
                var methodDisplay = FormatMethodSignature(method);

                if (methodDisplay == null)
                {
                    continue;
                }

                builder.Append("  + ");
                builder.Append(methodDisplay);

                var fileName = frame.FileName;
                if (fileName != null)
                {
                    builder.Append(string.Format(CultureInfo.CurrentUICulture, ScriptingResources.AtFileLine, fileName, frame.FileLineNumber));
                }

                builder.AppendLine();
            }

            return pooled.ToStringAndFree();
        }

        /// <summary>
        /// Returns a method signature display string. Used to display stack frames.
        /// </summary>
        /// <returns>Null if the method is a compiler generated method that shouldn't be displayed to the user.</returns>
        internal virtual string FormatMethodSignature(MethodBase method)
        {
            var declaringType = method.DeclaringType;

            if (IsHiddenMember(declaringType.GetTypeInfo()) ||
                IsHiddenMember(method) ||
                method.GetCustomAttributes<DebuggerHiddenAttribute>().Any() ||
                declaringType.GetTypeInfo().GetCustomAttributes<DebuggerHiddenAttribute>().Any())
            {
                return null;
            }

            var options = new CommonTypeNameFormatter.Options(useHexadecimalArrayBounds: false, showNamespaces: true);

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            builder.Append(TypeNameFormatter.FormatTypeName(declaringType, options));
            builder.Append('.');
            builder.Append(method.Name);
            builder.Append(TypeNameFormatter.FormatTypeArguments(method.GetGenericArguments(), options));

            builder.Append('(');

            bool first = true;
            foreach (var parameter in method.GetParameters())
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }

                builder.Append(FormatRefKind(parameter));
                builder.Append(TypeNameFormatter.FormatTypeName(parameter.ParameterType, options));
            }

            builder.Append(')');

            return pooled.ToStringAndFree();
        }

        /// <summary>
        /// Returns true if the member shouldn't be displayed (e.g. it's a compiler generated field).
        /// </summary>
        protected abstract bool IsHiddenMember(MemberInfo member);

        protected abstract string FormatRefKind(ParameterInfo parameter);
    }
}
