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

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportRoslynLanguagesLspRequestHandlerProvider, Shared]
    internal class CompletionHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> _completionProviders;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandlerProvider(
            IGlobalOptionService globalOptions,
            [ImportMany] IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders)
        {
            _globalOptions = globalOptions;
            _completionProviders = completionProviders;
        }

        public override ImmutableArray<LazyRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
        {
            var completionCache = new CompletionListCache();
            return ImmutableArray.Create(
                CreateLazyRequestHandlerMetadata(() => new CompletionHandler(_globalOptions, _completionProviders, completionCache)),
                CreateLazyRequestHandlerMetadata(() => new CompletionResolveHandler(_globalOptions, completionCache)));
        }
    }
}
