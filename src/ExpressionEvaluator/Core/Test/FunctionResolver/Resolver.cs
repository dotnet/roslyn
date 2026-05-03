// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class Resolver : FunctionResolverBase<Process, Module, Request>
    {
        internal static readonly Resolver CSharpResolver = CreateFrom(new Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.CSharpFunctionResolver());
        internal static readonly Resolver VisualBasicResolver = CreateFrom(new Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.VisualBasicFunctionResolver());

        private readonly bool _ignoreCase;
        private readonly Guid _languageId;
        private readonly Dictionary<Process, List<Request>> _requests;

        private static Resolver CreateFrom(FunctionResolver resolver)
        {
            return new Resolver(resolver.IgnoreCase, resolver.LanguageId);
        }

        private Resolver(bool ignoreCase, Guid languageId)
        {
            _ignoreCase = ignoreCase;
            _languageId = languageId;
            _requests = [];
        }

        internal void EnableResolution(Process process, Request request)
        {
            List<Request> requests;
            if (!_requests.TryGetValue(process, out requests))
            {
                requests = [];
                _requests.Add(process, requests);
            }
            requests.Add(request);
            base.EnableResolution(process, request, OnFunctionResolved);
        }

        internal void OnModuleLoad(Process process, Module module)
        {
            base.OnModuleLoad(process, module, OnFunctionResolved);
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

        internal override unsafe bool TryGetMetadata(Module module, out byte* pointer, out int length)
            => module.TryGetMetadata(out pointer, out length);

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

        internal override bool IgnoreCase => _ignoreCase;

        internal override Guid GetLanguageId(Request request)
        {
            return request.LanguageId;
        }

        internal override Guid LanguageId => _languageId;

        private static void OnFunctionResolved(Module module, Request request, int token, int version, int ilOffset)
        {
            request.OnFunctionResolved(module, token, version, ilOffset);
        }
    }
}
