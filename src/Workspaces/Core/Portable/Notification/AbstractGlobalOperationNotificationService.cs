// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Notification
{
    internal abstract class AbstractGlobalOperationNotificationService : IGlobalOperationNotificationService
    {
        public abstract event EventHandler Started;
        public abstract event EventHandler<GlobalOperationEventArgs> Stopped;

        public abstract GlobalOperationRegistration Start(string reason);

        public abstract void Cancel(GlobalOperationRegistration registration);
        public abstract void Done(GlobalOperationRegistration registration);
    }
}
