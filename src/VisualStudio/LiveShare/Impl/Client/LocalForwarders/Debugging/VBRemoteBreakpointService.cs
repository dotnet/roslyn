// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.LocalForwarders
{
    [ExportLanguageServiceFactory(typeof(IBreakpointResolutionService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspBreakpointServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VBLspBreakpointServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new VBRemoteBreakpointService(languageServices);
    }

    internal class VBRemoteBreakpointService : IBreakpointResolutionService
    {
        private readonly IBreakpointResolutionService originalService;

        public VBRemoteBreakpointService(HostLanguageServices languageServices)
            => this.originalService = languageServices.GetOriginalLanguageService<IBreakpointResolutionService>();

        public Task<BreakpointResolutionResult> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default)
            => this.originalService.ResolveBreakpointAsync(document, textSpan, cancellationToken);

        public Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken = default)
        {
            // NotSupported:
            // The C# language service requires semantics to resolve breakpoints with name and OmniSharp doesnt support this.
            return Task.FromResult<IEnumerable<BreakpointResolutionResult>>(ImmutableArray<BreakpointResolutionResult>.Empty);
        }
    }
}
