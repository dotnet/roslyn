// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Notification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Delivers global notifications to remote services.
    /// </summary>
    internal sealed class GlobalNotificationRemoteDeliveryService : IDisposable
    {
        private enum GlobalNotificationState
        {
            NotStarted,
            Started
        }

        /// <summary>
        /// Lock for the <see cref="_globalNotificationsTask"/> task chain.  Each time we hear 
        /// about a global operation starting or stopping (i.e. a build) we will '.ContinueWith'
        /// this task chain with a new notification to the OOP side.  This way all the messages
        /// are properly serialized and appear in the right order (i.e. we don't hear about a 
        /// stop prior to hearing about the relevant start).
        /// </summary>
        private readonly object _globalNotificationsGate = new object();
        private Task<GlobalNotificationState> _globalNotificationsTask = Task.FromResult(GlobalNotificationState.NotStarted);

        private readonly HostWorkspaceServices _services;
        private readonly CancellationToken _cancellationToken;

        public GlobalNotificationRemoteDeliveryService(HostWorkspaceServices services, CancellationToken cancellationToken)
        {
            _services = services;
            _cancellationToken = cancellationToken;

            RegisterGlobalOperationNotifications();
        }

        public void Dispose()
        {
            UnregisterGlobalOperationNotifications();
        }

        private void RegisterGlobalOperationNotifications()
        {
            var globalOperationService = _services.GetService<IGlobalOperationNotificationService>();
            if (globalOperationService != null)
            {
                globalOperationService.Started += OnGlobalOperationStarted;
                globalOperationService.Stopped += OnGlobalOperationStopped;
            }
        }

        private void UnregisterGlobalOperationNotifications()
        {
            var globalOperationService = _services.GetService<IGlobalOperationNotificationService>();
            if (globalOperationService != null)
            {
                globalOperationService.Started -= OnGlobalOperationStarted;
                globalOperationService.Stopped -= OnGlobalOperationStopped;
            }
        }

        private void OnGlobalOperationStarted(object? sender, EventArgs e)
        {
            lock (_globalNotificationsGate)
            {
                // Pass TaskContinuationOptions.OnlyOnRanToCompletion to avoid delivering further notifications once the task gets canceled or fails.
                // The cancellation happens only when VS is shutting down. The task might fail if communication with OOP fails. 
                // Once that happens there is not point in sending more notifications to the remote service.

                _globalNotificationsTask = _globalNotificationsTask.SafeContinueWithFromAsync(
                    SendStartNotificationAsync, _cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }
        }

        private async Task<GlobalNotificationState> SendStartNotificationAsync(Task<GlobalNotificationState> previousTask)
        {
            // Can only transition from NotStarted->Started.  If we hear about
            // anything else, do nothing.
            if (previousTask.Result != GlobalNotificationState.NotStarted)
            {
                return previousTask.Result;
            }

            var client = await RemoteHostClient.TryGetClientAsync(_services, _cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return previousTask.Result;
            }

            _ = await client.TryInvokeAsync<IRemoteGlobalNotificationDeliveryService>(
                (service, cancellationToken) => service.OnGlobalOperationStartedAsync(cancellationToken),
                _cancellationToken).ConfigureAwait(false);

            return GlobalNotificationState.Started;
        }

        private void OnGlobalOperationStopped(object? sender, EventArgs e)
        {
            lock (_globalNotificationsGate)
            {
                // Pass TaskContinuationOptions.OnlyOnRanToCompletion to avoid delivering further notifications once the task gets canceled or fails.
                // The cancellation happens only when VS is shutting down. The task might fail if communication with OOP fails. 
                // Once that happens there is not point in sending more notifications to the remote service.

                _globalNotificationsTask = _globalNotificationsTask.SafeContinueWithFromAsync(
                    previous => SendStoppedNotificationAsync(previous), _cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }
        }

        private async Task<GlobalNotificationState> SendStoppedNotificationAsync(Task<GlobalNotificationState> previousTask)
        {
            // Can only transition from Started->NotStarted.  If we hear about
            // anything else, do nothing.
            if (previousTask.Result != GlobalNotificationState.Started)
            {
                return previousTask.Result;
            }

            var client = await RemoteHostClient.TryGetClientAsync(_services, _cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return previousTask.Result;
            }

            _ = await client.TryInvokeAsync<IRemoteGlobalNotificationDeliveryService>(
                (service, cancellationToken) => service.OnGlobalOperationStoppedAsync(cancellationToken),
                _cancellationToken).ConfigureAwait(false);

            // Mark that we're stopped now.
            return GlobalNotificationState.NotStarted;
        }
    }
}
