// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare
{
    [ExportLanguageServiceFactory(typeof(IBreakpointResolutionService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspBreakpointServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpRemoteBreakpointService(languageServices);
        }
    }

    internal class CSharpRemoteBreakpointService : IBreakpointResolutionService
    {
        private readonly IBreakpointResolutionService originalService;

        public CSharpRemoteBreakpointService(HostLanguageServices languageServices)
        {
            this.originalService = languageServices.GetOriginalLanguageService<IBreakpointResolutionService>();
        }

        public Task<BreakpointResolutionResult> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default)
        {
            return this.originalService.ResolveBreakpointAsync(document, textSpan, cancellationToken);
        }

        public Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken = default)
        {
            // NotSupported:
            // The C# language service requires semantics to resolve breakpoints with name and OmniSharp doesnt support this.
            return Task.FromResult<IEnumerable<BreakpointResolutionResult>>(ImmutableArray<BreakpointResolutionResult>.Empty);
        }
    }

}
