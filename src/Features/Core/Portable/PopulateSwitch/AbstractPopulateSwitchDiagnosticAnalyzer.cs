using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics.PopulateSwitch
{
    internal abstract class AbstractPopulateSwitchDiagnosticAnalyzerBase<TLanguageKindEnum, TSwitchBlockSyntax> : DiagnosticAnalyzer, IBuiltInAnalyzer where TLanguageKindEnum : struct where TSwitchBlockSyntax : SyntaxNode
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.AddMissingSwitchCases), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.PopulateSwitch), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(IDEDiagnosticIds.PopulateSwitchDiagnosticId,
                                                                    s_localizableTitle,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Warning,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        #region Interface methods

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                nodeContext =>
                {
                    Diagnostic diagnostic;
                    if (TryPopulateSwitch(nodeContext.SemanticModel, nodeContext.Node, out diagnostic, nodeContext.CancellationToken))
                    {
                        nodeContext.ReportDiagnostic(diagnostic);
                    }
                },
                SyntaxKindsOfInterest);
        }

        protected abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }

        #endregion

        protected abstract SyntaxNode GetExpression(TSwitchBlockSyntax node);
        protected abstract List<SyntaxNode> GetCaseLabels(TSwitchBlockSyntax switchBlock, out bool hasDefaultCase);

        private bool SwitchIsFullyPopulated(SemanticModel model, TSwitchBlockSyntax node)
        {
            var enumType = model.GetTypeInfo(GetExpression(node)).Type as INamedTypeSymbol;
            if (enumType == null || enumType.TypeKind != TypeKind.Enum)
            {
                return true;
            }

            // ignore enums marked with Flags per spec: https://github.com/dotnet/roslyn/issues/6766#issuecomment-156878851
            foreach (var attribute in enumType.GetAttributes())
            {
                var containingClass = attribute.AttributeClass.ToDisplayString();
                if (containingClass == typeof(System.FlagsAttribute).FullName)
                {
                    return true;
                }
            }

            bool hasDefaultCase;
            var caseLabels = GetCaseLabels(node, out hasDefaultCase);

            if (!hasDefaultCase)
            {
                return false;
            }

            var labelSymbols = new List<ISymbol>();
            foreach (var label in caseLabels)
            {
                var symbol = model.GetSymbolInfo(label).Symbol;
                if (symbol == null)
                {
                    return true;
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
                    return false;
                }
            }

            return true;
        }

        private bool TryPopulateSwitch(SemanticModel model, SyntaxNode node, out Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var switchBlock = (TSwitchBlockSyntax)node;

            diagnostic = default(Diagnostic);

            if (SwitchIsFullyPopulated(model, switchBlock))
            {
                return false;
            }

            var tree = model.SyntaxTree;
            var span = GetExpression(switchBlock).Span;
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
