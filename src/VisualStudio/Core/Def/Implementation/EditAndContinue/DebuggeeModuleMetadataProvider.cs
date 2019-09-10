// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Roslyn.Utilities;

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

        private readonly DebuggeeModuleInfoCache _baselineMetadata;

        [ImportingConstructor]
        public DebuggeeModuleMetadataProvider()
        {
            _baselineMetadata = new DebuggeeModuleInfoCache();
        }

        private void OnModuleInstanceUnload(Guid mvid)
            => _baselineMetadata.Remove(mvid);

        /// <summary>
        /// Finds a module of given MVID in one of the processes being debugged and returns its baseline metadata.
        /// Shall only be called while in debug mode.
        /// Shall only be called on MTA thread.
        /// </summary>
        public DebuggeeModuleInfo TryGetBaselineModuleInfo(Guid mvid)
        {
            Contract.ThrowIfFalse(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA);

            return _baselineMetadata.GetOrAdd(mvid, m =>
            {
                using (DebuggerComponent.ManagedEditAndContinueService())
                {
                    var clrModuleInstance = FindClrModuleInstance(m);
                    if (clrModuleInstance == null)
                    {
                        return default;
                    }

                    if (!TryGetBaselineModuleInfo(clrModuleInstance, out var metadata, out var symReader))
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

                    return new DebuggeeModuleInfo(metadata, symReader);
                }
            });
        }

        private static bool TryGetBaselineModuleInfo(DkmClrModuleInstance module, out ModuleMetadata metadata, out ISymUnmanagedReader5 symReader)
        {
            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA);

            IntPtr metadataPtr;
            uint metadataSize;
            try
            {
                metadataPtr = module.GetBaselineMetaDataBytesPtr(out metadataSize);
            }
            catch (Exception e) when (DkmExceptionUtilities.IsBadOrMissingMetadataException(e))
            {
                metadata = null;
                symReader = null;
                return false;
            }

            symReader = module.GetSymUnmanagedReader() as ISymUnmanagedReader5;
            if (symReader == null)
            {
                metadata = null;
                symReader = null;
                return false;
            }

            metadata = ModuleMetadata.CreateFromMetadata(metadataPtr, (int)metadataSize);
            return true;
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
