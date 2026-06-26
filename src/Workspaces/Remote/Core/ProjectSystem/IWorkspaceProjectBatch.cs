// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote.ProjectSystem;

// We aren't actually running in an AOT process, but we want to be compatible with other libraries that are
// configured for AOT. Thus suppressing the warnings for now.
#pragma warning disable StreamJsonRpc0002 // Declare partial interface
#pragma warning disable StreamJsonRpc0008 // Add methods to PolyType shape for RPC contract interface

/// <summary>
/// A batch returned from <see cref="IWorkspaceProject.StartBatchAsync(CancellationToken)" />. <see cref="ApplyAsync" /> must be called before disposing the batch object, which is otherwise a no-op.
/// The dispose just releases any lifetime object tracking since this is an [RpcMarshalable] type.
/// </summary>
[RpcMarshalable]
internal interface IWorkspaceProjectBatch : IDisposable
{
    Task ApplyAsync(CancellationToken cancellationToken);
}
#pragma warning restore StreamJsonRpc0008 // Add methods to PolyType shape for RPC contract interface
#pragma warning restore StreamJsonRpc0002 // Declare partial interface
