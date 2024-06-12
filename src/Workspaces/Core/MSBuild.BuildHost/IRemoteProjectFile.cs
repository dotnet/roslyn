// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Logging;

#if !DOTNET_BUILD_FROM_SOURCE
using StreamJsonRpc;
#endif

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

/// <summary>
/// A trimmed down interface of <see cref="IProjectFile"/> that is usable for RPC to the build host process and meets all the requirements of being an RPC marshable interface.
/// </summary>
#if !DOTNET_BUILD_FROM_SOURCE
[RpcMarshalable]
#endif
internal interface IRemoteProjectFile : IDisposable
{
    Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken);
    Task<ImmutableArray<DiagnosticLogItem>> GetDiagnosticLogItemsAsync(CancellationToken cancellationToken);
}
