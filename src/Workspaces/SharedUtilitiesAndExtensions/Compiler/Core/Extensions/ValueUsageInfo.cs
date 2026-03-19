// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis;

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

    public static bool IsReference(this ValueUsageInfo valueUsageInfo)
        => (valueUsageInfo & ValueUsageInfo.Reference) != 0;
}
