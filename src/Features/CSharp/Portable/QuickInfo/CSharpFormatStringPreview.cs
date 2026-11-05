// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

internal enum FormatStringValueReason
{
    UnknownInput,
    Known,
    MutationOrEscape,
    UnsupportedOperation,
    InvalidValue,
    WorkLimit,
}

/// <summary>A source value in its bound type. Known null is different from an unknown value.</summary>
internal readonly record struct CSharpFormatStringValue(Optional<object?> Value, ITypeSymbol? Type, FormatStringValueReason Reason)
{
    public bool IsKnown => Value.HasValue;

    public static CSharpFormatStringValue Create(Optional<object?> value, ITypeSymbol? type)
        => new(value, type, value.HasValue ? FormatStringValueReason.Known : FormatStringValueReason.UnknownInput);
}

internal enum FormatStringProviderKind
{
    Absent,
    Null,
    ExplicitCulture,
    Unresolved,
}

/// <summary>The editor's selected context, rather than a prediction of ambient runtime culture.</summary>
internal readonly record struct CSharpFormatStringContext(
    CultureInfo? Culture = null,
    FormatStringProviderKind ProviderKind = FormatStringProviderKind.Absent,
    bool? UsesUserOverrides = null,
    bool HasUnknownFormatter = false)
{
    private static readonly CultureInfo s_defaultCulture = CultureInfo.GetCultureInfo("en-US");

    public CultureInfo EffectiveCulture => Culture ?? s_defaultCulture;
}

internal enum FormatStringOutputKind
{
    Example,
    Preview,
    Omitted,
}
