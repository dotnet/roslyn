// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.InlineHints;

internal sealed class InlineHintsViewOptionsStorage
{
    public static readonly Option2<bool> DisplayAllHintsWhilePressingAltF1 = new(
        "dotnet_display_inline_hints_while_pressing_alt_f1", defaultValue: true);

    public static readonly PerLanguageOption2<bool> ColorHints = new(
        "dotnet_colorize_inline_hints", defaultValue: true);
}
