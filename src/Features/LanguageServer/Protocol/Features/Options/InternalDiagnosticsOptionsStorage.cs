// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class InternalDiagnosticsOptionsStorage
{
    private static readonly EditorConfigValueSerializer<DiagnosticMode> s_editorConfigValueSerializer = EditorConfigValueSerializer.CreateSerializerForEnum<DiagnosticMode>();
    /// <summary>
    /// Diagnostic mode setting for Razor.  This should always be <see cref="DiagnosticMode.LspPull"/> as there is no push support in Razor.
    /// This option is only for passing to the diagnostics service and can be removed when we switch all of Roslyn to LSP pull.
    /// </summary>
    public static readonly Option2<DiagnosticMode> RazorDiagnosticMode = new(
        "InternalDiagnosticsOptions_RazorDiagnosticMode", defaultValue: DiagnosticMode.LspPull, serializer: s_editorConfigValueSerializer);

    /// <summary>
    /// Diagnostic mode setting for Live Share.  This should always be <see cref="DiagnosticMode.LspPull"/> as there is no push support in Live Share.
    /// This option is only for passing to the diagnostics service and can be removed when we switch all of Roslyn to LSP pull.
    /// </summary>
    public static readonly Option2<DiagnosticMode> LiveShareDiagnosticMode = new(
        "InternalDiagnosticsOptions_LiveShareDiagnosticMode", defaultValue: DiagnosticMode.LspPull, serializer: s_editorConfigValueSerializer);

    public static readonly Option2<DiagnosticMode> NormalDiagnosticMode = new(
        "dotnet_normal_diagnostic_mode", defaultValue: DiagnosticMode.Default, serializer: s_editorConfigValueSerializer);
}
