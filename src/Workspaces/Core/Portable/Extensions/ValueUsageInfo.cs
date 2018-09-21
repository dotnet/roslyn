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
        /// For example, 'nameof(x)', 'typeof(x)', 'sizeof(x)' does not read or write the underlying value stored in 'x'.
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
        /// <summary>
        /// True if a read is performed from the given expression.  Note: writes may also be performed
        /// to the expression as well.  For example, "++a".  In this expression 'a' is both read from
        /// and written to.
        /// </summary>
        public static bool IsReadFrom(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & (ValueUsageInfo.Read | ValueUsageInfo.ReadableRef)) != 0;

        /// <summary>
        /// True if a read is performed to the given expression.  Note: unlike <see cref="IsReadFrom(ValueUsageInfo)"/>, this
        /// will not return true if writes are performed on the expression as well.  For example,
        /// "++a" will return 'false'.  However, 'a' in "in a" or "x = a" will return true.
        /// </summary>
        public static bool IsOnlyReadFrom(this ValueUsageInfo valueUsageInfo)
           => valueUsageInfo == ValueUsageInfo.Read || valueUsageInfo == ValueUsageInfo.ReadableRef;

        /// <summary>
        /// True if a write is performed to the given expression.  Note: reads may also be performed
        /// to the expression as well.  For example, "++a".  In this expression 'a' is both read from
        /// and written to.
        /// </summary>
        public static bool IsWrittenTo(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & (ValueUsageInfo.Write | ValueUsageInfo.WritableRef)) != 0;

        /// <summary>
        /// True if a write is performed to the given expression.  Note: unlike <see cref="IsWrittenTo(ValueUsageInfo)"/>, this
        /// will not return true if reads are performed on the expression as well.  For example,
        /// "++a" will return 'false'.  However, 'a' in "out a" or "a = 1" will return true.
        /// </summary>
        public static bool IsOnlyWrittenTo(this ValueUsageInfo valueUsageInfo)
           => valueUsageInfo == ValueUsageInfo.Write || valueUsageInfo == ValueUsageInfo.WritableRef;

        public static bool IsInOutContext(this ValueUsageInfo valueUsageInfo)
            => valueUsageInfo == ValueUsageInfo.WritableRef;

        public static bool IsInRefContext(this ValueUsageInfo valueUsageInfo)
            => valueUsageInfo == ValueUsageInfo.ReadableWritableRef;

        public static bool IsInRefOrOutContext(this ValueUsageInfo valueUsageInfo)
            => valueUsageInfo.IsInRefContext() || valueUsageInfo.IsInOutContext();

        public static bool IsInInContext(this ValueUsageInfo valueUsageInfo)
            => valueUsageInfo == ValueUsageInfo.ReadableRef;

        /// <summary>
        /// Represents a reference that neither reads nor writes the underlying value.
        /// For example, 'nameof(x)', 'typeof(x)', 'sizeof(x)' does not read or write the underlying value stored in 'x'.
        /// </summary>
        public static bool IsInNonReadNonWriteContext(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & ValueUsageInfo.NonReadWriteRef) != 0;
    }
}
