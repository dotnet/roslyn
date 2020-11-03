// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal static class ConversionOperatorExtensions
    {
        public static ImmutableArray<SpecialType>? GetBuiltInNumericConversions(this ITypeSymbol container)
        {
            // Source: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#explicit-numeric-conversions
            return container.SpecialType switch
            {
                SpecialType.System_SByte => new[] {
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_UInt32,
                    SpecialType.System_UInt64,
                    SpecialType.System_UInt16}.ToImmutableArray(),
                SpecialType.System_Byte => new[] {
                    SpecialType.System_Char,
                    SpecialType.System_SByte}.ToImmutableArray(),
                SpecialType.System_Int16 => new[] {
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_UInt32,
                    SpecialType.System_UInt64,
                    SpecialType.System_UInt16,
                    SpecialType.System_SByte}.ToImmutableArray(),
                SpecialType.System_UInt16 => new[]{
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_SByte,
                    SpecialType.System_Int16}.ToImmutableArray(),
                SpecialType.System_Int32 => new[]{
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_SByte,
                    SpecialType.System_Int16,
                    SpecialType.System_UInt32,
                    SpecialType.System_UInt16,
                    SpecialType.System_UInt64}.ToImmutableArray(),
                SpecialType.System_UInt32 => new[]{
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_Int32,
                    SpecialType.System_SByte,
                    SpecialType.System_Int16,
                    SpecialType.System_UInt16}.ToImmutableArray(),
                SpecialType.System_Int64 => new[]{
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_Int32,
                    SpecialType.System_UInt32,
                    SpecialType.System_UInt64,
                    SpecialType.System_UInt16,
                    SpecialType.System_SByte,
                    SpecialType.System_Int16}.ToImmutableArray(),
                SpecialType.System_UInt64 => new[]{
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_Int32,
                    SpecialType.System_Int64,
                    SpecialType.System_UInt32,
                    SpecialType.System_UInt16,
                    SpecialType.System_SByte,
                    SpecialType.System_Int16}.ToImmutableArray(),
                SpecialType.System_Char => new[]{
                    SpecialType.System_Byte,
                    SpecialType.System_SByte,
                    SpecialType.System_Int16}.ToImmutableArray(),
                SpecialType.System_Single => new[]{
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_Decimal,
                    SpecialType.System_Int32,
                    SpecialType.System_Int64,
                    SpecialType.System_UInt32,
                    SpecialType.System_UInt64,
                    SpecialType.System_UInt16,
                    SpecialType.System_SByte,
                    SpecialType.System_Int16}.ToImmutableArray(),
                SpecialType.System_Double => new[]{
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_Decimal,
                    SpecialType.System_Single,
                    SpecialType.System_Int32,
                    SpecialType.System_Int64,
                    SpecialType.System_UInt32,
                    SpecialType.System_UInt64,
                    SpecialType.System_UInt16,
                    SpecialType.System_SByte,
                    SpecialType.System_Int16}.ToImmutableArray(),
                SpecialType.System_Decimal => new[]{
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_Double,
                    SpecialType.System_Single,
                    SpecialType.System_Int32,
                    SpecialType.System_Int64,
                    SpecialType.System_UInt32,
                    SpecialType.System_UInt64,
                    SpecialType.System_UInt16,
                    SpecialType.System_SByte,
                    SpecialType.System_Int16}.ToImmutableArray(),
                _ => null,
            };
        }
    }
}
