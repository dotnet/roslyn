//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System;

//namespace Microsoft.CodeAnalysis.Notification
//{
//    internal abstract partial class AbstractGlobalOperationNotificationService : IGlobalOperationNotificationService
//    {
//        public abstract event EventHandler? Started;
//        public abstract event EventHandler? Stopped;

//        protected abstract void Done(GlobalOperationRegistration registration);
//        protected abstract void StartWorker(string reason);

//        public IDisposable Start(string operation)
//        {
//            StartWorker(reason);
//            return new GlobalOperationRegistration(this, operation);
//        }
//    }
//}
