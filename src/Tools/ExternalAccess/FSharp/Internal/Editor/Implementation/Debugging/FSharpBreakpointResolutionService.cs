// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.Implementation.Debugging
{
    [Shared]
    [ExportLanguageService(typeof(IBreakpointResolutionService), LanguageNames.FSharp)]
    internal class FSharpBreakpointResolutionService : IBreakpointResolutionService
    {
        private readonly IFSharpBreakpointResolutionService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpBreakpointResolutionService(IFSharpBreakpointResolutionService service)
        {
            _service = service;
        }

        public async Task<BreakpointResolutionResult?> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default)
            => (await _service.ResolveBreakpointAsync(document, textSpan, cancellationToken).ConfigureAwait(false))?.UnderlyingObject;

        public async Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken = default)
            => (await _service.ResolveBreakpointsAsync(solution, name, cancellationToken).ConfigureAwait(false)).Select(r => r.UnderlyingObject);
    }
}
