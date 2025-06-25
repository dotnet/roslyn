// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static partial class DiagnosticSeverityExtensions
{
    public static string ToEditorConfigString(this DiagnosticSeverity diagnosticSeverity)
    {
        return diagnosticSeverity switch
        {
            DiagnosticSeverity.Hidden => EditorConfigSeverityStrings.Silent,
            DiagnosticSeverity.Info => EditorConfigSeverityStrings.Suggestion,
            DiagnosticSeverity.Warning => EditorConfigSeverityStrings.Warning,
            DiagnosticSeverity.Error => EditorConfigSeverityStrings.Error,
            _ => throw ExceptionUtilities.UnexpectedValue(diagnosticSeverity)
        };
    }
}
