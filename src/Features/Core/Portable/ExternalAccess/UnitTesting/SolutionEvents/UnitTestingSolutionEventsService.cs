// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.SolutionEvents;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionEvents
{
    [Export(typeof(ISolutionEventsService)), Shared]
    internal class UnitTestingSolutionEventsService : ISolutionEventsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnitTestingSolutionEventsService()
        {
        }

        public ValueTask OnSolutionEventAsync(Solution solution, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask OnDocumentEventAsync(Solution solution, DocumentId documentId, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask OnProjectEventAsync(Solution solution, ProjectId projectId, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask OnSolutionChangedAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask OnProjectChangedAsync(Solution oldSolution, Solution newSolution, ProjectId projectId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask OnDocumentChangedAsync(Solution oldSolution, Solution newSolution, DocumentId documentId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
