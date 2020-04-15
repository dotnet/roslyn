// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Notification
{
    internal class GlobalOperationNotificationService : AbstractGlobalOperationNotificationService
    {
        private const string GlobalOperationStartedEventName = "GlobalOperationStarted";
        private const string GlobalOperationStoppedEventName = "GlobalOperationStopped";

        private readonly object _gate = new object();

        private readonly HashSet<GlobalOperationRegistration> _registrations = new HashSet<GlobalOperationRegistration>();
        private readonly HashSet<string> _operations = new HashSet<string>();

        private readonly TaskQueue _eventQueue;
        private readonly EventMap _eventMap = new EventMap();

        public GlobalOperationNotificationService(IAsynchronousOperationListener listener)
            => _eventQueue = new TaskQueue(listener, TaskScheduler.Default);

        public override GlobalOperationRegistration Start(string operation)
        {
            lock (_gate)
            {
                // create new registration
                var registration = new GlobalOperationRegistration(this, operation);

                // states
                _registrations.Add(registration);
                _operations.Add(operation);

                // the very first one
                if (_registrations.Count == 1)
                {
                    Contract.ThrowIfFalse(_operations.Count == 1);
                    RaiseGlobalOperationStartedAsync();
                }

                return registration;
            }
        }

        private Task RaiseGlobalOperationStartedAsync()
        {
            var ev = _eventMap.GetEventHandlers<EventHandler>(GlobalOperationStartedEventName);
            if (ev.HasHandlers)
            {
                return _eventQueue.ScheduleTask(GlobalOperationStartedEventName, () => ev.RaiseEvent(handler => handler(this, EventArgs.Empty)), CancellationToken.None);
            }

            return Task.CompletedTask;
        }

        private Task RaiseGlobalOperationStoppedAsync(IReadOnlyList<string> operations, bool cancelled)
        {
            var ev = _eventMap.GetEventHandlers<EventHandler<GlobalOperationEventArgs>>(GlobalOperationStoppedEventName);
            if (ev.HasHandlers)
            {
                var args = new GlobalOperationEventArgs(operations, cancelled);
                return _eventQueue.ScheduleTask(GlobalOperationStoppedEventName, () => ev.RaiseEvent(handler => handler(this, args)), CancellationToken.None);
            }

            return Task.CompletedTask;
        }

        public override event EventHandler Started
        {
            add
            {
                // currently, if one subscribes while a global operation is already in progress, it will not be notified for 
                // that one.
                _eventMap.AddEventHandler(GlobalOperationStartedEventName, value);
            }

            remove
            {
                _eventMap.RemoveEventHandler(GlobalOperationStartedEventName, value);
            }
        }

        public override event EventHandler<GlobalOperationEventArgs> Stopped
        {
            add
            {
                // currently, if one subscribes while a global operation is already in progress, it will not be notified for 
                // that one.
                _eventMap.AddEventHandler(GlobalOperationStoppedEventName, value);
            }

            remove
            {
                _eventMap.RemoveEventHandler(GlobalOperationStoppedEventName, value);
            }
        }

        public override void Cancel(GlobalOperationRegistration registration)
        {
            lock (_gate)
            {
                var result = _registrations.Remove(registration);
                Contract.ThrowIfFalse(result);

                if (_registrations.Count == 0)
                {
                    var operations = _operations.AsImmutable();
                    _operations.Clear();

                    // We don't care if an individual operation has canceled.
                    // We only care whether whole thing has cancelled or not.
                    RaiseGlobalOperationStoppedAsync(operations, cancelled: true);
                }
            }
        }

        public override void Done(GlobalOperationRegistration registration)
        {
            lock (_gate)
            {
                var result = _registrations.Remove(registration);
                Contract.ThrowIfFalse(result);

                if (_registrations.Count == 0)
                {
                    var operations = _operations.AsImmutable();
                    _operations.Clear();

                    RaiseGlobalOperationStoppedAsync(operations, cancelled: false);
                }
            }
        }

        ~GlobalOperationNotificationService()
        {
            if (!Environment.HasShutdownStarted)
            {
                Contract.ThrowIfFalse(_registrations.Count == 0);
                Contract.ThrowIfFalse(_operations.Count == 0);
            }
        }
    }
}
