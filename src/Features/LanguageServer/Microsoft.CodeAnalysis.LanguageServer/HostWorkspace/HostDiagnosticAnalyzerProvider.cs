// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
internal class HostDiagnosticAnalyzerProvider : IHostDiagnosticAnalyzerProvider
{
    public ImmutableArray<(AnalyzerFileReference reference, string extensionId)> GetAnalyzerReferencesInExtensions()
    {
        // Right now we don't expose any way for the extensions in VS Code to provide analyzer references.
        return ImmutableArray<(AnalyzerFileReference reference, string extensionId)>.Empty;
    }
}
