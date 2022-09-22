// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote.Services;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteGlobalNotificationDeliveryService : BrokeredServiceBase, IRemoteGlobalNotificationDeliveryService
    {
        internal sealed class Factory : FactoryBase<IRemoteGlobalNotificationDeliveryService>
        {
            protected override IRemoteGlobalNotificationDeliveryService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteGlobalNotificationDeliveryService(arguments);
        }

        public RemoteGlobalNotificationDeliveryService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask OnGlobalOperationStartedAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                var globalOperationNotificationService = GetGlobalOperationNotificationService();
                globalOperationNotificationService?.OnStarted();
                return default;
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask OnGlobalOperationStoppedAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                var globalOperationNotificationService = GetGlobalOperationNotificationService();
                globalOperationNotificationService?.OnStopped();
                return default;
            }, cancellationToken);
        }

        private RemoteGlobalOperationNotificationService? GetGlobalOperationNotificationService()
            => GetWorkspace().Services.GetService<IGlobalOperationNotificationService>() as RemoteGlobalOperationNotificationService;
    }
}
