// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Debugging
{
    [ExportLanguageService(typeof(IBreakpointResolutionService), LanguageNames.CSharp), Shared]
    internal partial class CSharpBreakpointResolutionService : IBreakpointResolutionService
    {
        internal static async Task<BreakpointResolutionResult> GetBreakpointAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            TextSpan span;
            if (!BreakpointSpans.TryGetBreakpointSpan(tree, position, cancellationToken, out span))
            {
                return null;
            }

            if (span.Length == 0)
            {
                return BreakpointResolutionResult.CreateLineResult(document);
            }

            return BreakpointResolutionResult.CreateSpanResult(document, span);
        }

        public Task<BreakpointResolutionResult> ResolveBreakpointAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return GetBreakpointAsync(document, textSpan.Start, cancellationToken);
        }

        public Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken)
        {
            return new BreakpointResolver(solution, name).DoAsync(cancellationToken);
        }
    }
}
