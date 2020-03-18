// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
