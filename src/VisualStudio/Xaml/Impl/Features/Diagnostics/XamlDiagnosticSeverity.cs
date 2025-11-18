// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Diagnostics;

internal enum XamlDiagnosticSeverity
{
    /// <summary>
    /// Represents an error.
    /// </summary>
    Error,

    /// <summary>
    /// Represent a warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Represents an informational note.
    /// </summary>
    Message,

    /// <summary>
    /// Represents a hidden note.
    /// </summary>
    Hidden,

    /// <summary>
    /// Represents a hinted suggestion.
    /// </summary>
    HintedSuggestion
}
