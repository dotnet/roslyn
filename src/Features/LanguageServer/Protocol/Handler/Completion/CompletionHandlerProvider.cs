﻿// Licensed to the .NET Foundation under one or more agreements.
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
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspRequestHandlerProvider, Shared]
    [ProvidesMethod(LSP.Methods.TextDocumentCompletionName)]
    [ProvidesMethod(LSP.Methods.TextDocumentCompletionResolveName)]
    internal class CompletionHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> _completionProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandlerProvider(
            [ImportMany] IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders)
        {
            _completionProviders = completionProviders;
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers()
        {
            var completionListCache = new CompletionListCache();
            return ImmutableArray.Create<IRequestHandler>(
                new CompletionHandler(_completionProviders, completionListCache),
                new CompletionResolveHandler(completionListCache));
        }
    }
}
