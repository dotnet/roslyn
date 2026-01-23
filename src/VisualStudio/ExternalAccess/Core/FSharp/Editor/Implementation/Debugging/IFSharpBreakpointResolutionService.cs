// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor.Implementation.Debugging;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging;
#endif

internal interface IFSharpBreakpointResolutionService
{
    Task<FSharpBreakpointResolutionResult?> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default);

    Task<IEnumerable<FSharpBreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken = default);
}
