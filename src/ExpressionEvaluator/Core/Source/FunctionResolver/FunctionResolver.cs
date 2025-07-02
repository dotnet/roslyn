// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        IDkmMetaDataPointerInvalidatedNotification,
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

        void IDkmMetaDataPointerInvalidatedNotification.OnMetaDataPointerInvalidated(DkmClrModuleInstance moduleInstance)
        {
            // Implementing IDkmMetaDataPointerInvalidatedNotification
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

        internal sealed override unsafe bool TryGetMetadata(DkmClrModuleInstance module, out byte* pointer, out int length)
        {
            try
            {
                pointer = (byte*)module.GetMetaDataBytesPtr(out var size);
                length = (int)size;
                return true;
            }
            catch (Exception e) when (DkmExceptionUtilities.IsBadOrMissingMetadataException(e))
            {
                pointer = null;
                length = 0;
                return false;
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
