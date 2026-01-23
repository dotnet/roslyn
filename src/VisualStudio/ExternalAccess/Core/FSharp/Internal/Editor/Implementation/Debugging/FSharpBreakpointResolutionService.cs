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
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Editor.Implementation.Debugging;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Internal.Editor.Implementation.Debugging;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.Implementation.Debugging;
#endif

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
