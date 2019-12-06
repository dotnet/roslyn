// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
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
        private readonly IVSTypeScriptBreakpointResolutionServiceImplementation _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptBreakpointResolutionService(IVSTypeScriptBreakpointResolutionServiceImplementation implementation)
        {
            _implementation = implementation;
        }

        public async Task<BreakpointResolutionResult?> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default)
            => (await _implementation.ResolveBreakpointAsync(document, textSpan, cancellationToken).ConfigureAwait(false)).UnderlyingObject;

        public async Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken = default)
            => (await _implementation.ResolveBreakpointsAsync(solution, name, cancellationToken).ConfigureAwait(false)).Select(r => r.UnderlyingObject);

    }
}
