// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Test.Utilities.Notification
{
    [Export(typeof(IGlobalOperationNotificationService)), PartNotDiscoverable, Shared]
    internal sealed class TestGlobalOperationNotificationService : AbstractGlobalOperationNotificationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestGlobalOperationNotificationService(
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(listenerProvider)
        {
        }
    }
}
