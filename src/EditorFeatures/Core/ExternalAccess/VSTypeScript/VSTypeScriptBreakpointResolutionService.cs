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
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[Shared]
[ExportLanguageService(typeof(IBreakpointResolutionService), InternalLanguageNames.TypeScript)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptBreakpointResolutionService(IVSTypeScriptBreakpointResolutionServiceImplementation implementation) : IBreakpointResolutionService
{
    private readonly IVSTypeScriptBreakpointResolutionServiceImplementation _implementation = implementation;

    public async Task<BreakpointResolutionResult?> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default)
        => (await _implementation.ResolveBreakpointAsync(document, textSpan, cancellationToken).ConfigureAwait(false)).UnderlyingObject;

    public async Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken = default)
        => (await _implementation.ResolveBreakpointsAsync(solution, name, cancellationToken).ConfigureAwait(false)).Select(r => r.UnderlyingObject);

}
