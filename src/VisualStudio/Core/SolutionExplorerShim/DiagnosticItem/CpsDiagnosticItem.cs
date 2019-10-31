// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed partial class CpsDiagnosticItem : BaseDiagnosticItem
    {
        private readonly CpsDiagnosticItemSource _source;

        public CpsDiagnosticItem(CpsDiagnosticItemSource source, DiagnosticDescriptor descriptor, ReportDiagnostic effectiveSeverity, string language)
            : base(descriptor, effectiveSeverity, language)
        {
            _source = source;
        }

        public override ProjectId ProjectId => _source.ProjectId;
        protected override AnalyzerReference AnalyzerReference => _source.AnalyzerReference;
        public override IContextMenuController ContextMenuController => _source.DiagnosticItemContextMenuController;
    }
}
