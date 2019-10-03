// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.UI.Interfaces;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(IDebugStateChangeListener))]
    internal sealed class VisualStudioDebugStateChangeListener : IDebugStateChangeListener
    {
        private readonly IDebuggingWorkspaceService _debuggingService;
        private readonly IEditAndContinueWorkspaceService _encService;
        private readonly VisualStudioDebuggeeModuleMetadataProvider _moduleMetadataProvider;

        /// <summary>
        /// Concord component. Singleton created on demand during debugging session and discarded as soon as the session ends.
        /// </summary>
        private sealed class DebuggerService : IDkmCustomMessageForwardReceiver, IDkmModuleInstanceLoadNotification, IDkmModuleInstanceUnloadNotification
        {
            private VisualStudioDebugStateChangeListener _listener;

            /// <summary>
            /// Message source id as specified in ManagedEditAndContinueService.vsdconfigxml.
            /// </summary>
            public static readonly Guid MessageSourceId = new Guid("730432E7-1B68-4B3A-BD6A-BD4C13E0566B");

            DkmCustomMessage IDkmCustomMessageForwardReceiver.SendLower(DkmCustomMessage customMessage)
            {
                _listener = (VisualStudioDebugStateChangeListener)customMessage.Parameter1;
                return null;
            }

            /// <summary>
            /// <see cref="IDkmModuleInstanceLoadNotification"/> is implemented by components that want to listen
            /// for the ModuleInstanceLoad event. When this notification fires, the target process
            /// will be suspended and can be examined. ModuleInstanceLoad is fired when a module is
            /// loaded by a target process. Among other things, this event is used for symbol
            /// providers to load symbols, and for the breakpoint manager to set breakpoints.
            /// ModuleInstanceLoad fires for all modules, even if there are no symbols loaded.
            /// </summary>
            void IDkmModuleInstanceLoadNotification.OnModuleInstanceLoad(DkmModuleInstance moduleInstance, DkmWorkList workList, DkmEventDescriptorS eventDescriptor)
            {
                if (moduleInstance is DkmClrModuleInstance clrModuleInstance)
                {
                    _listener.OnManagedModuleInstanceLoaded(clrModuleInstance);
                }
            }

            /// <summary>
            /// <see cref="IDkmModuleInstanceUnloadNotification"/> is implemented by components that want to listen
            /// for the ModuleInstanceUnload event. When this notification fires, the target process
            /// will be suspended and can be examined. ModuleInstanceUnload is sent when the monitor
            /// detects that a module has unloaded from within the target process.
            /// </summary>
            public void OnModuleInstanceUnload(DkmModuleInstance moduleInstance, DkmWorkList workList, DkmEventDescriptor eventDescriptor)
            {
                if (moduleInstance is DkmClrModuleInstance clrModuleInstance)
                {
                    _listener.OnManagedModuleInstanceUnloaded(clrModuleInstance);
                }
            }
        }

        [ImportingConstructor]
        public VisualStudioDebugStateChangeListener(VisualStudioWorkspace workspace, VisualStudioDebuggeeModuleMetadataProvider moduleMetadataProvider)
        {
            _moduleMetadataProvider = moduleMetadataProvider;
            _debuggingService = workspace.Services.GetRequiredService<IDebuggingWorkspaceService>();
            _encService = workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();
        }

        public void StartDebugging()
        {
            // hook up a callbacks (the call blocks until the message is processed):
            using (DebuggerComponent.ManagedEditAndContinueService())
            {
                DkmCustomMessage.Create(
                    Connection: null,
                    Process: null,
                    SourceId: DebuggerService.MessageSourceId,
                    MessageCode: 0,
                    Parameter1: this,
                    Parameter2: null).SendLower();
            }

            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Design, DebuggingState.Run);

            _encService.StartDebuggingSession();
        }

        public void EnterBreakState(BreakStateKind kind)
        {
            // When stopped at exception - start an edit session as usual and report a rude edit for all changes we see.
            bool stoppedAtException = kind == BreakStateKind.StoppedAtException;

            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Break);
            _encService.StartEditSession();
        }

        public void ExitBreakState()
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Break, DebuggingState.Run);
            _encService.EndEditSession();
        }

        public void StopDebugging()
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Design);
            _encService.EndDebuggingSession();
        }

        internal void OnManagedModuleInstanceLoaded(DkmClrModuleInstance moduleInstance)
            => _encService.OnManagedModuleInstanceLoaded(moduleInstance.Mvid);

        internal void OnManagedModuleInstanceUnloaded(DkmClrModuleInstance moduleInstance)
            => _encService.OnManagedModuleInstanceUnloaded(moduleInstance.Mvid);
    }
}
