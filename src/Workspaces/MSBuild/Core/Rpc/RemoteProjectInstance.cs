// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class RemoteProjectInstance
{
    private readonly RpcClient _client;
    private readonly int _remoteProjectInstanceTargetObject;

    public RemoteProjectInstance(RpcClient client, int remoteProjectInstanceTargetObject)
    {
        _client = client;
        _remoteProjectInstanceTargetObject = remoteProjectInstanceTargetObject;
    }

    public async Task<ImmutableArray<DiagnosticLogItem>> GetDiagnosticLogItemsAsync(CancellationToken cancellationToken)
    {
        var diagnostics = await _client.InvokeAsync<DiagnosticLogItem[]>(_remoteProjectInstanceTargetObject, nameof(IProjectInstance.GetDiagnosticLogItems), parameters: [], cancellationToken).ConfigureAwait(false);
        return diagnostics.ToImmutableArray();
    }

    public Task<string> GetPropertyValueAsync(string propertyName, CancellationToken cancellationToken)
        => _client.InvokeAsync<string>(_remoteProjectInstanceTargetObject, nameof(IProjectInstance.GetPropertyValue), parameters: [propertyName], cancellationToken);
}
