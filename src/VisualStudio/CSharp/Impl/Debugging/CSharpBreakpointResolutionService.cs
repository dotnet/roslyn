// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Debugging
{
    [ExportLanguageService(typeof(IBreakpointResolutionService), LanguageNames.CSharp), Shared]
    internal partial class CSharpBreakpointResolutionService : IBreakpointResolutionService
    {
        public Task<BreakpointResolutionResult> ResolveBreakpointAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return BreakpointGetter.GetBreakpointAsync(document, textSpan.Start, cancellationToken);
        }

        public Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken)
        {
            return new BreakpointResolver(solution, name).DoAsync(cancellationToken);
        }
    }
}
