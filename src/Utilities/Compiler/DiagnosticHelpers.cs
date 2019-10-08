// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class DiagnosticHelpers
    {
        public const DiagnosticSeverity DefaultDiagnosticSeverity =
#if BUILDING_VSIX
            DiagnosticSeverity.Info;
#else
            DiagnosticSeverity.Warning;
#endif

        public const bool EnabledByDefaultIfNotBuildingVSIX =
#if BUILDING_VSIX
            false;
#else
            true;
#endif

        public const bool EnabledByDefaultOnlyIfBuildingVSIX =
#if BUILDING_VSIX
            true;
#else
            false;
#endif

        public const bool EnabledByDefaultForVsixAndNuget = true;

        public static bool TryConvertToUInt64(object value, SpecialType specialType, out ulong convertedValue)
        {
            bool success = false;
            convertedValue = 0;
            if (value != null)
            {
                switch (specialType)
                {
                    case SpecialType.System_Int16:
                        convertedValue = unchecked((ulong)((short)value));
                        success = true;
                        break;
                    case SpecialType.System_Int32:
                        convertedValue = unchecked((ulong)((int)value));
                        success = true;
                        break;
                    case SpecialType.System_Int64:
                        convertedValue = unchecked((ulong)((long)value));
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
                        convertedValue = unchecked((ulong)((sbyte)value));
                        success = true;
                        break;
                    case SpecialType.System_Char:
                        convertedValue = (char)value;
                        success = true;
                        break;
                    case SpecialType.System_Boolean:
                        convertedValue = (ulong)((bool)value == true ? 1 : 0);
                        success = true;
                        break;
                }
            }

            return success;
        }

        internal static bool TryGetEnumMemberValues(INamedTypeSymbol enumType, out IList<ulong> values)
        {
            Debug.Assert(enumType != null);
            Debug.Assert(enumType.TypeKind == TypeKind.Enum);

            values = new List<ulong>();
            foreach (var member in enumType.GetMembers())
            {
                if (!member.IsImplicitlyDeclared && member is IFieldSymbol field)
                {
                    if (!field.HasConstantValue)
                    {
                        return false;
                    }

                    if (!TryConvertToUInt64(field.ConstantValue, enumType.EnumUnderlyingType.SpecialType, out ulong convertedValue))
                    {
                        return false;
                    }

                    values.Add(convertedValue);
                }
            }

            return true;
        }

        public static string GetMemberName(ISymbol symbol)
        {
            // For Types
            if (symbol.Kind == SymbolKind.NamedType)
            {
                if ((symbol as INamedTypeSymbol).IsGenericType)
                {
                    return symbol.MetadataName;
                }
            }

            // For other language constructs
            return symbol.Name;
        }
    }
}
