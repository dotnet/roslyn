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
    using static ObjectFormatterHelpers;

    // TODO (acasey): input validation

    /// <summary>
    /// Object pretty printer.
    /// </summary>
    public abstract partial class CommonObjectFormatter : ObjectFormatter
    {
        public override string FormatObject(object obj)
        {
            var formatter = new Visitor(this, InternalBuilderOptions, PrimitiveOptions, MemberDisplayFormat);
            return formatter.FormatObject(obj);
        }

        internal virtual BuilderOptions InternalBuilderOptions =>
            new BuilderOptions(
                indentation: "  ",
                newLine: Environment.NewLine,
                ellipsis: "...",
                lineLengthLimit: int.MaxValue,
                totalLengthLimit: 1024);

        protected virtual CommonPrimitiveFormatter.Options PrimitiveOptions => default(CommonPrimitiveFormatter.Options);
        protected virtual MemberDisplayFormat MemberDisplayFormat => default(MemberDisplayFormat);

        protected abstract CommonTypeNameFormatter TypeNameFormatter { get; }
        protected abstract CommonPrimitiveFormatter PrimitiveFormatter { get; }

        protected abstract bool TryFormatCompositeObject(object obj, out string value, out bool suppressMembers);

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
            foreach (var frame in trace.GetFrames())
            {
                if (!frame.HasMethod())
                {
                    continue;
                }

                var method = frame.GetMethod();
                var type = method.DeclaringType;

                // TODO (https://github.com/dotnet/roslyn/issues/5250): look for other types indicating that we're in Roslyn code
                if (type == typeof(CommandLineRunner))
                {
                    break;
                }

                // TODO: we don't want to include awaiter helpers, shouldn't they be marked by DebuggerHidden in FX?
                if (IsTaskAwaiter(type) || IsTaskAwaiter(type.DeclaringType))
                {
                    continue;
                }

                string methodDisplay = FormatMethodSignature(method, PrimitiveOptions.UseHexadecimalNumbers);

                if (methodDisplay == null)
                {
                    continue;
                }

                builder.Append("  + ");
                builder.Append(methodDisplay);

                if (frame.HasSource())
                {
                    builder.Append(string.Format(CultureInfo.CurrentUICulture, ScriptingResources.AtFileLine, frame.GetFileName(), frame.GetFileLineNumber()));
                }

                builder.AppendLine();
            }

            return pooled.ToStringAndFree();
        }

        /// <summary>
        /// Returns a method signature display string. Used to display stack frames.
        /// </summary>
        /// <returns>Null if the method is a compiler generated method that shouldn't be displayed to the user.</returns>
        internal virtual string FormatMethodSignature(MethodBase method, bool useHexadecimalArrayBounds)
        {
            var declaringType = method.DeclaringType;

            if (IsHiddenMember(declaringType.GetTypeInfo()) ||
                IsHiddenMember(method) ||
                method.GetCustomAttributes<DebuggerHiddenAttribute>().Any() ||
                declaringType.GetTypeInfo().GetCustomAttributes<DebuggerHiddenAttribute>().Any())
            {
                return null;
            }

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            builder.Append(TypeNameFormatter.FormatTypeName(declaringType, useHexadecimalArrayBounds));
            builder.Append('.');
            builder.Append(method.Name);
            builder.Append(TypeNameFormatter.FormatTypeArguments(method.GetGenericArguments(), useHexadecimalArrayBounds));

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
                builder.Append(TypeNameFormatter.FormatTypeName(parameter.ParameterType, useHexadecimalArrayBounds));
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
