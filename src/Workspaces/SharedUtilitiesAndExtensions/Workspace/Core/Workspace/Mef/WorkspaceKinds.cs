// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis;

[Flags]
internal enum WorkspaceKinds
{
    Unknown = 0,

    Host = 1,
    Debugger = 1 << 1,
    Interactive = 1 << 2,
    MetadataAsSource = 1 << 3,
    MiscellaneousFiles = 1 << 4,
    Preview = 1 << 5,
    SemanticSearch = 1 << 6,

    Custom = 1 << 7,
    MSBuild = 1 << 8,
    CloudEnvironmentClientWorkspace = 1 << 9,
    RemoteWorkspace = 1 << 10,
}

internal static class WorkspaceKindsFactory
{
    internal static WorkspaceKinds FromKind(string kind)
        => kind switch
        {
            nameof(WorkspaceKind.Host) => WorkspaceKinds.Host,
            nameof(WorkspaceKind.Debugger) => WorkspaceKinds.Debugger,
            nameof(WorkspaceKind.Interactive) => WorkspaceKinds.Interactive,
            nameof(WorkspaceKind.MetadataAsSource) => WorkspaceKinds.MetadataAsSource,
            nameof(WorkspaceKind.MiscellaneousFiles) => WorkspaceKinds.MiscellaneousFiles,
            nameof(WorkspaceKind.Preview) => WorkspaceKinds.Preview,
            nameof(WorkspaceKind.MSBuild) => WorkspaceKinds.MSBuild,
#if !CODE_STYLE
            nameof(WorkspaceKind.Custom) => WorkspaceKinds.Custom,
            nameof(WorkspaceKind.SemanticSearch) => WorkspaceKinds.SemanticSearch,
            nameof(WorkspaceKind.CloudEnvironmentClientWorkspace) => WorkspaceKinds.CloudEnvironmentClientWorkspace,
            nameof(WorkspaceKind.RemoteWorkspace) => WorkspaceKinds.RemoteWorkspace,
#endif
            _ => WorkspaceKinds.Unknown
        };
}
