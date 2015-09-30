// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    // Implementation for "displaying type name as string" aspect of default (C#) Formatter component
    internal abstract partial class Formatter
    {
        /// <returns>The qualified name (i.e. including containing types and namespaces) of a named,
        /// pointer, or array type.</returns>
        internal string GetTypeName(TypeAndCustomInfo typeAndInfo, bool escapeKeywordIdentifiers, out bool sawInvalidIdentifier)
        {
            var type = typeAndInfo.Type;
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var dynamicFlags = DynamicFlagsCustomTypeInfo.Create(typeAndInfo.Info);
            var index = 0;
            var pooled = PooledStringBuilder.GetInstance();
            AppendQualifiedTypeName(pooled.Builder, type, dynamicFlags, ref index, escapeKeywordIdentifiers, out sawInvalidIdentifier);
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
        protected void AppendQualifiedTypeName(
            StringBuilder builder,
            Type type,
            DynamicFlagsCustomTypeInfo dynamicFlags,
            ref int index,
            bool escapeKeywordIdentifiers,
            out bool sawInvalidIdentifier)
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
                var elementType = type.GetElementType();
                if (elementType == null)
                {
                    // Null for function pointers.
                    break;
                }
                index++;
                pointerCount++;
                type = elementType;
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
            AppendQualifiedTypeNameInternal(builder, type, dynamicFlags, ref index, escapeKeywordIdentifiers, out sawInvalidIdentifier);
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
        private void AppendQualifiedTypeNameInternal(
            StringBuilder builder,
            Type type,
            DynamicFlagsCustomTypeInfo dynamicFlags,
            ref int index,
            bool escapeKeywordIdentifiers,
            out bool sawInvalidIdentifier)
        {
            var isDynamic = dynamicFlags[index++] && type.IsObject();
            if (AppendSpecialTypeName(builder, type, isDynamic))
            {
                sawInvalidIdentifier = false;
                return;
            }

            Debug.Assert(!isDynamic, $"Dynamic should have been handled by {nameof(AppendSpecialTypeName)}");
            Debug.Assert(!IsPredefinedType(type));

            if (type.IsGenericParameter)
            {
                AppendIdentifier(builder, escapeKeywordIdentifiers, type.Name, out sawInvalidIdentifier);
                return;
            }

            // Note: in the Reflection/LMR object model, all type arguments are on the most nested type.
            var hasTypeArguments = type.IsGenericType;
            var typeArguments = type.IsGenericType
                ? type.GetGenericArguments()
                : null;
            Debug.Assert(hasTypeArguments == (typeArguments != null));

            var numTypeArguments = hasTypeArguments ? typeArguments.Length : 0;

            sawInvalidIdentifier = false;
            bool sawSingleInvalidIdentifier;
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

                AppendNamespacePrefix(builder, stack[lastContainingTypeIndex], escapeKeywordIdentifiers, out sawSingleInvalidIdentifier);
                sawInvalidIdentifier |= sawSingleInvalidIdentifier;

                var typeArgumentOffset = 0;

                // Pop from outside, in.
                for (int i = lastContainingTypeIndex; i >= 0; i--)
                {
                    var containingType = stack[i];

                    // ACASEY: I explored the type in the debugger and couldn't find the arity stored/exposed separately.
                    int arity = hasTypeArguments ? containingType.GetGenericArguments().Length - typeArgumentOffset : 0;

                    AppendUnqualifiedTypeName(builder, containingType, dynamicFlags, ref index, escapeKeywordIdentifiers, typeArguments, typeArgumentOffset, arity, out sawSingleInvalidIdentifier);
                    sawInvalidIdentifier |= sawSingleInvalidIdentifier;
                    builder.Append('.');

                    typeArgumentOffset += arity;
                }

                stack.Free();

                AppendUnqualifiedTypeName(builder, type, dynamicFlags, ref index, escapeKeywordIdentifiers, typeArguments, typeArgumentOffset, numTypeArguments - typeArgumentOffset, out sawSingleInvalidIdentifier);
                sawInvalidIdentifier |= sawSingleInvalidIdentifier;
            }
            else
            {
                AppendNamespacePrefix(builder, type, escapeKeywordIdentifiers, out sawSingleInvalidIdentifier);
                sawInvalidIdentifier |= sawSingleInvalidIdentifier;
                AppendUnqualifiedTypeName(builder, type, dynamicFlags, ref index, escapeKeywordIdentifiers, typeArguments, 0, numTypeArguments, out sawSingleInvalidIdentifier);
                sawInvalidIdentifier |= sawSingleInvalidIdentifier;
            }
        }

        /// <summary>
        /// Helper for appending the qualified name of the containing namespace of a type.
        /// NOTE: Unless the qualified name is empty, there will always be a trailing dot.
        /// </summary>
        private void AppendNamespacePrefix(StringBuilder builder, Type type, bool escapeKeywordIdentifiers, out bool sawInvalidIdentifier)
        {
            sawInvalidIdentifier = false;

            var @namespace = type.Namespace;
            if (!string.IsNullOrEmpty(@namespace))
            {
                if (@namespace.Contains("."))
                {
                    bool sawSingleInvalidIdentifier;
                    var pooled = PooledStringBuilder.GetInstance();
                    var identifierBuilder = pooled.Builder;
                    foreach (var ch in @namespace)
                    {
                        if (ch == '.')
                        {
                            AppendIdentifier(builder, escapeKeywordIdentifiers, identifierBuilder.ToString(), out sawSingleInvalidIdentifier);
                            sawInvalidIdentifier |= sawSingleInvalidIdentifier;
                            builder.Append(ch);
                            identifierBuilder.Clear();
                        }
                        else
                        {
                            identifierBuilder.Append(ch);
                        }
                    }
                    AppendIdentifier(builder, escapeKeywordIdentifiers, identifierBuilder.ToString(), out sawSingleInvalidIdentifier);
                    sawInvalidIdentifier |= sawSingleInvalidIdentifier;
                    pooled.Free();
                }
                else
                {
                    AppendIdentifier(builder, escapeKeywordIdentifiers, @namespace, out sawInvalidIdentifier);
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
        /// <param name="sawInvalidIdentifier">True if the name includes an invalid identifier (see <see cref="IsValidIdentifier"/>); false otherwise.</param>
        /// <remarks>
        /// We're passing the full array plus bounds, rather than a tailored array, to avoid creating a lot of short-lived
        /// temporary arrays.
        /// </remarks>
        private void AppendUnqualifiedTypeName(
            StringBuilder builder,
            Type type,
            DynamicFlagsCustomTypeInfo dynamicFlags,
            ref int index,
            bool escapeKeywordIdentifiers,
            Type[] typeArguments,
            int typeArgumentOffset,
            int arity,
            out bool sawInvalidIdentifier)
        {
            if (typeArguments == null || arity == 0)
            {
                AppendIdentifier(builder, escapeKeywordIdentifiers, type.Name, out sawInvalidIdentifier);
                return;
            }

            var mangledName = type.Name;
            var separatorIndex = mangledName.IndexOf('`');
            var unmangledName = separatorIndex < 0 ? mangledName : mangledName.Substring(0, separatorIndex);
            AppendIdentifier(builder, escapeKeywordIdentifiers, unmangledName, out sawInvalidIdentifier);

            bool argumentsSawInvalidIdentifier;
            AppendGenericTypeArgumentList(builder, typeArguments, typeArgumentOffset, dynamicFlags, ref index, arity, escapeKeywordIdentifiers, out argumentsSawInvalidIdentifier);
            sawInvalidIdentifier |= argumentsSawInvalidIdentifier;
        }

        protected void AppendIdentifier(StringBuilder builder, bool escapeKeywordIdentifiers, string identifier, out bool sawInvalidIdentifier)
        {
            if (escapeKeywordIdentifiers)
            {
                AppendIdentifierEscapingPotentialKeywords(builder, identifier, out sawInvalidIdentifier);
            }
            else
            {
                sawInvalidIdentifier = !IsValidIdentifier(identifier);
                builder.Append(identifier);
            }
        }

        internal string GetIdentifierEscapingPotentialKeywords(string identifier, out bool sawInvalidIdentifier)
        {
            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;
            AppendIdentifierEscapingPotentialKeywords(builder, identifier, out sawInvalidIdentifier);
            return pooled.ToStringAndFree();
        }

        #region Language-specific type name formatting behavior

        protected abstract void AppendIdentifierEscapingPotentialKeywords(StringBuilder builder, string identifier, out bool sawInvalidIdentifier);

        protected abstract void AppendGenericTypeArgumentList(
            StringBuilder builder,
            Type[] typeArguments,
            int typeArgumentOffset,
            DynamicFlagsCustomTypeInfo dynamicFlags,
            ref int index,
            int arity,
            bool escapeKeywordIdentifiers,
            out bool sawInvalidIdentifier);

        protected abstract void AppendRankSpecifier(StringBuilder builder, int rank);

        protected abstract bool AppendSpecialTypeName(StringBuilder builder, Type type, bool isDynamic);

        #endregion
    }
}
