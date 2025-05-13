// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal sealed partial class NewUnitTestingIncrementalAnalyzerProvider : IUnitTestingIncrementalAnalyzerProvider
{
    private readonly string? _workspaceKind;
    private readonly SolutionServices _services;
    private readonly INewUnitTestingIncrementalAnalyzerProviderImplementation _incrementalAnalyzerProvider;

    private IUnitTestingIncrementalAnalyzer? _lazyAnalyzer;

    private NewUnitTestingIncrementalAnalyzerProvider(
        string? workspaceKind,
        SolutionServices services,
        INewUnitTestingIncrementalAnalyzerProviderImplementation incrementalAnalyzerProvider)
    {
        _workspaceKind = workspaceKind;
        _services = services;
        _incrementalAnalyzerProvider = incrementalAnalyzerProvider;
    }

    // NOTE: We're currently expecting the analyzer to be singleton, so that
    //       analyzers returned when calling this method twice would pass a reference equality check.
    //       One instance should be created by SolutionCrawler, another one by us, when calling the
    //       UnitTestingSolutionCrawlerServiceAccessor.Reanalyze method.
    public IUnitTestingIncrementalAnalyzer CreateIncrementalAnalyzer()
        => _lazyAnalyzer ??= new NewUnitTestingIncrementalAnalyzer(_incrementalAnalyzerProvider.CreateIncrementalAnalyzer());

    public void Reanalyze()
    {
        var solutionCrawlerService = _services.GetService<IUnitTestingSolutionCrawlerService>();
        solutionCrawlerService?.Reanalyze(
            _workspaceKind, _services, this.CreateIncrementalAnalyzer(), projectIds: null, documentIds: null);
    }

    public static NewUnitTestingIncrementalAnalyzerProvider? TryRegister(string? workspaceKind, SolutionServices services, string analyzerName, INewUnitTestingIncrementalAnalyzerProviderImplementation provider)
    {
        Contract.ThrowIfNull(workspaceKind);
        var solutionCrawlerRegistrationService = services.GetService<IUnitTestingSolutionCrawlerRegistrationService>();
        if (solutionCrawlerRegistrationService == null)
        {
            return null;
        }

        var analyzerProvider = new NewUnitTestingIncrementalAnalyzerProvider(workspaceKind, services, provider);

        var metadata = new UnitTestingIncrementalAnalyzerProviderMetadata(
            analyzerName,
            [workspaceKind]);

        solutionCrawlerRegistrationService.AddAnalyzerProvider(analyzerProvider, metadata);
        return analyzerProvider;
    }
}
