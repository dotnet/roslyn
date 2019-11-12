﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [Shared]
    [ExportLanguageService(typeof(IBreakpointResolutionService), InternalLanguageNames.TypeScript)]
    internal sealed class VSTypeScriptBreakpointResolutionService : IBreakpointResolutionService
    {
        private readonly IVSTypeScriptBreakpointResolutionService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptBreakpointResolutionService(IVSTypeScriptBreakpointResolutionService service)
        {
            _service = service;
        }

        public async Task<BreakpointResolutionResult> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default)
        {
            var result = await _service.ResolveBreakpointAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            return result.IsLineBreakpoint ?
                BreakpointResolutionResult.CreateLineResult(result.Document, result.LocationNameOpt) :
                BreakpointResolutionResult.CreateSpanResult(result.Document, result.TextSpan, result.LocationNameOpt);
        }

        public Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken = default)
            => Task.FromResult(SpecializedCollections.EmptyEnumerable<BreakpointResolutionResult>());
    }
}
