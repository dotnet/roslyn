// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Experiments
{
    internal interface IExperimentationService : IWorkspaceService
    {
        ValueTask<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(IExperimentationService)), Shared]
    internal class DefaultExperimentationService : IExperimentationService
    {
        public ValueTask<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken) => new ValueTask<bool>(false);
    }

    internal static class WellKnownExperimentNames
    {
        public const string RoslynFeatureOOP = nameof(RoslynFeatureOOP);
        public const string RoslynOOP64bit = nameof(RoslynOOP64bit);
    }
}
