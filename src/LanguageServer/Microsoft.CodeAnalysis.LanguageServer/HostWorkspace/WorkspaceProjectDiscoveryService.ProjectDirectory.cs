// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed partial class WorkspaceProjectDiscoveryService
{
    private sealed record ProjectDirectory(string WorkspaceFolder, ImmutableArray<string> Projects, IFileChangeContext Watcher);
}
