// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <inheritdoc cref="ILspBuildOnlyDiagnostics"/>
[MetadataView]
internal partial interface ILspBuildOnlyDiagnosticsMetadata
{
    string LanguageName { get; }
    string[] BuildOnlyDiagnostics { get; }
}
