// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal static class FadingOptions
{
    public static readonly PerLanguageOption2<bool> FadeOutUnusedImports = new("dotnet_fade_out_unused_imports", defaultValue: true);
    public static readonly PerLanguageOption2<bool> FadeOutUnusedMembers = new("dotnet_fade_out_unused_members", defaultValue: true);
    public static readonly PerLanguageOption2<bool> FadeOutUnreachableCode = new("dotnet_fade_out_unreachable_code", defaultValue: true);
}
