// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote.Services;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : ServiceBase, IRemoteGlobalNotificationDeliveryService
    {
        /// <summary>
        /// Remote API.
        /// </summary>
        public void OnGlobalOperationStarted()
        {
            RunService(() =>
            {
                var globalOperationNotificationService = GetGlobalOperationNotificationService();
                globalOperationNotificationService?.OnStarted();
            }, CancellationToken.None);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public void OnGlobalOperationStopped(IReadOnlyList<string> operations, bool cancelled)
        {
            RunService(() =>
            {
                var globalOperationNotificationService = GetGlobalOperationNotificationService();
                globalOperationNotificationService?.OnStopped(operations, cancelled);
            }, CancellationToken.None);
        }

        private RemoteGlobalOperationNotificationService? GetGlobalOperationNotificationService()
            => GetWorkspace().Services.GetService<IGlobalOperationNotificationService>() as RemoteGlobalOperationNotificationService;
    }
}
