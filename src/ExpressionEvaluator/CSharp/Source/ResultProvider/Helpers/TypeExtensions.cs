// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;
using TypeCode = Microsoft.VisualStudio.Debugger.Metadata.TypeCode;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal static class TypeExtensions
    {
        internal static bool IsPredefinedType(this Type type)
        {
            return type.GetPredefinedTypeName() != null;
        }

        internal static string GetPredefinedTypeName(this Type type)
        {
            if (type.IsEnum)
            {
                return null;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    if (type.IsObject())
                    {
                        return "object";
                    }
                    return null;
                case TypeCode.Boolean:
                    return "bool";
                case TypeCode.Char:
                    return "char";
                case TypeCode.SByte:
                    return "sbyte";
                case TypeCode.Byte:
                    return "byte";
                case TypeCode.Int16:
                    return "short";
                case TypeCode.UInt16:
                    return "ushort";
                case TypeCode.Int32:
                    return "int";
                case TypeCode.UInt32:
                    return "uint";
                case TypeCode.Int64:
                    return "long";
                case TypeCode.UInt64:
                    return "ulong";
                case TypeCode.Single:
                    return "float";
                case TypeCode.Double:
                    return "double";
                case TypeCode.Decimal:
                    return "decimal";
                case TypeCode.String:
                    return "string";
                default:
                    return null;
            }
        }
    }
}
