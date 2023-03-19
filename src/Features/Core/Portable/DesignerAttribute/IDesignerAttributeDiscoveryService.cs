// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    internal partial interface IDesignerAttributeDiscoveryService : IWorkspaceService
    {
        public interface ICallback
        {
            ValueTask ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken);
        }

        ValueTask ProcessSolutionAsync(
            Solution solution, DocumentId? priorityDocumentId, bool useFrozenSnapshots, ICallback callback, CancellationToken cancellationToken);
    }
}
