﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Debugging
{
    [ExportLanguageService(typeof(IBreakpointResolutionService), LanguageNames.CSharp), Shared]
    internal partial class CSharpBreakpointResolutionService : IBreakpointResolutionService
    {
        [ImportingConstructor]
        public CSharpBreakpointResolutionService()
        {
        }

        /// <summary>
        /// Returns null if a breakpoint can't be placed at the specified position.
        /// </summary>
        public async Task<BreakpointResolutionResult> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            try
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (!BreakpointSpans.TryGetBreakpointSpan(tree, textSpan.Start, cancellationToken, out var span))
                {
                    return null;
                }

                if (span.Length == 0)
                {
                    return BreakpointResolutionResult.CreateLineResult(document);
                }

                return BreakpointResolutionResult.CreateSpanResult(document, span);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return null;
            }
        }

        public Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken)
        {
            return new BreakpointResolver(solution, name).DoAsync(cancellationToken);
        }
    }
}
