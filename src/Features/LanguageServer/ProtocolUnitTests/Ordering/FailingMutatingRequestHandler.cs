﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    [Shared, ExportLspRequestHandlerProvider, PartNotDiscoverable]
    internal class FailingMutatingRequestHandlerProvider : AbstractRequestHandlerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FailingMutatingRequestHandlerProvider()
        {
        }

        protected override ImmutableArray<IRequestHandler> InitializeHandlers()
        {
            return ImmutableArray.Create<IRequestHandler>(new FailingMutatingRequestHandler());
        }
    }

    [LspMethod(MethodName, mutatesSolutionState: true)]
    internal class FailingMutatingRequestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public const string MethodName = nameof(FailingMutatingRequestHandler);
        private const int Delay = 100;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequest request) => null;

        public async Task<TestResponse> HandleRequestAsync(TestRequest request, RequestContext context, CancellationToken cancellationToken)
        {
            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

            throw new InvalidOperationException();
        }
    }
}
