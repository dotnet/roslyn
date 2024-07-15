// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Formatting;

internal static class FormattingDiagnosticIds
{
    /// <summary>
    /// This is the ID reported for formatting diagnostics.
    /// </summary>
    public const string FormattingDiagnosticId = "IDE0055";

    /// <summary>
    /// This special diagnostic can be suppressed via <c>#pragma</c> to prevent the formatter from making changes to
    /// code formatting within the span where the diagnostic is suppressed.
    /// </summary>
    public const string FormatDocumentControlDiagnosticId = "format";
}
