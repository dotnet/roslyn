// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    public class CSharpTypeNameFormatter : CommonTypeNameFormatter
    {
        protected override CommonPrimitiveFormatter PrimitiveFormatter { get; }

        public CSharpTypeNameFormatter(CommonPrimitiveFormatter primitiveFormatter)
        {
            PrimitiveFormatter = primitiveFormatter;
        }

        protected override string GenericParameterOpening => "<";
        protected override string GenericParameterClosing => ">";
        protected override string ArrayOpening => "[";
        protected override string ArrayClosing => "]";

        protected override string GetPrimitiveTypeName(SpecialType type)
        {
            switch (type)
            {
                case SpecialType.System_Boolean: return "bool";
                case SpecialType.System_Byte: return "byte";
                case SpecialType.System_Char: return "char";
                case SpecialType.System_Decimal: return "decimal";
                case SpecialType.System_Double: return "double";
                case SpecialType.System_Int16: return "short";
                case SpecialType.System_Int32: return "int";
                case SpecialType.System_Int64: return "long";
                case SpecialType.System_SByte: return "sbyte";
                case SpecialType.System_Single: return "float";
                case SpecialType.System_String: return "string";
                case SpecialType.System_UInt16: return "ushort";
                case SpecialType.System_UInt32: return "uint";
                case SpecialType.System_UInt64: return "ulong";
                case SpecialType.System_Object: return "object";

                default:
                    return null;
            }
        }

        public override string FormatTypeName(Type type, bool useHexadecimalArrayBounds)
        {
            string stateMachineName;
            if (GeneratedNames.TryParseSourceMethodNameFromGeneratedName(type.Name, GeneratedNameKind.StateMachineType, out stateMachineName))
            {
                return stateMachineName;
            }

            return base.FormatTypeName(type, useHexadecimalArrayBounds);
        }
    }
}
