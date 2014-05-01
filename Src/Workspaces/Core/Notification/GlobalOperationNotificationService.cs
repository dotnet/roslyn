// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private readonly object gate = new object();

        private readonly HashSet<GlobalOperationRegistration> registrations = new HashSet<GlobalOperationRegistration>();
        private readonly HashSet<string> operations = new HashSet<string>();

        private readonly SimpleTaskQueue eventQueue = new SimpleTaskQueue(TaskScheduler.Default);
        private readonly EventMap eventMap = new EventMap();

        public GlobalOperationNotificationService()
        {
            // left  blank
        }

        public override GlobalOperationRegistration Start(string operation)
        {
            lock (gate)
            {
                // create new registration
                var registration = new GlobalOperationRegistration(this, operation);

                // states
                this.registrations.Add(registration);
                this.operations.Add(operation);

                // the very first one
                if (this.registrations.Count == 1)
                {
                    Contract.ThrowIfFalse(this.operations.Count == 1);
                    RaiseGlobalOperationStarted();
                }

                return registration;
            }
        }

        protected virtual Task RaiseGlobalOperationStarted()
        {
            var handlers = this.eventMap.GetEventHandlers<EventHandler>(GlobalOperationStartedEventName);
            if (handlers.Length > 0)
            {
                return this.eventQueue.ScheduleTask(() =>
                {
                    foreach (var handler in handlers)
                    {
                        handler(this, EventArgs.Empty);
                    }
                });
            }

            return SpecializedTasks.EmptyTask;
        }

        protected virtual Task RaiseGlobalOperationStopped(IReadOnlyList<string> operations, bool cancelled)
        {
            var handlers = this.eventMap.GetEventHandlers<EventHandler<GlobalOperationEventArgs>>(GlobalOperationStoppedEventName);
            if (handlers.Length > 0)
            {
                var args = new GlobalOperationEventArgs(operations, cancelled);

                return this.eventQueue.ScheduleTask(() =>
                {
                    foreach (var handler in handlers)
                    {
                        handler(this, args);
                    }
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
                this.eventMap.AddEventHandler(GlobalOperationStartedEventName, value);
            }

            remove
            {
                this.eventMap.RemoveEventHandler(GlobalOperationStartedEventName, value);
            }
        }

        public override event EventHandler<GlobalOperationEventArgs> Stopped
        {
            add
            {
                // currently, if one subscribes while a global operation is already in progress, it will not be notified for 
                // that one.
                this.eventMap.AddEventHandler(GlobalOperationStoppedEventName, value);
            }

            remove
            {
                this.eventMap.RemoveEventHandler(GlobalOperationStoppedEventName, value);
            }
        }

        public override void Cancel(GlobalOperationRegistration registration)
        {
            lock (gate)
            {
                var result = this.registrations.Remove(registration);
                Contract.ThrowIfFalse(result);

                if (this.registrations.Count == 0)
                {
                    var operations = this.operations.AsImmutable();
                    this.operations.Clear();

                    // We don't care if an individual operation has canceled.
                    // We only care whether whole thing has cancelled or not.
                    RaiseGlobalOperationStopped(operations, cancelled: true);
                }
            }
        }

        public override void Done(GlobalOperationRegistration registration)
        {
            lock (gate)
            {
                var result = this.registrations.Remove(registration);
                Contract.ThrowIfFalse(result);

                if (this.registrations.Count == 0)
                {
                    var operations = this.operations.AsImmutable();
                    this.operations.Clear();

                    RaiseGlobalOperationStopped(operations, cancelled: false);
                }
            }
        }

        ~GlobalOperationNotificationService()
        {
            Contract.ThrowIfFalse(this.registrations.Count == 0);
            Contract.ThrowIfFalse(this.operations.Count == 0);
        }
    }
}
