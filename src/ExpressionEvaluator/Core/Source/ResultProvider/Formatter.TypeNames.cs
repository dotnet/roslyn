// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    // Implementation for "displaying type name as string" aspect of default (C#) Formatter component
    internal abstract partial class Formatter
    {
        /// <returns>The qualified name (i.e. including containing types and namespaces) of a named,
        /// pointer, or array type.</returns>
        internal string GetTypeName(TypeAndCustomInfo typeAndInfo, bool escapeKeywordIdentifiers = false)
        {
            var type = typeAndInfo.Type;
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var dynamicFlags = new DynamicFlagsCustomTypeInfo(typeAndInfo.Info);
            var index = 0;
            var pooled = PooledStringBuilder.GetInstance();
            AppendQualifiedTypeName(pooled.Builder, type, dynamicFlags, ref index, escapeKeywordIdentifiers);
            return pooled.ToStringAndFree();
        }

        /// <summary>
        /// Append the qualified name (i.e. including containing types and namespaces) of a named,
        /// pointer, or array type to <paramref name="builder"/>.
        /// </summary>
        /// <remarks>
        /// Keyword strings are appended for primitive types (e.g. "int" for "System.Int32").
        /// Question mark syntax is used for <see cref="Nullable{T}"/>.
        /// No special handling is required for anonymous types - they are expected to be
        /// emitted with <see cref="DebuggerDisplayAttribute.Type"/> set to "&lt;Anonymous Type&gt;.
        /// This is fortunate, since we don't have a good way to recognize them in metadata.
        /// Does not call itself (directly).
        /// </remarks>
        protected void AppendQualifiedTypeName(StringBuilder builder, Type type, DynamicFlagsCustomTypeInfo dynamicFlags, ref int index, bool escapeKeywordIdentifiers)
        {
            Type originalType = type;

            // Can have an array of pointers, but not a pointer to an array, so consume these first.
            // We'll reconstruct this information later from originalType.
            while (type.IsArray)
            {
                index++;
                type = type.GetElementType();
            }

            int pointerCount = 0;
            while (type.IsPointer)
            {
                index++;
                pointerCount++;
                type = type.GetElementType();
            }

            int nullableCount = 0;
            Type typeArg;
            while ((typeArg = type.GetNullableTypeArgument()) != null)
            {
                index++;
                nullableCount++;
                type = typeArg;
            }
            Debug.Assert(nullableCount < 2, "Benign: someone is nesting nullables.");

            Debug.Assert(pointerCount == 0 || nullableCount == 0, "Benign: pointer to nullable?");

            int oldLength = builder.Length;
            AppendQualifiedTypeNameInternal(builder, type, dynamicFlags, ref index, escapeKeywordIdentifiers);
            string name = builder.ToString(oldLength, builder.Length - oldLength);

            builder.Append('?', nullableCount);
            builder.Append('*', pointerCount);

            type = originalType;
            while (type.IsArray)
            {
                AppendRankSpecifier(builder, type.GetArrayRank());

                type = type.GetElementType();
            }
        }

        /// <summary>
        /// Append the qualified name (i.e. including containing types and namespaces) of a named type
        /// (i.e. not a pointer or array type) to <paramref name="builder"/>.
        /// </summary>
        /// <remarks>
        /// Keyword strings are appended for primitive types (e.g. "int" for "System.Int32").
        /// </remarks>
        /// <remarks>
        /// Does not call itself or <see cref="AppendQualifiedTypeName"/> (directly).
        /// </remarks>
        private void AppendQualifiedTypeNameInternal(StringBuilder builder, Type type, DynamicFlagsCustomTypeInfo dynamicFlags, ref int index, bool escapeKeywordIdentifiers)
        {
            var isDynamic = dynamicFlags[index++] && type.IsObject();
            if (AppendSpecialTypeName(builder, type, isDynamic, escapeKeywordIdentifiers))
            {
                return;
            }
            Debug.Assert(!isDynamic, $"Dynamic should have been handled by {nameof(AppendSpecialTypeName)}");

            Debug.Assert(!IsPredefinedType(type));

            // Note: in the Reflection/LMR object model, all type arguments are on the most nested type.
            var hasTypeArguments = type.IsGenericType;
            var typeArguments = type.IsGenericType
                ? type.GetGenericArguments()
                : null;
            Debug.Assert(hasTypeArguments == (typeArguments != null));

            var numTypeArguments = hasTypeArguments ? typeArguments.Length : 0;

            if (type.IsNested)
            {
                // Push from inside, out.
                var stack = ArrayBuilder<Type>.GetInstance();
                {
                    var containingType = type.DeclaringType;
                    while (containingType != null)
                    {
                        stack.Add(containingType);
                        containingType = containingType.DeclaringType;
                    }
                }

                var lastContainingTypeIndex = stack.Count - 1;

                AppendNamespacePrefix(builder, stack[lastContainingTypeIndex], escapeKeywordIdentifiers);

                var typeArgumentOffset = 0;

                // Pop from outside, in.
                for (int i = lastContainingTypeIndex; i >= 0; i--)
                {
                    var containingType = stack[i];

                    // ACASEY: I explored the type in the debugger and couldn't find the arity stored/exposed separately.
                    int arity = hasTypeArguments ? containingType.GetGenericArguments().Length - typeArgumentOffset : 0;

                    AppendUnqualifiedTypeName(builder, containingType, dynamicFlags, ref index, escapeKeywordIdentifiers, typeArguments, typeArgumentOffset, arity);
                    builder.Append('.');

                    typeArgumentOffset += arity;
                }

                stack.Free();

                AppendUnqualifiedTypeName(builder, type, dynamicFlags, ref index, escapeKeywordIdentifiers, typeArguments, typeArgumentOffset, numTypeArguments - typeArgumentOffset);
            }
            else
            {
                AppendNamespacePrefix(builder, type, escapeKeywordIdentifiers);
                AppendUnqualifiedTypeName(builder, type, dynamicFlags, ref index, escapeKeywordIdentifiers, typeArguments, 0, numTypeArguments);
            }
        }

        /// <summary>
        /// Helper for appending the qualified name of the containing namespace of a type.
        /// NOTE: Unless the qualified name is empty, there will always be a trailing dot.
        /// </summary>
        private void AppendNamespacePrefix(StringBuilder builder, Type type, bool escapeKeywordIdentifiers)
        {
            var @namespace = type.Namespace;
            if (!string.IsNullOrEmpty(@namespace))
            {
                if (escapeKeywordIdentifiers && @namespace.Contains("."))
                {
                    var pooled = PooledStringBuilder.GetInstance();
                    var identifierBuilder = pooled.Builder;
                    foreach (var ch in @namespace)
                    {
                        if (ch == '.')
                        {
                            AppendIdentifier(builder, escapeKeywordIdentifiers, identifierBuilder.ToString());
                            builder.Append(ch);
                            identifierBuilder.Clear();
                        }
                        else
                        {
                            identifierBuilder.Append(ch);
                        }
                    }
                    AppendIdentifier(builder, escapeKeywordIdentifiers, identifierBuilder.ToString());
                    pooled.Free();
                }
                else
                {
                    AppendIdentifier(builder, escapeKeywordIdentifiers, @namespace);
                }
                builder.Append('.');
            }
        }

        /// <summary>
        /// Append the name of the type and its type arguments.  Do not append the type's containing type or namespace.
        /// </summary>
        /// <param name="builder">Builder to which the name will be appended.</param>
        /// <param name="type">Type, the name of which will be appended.</param>
        /// <param name="dynamicFlags">Flags indicating which occurrences of &quot;object&quot; need to be replaced by &quot;dynamic&quot;.</param>
        /// <param name="index">Current index into <paramref name="dynamicFlags"/>.</param>
        /// <param name="escapeKeywordIdentifiers">True if identifiers that are also keywords should be prefixed with '@'.</param>
        /// <param name="typeArguments">
        /// The type arguments of the type passed to <see cref="AppendQualifiedTypeNameInternal"/>, which might be nested
        /// within <paramref name="type"/>.  In the Reflection/LMR object model, all type arguments are passed to the
        /// most nested type.  To get back to the C# model, we have to propagate them out to containing types.
        /// </param>
        /// <param name="typeArgumentOffset">
        /// The first position in <paramref name="typeArguments"/> that is a type argument to <paramref name="type"/>,
        /// from a C# perspective.
        /// </param>
        /// <param name="arity">
        /// The number of type parameters of <paramref name="type"/>, from a C# perspective.
        /// </param>
        /// <remarks>
        /// We're passing the full array plus bounds, rather than a tailored array, to avoid creating a lot of short-lived
        /// temporary arrays.
        /// </remarks>
        private void AppendUnqualifiedTypeName(StringBuilder builder, Type type, DynamicFlagsCustomTypeInfo dynamicFlags, ref int index, bool escapeKeywordIdentifiers, Type[] typeArguments, int typeArgumentOffset, int arity)
        {
            if (typeArguments == null || arity == 0)
            {
                AppendIdentifier(builder, escapeKeywordIdentifiers, type.Name);
                return;
            }

            var mangledName = type.Name;
            var separatorIndex = mangledName.IndexOf('`');
            AppendIdentifier(builder, escapeKeywordIdentifiers, mangledName, separatorIndex);

            AppendGenericTypeArgumentList(builder, typeArguments, typeArgumentOffset, dynamicFlags, ref index, arity, escapeKeywordIdentifiers);
        }

        protected void AppendIdentifier(StringBuilder builder, bool escapeKeywordIdentifiers, string identifier, int length = -1)
        {
            Debug.Assert(length < 0 || (0 <= length && length <= identifier.Length));

            if (escapeKeywordIdentifiers)
            {
                if (length >= 0)
                {
                    identifier = identifier.Substring(0, length);
                }

                AppendIdentifierEscapingPotentialKeywords(builder, identifier);
            }
            else if (length < 0)
            {
                builder.Append(identifier);
            }
            else
            {
                builder.Append(identifier, 0, length);
            }
        }

        internal string GetIdentifierEscapingPotentialKeywords(string identifier)
        {
            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;
            AppendIdentifierEscapingPotentialKeywords(builder, identifier);
            return pooled.ToStringAndFree();
        }

        #region Language-specific type name formatting behavior

        protected abstract void AppendIdentifierEscapingPotentialKeywords(StringBuilder builder, string identifier);

        protected abstract void AppendGenericTypeArgumentList(
            StringBuilder builder, 
            Type[] typeArguments, 
            int typeArgumentOffset,
            DynamicFlagsCustomTypeInfo dynamicFlags,
            ref int index,
            int arity, 
            bool escapeKeywordIdentifiers);

        protected abstract void AppendRankSpecifier(StringBuilder builder, int rank);

        protected abstract bool AppendSpecialTypeName(StringBuilder builder, Type type, bool isDynamic, bool escapeKeywordIdentifiers);

        #endregion
    }
}
