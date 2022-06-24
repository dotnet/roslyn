// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote;

#if TODO // Uncomment once https://github.com/microsoft/vs-streamjsonrpc/issues/789 is fixed
internal interface IRemoteOptionsCallback<TOptions>
{
    ValueTask<TOptions> GetOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken);
}
#endif
