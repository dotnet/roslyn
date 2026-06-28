// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// MEF metadata class used to find <see cref="ILspBuildOnlyDiagnostics"/> exports.
/// </summary>
internal sealed class LspBuildOnlyDiagnosticsMetadata(IDictionary<string, object> data)
{
    public string LanguageName { get; } = (string)data[nameof(LspBuildOnlyDiagnosticsAttribute.LanguageName)];
    public string[] BuildOnlyDiagnostics { get; } = (string[])data[nameof(LspBuildOnlyDiagnosticsAttribute.BuildOnlyDiagnostics)];
}
