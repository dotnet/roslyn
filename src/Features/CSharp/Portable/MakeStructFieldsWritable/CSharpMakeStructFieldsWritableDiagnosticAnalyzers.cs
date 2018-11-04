using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable
{
    internal sealed class CSharpMakeStructFieldsWritableDiagnosticAnalyzers : AbstractCodeQualityDiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_diagnosticDescriptor = CreateDescriptor(
            IDEDiagnosticIds.MakeStructFieldsWritable,
            new LocalizableResourceString("test", FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString("test", FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: true);

        public CSharpMakeStructFieldsWritableDiagnosticAnalyzers()
            : base(ImmutableArray.Create(s_diagnosticDescriptor), GeneratedCodeAnalysisFlags.ReportDiagnostics)
        {
        }


        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }

        public override bool OpenFileOnly(Workspace workspace) => true;

        protected override void InitializeWorker(AnalysisContext context)
        {
            throw new NotImplementedException();
        }
    }
}
