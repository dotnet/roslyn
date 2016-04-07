using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PopulateSwitch
{
    internal abstract class AbstractPopulateSwitchDiagnosticAnalyzerBase<TLanguageKindEnum, TSwitchBlockSyntax, TExpressionSyntax> : DiagnosticAnalyzer, IBuiltInAnalyzer
        where TLanguageKindEnum : struct
        where TSwitchBlockSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.Add_missing_switch_cases), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.PopulateSwitch), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(IDEDiagnosticIds.PopulateSwitchDiagnosticId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.Style,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true);

        #region Interface methods

        protected abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }
        protected abstract TExpressionSyntax GetExpression(TSwitchBlockSyntax node);
        protected abstract List<TExpressionSyntax> GetCaseLabels(TSwitchBlockSyntax switchBlock, out bool hasDefaultCase);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKindsOfInterest);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var model = context.SemanticModel;
            var tree = model.SyntaxTree;
            var switchBlock = (TSwitchBlockSyntax)context.Node;

            bool missingCases;
            bool missingDefaultCase;
            if (SwitchIsIncomplete(model, switchBlock, out missingCases, out missingDefaultCase) &&
                !tree.OverlapsHiddenPosition(switchBlock.Span, context.CancellationToken))
            {
                Debug.Assert(missingCases || missingDefaultCase);
                var properties = ImmutableDictionary<string, string>.Empty
                    .Add(PopulateSwitchHelpers.MissingCases, missingCases.ToString())
                    .Add(PopulateSwitchHelpers.MissingDefaultCase, missingDefaultCase.ToString());

                var diagnostic = Diagnostic.Create(
                    s_descriptor, switchBlock.GetLocation(), properties: properties);
                context.ReportDiagnostic(diagnostic);
            }
        }

        #endregion

        private bool SwitchIsIncomplete(
            SemanticModel model, TSwitchBlockSyntax node,
            out bool missingCases, out bool missingDefaultCase)
        {
            bool hasDefaultCase;
            var caseLabels = GetCaseLabels(node, out hasDefaultCase);

            missingDefaultCase = !hasDefaultCase;
            missingCases = false;

            // We know the switch has a 'default' label.  Now we need to determine if there are 
            // any missing labels so that we can offer to generate them for the user.

            // If we can't determine the type of this switch, or we're switching over someting
            // that sin't an enum, just consider this switch complete.  We can't add any cases
            // here.
            var enumType = model.GetTypeInfo(GetExpression(node)).Type as INamedTypeSymbol;
            if (enumType != null && enumType.TypeKind == TypeKind.Enum)
            {
                var missingSwitchCases = PopulateSwitchHelpers.GetMissingSwitchCases(model, enumType, caseLabels);
                missingCases = missingSwitchCases.Count > 0;
            }

            // The switch is incomplete if we're missing any cases or we're missing a default case.
            return missingDefaultCase || missingCases;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SyntaxAnalysis;
    }
}
