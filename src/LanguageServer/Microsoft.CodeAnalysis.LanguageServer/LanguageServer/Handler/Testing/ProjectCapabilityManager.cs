// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

[ExportCSharpVisualBasicLspService(typeof(ProjectCapabilityManager)), Shared(LspServiceComposition.SharingBoundary)]
internal sealed class ProjectCapabilityManager : ILspService
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Constructed directly by tests")]
    public ProjectCapabilityManager()
    {
    }

    private readonly ConcurrentDictionary<ProjectId, ImmutableHashSet<string>> _projectCapabilities = new();

    public void UpdateCapabilities(ProjectId projectId, IEnumerable<string> capabilities)
        => _projectCapabilities[projectId] = capabilities.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    public void RemoveProject(ProjectId projectId)
        => _projectCapabilities.TryRemove(projectId, out _);

    public bool HasCapability(ProjectId projectId, string capability)
        => _projectCapabilities.TryGetValue(projectId, out var capabilities) && capabilities.Contains(capability);
}
