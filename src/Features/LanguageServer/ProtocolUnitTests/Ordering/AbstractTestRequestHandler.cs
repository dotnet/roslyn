// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    internal abstract class AbstractTestRequestHandler : AbstractRequestHandler<OrderedLspRequest, OrderedLspRequest>
    {
        protected abstract TimeSpan Delay { get; }

        public AbstractTestRequestHandler(ILspSolutionProvider solutionProvider)
            : base(solutionProvider)
        {
        }

        public override async Task<OrderedLspRequest> HandleRequestAsync(OrderedLspRequest request, ClientCapabilities clientCapabilities, string clientName, CancellationToken cancellationToken)
        {
            request.StartTime = DateTime.UtcNow;

            await Task.Delay(Delay, cancellationToken);

            request.EndTime = DateTime.UtcNow;
            return request;
        }
    }
}
