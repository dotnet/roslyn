using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System;

namespace Microsoft.CodeAnalysis.Diagnostics.PopulateSwitch
{
    internal abstract class AbstractPopulateSwitchDiagnosticAnalyzerBase<TLanguageKindEnum, TSwitchBlockSyntax, TExpressionSyntax> : DiagnosticAnalyzer, IBuiltInAnalyzer
        where TLanguageKindEnum : struct
        where TSwitchBlockSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.AddMissingSwitchCases), FeaturesResources.ResourceManager, typeof(FeaturesResources));
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

            if (SwitchIsIncomplete(model, switchBlock) &&
                !tree.OverlapsHiddenPosition(switchBlock.Span, context.CancellationToken))
            {
                var diagnostic = Diagnostic.Create(s_descriptor, tree.GetLocation((Text.TextSpan)switchBlock.Span));
                context.ReportDiagnostic(diagnostic);
            }
        }

        #endregion

        private bool SwitchIsIncomplete(SemanticModel model, TSwitchBlockSyntax node)
        {
            bool hasDefaultCase;
            var caseLabels = GetCaseLabels(node, out hasDefaultCase);

            if (!hasDefaultCase)
            {
                // If the switch doesn't have a 'default' label, always provide the option to the
                // user to add one.
                return true;
            }

            // We know the switch has a 'default' label.  Now we need to determine if there are 
            // any missing labels so that we can offer to generate them for the user.

            var enumType = model.GetTypeInfo(GetExpression(node)).Type as INamedTypeSymbol;
            if (enumType == null || enumType.TypeKind != TypeKind.Enum)
            {
                // If we can't determine the type of this switch, or we're switching over someting
                // that sin't an enum, just consider this switch complete.  We can't add any cases
                // here.
                return false;
            }

            var labelSymbols = new HashSet<ISymbol>();
            foreach (var label in caseLabels)
            {
                var symbol = model.GetSymbolInfo(label).Symbol;
                if (symbol == null)
                {
                    // something is wrong with the label and the SemanticModel was unable to 
                    // determine its symbol.  Abort the analyzer by considering this switch
                    // statement as complete.
                    return false;
                }

                labelSymbols.Add(symbol);
            }

            foreach (var member in enumType.GetMembers())
            {
                // skip `.ctor` and `__value`
                var fieldSymbol = member as IFieldSymbol;
                if (fieldSymbol == null || fieldSymbol.Type.SpecialType != SpecialType.None)
                {
                    continue;
                }
                
                if (!labelSymbols.Contains(member))
                {
                    // We're missing a label for this member.  The switch is not complete.
                    return true;
                }
            }

            return false;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
