// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteGlobalNotificationDeliveryService
    {
        ValueTask OnGlobalOperationStartedAsync(CancellationToken cancellationToken);

        ValueTask OnGlobalOperationStoppedAsync(CancellationToken cancellationToken);
    }
}
