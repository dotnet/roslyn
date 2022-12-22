// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class InternalDiagnosticsOptions
{
    /// <summary>
    /// Diagnostic mode setting for Razor.  This should always be <see cref="DiagnosticMode.LspPull"/> as there is no push support in Razor.
    /// This option is only for passing to the diagnostics service and can be removed when we switch all of Roslyn to LSP pull.
    /// </summary>
    public static readonly Option2<DiagnosticMode> RazorDiagnosticMode = new(nameof(InternalDiagnosticsOptions), "RazorDiagnosticMode", defaultValue: DiagnosticMode.LspPull);

    /// <summary>
    /// Diagnostic mode setting for Live Share.  This should always be <see cref="DiagnosticMode.LspPull"/> as there is no push support in Live Share.
    /// This option is only for passing to the diagnostics service and can be removed when we switch all of Roslyn to LSP pull.
    /// </summary>
    public static readonly Option2<DiagnosticMode> LiveShareDiagnosticMode = new(nameof(InternalDiagnosticsOptions), "LiveShareDiagnosticMode", defaultValue: DiagnosticMode.LspPull);

    public static readonly Option2<DiagnosticMode> NormalDiagnosticMode = new("InternalDiagnosticsOptions", "NormalDiagnosticMode", defaultValue: DiagnosticMode.Default,
        storageLocation: new LocalUserProfileStorageLocation(@"Roslyn\Internal\Diagnostics\NormalDiagnosticMode"));
}
