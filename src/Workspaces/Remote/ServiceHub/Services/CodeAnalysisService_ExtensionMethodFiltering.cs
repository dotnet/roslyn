// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteExtensionMethodFilteringService
    {
        public Task<IEnumerable<(string, IEnumerable<string>)>> GetPossibleExtensionMethodMatchesAsync(ProjectId projectId, string[] targetTypeNames, bool loadOnly, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

                    var project = solution.GetProject(projectId)!;
                    var result = await ExtensionMethodFilteringService.GetPossibleExtensionMethodMatchesAsync(
                        project, targetTypeNames.ToImmutableHashSet(), loadOnly, cancellationToken).ConfigureAwait(false);

                    return (IEnumerable<(string, IEnumerable<string>)>)result.SelectAsArray(kvp => (kvp.Key, (IEnumerable<string>)kvp.Value.ToImmutableArray()));
                }
            }, cancellationToken);
        }
    }
}
