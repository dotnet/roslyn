// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name("DiagnosticsItemProvider")]
    [Order]
    internal sealed class DiagnosticItemProvider : AttachedCollectionSourceProvider<AnalyzerItem>
    {
        [Import(typeof(AnalyzersCommandHandler))]
        private IAnalyzersCommandHandler _commandHandler = null;

        [Import]
        private IDiagnosticAnalyzerService _diagnosticAnalyzerService = null;

        protected override IAttachedCollectionSource CreateCollectionSource(AnalyzerItem item, string relationshipName)
        {
            if (relationshipName == KnownRelationships.Contains)
            {
                return new DiagnosticItemSource(item, _commandHandler, _diagnosticAnalyzerService);
            }

            return null;
        }
    }
}
