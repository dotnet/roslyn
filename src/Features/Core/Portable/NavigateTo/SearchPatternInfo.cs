// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PatternMatching;

namespace Microsoft.CodeAnalysis.NavigateTo;

/// <summary>
/// Holds the result of parsing and analyzing a NavigateTo search pattern. Bundles the
/// split name/container and pre-compiled regex query so callers can pass a single value
/// through the search pipeline. A non-null <see cref="RegexQuery"/> indicates regex mode.
/// </summary>
internal readonly record struct SearchPatternInfo(
    string Name,
    string? Container,
    RegexQuery? RegexQuery)
{
    public bool IsRegex => RegexQuery is not null;
}
