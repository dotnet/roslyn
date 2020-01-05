// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.UI.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(IDebugStateChangeListener))]
    internal sealed class VisualStudioDebugStateChangeListener : IDebugStateChangeListener
    {
        private readonly Workspace _workspace;
        private readonly IDebuggingWorkspaceService _debuggingService;

        // EnC service or null if EnC is disabled for the debug session.
        private IEditAndContinueWorkspaceService? _encService;

        /// <summary>
        /// Concord component. Singleton created on demand during debugging session and discarded as soon as the session ends.
        /// </summary>
        private sealed class DebuggerService : IDkmCustomMessageForwardReceiver, IDkmModuleInstanceLoadNotification, IDkmModuleInstanceUnloadNotification
        {
            private IEditAndContinueWorkspaceService? _encService;

            /// <summary>
            /// Message source id as specified in ManagedEditAndContinueService.vsdconfigxml.
            /// </summary>
            public static readonly Guid MessageSourceId = new Guid("730432E7-1B68-4B3A-BD6A-BD4C13E0566B");

            DkmCustomMessage? IDkmCustomMessageForwardReceiver.SendLower(DkmCustomMessage customMessage)
            {
                // Initialize the listener before OnModuleInstanceLoad/OnModuleInstanceUnload can be triggered.
                // These events are only called when managed debugging is being used due to RuntimeId=DkmRuntimeId.Clr filter in vsdconfigxml.
                _encService = (IEditAndContinueWorkspaceService)customMessage.Parameter1;
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
                    Contract.ThrowIfNull(_encService);
                    _encService.OnManagedModuleInstanceLoaded(clrModuleInstance.Mvid);
                }
            }

            /// <summary>
            /// <see cref="IDkmModuleInstanceUnloadNotification"/> is implemented by components that want to listen
            /// for the ModuleInstanceUnload event. When this notification fires, the target process
            /// will be suspended and can be examined. ModuleInstanceUnload is sent when the monitor
            /// detects that a module has unloaded from within the target process.
            /// </summary>
            void IDkmModuleInstanceUnloadNotification.OnModuleInstanceUnload(DkmModuleInstance moduleInstance, DkmWorkList workList, DkmEventDescriptor eventDescriptor)
            {
                if (moduleInstance is DkmClrModuleInstance clrModuleInstance)
                {
                    Contract.ThrowIfNull(_encService);
                    _encService.OnManagedModuleInstanceUnloaded(clrModuleInstance.Mvid);
                }
            }
        }

        [ImportingConstructor]
        public VisualStudioDebugStateChangeListener(VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
            _debuggingService = workspace.Services.GetRequiredService<IDebuggingWorkspaceService>();
        }

        /// <summary>
        /// Called by the debugger when a debugging session starts and managed debugging is being used.
        /// </summary>
        public void StartDebugging(DebugSessionOptions options)
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Design, DebuggingState.Run);

            if ((options & DebugSessionOptions.EditAndContinueDisabled) == 0)
            {
                _encService = _workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();

                // hook up a callbacks (the call blocks until the message is processed):
                using (DebuggerComponent.ManagedEditAndContinueService())
                {
                    DkmCustomMessage.Create(
                        Connection: null,
                        Process: null,
                        SourceId: DebuggerService.MessageSourceId,
                        MessageCode: 0,
                        Parameter1: _encService,
                        Parameter2: null).SendLower();
                }

                _encService.StartDebuggingSession();
            }
            else
            {
                _encService = null;
            }
        }

        public void EnterBreakState()
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Break);
            _encService?.StartEditSession();
        }

        public void ExitBreakState()
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Break, DebuggingState.Run);
            _encService?.EndEditSession();
        }

        public void StopDebugging()
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Design);
            _encService?.EndDebuggingSession();
        }
    }
}
