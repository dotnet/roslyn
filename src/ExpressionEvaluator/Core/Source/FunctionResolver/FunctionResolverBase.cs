// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class FunctionResolverBase<TProcess, TModule, TRequest>
        where TProcess : class
        where TModule : class
        where TRequest : class
    {
        internal void EnableResolution(TProcess process, TRequest request)
        {
            ResolveRequest(process, request);
        }

        internal void OnModuleLoad(TProcess process, TModule module)
        {
            var requests = GetRequests(process);
            ResolveRequests(process, module, requests);
        }

        internal abstract bool ShouldEnableFunctionResolver(TProcess process);
        internal abstract IEnumerable<TModule> GetAllModules(TProcess process);
        internal abstract string GetModuleName(TModule module);
        internal abstract MetadataReader GetModuleMetadata(TModule module);
        internal abstract TRequest[] GetRequests(TProcess process);
        internal abstract string GetRequestModuleName(TRequest request);
        internal abstract RequestSignature GetParsedSignature(TRequest request);
        internal abstract void OnFunctionResolved(TModule module, TRequest request, int token, int version, int ilOffset);

        private void ResolveRequest(TProcess process, TRequest request)
        {
            var moduleName = GetRequestModuleName(request);
            var signature = GetParsedSignature(request);
            if (signature == null)
            {
                return;
            }

            bool checkEnabled = true;
            foreach (var module in GetAllModules(process))
            {
                if (checkEnabled)
                {
                    if (!ShouldEnableFunctionResolver(process))
                    {
                        return;
                    }
                    checkEnabled = false;
                }
                if (!ShouldModuleHandleRequest(module, moduleName))
                {
                    continue;
                }
                var reader = GetModuleMetadata(module);
                if (reader == null)
                {
                    continue;
                }
                var resolver = new MetadataResolver<TProcess, TModule, TRequest>(this, process, module, reader);
                resolver.Resolve(request, signature);
            }
        }

        private void ResolveRequests(TProcess process, TModule module, TRequest[] requests)
        {
            if (!ShouldEnableFunctionResolver(process))
            {
                return;
            }

            MetadataResolver<TProcess, TModule, TRequest> resolver = null;

            foreach (var request in requests)
            {
                var moduleName = GetRequestModuleName(request);
                if (!ShouldModuleHandleRequest(module, moduleName))
                {
                    continue;
                }

                var signature = GetParsedSignature(request);
                if (signature == null)
                {
                    continue;
                }

                if (resolver == null)
                {
                    var reader = GetModuleMetadata(module);
                    if (reader == null)
                    {
                        return;
                    }
                    resolver = new MetadataResolver<TProcess, TModule, TRequest>(this, process, module, reader);
                }

                resolver.Resolve(request, signature);
            }
        }

        private bool ShouldModuleHandleRequest(TModule module, string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                return true;
            }
            var name = GetModuleName(module);
            return moduleName.Equals(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
