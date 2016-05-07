using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.AddBraces
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal abstract class AbstractAddBracesDiagnosticAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer, IBuiltInAnalyzer
        where TLanguageKindEnum : struct
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.AddBraces), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.AddBraces), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(IDEDiagnosticIds.AddBracesDiagnosticId,
                                                                    s_localizableTitle,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Warning,
                                                                    isEnabledByDefault: true);

        protected abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(s_descriptor);
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKindsOfInterest);
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);
    }
}