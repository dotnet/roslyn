// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static partial class DiagnosticHelpers
    {
        public static bool TryConvertToUInt64(object? value, SpecialType specialType, out ulong convertedValue)
        {
            bool success = false;
            convertedValue = 0;
            if (value != null)
            {
                switch (specialType)
                {
                    case SpecialType.System_Int16:
                        convertedValue = unchecked((ulong)(short)value);
                        success = true;
                        break;
                    case SpecialType.System_Int32:
                        convertedValue = unchecked((ulong)(int)value);
                        success = true;
                        break;
                    case SpecialType.System_Int64:
                        convertedValue = unchecked((ulong)(long)value);
                        success = true;
                        break;
                    case SpecialType.System_UInt16:
                        convertedValue = (ushort)value;
                        success = true;
                        break;
                    case SpecialType.System_UInt32:
                        convertedValue = (uint)value;
                        success = true;
                        break;
                    case SpecialType.System_UInt64:
                        convertedValue = (ulong)value;
                        success = true;
                        break;
                    case SpecialType.System_Byte:
                        convertedValue = (byte)value;
                        success = true;
                        break;
                    case SpecialType.System_SByte:
                        convertedValue = unchecked((ulong)(sbyte)value);
                        success = true;
                        break;
                    case SpecialType.System_Char:
                        convertedValue = (char)value;
                        success = true;
                        break;
                    case SpecialType.System_Boolean:
                        convertedValue = (ulong)((bool)value ? 1 : 0);
                        success = true;
                        break;
                }
            }

            return success;
        }

        public static string GetMemberName(ISymbol symbol)
        {
            // For Types
            if (symbol is INamedTypeSymbol namedType &&
                namedType.IsGenericType)
            {
                return symbol.MetadataName;
            }

            // For other language constructs
            return symbol.Name;
        }
    }
}
