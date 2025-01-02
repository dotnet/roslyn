// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting;

internal interface IRemoteUnitTestingSearchService
{
    ValueTask<UnitTestingSourceLocation?> GetSourceLocationAsync(
        Checksum solutionChecksum, ProjectId projectId, UnitTestingSearchQuery query, CancellationToken cancellationToken);
    ValueTask<ImmutableArray<UnitTestingSourceLocation>> GetSourceLocationsAsync(
        Checksum solutionChecksum, ProjectId projectId, UnitTestingSearchQuery query, CancellationToken cancellationToken);
}

[DataContract]
internal readonly struct UnitTestingSourceLocation(DocumentIdSpan documentIdSpan, FileLinePositionSpan span)
{
    [DataMember(Order = 0)]
    public readonly DocumentIdSpan DocumentIdSpan = documentIdSpan;
    [DataMember(Order = 1)]
    public readonly FileLinePositionSpan Span = span;

    public async Task<UnitTestingDocumentSpan?> TryRehydrateAsync(Solution solution, CancellationToken cancellationToken)
    {
        var documentSpan = await DocumentIdSpan.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
        if (documentSpan == null)
            return null;

        return new UnitTestingDocumentSpan(documentSpan.Value, Span);
    }
}
