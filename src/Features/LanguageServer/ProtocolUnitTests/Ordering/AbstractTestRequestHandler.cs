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
    internal abstract class AbstractTestRequestHandler : AbstractRequestHandler<OrderedLspRequest, OrderedLspResponse>
    {
        private const int Delay = 100;

        public AbstractTestRequestHandler(ILspSolutionProvider solutionProvider)
            : base(solutionProvider)
        {
        }

        public override async Task<OrderedLspResponse> HandleRequestAsync(OrderedLspRequest request, RequestContext context, CancellationToken cancellationToken)
        {
            var response = new OrderedLspResponse();

            response.RequestOrder = request.RequestOrder;
            response.StartTime = DateTime.UtcNow;

            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

            // some busy work
            response.ToString();

            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

            response.EndTime = DateTime.UtcNow;

            return response;
        }
    }
}
