// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    [Flags]
    internal enum ValueUsageInfo
    {
        /// <summary>
        /// Represents default value indicating no usage.
        /// </summary>
        None = 0x00000,

        /// <summary>
        /// Represents a value read.
        /// For example, reading the value of a local/field/parameter.
        /// </summary>
        Read = 0x00001,

        /// <summary>
        /// Represents a value write.
        /// For example, assigning a value to a local/field/parameter.
        /// </summary>
        Write = 0x00010,

        /// <summary>
        /// Represents a readable reference being taken to the value.
        /// For example, passing an argument to an "in" or "ref readonly" parameter.
        /// </summary>
        ReadableReference = 0x00100,

        /// <summary>
        /// Represents a readable reference being taken to the value.
        /// For example, passing an argument to an "out" parameter.
        /// </summary>
        WritableReference = 0x01000,

        /// <summary>
        /// Represents a symbol reference that neither reads nor writes the underlying value.
        /// For example, 'nameof(x)' does not read or write the underlying value stored in 'x'.
        /// </summary>
        NonReadWriteReference = 0x10000,

        /// <summary>
        /// Represents a value read and/or write.
        /// For example, an increment or compound assignment operation.
        /// </summary>
        ReadWrite = Read | Write,

        /// <summary>
        /// Represents a value read or write.
        /// For example, passing an argument to a "ref" parameter.
        /// </summary>
        ReadableWritableReference = ReadableReference | WritableReference
    }

    internal static class ValueUsageInfoExtensions
    {
        public static bool ContainsReadOrReadableReference(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & (ValueUsageInfo.Read | ValueUsageInfo.ReadableReference)) != 0;

        public static bool ContainsWriteOrWritableReference(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & (ValueUsageInfo.Write | ValueUsageInfo.WritableReference)) != 0;

        public static bool ContainsNonReadWriteReference(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & ValueUsageInfo.NonReadWriteReference) != 0;

        public static ImmutableArray<string> ToValues(this ValueUsageInfo valueUsageInfo)
        {
            if (valueUsageInfo == ValueUsageInfo.None)
            {
                return ImmutableArray<string>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<string>();
            foreach (ValueUsageInfo value in Enum.GetValues(typeof(ValueUsageInfo)))
            {
                bool singleBitIsSet = (value & (value - 1)) == 0;
                if (singleBitIsSet && (valueUsageInfo & value) != 0)
                {
                    builder.Add(value.ToString());
                }
            }

            return builder.ToImmutable();
        }
    }
}
