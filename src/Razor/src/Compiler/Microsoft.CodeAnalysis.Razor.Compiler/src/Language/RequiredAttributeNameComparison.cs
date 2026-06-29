// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Acceptable <see cref="RequiredAttributeDescriptor.NameComparison"/> comparison values.
/// </summary>
public enum RequiredAttributeNameComparison : byte
{
    /// <summary>
    /// HTML attribute name case insensitively matches <see cref="RequiredAttributeDescriptor.Name"/>.
    /// </summary>
    FullMatch,

    /// <summary>
    /// HTML attribute name case insensitively starts with <see cref="RequiredAttributeDescriptor.Name"/>.
    /// </summary>
    PrefixMatch
}
