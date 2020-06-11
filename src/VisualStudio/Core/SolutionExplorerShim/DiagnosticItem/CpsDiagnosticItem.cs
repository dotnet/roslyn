// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
