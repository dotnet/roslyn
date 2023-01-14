// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class EnumValueUtilities
    {
        /// <summary>
        /// Determines, using heuristics, what the next likely value is in this enum.
        /// </summary>
        public static object GetNextEnumValue(INamedTypeSymbol enumType)
        {
            var orderedExistingConstants = enumType.GetMembers()
                                            .OfType<IFieldSymbol>()
                                            .Where(f => f.HasConstantValue)
                                            .Select(f => f.ConstantValue)
                                            .OfType<IComparable>()
                                            .OrderByDescending(f => f).ToList();
            var existingConstants = orderedExistingConstants.ToSet();

            if (LooksLikeFlagsEnum(orderedExistingConstants))
            {
                if (orderedExistingConstants.Count == 0)
                {
                    return CreateOne(enumType.EnumUnderlyingType.SpecialType);
                }
                else
                {
                    var largest = orderedExistingConstants[0];
                    return Multiply(largest, 2);
                }
            }
            else if (orderedExistingConstants.Count > 0)
            {
                for (uint i = 1; i <= existingConstants.Count; i++)
                {
                    var nextValue = Add(orderedExistingConstants[0], i);
                    if (!existingConstants.Contains(nextValue))
                    {
                        return nextValue;
                    }
                }
            }

            return null;
        }

        private static object CreateOne(SpecialType specialType)
            => specialType switch
            {
                SpecialType.System_SByte => (sbyte)1,
                SpecialType.System_Byte => (byte)1,
                SpecialType.System_Int16 => (short)1,
                SpecialType.System_UInt16 => (ushort)1,
                SpecialType.System_Int32 => 1,
                SpecialType.System_UInt32 => (uint)1,
                SpecialType.System_Int64 => (long)1,
                SpecialType.System_UInt64 => (ulong)1,
                _ => 1,
            };

        private static IComparable Multiply(IComparable value, uint number)
            => value switch
            {
                long v => unchecked(v * number),
                ulong v => unchecked(v * number),
                int v => unchecked((int)(v * number)),
                uint v => unchecked(v * number),
                short v => unchecked((short)(v * number)),
                ushort v => unchecked((ushort)(v * number)),
                sbyte v => unchecked((sbyte)(v * number)),
                byte v => unchecked((byte)(v * number)),
                _ => null,
            };

        private static IComparable Add(IComparable value, uint number)
            => value switch
            {
                long v => unchecked(v + number),
                ulong v => unchecked(v + number),
                int v => unchecked((int)(v + number)),
                uint v => unchecked(v + number),
                short v => unchecked((short)(v + number)),
                ushort v => unchecked((ushort)(v + number)),
                sbyte v => unchecked((sbyte)(v + number)),
                byte v => unchecked((byte)(v + number)),
                _ => null,
            };

        private static bool GreaterThanOrEqualsZero(IComparable value)
            => value switch
            {
                long v => v >= 0,
                ulong v => v >= 0,
                int v => v >= 0,
                uint v => v >= 0,
                short v => v >= 0,
                ushort v => v >= 0,
                sbyte v => v >= 0,
                byte v => v >= 0,
                _ => false,
            };

        private static bool LooksLikeFlagsEnum(List<IComparable> existingConstants)
        {
            if (existingConstants.Count >= 1 &&
               IntegerUtilities.HasOneBitSet(existingConstants[0]) &&
               Multiply(existingConstants[0], 2).CompareTo(existingConstants[0]) > 0 &&
               existingConstants.All(GreaterThanOrEqualsZero))
            {
                if (existingConstants.Count == 1)
                {
                    return true;
                }

                if (existingConstants[0].Equals(Multiply(existingConstants[1], 2)))
                {
                    // If you only have two values, and they're 1 and 2, then don't assume this is a
                    // flags enum.  The person could have been trying to type, 1, 2, 3 instead.
                    if (existingConstants[0].Equals(Convert.ChangeType(2, existingConstants[0].GetType())) &&
                        existingConstants[1].Equals(Convert.ChangeType(1, existingConstants[1].GetType())))
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
