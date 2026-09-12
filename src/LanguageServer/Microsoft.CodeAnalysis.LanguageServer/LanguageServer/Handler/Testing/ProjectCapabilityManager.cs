// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

[ExportCSharpVisualBasicLspServiceFactory(typeof(ProjectCapabilityManager)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ProjectCapabilityManagerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new ProjectCapabilityManager();
}

internal sealed class ProjectCapabilityManager : ILspService
{
    private readonly ConcurrentDictionary<ProjectId, ImmutableHashSet<string>> _projectCapabilities = new();

    public void UpdateCapabilities(ProjectId projectId, IEnumerable<string> capabilities)
        => _projectCapabilities[projectId] = capabilities.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    public void RemoveProject(ProjectId projectId)
        => _projectCapabilities.TryRemove(projectId, out _);

    public bool HasCapability(ProjectId projectId, string capability)
        => _projectCapabilities.TryGetValue(projectId, out var capabilities) && capabilities.Contains(capability);
}
