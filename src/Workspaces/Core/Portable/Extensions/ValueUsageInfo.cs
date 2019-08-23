// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

#if !CODE_STYLE
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
#endif

namespace Microsoft.CodeAnalysis
{
    [Flags]
    internal enum ValueUsageInfo
    {
        /// <summary>
        /// Represents default value indicating no usage.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Represents a value read.
        /// For example, reading the value of a local/field/parameter.
        /// </summary>
        Read = 0x1,

        /// <summary>
        /// Represents a value write.
        /// For example, assigning a value to a local/field/parameter.
        /// </summary>
        Write = 0x2,

        /// <summary>
        /// Represents a reference being taken for the symbol.
        /// For example, passing an argument to an "in", "ref" or "out" parameter.
        /// </summary>
        Reference = 0x4,

        /// <summary>
        /// Represents a name-only reference that neither reads nor writes the underlying value.
        /// For example, 'nameof(x)' or reference to a symbol 'x' in a documentation comment
        /// does not read or write the underlying value stored in 'x'.
        /// </summary>
        Name = 0x8,

        /// <summary>
        /// Represents a value read and/or write.
        /// For example, an increment or compound assignment operation.
        /// </summary>
        ReadWrite = Read | Write,

        /// <summary>
        /// Represents a readable reference being taken to the value.
        /// For example, passing an argument to an "in" or "ref readonly" parameter.
        /// </summary>
        ReadableReference = Read | Reference,

        /// <summary>
        /// Represents a readable reference being taken to the value.
        /// For example, passing an argument to an "out" parameter.
        /// </summary>
        WritableReference = Write | Reference,

        /// <summary>
        /// Represents a value read or write.
        /// For example, passing an argument to a "ref" parameter.
        /// </summary>
        ReadableWritableReference = Read | Write | Reference
    }

    internal static class ValueUsageInfoExtensions
    {
        public static bool IsReadFrom(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & ValueUsageInfo.Read) != 0;

        public static bool IsWrittenTo(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & ValueUsageInfo.Write) != 0;

        public static bool IsNameOnly(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & ValueUsageInfo.Name) != 0;

#if !CODE_STYLE
        public static string ToLocalizableString(this ValueUsageInfo value)
        {
            // We don't support localizing value combinations.
            Debug.Assert(value.IsSingleBitSet());

            switch (value)
            {
                case ValueUsageInfo.Read:
                    return WorkspacesResources.ValueUsageInfo_Read;

                case ValueUsageInfo.Write:
                    return WorkspacesResources.ValueUsageInfo_Write;

                case ValueUsageInfo.Reference:
                    return WorkspacesResources.ValueUsageInfo_Reference;

                case ValueUsageInfo.Name:
                    return WorkspacesResources.ValueUsageInfo_Name;

                default:
                    Debug.Fail($"Unhandled value: '{value.ToString()}'");
                    return value.ToString();
            }
        }

        public static bool IsSingleBitSet(this ValueUsageInfo valueUsageInfo)
            => valueUsageInfo != ValueUsageInfo.None && (valueUsageInfo & (valueUsageInfo - 1)) == 0;

        public static ImmutableArray<string> ToLocalizableValues(this ValueUsageInfo valueUsageInfo)
        {
            if (valueUsageInfo == ValueUsageInfo.None)
            {
                return ImmutableArray<string>.Empty;
            }

            var builder = ArrayBuilder<string>.GetInstance();
            foreach (ValueUsageInfo value in Enum.GetValues(typeof(ValueUsageInfo)))
            {
                if (value.IsSingleBitSet() && (valueUsageInfo & value) != 0)
                {
                    builder.Add(value.ToLocalizableString());
                }
            }

            return builder.ToImmutableAndFree();
        }
#endif
    }
}
