// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Notification;

[Export(typeof(IGlobalOperationNotificationService)), Shared]
internal partial class VisualStudioGlobalOperationNotificationService : AbstractGlobalOperationNotificationService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioGlobalOperationNotificationService(
        IAsynchronousOperationListenerProvider listenerProvider)
        : base(listenerProvider)
    {
    }
}
