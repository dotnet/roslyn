// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class SpecialTypeExtensions
{
    public static PredefinedType ToPredefinedType(this SpecialType specialType)
        => specialType switch
        {
            SpecialType.System_Object => PredefinedType.Object,
            SpecialType.System_Void => PredefinedType.Void,
            SpecialType.System_Boolean => PredefinedType.Boolean,
            SpecialType.System_Char => PredefinedType.Char,
            SpecialType.System_SByte => PredefinedType.SByte,
            SpecialType.System_Byte => PredefinedType.Byte,
            SpecialType.System_Int16 => PredefinedType.Int16,
            SpecialType.System_UInt16 => PredefinedType.UInt16,
            SpecialType.System_Int32 => PredefinedType.Int32,
            SpecialType.System_UInt32 => PredefinedType.UInt32,
            SpecialType.System_Int64 => PredefinedType.Int64,
            SpecialType.System_UInt64 => PredefinedType.UInt64,
            SpecialType.System_Decimal => PredefinedType.Decimal,
            SpecialType.System_Single => PredefinedType.Single,
            SpecialType.System_Double => PredefinedType.Double,
            SpecialType.System_String => PredefinedType.String,
            SpecialType.System_DateTime => PredefinedType.DateTime,
            SpecialType.System_IntPtr => PredefinedType.IntPtr,
            SpecialType.System_UIntPtr => PredefinedType.UIntPtr,
            _ => PredefinedType.None,
        };
}
