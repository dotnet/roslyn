// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.CodeAnalysis.Host.Mef;

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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LegacyDiagnosticItemProvider(
            [Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
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
