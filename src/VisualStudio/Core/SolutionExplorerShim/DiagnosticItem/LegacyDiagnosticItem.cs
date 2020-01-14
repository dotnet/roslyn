// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed partial class LegacyDiagnosticItem : BaseDiagnosticItem
    {
        private readonly AnalyzerItem _analyzerItem;
        private readonly IContextMenuController _contextMenuController;

        public LegacyDiagnosticItem(AnalyzerItem analyzerItem, DiagnosticDescriptor descriptor, ReportDiagnostic effectiveSeverity, string language, IContextMenuController contextMenuController)
            : base(descriptor, effectiveSeverity, language)
        {
            _analyzerItem = analyzerItem;
            _contextMenuController = contextMenuController;
        }

        public override ProjectId ProjectId => _analyzerItem.AnalyzersFolder.ProjectId;
        protected override AnalyzerReference AnalyzerReference => _analyzerItem.AnalyzerReference;
        public override IContextMenuController ContextMenuController => _contextMenuController;
    }
}
