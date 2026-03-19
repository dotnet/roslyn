// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Indicates kind of a <see cref="TextDocument"/>
/// </summary>
public enum TextDocumentKind
{
    /// <summary>
    /// Indicates a regular source <see cref="CodeAnalysis.Document"/>
    /// </summary>
    Document,

    /// <summary>
    /// Indicates an <see cref="CodeAnalysis.AdditionalDocument"/>
    /// </summary>
    AdditionalDocument,

    /// <summary>
    /// Indicates an <see cref="CodeAnalysis.AnalyzerConfigDocument"/>
    /// </summary>
    AnalyzerConfigDocument
}
