// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.Notification
{
    internal abstract class AbstractGlobalOperationNotificationService : IGlobalOperationNotificationService
    {
        public abstract event EventHandler Started;
        public abstract event EventHandler<GlobalOperationEventArgs> Stopped;

        public abstract GlobalOperationRegistration Start(string reason);

        public abstract void Done(GlobalOperationRegistration registration);
    }
}
