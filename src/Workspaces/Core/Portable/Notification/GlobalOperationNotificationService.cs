// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private readonly SimpleTaskQueue _eventQueue = new SimpleTaskQueue(TaskScheduler.Default);
        private readonly EventMap _eventMap = new EventMap();

        public GlobalOperationNotificationService()
        {
            // left  blank
        }

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
                    RaiseGlobalOperationStarted();
                }

                return registration;
            }
        }

        protected virtual Task RaiseGlobalOperationStarted()
        {
            var ev = _eventMap.GetEventHandlers<EventHandler>(GlobalOperationStartedEventName);
            if (ev.HasHandlers)
            {
                return _eventQueue.ScheduleTask(() =>
                {
                    ev.RaiseEvent(handler => handler(this, EventArgs.Empty));
                });
            }

            return SpecializedTasks.EmptyTask;
        }

        protected virtual Task RaiseGlobalOperationStopped(IReadOnlyList<string> operations, bool cancelled)
        {
            var ev = _eventMap.GetEventHandlers<EventHandler<GlobalOperationEventArgs>>(GlobalOperationStoppedEventName);
            if (ev.HasHandlers)
            {
                var args = new GlobalOperationEventArgs(operations, cancelled);

                return _eventQueue.ScheduleTask(() =>
                {
                    ev.RaiseEvent(handler => handler(this, args));
                });
            }

            return SpecializedTasks.EmptyTask;
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
                    RaiseGlobalOperationStopped(operations, cancelled: true);
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

                    RaiseGlobalOperationStopped(operations, cancelled: false);
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
