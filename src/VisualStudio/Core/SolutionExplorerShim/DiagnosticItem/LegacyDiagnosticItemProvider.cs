// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(LegacyDiagnosticItemProvider))]
    [Order]
    internal sealed class LegacyDiagnosticItemProvider : AttachedCollectionSourceProvider<AnalyzerItem>
    {
        private readonly IAnalyzersCommandHandler _commandHandler;
        private readonly IServiceProvider _serviceProvider;

        private IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        [ImportingConstructor]
        public LegacyDiagnosticItemProvider(
            [Import(typeof(AnalyzersCommandHandler))]IAnalyzersCommandHandler commandHandler,
            [Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider)
        {
            _commandHandler = commandHandler;
            _serviceProvider = serviceProvider;
        }

        protected override IAttachedCollectionSource CreateCollectionSource(AnalyzerItem item, string relationshipName)
        {
            if (relationshipName == KnownRelationships.Contains)
            {
                var analyzerService = GetAnalyzerService();
                return new LegacyDiagnosticItemSource(item, _commandHandler, analyzerService);
            }

            return null;
        }

        private IDiagnosticAnalyzerService GetAnalyzerService()
        {
            if (_diagnosticAnalyzerService == null)
            {
                var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
                _diagnosticAnalyzerService = componentModel.GetService<IDiagnosticAnalyzerService>();
            }

            return _diagnosticAnalyzerService;
        }
    }
}
