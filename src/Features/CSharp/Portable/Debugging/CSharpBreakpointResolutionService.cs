// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Debugging
{
    [ExportLanguageService(typeof(IBreakpointResolutionService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpBreakpointResolutionService : IBreakpointResolutionService
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpBreakpointResolutionService()
        {
        }

        /// <summary>
        /// Returns null if a breakpoint can't be placed at the specified position.
        /// </summary>
        public async Task<BreakpointResolutionResult?> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            try
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (tree == null || !BreakpointSpans.TryGetBreakpointSpan(tree, textSpan.Start, cancellationToken, out var span))
                {
                    return null;
                }

                if (span.Length == 0)
                {
                    return BreakpointResolutionResult.CreateLineResult(document);
                }

                return BreakpointResolutionResult.CreateSpanResult(document, span);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }

        public Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken)
            => new BreakpointResolver(solution, name).DoAsync(cancellationToken);
    }
}
