// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Allows the host to provide fallback editorconfig options for a language loaded into the workspace.
/// </summary>
internal interface IFallbackAnalyzerConfigOptionsProvider : IWorkspaceService
{
    StructuredAnalyzerConfigOptions GetOptions(string language);
}

[Shared]
[ExportWorkspaceService(typeof(IFallbackAnalyzerConfigOptionsProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultFallbackAnalyzerConfigOptionsProvider() : IFallbackAnalyzerConfigOptionsProvider
{
    public StructuredAnalyzerConfigOptions GetOptions(string language)
        => StructuredAnalyzerConfigOptions.Empty;
}
