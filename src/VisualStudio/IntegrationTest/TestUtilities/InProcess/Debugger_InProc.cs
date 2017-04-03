// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using EnvDTE;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class Debugger_InProc : InProcComponent
    {
        public static Debugger_InProc Create() => new Debugger_InProc();

        public void StartDebugging(bool waitForBreakMode)
        {
            var debuggerEventsMonitor = new VsDebuggerEventsMonitor(GetDTE().Events.DebuggerEvents);
            if (waitForBreakMode)
            {
                StartAndWaitForDebuggerBreakMode(debuggerEventsMonitor);
            }
            else
            {
                StartAndWaitForDebuggerRunMode(debuggerEventsMonitor);
            }
        }

        public void Continue(bool waitForBreakMode)
        {
            Helper.Retry(() => GetDTE().Debugger.Go(waitForBreakMode), TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(20));
        }

        private void StartAndWaitForDebuggerMode(
            Action<VsDebuggerEventsMonitor.DebuggerModeChangeEventHandler> addEvent,
            Action<VsDebuggerEventsMonitor.DebuggerModeChangeEventHandler> removeEvent)
        {
            using (var resetEvent = new ManualResetEventSlim(false))
            {
                VsDebuggerEventsMonitor.DebuggerModeChangeEventHandler setEvent = (mode, reason) => resetEvent.Set();
                addEvent(setEvent);
                ExecuteCommand("Debug.Start");
                resetEvent.Wait();
                removeEvent(setEvent);
            }
        }

        private void StartAndWaitForDebuggerDesignMode(VsDebuggerEventsMonitor debuggerEventsMonitor)
            => StartAndWaitForDebuggerMode(
                @event => debuggerEventsMonitor.OnEnterDesignMode += @event,
                @event => debuggerEventsMonitor.OnEnterDesignMode -= @event);

        private void StartAndWaitForDebuggerRunMode(VsDebuggerEventsMonitor debuggerEventsMonitor)
            => StartAndWaitForDebuggerMode(
                @event => debuggerEventsMonitor.OnEnterRunMode += @event,
                @event => debuggerEventsMonitor.OnEnterRunMode -= @event);

        private void StartAndWaitForDebuggerBreakMode(VsDebuggerEventsMonitor debuggerEventsMonitor)
            => StartAndWaitForDebuggerMode(
                @event => debuggerEventsMonitor.OnEnterBreakMode += @event,
                @event => debuggerEventsMonitor.OnEnterBreakMode -= @event);

        public enum DebuggerMode
        {
            BreakMode = dbgDebugMode.dbgBreakMode,
            DesignMode = dbgDebugMode.dbgDesignMode,
            RunMode = dbgDebugMode.dbgRunMode
        }

        internal class VsDebuggerEventsMonitor
        {
            private DebuggerEvents debuggerEvents;

            internal delegate void DebuggerModeChangeEventHandler(DebuggerMode mode, EnvDTE80.dbgEventReason2 reason);
            internal delegate void DebuggerExceptionEventHandler(string type, string name, int code, string description);

            public event DebuggerModeChangeEventHandler OnEnterBreakMode;
            public event DebuggerModeChangeEventHandler OnEnterRunMode;
            public event DebuggerModeChangeEventHandler OnEnterDesignMode;
            public event DebuggerModeChangeEventHandler OnDebuggerModeChange;
            public event DebuggerExceptionEventHandler OnExceptionThrown;
            public event DebuggerExceptionEventHandler OnExceptionNotHandled;

            public VsDebuggerEventsMonitor(DebuggerEvents debuggerEvents)
            {
                this.debuggerEvents = debuggerEvents;
                this.debuggerEvents.OnEnterBreakMode += FireEnterBreakModeEvent;
                this.debuggerEvents.OnEnterDesignMode += FireEnterDesignModeEvent;
                this.debuggerEvents.OnEnterRunMode += FireEnterRunModeEvent;
                this.debuggerEvents.OnExceptionThrown += FireExceptionThrownEvent;
                this.debuggerEvents.OnExceptionNotHandled += FireExceptionNotHandledEvent;
            }

            private void FireEnterBreakModeEvent(EnvDTE.dbgEventReason reason, ref EnvDTE.dbgExecutionAction executionAction)
            {
                OnEnterBreakMode?.Invoke(DebuggerMode.BreakMode, (EnvDTE80.dbgEventReason2)reason);
                OnDebuggerModeChange?.Invoke(DebuggerMode.BreakMode, (EnvDTE80.dbgEventReason2)reason);
            }

            private void FireEnterDesignModeEvent(EnvDTE.dbgEventReason reason)
            {
                OnEnterDesignMode?.Invoke(DebuggerMode.DesignMode, (EnvDTE80.dbgEventReason2)reason);
                OnDebuggerModeChange?.Invoke(DebuggerMode.DesignMode, (EnvDTE80.dbgEventReason2)reason);
            }

            private void FireEnterRunModeEvent(EnvDTE.dbgEventReason reason)
            {
                OnEnterRunMode?.Invoke(DebuggerMode.RunMode, (EnvDTE80.dbgEventReason2)reason);
                OnDebuggerModeChange?.Invoke(DebuggerMode.RunMode, (EnvDTE80.dbgEventReason2)reason);
            }

            private void FireExceptionThrownEvent(string exceptionType, string name, int code, string description, ref EnvDTE.dbgExceptionAction exceptionAction)
            {
                OnExceptionThrown?.Invoke(exceptionType, name, code, description);
            }

            private void FireExceptionNotHandledEvent(string exceptionType, string name, int code, string description, ref EnvDTE.dbgExceptionAction exceptionAction)
            {
                OnExceptionNotHandled?.Invoke(exceptionType, name, code, description);
            }
        }
    }
}