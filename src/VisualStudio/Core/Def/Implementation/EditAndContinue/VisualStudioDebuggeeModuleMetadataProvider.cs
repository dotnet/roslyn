// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Shared]
    [Export(typeof(IDebuggeeModuleMetadataProvider))]
    [Export(typeof(VisualStudioDebuggeeModuleMetadataProvider))]
    internal sealed class VisualStudioDebuggeeModuleMetadataProvider : IDebuggeeModuleMetadataProvider
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
                private readonly VisualStudioDebuggeeModuleMetadataProvider _provider;
                private readonly Guid _mvid;

                public DataItem(VisualStudioDebuggeeModuleMetadataProvider provider, Guid mvid)
                {
                    _provider = provider;
                    _mvid = mvid;
                }

                protected override void OnClose() => _provider.OnModuleInstanceUnload(_mvid);
            }

            DkmCustomMessage IDkmCustomMessageForwardReceiver.SendLower(DkmCustomMessage customMessage)
            {
                var provider = (VisualStudioDebuggeeModuleMetadataProvider)customMessage.Parameter1;
                var clrModuleInstance = (DkmClrModuleInstance)customMessage.Parameter2;

                // Note that this call has to be made in a Concord component.
                // The debugger tracks what the current Concord component is and associates the data item with it.
                clrModuleInstance.SetDataItem(DkmDataCreationDisposition.CreateAlways, new DataItem(provider, clrModuleInstance.Mvid));
                return null;
            }
        }

        private readonly DebuggeeModuleInfoCache _baselineMetadata;

        public VisualStudioDebuggeeModuleMetadataProvider()
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
        /// <returns>Null, if the module with the specified MVID is not found.</returns>
        public DebuggeeModuleInfo TryGetBaselineModuleInfo(Guid mvid)
        {
            Contract.ThrowIfFalse(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA);

            return _baselineMetadata.GetOrAdd(mvid, m =>
            {
                using (DebuggerComponent.ManagedEditAndContinueService())
                {
                    var clrModuleInstance = EnumerateClrModuleInstances(m).FirstOrDefault();
                    if (clrModuleInstance == null)
                    {
                        return null;
                    }

                    // The DebuggeeModuleInfo holds on a pointer to module metadata and on a SymReader instance.
                    // 
                    // The module metadata lifetime is that of the module instance, so we need to stop using the module
                    // as soon as the module instance is unloaded. We do so by associating a DataItem with the module 
                    // instance and removing the metadata from our cache when the DataItem is disposed.
                    // 
                    // The SymReader instance is shared across all instancese of the module.
                    if (!clrModuleInstance.TryGetModuleInfo(out var info))
                    {
                        return null;
                    }

                    // hook up a callback on module unload (the call blocks until the message is processed):
                    DkmCustomMessage.Create(
                        Connection: clrModuleInstance.Process.Connection,
                        Process: clrModuleInstance.Process,
                        SourceId: DebuggerService.MessageSourceId,
                        MessageCode: 0,
                        Parameter1: this,
                        Parameter2: clrModuleInstance).SendLower();

                    return info;
                }
            });
        }

        /// <summary>
        /// Check that EnC is available for all instances of the given module.
        /// </summary>
        public bool IsEditAndContinueAvailable(Guid mvid, out int errorCode, out string localizedMessage)
        {
            using (DebuggerComponent.ManagedEditAndContinueService())
            {
                foreach (var clrModuleInstance in EnumerateClrModuleInstances(mvid))
                {
                    var availability = clrModuleInstance.GetEncAvailability(out localizedMessage);
                    if (availability != DkmEncAvailableStatus.Available)
                    {
                        errorCode = (int)availability;
                        return false;
                    }
                }
            }

            errorCode = -1;
            localizedMessage = null;
            return true;
        }

        private static IEnumerable<DkmClrModuleInstance> EnumerateClrModuleInstances(Guid? mvid)
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
                                (!mvid.HasValue || clrModuleInstance.Mvid == mvid.Value))
                            {
                                yield return clrModuleInstance;
                            }
                        }
                    }
                }
            }
        }

        public void PrepareModuleForUpdate(Guid mvid)
        {
            // fire and forget
            _ = Task.Run(new Action(() =>
            {
                Contract.ThrowIfFalse(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA);

                using (DebuggerComponent.ManagedEditAndContinueService())
                {
                    var clrModuleInstance = EnumerateClrModuleInstances(mvid).FirstOrDefault();
                    if (clrModuleInstance != null)
                    {
                        ((ISymUnmanagedEncUpdate)clrModuleInstance.GetSymUnmanagedReader()).InitializeForEnc();
                    }
                }
            }));
        }
    }
}
