// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeLens
{
    internal interface IRemoteCodeLensReferencesService
    {
        ValueTask<ReferenceCount?> GetReferenceCountAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, int maxResultCount, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<ReferenceLocationDescriptor>?> FindReferenceLocationsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);
        ValueTask<string> GetFullyQualifiedNameAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);
    }
}
