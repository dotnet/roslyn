// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAnalyzerNodeSetup))]
    internal sealed class AnalyzerNodeSetup : IAnalyzerNodeSetup
    {
        private readonly AnalyzerItemsTracker _analyzerTracker;
        private readonly AnalyzersCommandHandler _analyzerCommandHandler;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerNodeSetup(AnalyzerItemsTracker analyzerTracker, AnalyzersCommandHandler analyzerCommandHandler)
        {
            _analyzerTracker = analyzerTracker;
            _analyzerCommandHandler = analyzerCommandHandler;
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            _analyzerTracker.Register();
            _analyzerCommandHandler.Initialize((IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService)));
        }

        public void Unregister()
        {
            _analyzerTracker.Unregister();
        }
    }
}
