// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Options;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportRoslynLanguagesLspRequestHandlerProvider, Shared]
    internal class CompletionHandlerProvider :
        IRequestHandlerProvider<CompletionHandler>,
        IRequestHandlerProvider<CompletionResolveHandler>
    {
        private readonly IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> _completionProviders;
        private readonly IGlobalOptionService _globalOptions;

        private readonly Lazy<CompletionListCache> _completionListCache = new(() => new());

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandlerProvider(
            IGlobalOptionService globalOptions,
            [ImportMany] IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders)
        {
            _globalOptions = globalOptions;
            _completionProviders = completionProviders;
        }

        CompletionHandler IRequestHandlerProvider<CompletionHandler>.CreateRequestHandler(WellKnownLspServerKinds serverKind)
            => new(_globalOptions, _completionProviders, _completionListCache.Value);

        CompletionResolveHandler IRequestHandlerProvider<CompletionResolveHandler>.CreateRequestHandler(WellKnownLspServerKinds serverKind)
            => new(_globalOptions, _completionListCache.Value);
    }
}
