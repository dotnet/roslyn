// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Debugger.Evaluation;
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
        internal abstract bool ShouldEnableFunctionResolver(TProcess process);
        internal abstract IEnumerable<TModule> GetAllModules(TProcess process);
        internal abstract string GetModuleName(TModule module);
        internal abstract unsafe bool TryGetMetadata(TModule module, out byte* pointer, out int length);
        internal abstract TRequest[] GetRequests(TProcess process);
        internal abstract string GetRequestModuleName(TRequest request);
        internal abstract RequestSignature GetParsedSignature(TRequest request);
        internal abstract bool IgnoreCase { get; }
        internal abstract Guid GetLanguageId(TRequest request);
        internal abstract Guid LanguageId { get; }

        internal void EnableResolution(TProcess process, TRequest request, OnFunctionResolvedDelegate<TModule, TRequest> onFunctionResolved)
        {
            if (!ShouldHandleRequest(request))
            {
                return;
            }

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

                var reader = GetMetadataReader(module);
                if (reader == null)
                {
                    // ignore modules with bad metadata
                    continue;
                }

                var resolver = CreateMetadataResolver(module, reader, onFunctionResolved);
                resolver.Resolve(request, signature);
            }
        }

        private unsafe MetadataReader GetMetadataReader(TModule module)
        {
            if (!TryGetMetadata(module, out var pointer, out var length))
            {
                return null;
            }

            try
            {
                return new MetadataReader(pointer, length);
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        internal void OnModuleLoad(TProcess process, TModule module, OnFunctionResolvedDelegate<TModule, TRequest> onFunctionResolved)
        {
            if (!ShouldEnableFunctionResolver(process))
            {
                return;
            }

            MetadataResolver<TProcess, TModule, TRequest> resolver = null;
            var requests = GetRequests(process);

            foreach (var request in requests)
            {
                if (!ShouldHandleRequest(request))
                {
                    continue;
                }

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
                    var reader = GetMetadataReader(module);
                    if (reader == null)
                    {
                        return;
                    }
                    resolver = CreateMetadataResolver(module, reader, onFunctionResolved);
                }

                resolver.Resolve(request, signature);
            }
        }

        private MetadataResolver<TProcess, TModule, TRequest> CreateMetadataResolver(
            TModule module,
            MetadataReader reader,
            OnFunctionResolvedDelegate<TModule, TRequest> onFunctionResolved)
        {
            return new MetadataResolver<TProcess, TModule, TRequest>(module, reader, IgnoreCase, onFunctionResolved);
        }

        private bool ShouldHandleRequest(TRequest request)
        {
            var languageId = GetLanguageId(request);
            // Handle requests with no language id, a matching language id,
            // or causality breakpoint requests (debugging web services).
            return languageId == Guid.Empty ||
                languageId == LanguageId ||
                languageId == DkmLanguageId.CausalityBreakpoint;
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
