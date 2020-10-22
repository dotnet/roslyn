// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Roslyn.Utilities;

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
        protected override AnalyzerReference AnalyzerReference
        {
            get
            {
                // _source.AnalyzerReference can be null if the source hasn't found it's reference yet;
                // once it has the property doesn't go null again, and only then would we create DiagnosticItems
                // for each diagnostic in the analyzer reference.
                Contract.ThrowIfNull(_source.AnalyzerReference);
                return _source.AnalyzerReference;
            }
        }

        public override IContextMenuController ContextMenuController => _source.DiagnosticItemContextMenuController;
    }
}
