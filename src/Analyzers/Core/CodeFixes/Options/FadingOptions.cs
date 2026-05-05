// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal static class FadingOptions
{
    public static readonly PerLanguageOption2<bool> FadeOutUnusedImports = new("dotnet_fade_out_unused_imports", defaultValue: true);
    public static readonly PerLanguageOption2<bool> FadeOutUnusedMembers = new("dotnet_fade_out_unused_members", defaultValue: true);
    public static readonly PerLanguageOption2<bool> FadeOutUnreachableCode = new("dotnet_fade_out_unreachable_code", defaultValue: true);

    // When adding a new fading option, be sure to update dictionary below with apppropriate diagnostic id mapping.

    private static readonly ImmutableDictionary<string, PerLanguageOption2<bool>> s_diagnosticToFadingOption = new Dictionary<string, PerLanguageOption2<bool>>
    {
        { IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId, FadeOutUnusedMembers },
        { IDEDiagnosticIds.RemoveUnreadMembersDiagnosticId, FadeOutUnusedMembers },
        { IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId, FadeOutUnreachableCode },
        { IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId, FadeOutUnusedImports },
        { IDEDiagnosticIds.RemoveUnnecessaryImportsGeneratedCodeDiagnosticId, FadeOutUnusedImports },
    }.ToImmutableDictionary();

    public static bool TryGetFadingOptionForDiagnostic(string diagnosticId, [NotNullWhen(true)] out PerLanguageOption2<bool>? fadingOption)
        => s_diagnosticToFadingOption.TryGetValue(diagnosticId, out fadingOption);
}
