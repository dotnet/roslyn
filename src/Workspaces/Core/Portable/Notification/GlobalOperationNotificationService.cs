// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Notification
{
    [ExportWorkspaceService(typeof(IGlobalOperationNotificationService)), Shared]
    internal partial class GlobalOperationNotificationService : IGlobalOperationNotificationService
    {
        private const string GlobalOperationStartedEventName = "GlobalOperationStarted";
        private const string GlobalOperationStoppedEventName = "GlobalOperationStopped";

        private readonly object _gate = new();

        private readonly HashSet<IDisposable> _registrations = new();
        private readonly HashSet<string> _operations = new();
        private readonly EventMap _eventMap = new();

        private readonly TaskQueue _eventQueue;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GlobalOperationNotificationService(IAsynchronousOperationListenerProvider listenerProvider)
        {
            _eventQueue = new TaskQueue(listenerProvider.GetListener(FeatureAttribute.GlobalOperation), TaskScheduler.Default);
        }

        public event EventHandler Started
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

        public event EventHandler Stopped
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

        private void RaiseGlobalOperationStarted()
        {
            var ev = _eventMap.GetEventHandlers<EventHandler>(GlobalOperationStartedEventName);
            if (ev.HasHandlers)
                _eventQueue.ScheduleTask(GlobalOperationStartedEventName, () => ev.RaiseEvent(handler => handler(this, EventArgs.Empty)), CancellationToken.None);
        }

        private void RaiseGlobalOperationStopped()
        {
            var ev = _eventMap.GetEventHandlers<EventHandler>(GlobalOperationStoppedEventName);
            if (ev.HasHandlers)
                _eventQueue.ScheduleTask(GlobalOperationStoppedEventName, () => ev.RaiseEvent(handler => handler(this, EventArgs.Empty)), CancellationToken.None);
        }

        public IGlobalOperationRegistration Start(string operation)
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
                    RaiseGlobalOperationStarted();
                }

                return registration;
            }
        }

        private void Done(GlobalOperationRegistration registration)
        {
            lock (_gate)
            {
                var result = _registrations.Remove(registration);
                Contract.ThrowIfFalse(result);

                if (_registrations.Count == 0)
                {
                    _operations.Clear();
                    RaiseGlobalOperationStopped();
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
