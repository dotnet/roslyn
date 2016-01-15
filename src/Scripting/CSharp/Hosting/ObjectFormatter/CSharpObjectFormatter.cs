// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    public sealed class CSharpObjectFormatter : ObjectFormatter
    {
        public static CSharpObjectFormatter Instance { get; } = new CSharpObjectFormatter();

        private CSharpObjectFormatter()
        {
        }

        internal override object VoidDisplayString => "<void>";
        internal override string NullLiteral => ObjectDisplay.NullLiteral;
        internal override string GenericParameterOpening => "<";
        internal override string GenericParameterClosing => ">";

        internal override string FormatLiteral(bool value)
        {
            return ObjectDisplay.FormatLiteral(value);
        }

        internal override string FormatLiteral(string value, bool quote, bool useHexadecimalNumbers = false)
        {
            var options = ObjectDisplayOptions.EscapeNonPrintableCharacters;
            if (quote)
            {
                options |= ObjectDisplayOptions.UseQuotes;
            }

            if (useHexadecimalNumbers)
            {
                options |= ObjectDisplayOptions.UseHexadecimalNumbers;
            }

            return ObjectDisplay.FormatLiteral(value, options);
        }

        internal override string FormatLiteral(char c, bool quote, bool includeCodePoints = false, bool useHexadecimalNumbers = false)
        {
            var options = ObjectDisplayOptions.EscapeNonPrintableCharacters;
            if (quote)
            {
                options |= ObjectDisplayOptions.UseQuotes;
            }

            if (includeCodePoints)
            {
                options |= ObjectDisplayOptions.IncludeCodePoints;
            }

            if (useHexadecimalNumbers)
            {
                options |= ObjectDisplayOptions.UseHexadecimalNumbers;
            }

            return ObjectDisplay.FormatLiteral(c, options);
        }

        internal override string FormatLiteral(sbyte value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        internal override string FormatLiteral(byte value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        internal override string FormatLiteral(short value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        internal override string FormatLiteral(ushort value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        internal override string FormatLiteral(int value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        internal override string FormatLiteral(uint value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        internal override string FormatLiteral(long value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        internal override string FormatLiteral(ulong value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        internal override string FormatLiteral(double value)
        {
            return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None);
        }

        internal override string FormatLiteral(float value)
        {
            return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None);
        }

        internal override string FormatLiteral(decimal value)
        {
            return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None);
        }

        internal override string FormatLiteral(DateTime value)
        {
            // DateTime is not primitive in C#
            return null;
        }

        internal override string GetPrimitiveTypeName(SpecialType type)
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

        internal override string FormatGeneratedTypeName(Type type)
        {
            string stateMachineName;
            if (GeneratedNames.TryParseSourceMethodNameFromGeneratedName(type.Name, GeneratedNameKind.StateMachineType, out stateMachineName))
            {
                return stateMachineName;
            }

            return null;
        }

        internal override string FormatArrayTypeName(Type arrayType, Array arrayOpt, ObjectFormattingOptions options)
        {
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
                    sb.Append('[');

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
                            AppendArrayBound(sb, lowerBound, options.UseHexadecimalNumbers);
                            sb.Append("..");
                            AppendArrayBound(sb, length + lowerBound, options.UseHexadecimalNumbers);
                        }
                        else
                        {
                            AppendArrayBound(sb, length, options.UseHexadecimalNumbers);
                        }
                    }

                    sb.Append(']');
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

        private void AppendArrayBound(StringBuilder sb, long bound, bool useHexadecimalNumbers)
        {
            if (bound <= int.MaxValue)
            {
                sb.Append(FormatLiteral((int)bound, useHexadecimalNumbers));
            }
            else
            {
                sb.Append(FormatLiteral(bound, useHexadecimalNumbers));
            }
        }

        private static void AppendArrayRank(StringBuilder sb, Type arrayType)
        {
            sb.Append('[');
            int rank = arrayType.GetArrayRank();
            if (rank > 1)
            {
                sb.Append(',', rank - 1);
            }
            sb.Append(']');
        }

        internal override string FormatMemberName(System.Reflection.MemberInfo member)
        {
            return member.Name;
        }

        internal override bool IsHiddenMember(System.Reflection.MemberInfo member)
        {
            // Generated fields, e.g. "<property_name>k__BackingField"
            return GeneratedNames.IsGeneratedMemberName(member.Name);
        }
    }
}
