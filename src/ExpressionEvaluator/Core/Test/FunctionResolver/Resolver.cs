// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class Resolver : FunctionResolverBase<Process, Module, Request>
    {
        private readonly Dictionary<Process, List<Request>> _requests;

        internal Resolver()
        {
            _requests = new Dictionary<Process, List<Request>>();
        }

        internal new void EnableResolution(Process process, Request request)
        {
            List<Request> requests;
            if (!_requests.TryGetValue(process, out requests))
            {
                requests = new List<Request>();
                _requests.Add(process, requests);
            }
            requests.Add(request);
            base.EnableResolution(process, request);
        }

        internal override bool ShouldEnableFunctionResolver(Process process)
        {
            return process.ShouldEnableFunctionResolver();
        }

        internal override IEnumerable<Module> GetAllModules(Process process)
        {
            return process.GetModules();
        }

        internal override string GetModuleName(Module module)
        {
            return module.Name;
        }

        internal override MetadataReader GetModuleMetadata(Module module)
        {
            return module.GetMetadata();
        }

        internal override Request[] GetRequests(Process process)
        {
            List<Request> requests;
            if (!_requests.TryGetValue(process, out requests))
            {
                return new Request[0];
            }
            return requests.ToArray();
        }

        internal override string GetRequestModuleName(Request request)
        {
            return request.ModuleName;
        }

        internal override RequestSignature GetParsedSignature(Request request)
        {
            return request.Signature;
        }

        internal override void OnFunctionResolved(Module module, Request request, int token, int version, int ilOffset)
        {
            request.OnFunctionResolved(module, token, version, ilOffset);
        }

        private void OnModuleLoaded(object sender, Module e)
        {
            OnModuleLoad((Process)sender, e);
        }
    }
}
