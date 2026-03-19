// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[Export(typeof(IAnalyzerNodeSetup))]
internal sealed class AnalyzerNodeSetup : IAnalyzerNodeSetup
{
    private readonly AnalyzerItemsTracker _analyzerTracker;
    private readonly AnalyzersCommandHandler _analyzerCommandHandler;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public AnalyzerNodeSetup(
        AnalyzerItemsTracker analyzerTracker,
        AnalyzersCommandHandler analyzerCommandHandler)
    {
        _analyzerTracker = analyzerTracker;
        _analyzerCommandHandler = analyzerCommandHandler;
    }

    public async Task InitializeAsync(IAsyncServiceProvider serviceProvider, ThreadSafeMenuCommandService menuCommandService, CancellationToken cancellationToken)
    {
        await _analyzerTracker.RegisterAsync(serviceProvider, cancellationToken).ConfigureAwait(false);
        _analyzerCommandHandler.Initialize(menuCommandService);
    }

    public void Unregister()
    {
        _analyzerTracker.Unregister();
    }
}
