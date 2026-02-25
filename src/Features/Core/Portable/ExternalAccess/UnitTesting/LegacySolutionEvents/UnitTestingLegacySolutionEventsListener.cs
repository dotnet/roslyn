// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LegacySolutionEvents;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.LegacySolutionEvents;

/// <summary>
/// Retrieves stream of workspace events and forwards them to the dedicated solution crawler instance that exists
/// for unit testing.
/// </summary>
[Export(typeof(ILegacySolutionEventsListener)), Shared]
internal sealed class UnitTestingLegacySolutionEventsListener : ILegacySolutionEventsListener
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public UnitTestingLegacySolutionEventsListener()
    {
    }

    private static IUnitTestingWorkCoordinator? GetCoordinator(Solution solution)
    {
        var service = solution.Services.GetService<IUnitTestingSolutionCrawlerRegistrationService>();
        if (service == null)
            return null;

        return service.Register(solution);
    }

    public bool ShouldReportChanges(SolutionServices services)
    {
        var service = services.GetService<IUnitTestingSolutionCrawlerRegistrationService>();
        if (service == null)
            return false;

        return service.HasRegisteredAnalyzerProviders;
    }

    public ValueTask OnWorkspaceChangedAsync(WorkspaceChangeEventArgs args, bool processSourceGeneratedDocuments, CancellationToken cancellationToken)
    {
        var coordinator = GetCoordinator(args.NewSolution);
        coordinator?.OnWorkspaceChanged(args, processSourceGeneratedDocuments);
        return ValueTask.CompletedTask;
    }
}
