// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
        ReadableRef = 0x00100,

        /// <summary>
        /// Represents a readable reference being taken to the value.
        /// For example, passing an argument to an "out" parameter.
        /// </summary>
        WritableRef = 0x01000,

        /// <summary>
        /// Represents a symbol reference that neither reads nor writes the underlying value.
        /// For example, 'nameof(x)' does not read or write the underlying value stored in 'x'.
        /// </summary>
        NonReadWriteRef = 0x10000,

        /// <summary>
        /// Represents a value read and/or write.
        /// For example, an increment or compound assignment operation.
        /// </summary>
        ReadWrite = Read | Write,

        /// <summary>
        /// Represents a value read or write.
        /// For example, passing an argument to a "ref" parameter.
        /// </summary>
        ReadableWritableRef = ReadableRef | WritableRef
    }

    internal static class ValueUsageInfoExtensions
    {
        public static bool ContainsReadOrReadableRef(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & (ValueUsageInfo.Read | ValueUsageInfo.ReadableRef)) != 0;

        public static bool ContainsWriteOrWritableRef(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & (ValueUsageInfo.Write | ValueUsageInfo.WritableRef)) != 0;

        public static bool ContainsNonReadWriteRef(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & ValueUsageInfo.NonReadWriteRef) != 0;
    }
}
