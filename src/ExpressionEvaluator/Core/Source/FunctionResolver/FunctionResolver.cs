// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.FunctionResolution;
using Microsoft.VisualStudio.Debugger.Symbols;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class FunctionResolver :
        FunctionResolverBase<DkmProcess, DkmClrModuleInstance, DkmRuntimeFunctionResolutionRequest>,
        IDkmRuntimeFunctionResolver,
        IDkmModuleInstanceLoadNotification,
        IDkmModuleInstanceUnloadNotification,
        IDkmModuleModifiedNotification,
        IDkmModuleSymbolsLoadedNotification
    {
        void IDkmRuntimeFunctionResolver.EnableResolution(DkmRuntimeFunctionResolutionRequest request, DkmWorkList workList)
        {
            if (request.LineOffset > 0)
            {
                return;
            }

            EnableResolution(request.Process, request, OnFunctionResolved(workList));
        }

        void IDkmModuleInstanceLoadNotification.OnModuleInstanceLoad(DkmModuleInstance moduleInstance, DkmWorkList workList, DkmEventDescriptorS eventDescriptor)
        {
            OnModuleLoad(moduleInstance, workList);
        }

        void IDkmModuleInstanceUnloadNotification.OnModuleInstanceUnload(DkmModuleInstance moduleInstance, DkmWorkList workList, DkmEventDescriptor eventDescriptor)
        {
            // Implementing IDkmModuleInstanceUnloadNotification
            // (with Synchronized="true" in .vsdconfigxml) prevents
            // caller from unloading modules while binding.
        }

        void IDkmModuleModifiedNotification.OnModuleModified(DkmModuleInstance moduleInstance)
        {
            // Implementing IDkmModuleModifiedNotification
            // (with Synchronized="true" in .vsdconfigxml) prevents
            // caller from modifying modules while binding.
        }

        void IDkmModuleSymbolsLoadedNotification.OnModuleSymbolsLoaded(DkmModuleInstance moduleInstance, DkmModule module, bool isReload, DkmWorkList workList, DkmEventDescriptor eventDescriptor)
        {
            OnModuleLoad(moduleInstance, workList);
        }

        private void OnModuleLoad(DkmModuleInstance moduleInstance, DkmWorkList workList)
        {
            var module = moduleInstance as DkmClrModuleInstance;
            Debug.Assert(module != null); // <Filter><RuntimeId RequiredValue="DkmRuntimeId.Clr"/></Filter> should ensure this.
            if (module == null)
            {
                // Only interested in managed modules.
                return;
            }

            if (module.Module == null)
            {
                // Only resolve breakpoints if symbols have been loaded.
                return;
            }

            OnModuleLoad(module.Process, module, OnFunctionResolved(workList));
        }

        internal sealed override bool ShouldEnableFunctionResolver(DkmProcess process)
        {
            var dataItem = process.GetDataItem<FunctionResolverDataItem>();
            if (dataItem == null)
            {
                var enable = ShouldEnable(process);
                dataItem = new FunctionResolverDataItem(enable);
                process.SetDataItem(DkmDataCreationDisposition.CreateNew, dataItem);
            }
            return dataItem.Enabled;
        }

        internal sealed override IEnumerable<DkmClrModuleInstance> GetAllModules(DkmProcess process)
        {
            foreach (var runtimeInstance in process.GetRuntimeInstances())
            {
                var runtime = runtimeInstance as DkmClrRuntimeInstance;
                if (runtime == null)
                {
                    continue;
                }
                foreach (var moduleInstance in runtime.GetModuleInstances())
                {
                    // Only interested in managed modules.
                    if (moduleInstance is DkmClrModuleInstance module)
                    {
                        yield return module;
                    }
                }
            }
        }

        internal sealed override string GetModuleName(DkmClrModuleInstance module)
        {
            return module.Name;
        }

        internal sealed override MetadataReader GetModuleMetadata(DkmClrModuleInstance module)
        {
            uint length;
            IntPtr ptr;
            try
            {
                ptr = module.GetMetaDataBytesPtr(out length);
            }
            catch (Exception e) when (DkmExceptionUtilities.IsBadOrMissingMetadataException(e))
            {
                return null;
            }
            Debug.Assert(length > 0);
            unsafe
            {
                return new MetadataReader((byte*)ptr, (int)length);
            }
        }

        internal sealed override DkmRuntimeFunctionResolutionRequest[] GetRequests(DkmProcess process)
        {
            return process.GetRuntimeFunctionResolutionRequests();
        }

        internal sealed override string GetRequestModuleName(DkmRuntimeFunctionResolutionRequest request)
        {
            return request.ModuleName;
        }

        internal sealed override Guid GetLanguageId(DkmRuntimeFunctionResolutionRequest request)
        {
            return request.CompilerId.LanguageId;
        }

        private static OnFunctionResolvedDelegate<DkmClrModuleInstance, DkmRuntimeFunctionResolutionRequest> OnFunctionResolved(DkmWorkList workList)
        {
            return (DkmClrModuleInstance module,
                        DkmRuntimeFunctionResolutionRequest request,
                        int token,
                        int version,
                        int ilOffset) =>
            {
                var address = DkmClrInstructionAddress.Create(
                    module.RuntimeInstance,
                    module,
                    new DkmClrMethodId(Token: token, Version: (uint)version),
                    NativeOffset: uint.MaxValue,
                    ILOffset: (uint)ilOffset,
                    CPUInstruction: null);
                // Use async overload of OnFunctionResolved to avoid deadlock.
                request.OnFunctionResolved(workList, address, result => { });
            };
        }

        private static readonly Guid s_messageSourceId = new Guid("ac353c9b-c599-427b-9424-cbe1ad19f81e");

        private static bool ShouldEnable(DkmProcess process)
        {
            var message = DkmCustomMessage.Create(
                process.Connection,
                process,
                s_messageSourceId,
                MessageCode: 1, // Is legacy EE enabled?
                Parameter1: null,
                Parameter2: null);
            try
            {
                var reply = message.SendLower();
                var result = (int)reply.Parameter1;
                // Possible values are 0 = false, 1 = true, 2 = not ready.
                // At this point, we should only get 0 or 1, but to be
                // safe, treat values other than 0 or 1 as false.
                Debug.Assert(result == 0 || result == 1);
                return result == 0;
            }
            catch (NotImplementedException)
            {
                return false;
            }
        }

        private sealed class FunctionResolverDataItem : DkmDataItem
        {
            internal FunctionResolverDataItem(bool enabled)
            {
                Enabled = enabled;
            }

            internal readonly bool Enabled;
        }
    }
}
