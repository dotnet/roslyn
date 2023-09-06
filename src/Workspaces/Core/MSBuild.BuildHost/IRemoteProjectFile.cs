// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

/// <summary>
/// A trimmed down interface of <see cref="IProjectFile"/> that is usable for RPC to the build host process and meets all the requirements of being an <see cref="RpcMarshalableAttribute"/> interface.
/// </summary>
[RpcMarshalable]
internal interface IRemoteProjectFile : IDisposable
{
    Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken);
    Task<ImmutableArray<DiagnosticLogItem>> GetDiagnosticLogItemsAsync(CancellationToken cancellationToken);
}
