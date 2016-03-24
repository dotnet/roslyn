using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics.PopulateSwitch
{
    internal abstract class PopulateSwitchDiagnosticAnalyzerBase<TLanguageKindEnum> : DiagnosticAnalyzer, IBuiltInAnalyzer where TLanguageKindEnum : struct
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.AddMissingStatements), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.PopulateSwitch), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(IDEDiagnosticIds.PopulateSwitchDiagnosticId,
                                                                    s_localizableTitle,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.EditAndContinue,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.EditAndContinue);

        #region Interface methods

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(s_descriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                (nodeContext) =>
                {
                    Diagnostic diagnostic;
                    if (TryPopulateSwitch(nodeContext.SemanticModel, nodeContext.Node, out diagnostic, nodeContext.CancellationToken))
                    {
                        nodeContext.ReportDiagnostic(diagnostic);
                    }
                },
                SyntaxKindsOfInterest);
        }

        public abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }

        #endregion

        protected abstract bool SwitchIsFullyPopulated(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract TextSpan GetDiagnosticSpan(SyntaxNode node);

        private bool TryPopulateSwitch(SemanticModel model, SyntaxNode node, out Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic = default(Diagnostic);

            if (SwitchIsFullyPopulated(model, node, cancellationToken))
            {
                return false;
            }

            var tree = model.SyntaxTree;
            var span = GetDiagnosticSpan(node);
            if (tree.OverlapsHiddenPosition(span, cancellationToken))
            {
                return false;
            }

            diagnostic = Diagnostic.Create(s_descriptor, tree.GetLocation(span));
            return true;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
        }
    }
}
