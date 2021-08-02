// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences
{
    internal class VisualStudioUpdateReferenceOperation : IUpdateReferenceOperation
    {
        private readonly IProjectSystemUpdateReferenceOperation _updateOperation;

        public VisualStudioUpdateReferenceOperation(IProjectSystemUpdateReferenceOperation updateOperation)
        {
            _updateOperation = updateOperation;
        }

        public Task<bool> ApplyAsync(CancellationToken cancellationToken)
         => _updateOperation.ApplyAsync(cancellationToken);

        public Task<bool> RevertAsync(CancellationToken cancellationToken)
         => _updateOperation.RevertAsync(cancellationToken);
    }
}
