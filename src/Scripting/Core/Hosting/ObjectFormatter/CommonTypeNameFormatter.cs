// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    using static ObjectFormatterHelpers;
    using TypeInfo = System.Reflection.TypeInfo;

    internal abstract partial class CommonTypeNameFormatter
    {
        protected abstract string GetPrimitiveTypeName(SpecialType type);

        protected abstract string GenericParameterOpening { get; }
        protected abstract string GenericParameterClosing { get; }

        protected abstract string ArrayOpening { get; }
        protected abstract string ArrayClosing { get; }

        protected abstract CommonPrimitiveFormatter PrimitiveFormatter { get; }

        // TODO (tomat): Use DebuggerDisplay.Type if specified?
        public virtual string FormatTypeName(Type type, CommonTypeNameFormatterOptions options)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            string primitiveTypeName = GetPrimitiveTypeName(GetPrimitiveSpecialType(type));
            if (primitiveTypeName != null)
            {
                return primitiveTypeName;
            }

            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (type.IsArray)
            {
                return FormatArrayTypeName(type, arrayOpt: null, options: options);
            }

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType)
            {
                return FormatGenericTypeName(typeInfo, options);
            }

            return FormatNonGenericTypeName(typeInfo, options);
        }

        private static string FormatNonGenericTypeName(TypeInfo typeInfo, CommonTypeNameFormatterOptions options)
        {
            if (options.ShowNamespaces)
            {
                return typeInfo.FullName.Replace('+', '.');
            }

            if (typeInfo.DeclaringType == null)
            {
                return typeInfo.Name;
            }

            var stack = ArrayBuilder<string>.GetInstance();

            do
            {
                stack.Push(typeInfo.Name);
                typeInfo = typeInfo.DeclaringType?.GetTypeInfo();
            } while (typeInfo != null);

            stack.ReverseContents();
            var typeName = string.Join(".", stack);
            stack.Free();

            return typeName;
        }

        public virtual string FormatTypeArguments(Type[] typeArguments, CommonTypeNameFormatterOptions options)
        {
            if (typeArguments == null)
            {
                throw new ArgumentNullException(nameof(typeArguments));
            }

            if (typeArguments.Length == 0)
            {
                return "";
            }

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            builder.Append(GenericParameterOpening);

            var first = true;
            foreach (var typeArgument in typeArguments)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }

                builder.Append(FormatTypeName(typeArgument, options));
            }

            builder.Append(GenericParameterClosing);

            return pooled.ToStringAndFree();
        }

        /// <summary>
        /// Formats an array type name (vector or multidimensional).
        /// </summary>
        public virtual string FormatArrayTypeName(Type arrayType, Array arrayOpt, CommonTypeNameFormatterOptions options)
        {
            if (arrayType == null)
            {
                throw new ArgumentNullException(nameof(arrayType));
            }

            StringBuilder sb = new StringBuilder();

            // print the inner-most element type first:
            Type elementType = arrayType.GetElementType();
            while (elementType.IsArray)
            {
                elementType = elementType.GetElementType();
            }

            sb.Append(FormatTypeName(elementType, options));

            // print all components of a jagged array:
            Type type = arrayType;
            do
            {
                if (arrayOpt != null)
                {
                    sb.Append(ArrayOpening);

                    int rank = type.GetArrayRank();

                    bool anyNonzeroLowerBound = false;
                    for (int i = 0; i < rank; i++)
                    {
                        if (arrayOpt.GetLowerBound(i) > 0)
                        {
                            anyNonzeroLowerBound = true;
                            break;
                        }
                    }

                    for (int i = 0; i < rank; i++)
                    {
                        int lowerBound = arrayOpt.GetLowerBound(i);
                        int length = arrayOpt.GetLength(i);

                        if (i > 0)
                        {
                            sb.Append(", ");
                        }

                        if (anyNonzeroLowerBound)
                        {
                            AppendArrayBound(sb, lowerBound, options.ArrayBoundRadix);
                            sb.Append("..");
                            AppendArrayBound(sb, length + lowerBound, options.ArrayBoundRadix);
                        }
                        else
                        {
                            AppendArrayBound(sb, length, options.ArrayBoundRadix);
                        }
                    }

                    sb.Append(ArrayClosing);
                    arrayOpt = null;
                }
                else
                {
                    AppendArrayRank(sb, type);
                }

                type = type.GetElementType();
            }
            while (type.IsArray);

            return sb.ToString();
        }

        private void AppendArrayBound(StringBuilder sb, long bound, int numberRadix)
        {
            var options = new CommonPrimitiveFormatterOptions(
                numberRadix: numberRadix,
                includeCodePoints: false,
                quoteStringsAndCharacters: true,
                escapeNonPrintableCharacters: true,
                cultureInfo: CultureInfo.InvariantCulture);
            var formatted = int.MinValue <= bound && bound <= int.MaxValue
                ? PrimitiveFormatter.FormatPrimitive((int)bound, options)
                : PrimitiveFormatter.FormatPrimitive(bound, options);
            sb.Append(formatted);
        }

        private void AppendArrayRank(StringBuilder sb, Type arrayType)
        {
            sb.Append('[');
            int rank = arrayType.GetArrayRank();
            if (rank > 1)
            {
                sb.Append(',', rank - 1);
            }
            sb.Append(']');
        }

        private string FormatGenericTypeName(TypeInfo typeInfo, CommonTypeNameFormatterOptions options)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;

            // consolidated generic arguments (includes arguments of all declaring types):
            // TODO (DevDiv #173210): shouldn't need parameters, but StackTrace gives us unconstructed symbols.
            Type[] genericArguments = typeInfo.IsGenericTypeDefinition ? typeInfo.GenericTypeParameters : typeInfo.GenericTypeArguments;

            if (typeInfo.DeclaringType != null)
            {
                var nestedTypes = ArrayBuilder<TypeInfo>.GetInstance();
                do
                {
                    nestedTypes.Add(typeInfo);
                    typeInfo = typeInfo.DeclaringType?.GetTypeInfo();
                }
                while (typeInfo != null);

                if (options.ShowNamespaces)
                {
                    var @namespace = nestedTypes.Last().Namespace;
                    if (@namespace != null)
                    {
                        builder.Append(@namespace + ".");
                    }
                }

                int typeArgumentIndex = 0;
                for (int i = nestedTypes.Count - 1; i >= 0; i--)
                {
                    AppendTypeInstantiation(builder, nestedTypes[i], genericArguments, ref typeArgumentIndex, options);
                    if (i > 0)
                    {
                        builder.Append('.');
                    }
                }

                nestedTypes.Free();
            }
            else
            {
                int typeArgumentIndex = 0;
                AppendTypeInstantiation(builder, typeInfo, genericArguments, ref typeArgumentIndex, options);
            }

            return pooledBuilder.ToStringAndFree();
        }

        private void AppendTypeInstantiation(
            StringBuilder builder,
            TypeInfo typeInfo,
            Type[] genericArguments,
            ref int genericArgIndex,
            CommonTypeNameFormatterOptions options)
        {
            // generic arguments of all the outer types and the current type;
            int currentArgCount = (typeInfo.IsGenericTypeDefinition ? typeInfo.GenericTypeParameters.Length : typeInfo.GenericTypeArguments.Length) - genericArgIndex;

            if (currentArgCount > 0)
            {
                string name = typeInfo.Name;

                int backtick = name.IndexOf('`');
                if (backtick > 0)
                {
                    builder.Append(name.Substring(0, backtick));
                }
                else
                {
                    builder.Append(name);
                }

                builder.Append(GenericParameterOpening);

                for (int i = 0; i < currentArgCount; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(FormatTypeName(genericArguments[genericArgIndex++], options));
                }

                builder.Append(GenericParameterClosing);
            }
            else
            {
                builder.Append(typeInfo.Name);
            }
        }
    }
}
