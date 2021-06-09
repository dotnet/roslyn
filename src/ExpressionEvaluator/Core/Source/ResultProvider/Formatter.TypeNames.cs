// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    // Implementation for "displaying type name as string" aspect of the Formatter component
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

            ReadOnlyCollection<byte> dynamicFlags = null;
            ReadOnlyCollection<string> tupleElementNames = null;
            var typeInfo = typeAndInfo.Info;
            if (typeInfo != null)
            {
                CustomTypeInfo.Decode(typeInfo.PayloadTypeId, typeInfo.Payload, out dynamicFlags, out tupleElementNames);
            }

            var dynamicFlagIndex = 0;
            var tupleElementIndex = 0;
            var pooled = PooledStringBuilder.GetInstance();
            AppendQualifiedTypeName(
                pooled.Builder,
                type,
                dynamicFlags,
                ref dynamicFlagIndex,
                tupleElementNames,
                ref tupleElementIndex,
                escapeKeywordIdentifiers,
                out sawInvalidIdentifier);
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
        /// </remarks>
        protected void AppendQualifiedTypeName(
            StringBuilder builder,
            Type type,
            ReadOnlyCollection<byte> dynamicFlags,
            ref int dynamicFlagIndex,
            ReadOnlyCollection<string> tupleElementNames,
            ref int tupleElementIndex,
            bool escapeKeywordIdentifiers,
            out bool sawInvalidIdentifier)
        {
            Type originalType = type;

            // Can have an array of pointers, but not a pointer to an array, so consume these first.
            // We'll reconstruct this information later from originalType.
            while (type.IsArray)
            {
                dynamicFlagIndex++;
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
                dynamicFlagIndex++;
                pointerCount++;
                type = elementType;
            }

            int nullableCount = 0;
            Type typeArg;
            while ((typeArg = type.GetNullableTypeArgument()) != null)
            {
                dynamicFlagIndex++;
                nullableCount++;
                type = typeArg;
            }
            Debug.Assert(nullableCount < 2, "Benign: someone is nesting nullables.");

            Debug.Assert(pointerCount == 0 || nullableCount == 0, "Benign: pointer to nullable?");

            AppendQualifiedTypeNameInternal(
                builder,
                type,
                dynamicFlags,
                ref dynamicFlagIndex,
                tupleElementNames,
                ref tupleElementIndex,
                escapeKeywordIdentifiers,
                out sawInvalidIdentifier);

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
            ReadOnlyCollection<byte> dynamicFlags,
            ref int dynamicFlagIndex,
            ReadOnlyCollection<string> tupleElementNames,
            ref int tupleElementIndex,
            bool escapeKeywordIdentifiers,
            out bool sawInvalidIdentifier)
        {
            var isDynamic = DynamicFlagsCustomTypeInfo.GetFlag(dynamicFlags, dynamicFlagIndex++) && type.IsObject();
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

            int cardinality;
            if (type.IsTupleCompatible(out cardinality))
            {
                if (cardinality == 1)
                {
                    // Not displayed as a tuple but is included in tuple element names.
                    tupleElementIndex++;
                }
                else
                {
                    AppendTupleElements(
                        builder,
                        type,
                        cardinality,
                        dynamicFlags,
                        ref dynamicFlagIndex,
                        tupleElementNames,
                        ref tupleElementIndex,
                        escapeKeywordIdentifiers,
                        out sawInvalidIdentifier);
                    return;
                }
            }

            // Note: in the Reflection/LMR object model, all type arguments are on the most nested type.
            var hasTypeArguments = type.IsGenericType;
            var typeArguments = hasTypeArguments
                ? type.GetGenericArguments()
                : null;
            Debug.Assert(hasTypeArguments == (typeArguments != null));

            var numTypeArguments = hasTypeArguments ? typeArguments.Length : 0;

            sawInvalidIdentifier = false;
            bool sawSingleInvalidIdentifier;
            var typeArgumentOffset = 0;

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

                // Pop from outside, in.
                for (int i = lastContainingTypeIndex; i >= 0; i--)
                {
                    var containingType = stack[i];

                    // ACASEY: I explored the type in the debugger and couldn't find the arity stored/exposed separately.
                    int arity = hasTypeArguments ? containingType.GetGenericArguments().Length - typeArgumentOffset : 0;

                    AppendUnqualifiedTypeName(
                        builder,
                        containingType,
                        dynamicFlags,
                        ref dynamicFlagIndex,
                        tupleElementNames,
                        ref tupleElementIndex,
                        escapeKeywordIdentifiers,
                        typeArguments,
                        typeArgumentOffset,
                        arity,
                        out sawSingleInvalidIdentifier);
                    sawInvalidIdentifier |= sawSingleInvalidIdentifier;
                    builder.Append('.');

                    typeArgumentOffset += arity;
                }

                stack.Free();
            }
            else
            {
                AppendNamespacePrefix(builder, type, escapeKeywordIdentifiers, out sawSingleInvalidIdentifier);
                sawInvalidIdentifier |= sawSingleInvalidIdentifier;
            }

            AppendUnqualifiedTypeName(
                builder,
                type,
                dynamicFlags,
                ref dynamicFlagIndex,
                tupleElementNames,
                ref tupleElementIndex,
                escapeKeywordIdentifiers,
                typeArguments,
                typeArgumentOffset,
                numTypeArguments - typeArgumentOffset,
                out sawSingleInvalidIdentifier);
            sawInvalidIdentifier |= sawSingleInvalidIdentifier;
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
        /// <param name="dynamicFlagIndex">Current index into <paramref name="dynamicFlags"/>.</param>
        /// <param name="tupleElementNames">Non-default tuple names.</param>
        /// <param name="tupleElementIndex">Current index into <paramref name="tupleElementNames"/>.</param>
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
            ReadOnlyCollection<byte> dynamicFlags,
            ref int dynamicFlagIndex,
            ReadOnlyCollection<string> tupleElementNames,
            ref int tupleElementIndex,
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
            AppendGenericTypeArguments(
                builder,
                typeArguments,
                typeArgumentOffset,
                dynamicFlags,
                ref dynamicFlagIndex,
                tupleElementNames,
                ref tupleElementIndex,
                arity,
                escapeKeywordIdentifiers,
                out argumentsSawInvalidIdentifier);
            sawInvalidIdentifier |= argumentsSawInvalidIdentifier;
        }

        private void AppendTupleElements(
            StringBuilder builder,
            Type type,
            int cardinality,
            ReadOnlyCollection<byte> dynamicFlags,
            ref int dynamicFlagIndex,
            ReadOnlyCollection<string> tupleElementNames,
            ref int tupleElementIndex,
            bool escapeKeywordIdentifiers,
            out bool sawInvalidIdentifier)
        {
            sawInvalidIdentifier = false;
#if DEBUG
            int lastNameIndex = tupleElementIndex + cardinality;
#endif
            int nameIndex = tupleElementIndex;
            builder.Append('(');
            bool any = false;
            while (true)
            {
                tupleElementIndex += cardinality;
                var typeArguments = type.GetGenericArguments();
                int nTypeArgs = typeArguments.Length;
                Debug.Assert(nTypeArgs > 0);
                Debug.Assert(nTypeArgs <= TypeHelpers.TupleFieldRestPosition);
                int nFields = Math.Min(nTypeArgs, TypeHelpers.TupleFieldRestPosition - 1);
                for (int i = 0; i < nFields; i++)
                {
                    if (any)
                    {
                        builder.Append(", ");
                    }
                    bool sawSingleInvalidIdentifier;
                    var name = CustomTypeInfo.GetTupleElementNameIfAny(tupleElementNames, nameIndex);
                    nameIndex++;
                    AppendTupleElement(
                        builder,
                        typeArguments[i],
                        name,
                        dynamicFlags,
                        ref dynamicFlagIndex,
                        tupleElementNames,
                        ref tupleElementIndex,
                        escapeKeywordIdentifiers,
                        sawInvalidIdentifier: out sawSingleInvalidIdentifier);
                    sawInvalidIdentifier |= sawSingleInvalidIdentifier;
                    any = true;
                }
                if (nTypeArgs < TypeHelpers.TupleFieldRestPosition)
                {
                    break;
                }
                Debug.Assert(!DynamicFlagsCustomTypeInfo.GetFlag(dynamicFlags, dynamicFlagIndex));
                dynamicFlagIndex++;
                type = typeArguments[nTypeArgs - 1];
                cardinality = type.GetTupleCardinalityIfAny();
            }
#if DEBUG
            Debug.Assert(nameIndex == lastNameIndex);
#endif
            builder.Append(')');
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

        #region Language-specific type name formatting behavior

        protected abstract void AppendIdentifierEscapingPotentialKeywords(StringBuilder builder, string identifier, out bool sawInvalidIdentifier);

        protected abstract void AppendGenericTypeArguments(
            StringBuilder builder,
            Type[] typeArguments,
            int typeArgumentOffset,
            ReadOnlyCollection<byte> dynamicFlags,
            ref int dynamicFlagIndex,
            ReadOnlyCollection<string> tupleElementNames,
            ref int tupleElementIndex,
            int arity,
            bool escapeKeywordIdentifiers,
            out bool sawInvalidIdentifier);

        protected abstract void AppendTupleElement(
            StringBuilder builder,
            Type type,
            string nameOpt,
            ReadOnlyCollection<byte> dynamicFlags,
            ref int dynamicFlagIndex,
            ReadOnlyCollection<string> tupleElementNames,
            ref int tupleElementIndex,
            bool escapeKeywordIdentifiers,
            out bool sawInvalidIdentifier);

        protected abstract void AppendRankSpecifier(StringBuilder builder, int rank);

        protected abstract bool AppendSpecialTypeName(StringBuilder builder, Type type, bool isDynamic);

        #endregion
    }
}
