// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

internal readonly record struct CSharpFormatStringContext(CultureInfo? Culture = null, bool HasUnknownFormatter = false)
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
