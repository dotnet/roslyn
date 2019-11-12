// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal partial class LegacyDiagnosticItemSource : BaseDiagnosticItemSource
    {
        private readonly AnalyzerItem _item;

        public LegacyDiagnosticItemSource(AnalyzerItem item, IAnalyzersCommandHandler commandHandler, IDiagnosticAnalyzerService diagnosticAnalyzerService)
            : base(item.AnalyzersFolder.Workspace, item.AnalyzersFolder.ProjectId, commandHandler, diagnosticAnalyzerService)
        {
            _item = item;
        }

        public override object SourceItem => _item;
        public override AnalyzerReference AnalyzerReference => _item.AnalyzerReference;

        protected override BaseDiagnosticItem CreateItem(DiagnosticDescriptor diagnostic, ReportDiagnostic effectiveSeverity, string language)
            => new LegacyDiagnosticItem(_item, diagnostic, effectiveSeverity, language, CommandHandler.DiagnosticContextMenuController);
    }
}
