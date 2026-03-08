// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis.ExternalAccess.Unified.FSharp.Editor.Implementation.Debugging;

namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.FSharp.Internal.Editor.Implementation.Debugging;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.Implementation.Debugging;
#endif

[Shared]
[ExportLanguageService(typeof(IBreakpointResolutionService), LanguageNames.FSharp)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class FSharpBreakpointResolutionService([Import(AllowDefault = true)] IFSharpBreakpointResolutionService? service) : IBreakpointResolutionService
{
    private readonly IFSharpBreakpointResolutionService? _service = service;

    public async Task<BreakpointResolutionResult?> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default)
    {
        if (_service == null)
            return null;

        return (await _service.ResolveBreakpointAsync(document, textSpan, cancellationToken).ConfigureAwait(false))?.UnderlyingObject;
    }

    public async Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken = default)
    {
        if (_service == null)
            return Enumerable.Empty<BreakpointResolutionResult>();

        return (await _service.ResolveBreakpointsAsync(solution, name, cancellationToken).ConfigureAwait(false)).Select(r => r.UnderlyingObject);
    }
}
