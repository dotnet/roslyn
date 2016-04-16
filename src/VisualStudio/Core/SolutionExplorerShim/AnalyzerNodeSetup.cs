// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAnalyzerNodeSetup))]
    internal sealed class AnalyzerNodeSetup : IAnalyzerNodeSetup
    {
        private readonly AnalyzerItemsTracker _analyzerTracker;
        private readonly AnalyzersCommandHandler _analyzerCommandHandler;

        [ImportingConstructor]
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
