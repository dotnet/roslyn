// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAnalyzerNodeSetup))]
    internal sealed class AnalyzerNodeSetup : IAnalyzerNodeSetup
    {
        private readonly IThreadingContext _threadingContext;
        private readonly AnalyzerItemsTracker _analyzerTracker;
        private readonly AnalyzersCommandHandler _analyzerCommandHandler;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerNodeSetup(
            IThreadingContext threadingContext,
            AnalyzerItemsTracker analyzerTracker,
            AnalyzersCommandHandler analyzerCommandHandler)
        {
            _threadingContext = threadingContext;
            _analyzerTracker = analyzerTracker;
            _analyzerCommandHandler = analyzerCommandHandler;
        }

        public async Task InitializeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await _analyzerTracker.RegisterAsync(serviceProvider, cancellationToken).ConfigureAwait(false);
            await _analyzerCommandHandler.InitializeAsync(
                await serviceProvider.GetServiceAsync<IMenuCommandService, IMenuCommandService>(_threadingContext.JoinableTaskFactory, throwOnFailure: false).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        public void Unregister()
        {
            _analyzerTracker.Unregister();
        }
    }
}
