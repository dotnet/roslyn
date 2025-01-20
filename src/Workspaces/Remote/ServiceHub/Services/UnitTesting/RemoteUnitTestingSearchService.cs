// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteUnitTestingSearchService : BrokeredServiceBase, IRemoteUnitTestingSearchService
{
    internal sealed class Factory : FactoryBase<IRemoteUnitTestingSearchService>
    {
        protected override IRemoteUnitTestingSearchService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteUnitTestingSearchService(arguments);
    }

    public RemoteUnitTestingSearchService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
    }

    public ValueTask<UnitTestingSourceLocation?> GetSourceLocationAsync(
        Checksum solutionChecksum,
        ProjectId projectId,
        UnitTestingSearchQuery query,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync<UnitTestingSourceLocation?>(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);

            var resultOpt = await UnitTestingSearchHelpers.GetSourceLocationAsync(project, query, cancellationToken).ConfigureAwait(false);
            if (resultOpt is null)
                return null;

            var result = resultOpt.Value;

            return new UnitTestingSourceLocation(
                new DocumentIdSpan(result.DocumentSpan.Document.Id, result.DocumentSpan.SourceSpan),
                result.Span);
        }, cancellationToken);
    }

    public ValueTask<ImmutableArray<UnitTestingSourceLocation>> GetSourceLocationsAsync(
        Checksum solutionChecksum,
        ProjectId projectId,
        UnitTestingSearchQuery query,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);

            var results = await UnitTestingSearchHelpers.GetSourceLocationsAsync(project, query, cancellationToken).ConfigureAwait(false);

            return results.SelectAsArray(r => new UnitTestingSourceLocation(
                new DocumentIdSpan(r.DocumentSpan.Document.Id, r.DocumentSpan.SourceSpan),
                r.Span));
        }, cancellationToken);
    }
}
