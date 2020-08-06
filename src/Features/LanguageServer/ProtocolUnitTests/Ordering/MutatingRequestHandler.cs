// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    [Shared, ExportLspMethod(MethodName, mutatesSolutionState: true)]
    internal class MutatingRequestHandler : AbstractRequestHandler<OrderedLspRequest, OrderedLspResponse>
    {
        public const string MethodName = nameof(MutatingRequestHandler);
        private const int Delay = 100;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MutatingRequestHandler(ILspSolutionProvider solutionProvider)
            : base(solutionProvider)
        {
        }

        public override async Task<OrderedLspResponse> HandleRequestAsync(OrderedLspRequest request, RequestContext context, CancellationToken cancellationToken)
        {
            var response = new OrderedLspResponse();

            response.Solution = context.Solution;
            response.RequestOrder = request.RequestOrder;
            response.StartTime = DateTime.UtcNow;

            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

            // Mutate the solution
            var solution = context.Solution;
            solution = solution.WithNewWorkspace(solution.Workspace, solution.WorkspaceVersion + 1);

            // TODO: Update the solution in the context somehow

            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

            response.EndTime = DateTime.UtcNow;

            return response;
        }
    }
}
