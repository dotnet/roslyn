// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer
{
    /// <summary>
    /// Marker interface for individual Roslyn languages to expose what diagnostics IDs they have are 'build only'. This
    /// affects how the LSP client will handle and dedupe related diagnostics produced by Roslyn for live diagnostics
    /// against the diagnostics produced by CPS when a build is performed.
    /// </summary>
    internal interface ILspBuildOnlyDiagnostics
    {
    }
}
