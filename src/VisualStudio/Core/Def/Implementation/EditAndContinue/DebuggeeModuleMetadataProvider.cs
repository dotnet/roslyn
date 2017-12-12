// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(IDebuggeeModuleMetadataProvider)), Shared]
    internal sealed class DebuggeeModuleMetadataProvider : IDebuggeeModuleMetadataProvider
    {
        /// <summary>
        /// Concord component. Singleton created on demand during debugging session and discarded as soon as the session ends.
        /// </summary>
        private sealed class DebuggerService : IDkmCustomMessageForwardReceiver
        {
            /// <summary>
            /// Component id as specified in ManagedEditAndContinueService.vsdconfigxml.
            /// </summary>
            public static readonly Guid ComponentId = new Guid("A96BBE03-0408-41E3-8613-6086FD494B43");

            /// <summary>
            /// Message source id as specified in ManagedEditAndContinueService.vsdconfigxml.
            /// </summary>
            public static readonly Guid MessageSourceId = new Guid("58CDF976-1923-48F7-8288-B4189F5700B1");

            private sealed class DataItem : DkmDataItem
            {
                private readonly DebuggeeModuleMetadataProvider _provider;
                private readonly Guid _mvid;

                public DataItem(DebuggeeModuleMetadataProvider provider, Guid mvid)
                {
                    _provider = provider;
                    _mvid = mvid;
                }

                protected override void OnClose() => _provider.OnModuleInstanceUnload(_mvid);
            }

            DkmCustomMessage IDkmCustomMessageForwardReceiver.SendLower(DkmCustomMessage customMessage)
            {
                var provider = (DebuggeeModuleMetadataProvider)customMessage.Parameter1;
                var clrModuleInstance = (DkmClrModuleInstance)customMessage.Parameter2;

                // Note that this call has to be made in a Concord component.
                // The debugger tracks what the current component is and associates the data item with it.
                clrModuleInstance.SetDataItem(DkmDataCreationDisposition.CreateAlways, new DataItem(provider, clrModuleInstance.Mvid));
                return null;
            }
        }

        private readonly DebuggeeModuleMetadataCache _baselineMetadata;

        public DebuggeeModuleMetadataProvider()
        {
            _baselineMetadata = new DebuggeeModuleMetadataCache();
        }

        private void OnModuleInstanceUnload(Guid mvid)
            => _baselineMetadata.Remove(mvid);

        /// <summary>
        /// Finds a module of given MVID in one of the processes being debugged and returns its baseline metadata.
        /// Shall only be called while in debug mode.
        /// </summary>
        public ModuleMetadata TryGetBaselineMetadata(Guid mvid)
        {
            return _baselineMetadata.GetOrAdd(mvid, m =>
            {
                DkmComponentManager.InitializeThread(DebuggerService.ComponentId);

                try
                {
                    var clrModuleInstance = FindClrModuleInstance(m);
                    if (clrModuleInstance == null)
                    {
                        return default;
                    }

                    var metadata = GetBaselineModuleMetadata(clrModuleInstance);
                    if (metadata == null)
                    {
                        return default;
                    }

                    // hook up a callback on module unload (the call blocks until the message is processed):
                    DkmCustomMessage.Create(
                        Connection: clrModuleInstance.Process.Connection,
                        Process: clrModuleInstance.Process,
                        SourceId: DebuggerService.MessageSourceId,
                        MessageCode: 0,
                        Parameter1: this,
                        Parameter2: clrModuleInstance).SendLower();

                    return metadata;
                }
                finally
                {
                    DkmComponentManager.UninitializeThread(DebuggerService.ComponentId);
                }
            });
        }

        private static ModuleMetadata GetBaselineModuleMetadata(DkmClrModuleInstance module)
        {
            IntPtr metadataPtr;
            uint metadataSize;
            try
            {
                metadataPtr = module.GetBaselineMetaDataBytesPtr(out metadataSize);
            }
            catch (Exception e) when (DkmExceptionUtilities.IsBadOrMissingMetadataException(e))
            {
                return null;
            }

            return ModuleMetadata.CreateFromMetadata(metadataPtr, (int)metadataSize);
        }

        private static DkmClrModuleInstance FindClrModuleInstance(Guid mvid)
        {
            foreach (var process in DkmProcess.GetProcesses())
            {
                foreach (var runtimeInstance in process.GetRuntimeInstances())
                {
                    if (runtimeInstance.TagValue == DkmRuntimeInstance.Tag.ClrRuntimeInstance)
                    {
                        foreach (var moduleInstance in runtimeInstance.GetModuleInstances())
                        {
                            if (moduleInstance.TagValue == DkmModuleInstance.Tag.ClrModuleInstance &&
                                moduleInstance is DkmClrModuleInstance clrModuleInstance &&
                                clrModuleInstance.Mvid == mvid)
                            {
                                return clrModuleInstance;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
