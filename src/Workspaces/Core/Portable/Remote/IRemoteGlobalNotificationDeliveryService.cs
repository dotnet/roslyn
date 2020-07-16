// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteGlobalNotificationDeliveryService
    {
        void OnGlobalOperationStarted();

        void OnGlobalOperationStopped(IReadOnlyList<string> operations, bool cancelled);
    }
}
