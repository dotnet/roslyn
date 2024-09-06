// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Notification;

internal partial class AbstractGlobalOperationNotificationService
{
    private class GlobalOperationRegistration(AbstractGlobalOperationNotificationService service) : IDisposable
    {
        public void Dispose()
        {
            // Inform any listeners that we're finished.
            service.Done(this);
        }
    }
}
