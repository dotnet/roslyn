// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
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

        public async Task<CodeAnalysis.Editor.Implementation.Debugging.BreakpointResolutionResult> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default)
        {
            var result = await _service.ResolveBreakpointAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (result != null)
            {
                if (result.IsLineBreakpoint)
                {
                    return CodeAnalysis.Editor.Implementation.Debugging.BreakpointResolutionResult.CreateLineResult(result.Document, result.LocationNameOpt);
                }
                else
                {
                    return CodeAnalysis.Editor.Implementation.Debugging.BreakpointResolutionResult.CreateSpanResult(result.Document, result.TextSpan, result.LocationNameOpt);
                }
            }
            else
            {
                return null;
            }
        }

        public Task<IEnumerable<CodeAnalysis.Editor.Implementation.Debugging.BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
