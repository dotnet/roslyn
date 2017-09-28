﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class EnumUtilities
    {
        /// <summary>
        /// Convert a boxed primitive (generally of the backing type of an enum) into a ulong.
        /// </summary>
        /// <remarks>
        /// </remarks>
        internal static ulong ConvertEnumUnderlyingTypeToUInt64(object value, SpecialType specialType)
        {
            Debug.Assert(value != null);
            Debug.Assert(value.GetType().GetTypeInfo().IsPrimitive);

            unchecked
            {
                switch (specialType)
                {
                    case SpecialType.System_SByte:
                        return (ulong)(sbyte)value;
                    case SpecialType.System_Int16:
                        return (ulong)(short)value;
                    case SpecialType.System_Int32:
                        return (ulong)(int)value;
                    case SpecialType.System_Int64:
                        return (ulong)(long)value;
                    case SpecialType.System_Byte:
                        return (byte)value;
                    case SpecialType.System_UInt16:
                        return (ushort)value;
                    case SpecialType.System_UInt32:
                        return (uint)value;
                    case SpecialType.System_UInt64:
                        return (ulong)value;

                    default:
                        // not using ExceptionUtilities.UnexpectedValue() because this is used by the Services layer
                        // which doesn't have those utilities.
                        throw new InvalidOperationException(string.Format("{0} is not a valid underlying type for an enum", specialType));
                }
            }
        }

        internal static T[] GetValues<T>() where T : struct
        {
            return (T[])Enum.GetValues(typeof(T));
        }
    }
}
