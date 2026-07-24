// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class RemoteProjectItemInstance
{
    private readonly RpcClient _client;
    private readonly int _remoteProjectItemInstanceTargetObject;

    public RemoteProjectItemInstance(RpcClient client, int remoteProjectItemInstanceTargetObject)
    {
        _client = client;
        _remoteProjectItemInstanceTargetObject = remoteProjectItemInstanceTargetObject;
    }

    public Task<string> GetMetadataValueAsync(string name, CancellationToken cancellationToken)
        => _client.InvokeAsync<string>(_remoteProjectItemInstanceTargetObject, nameof(IProjectItemInstance.GetMetadataValue), parameters: [name], cancellationToken);
}
