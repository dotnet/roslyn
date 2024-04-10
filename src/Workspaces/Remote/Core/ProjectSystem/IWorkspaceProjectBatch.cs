// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote.ProjectSystem;

/// <summary>
/// A batch returned from <see cref="IWorkspaceProject.StartBatchAsync(CancellationToken)" />. <see cref="ApplyAsync" /> must be called before disposing the batch object, which is otherwise a no-op.
/// The dispose just releases any lifetime object tracking since this is an [RpcMarshalable] type.
/// </summary>
[RpcMarshalable]
internal interface IWorkspaceProjectBatch : IDisposable
{
    Task ApplyAsync(CancellationToken cancellationToken);
}
