// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService : IFindUsagesService
    {
        private static Task<RemoteHostClient.Session> TryGetRemoteSessionAsync(
            Solution solution, object callback, CancellationToken cancellationToken)
        {
            return solution.TryCreateCodeAnalysisServiceSessionAsync(
                FindUsagesOptions.OutOfProcessAllowed, WellKnownExperimentNames.OutOfProcessAllowed, callback, cancellationToken);
        }
    }
}