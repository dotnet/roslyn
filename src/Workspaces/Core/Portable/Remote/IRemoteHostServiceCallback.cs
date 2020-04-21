// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteHostServiceCallback
    {
        Task GetAssetsAsync(int scopeId, Checksum[] checksums, string pipeName, CancellationToken cancellationToken);

        // TODO: remove (https://github.com/dotnet/roslyn/issues/43477)
        Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken);
    }
}
