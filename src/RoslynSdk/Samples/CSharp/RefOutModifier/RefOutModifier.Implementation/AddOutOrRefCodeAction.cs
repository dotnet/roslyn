using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using RefOutModifier.Properties;

namespace Roslyn.Samples.AddOrRemoveRefOutModifier
{
    internal class AddOutOrRefCodeAction : CodeAction
    {
        private readonly Document document;
        private readonly SemanticModel semanticModel;
        private readonly ArgumentSyntax argument;
        private readonly IEnumerable<ParameterSyntax> parameters;

        public static bool Applicable(SemanticModel semanticModel, ArgumentSyntax argument, IEnumerable<ParameterSyntax> parameters)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(argument.Expression);
            ISymbol symbol = symbolInfo.Symbol;

            if (symbol == null)
            {
                return true;
            }

            if (symbol.Kind != SymbolKind.Field &&
                symbol.Kind != SymbolKind.Parameter &&
                symbol.Kind != SymbolKind.Local)
            {
                return false;
            }

            if (symbol is IFieldSymbol field)
            {
                return !field.IsReadOnly;
            }

            return true;
        }

        public AddOutOrRefCodeAction(
            Document document,
            SemanticModel semanticModel,
            ArgumentSyntax argument,
            IEnumerable<ParameterSyntax> parameters)
        {
            this.document = document;
            this.semanticModel = semanticModel;
            this.argument = argument;
            this.parameters = parameters;
        }

        private SyntaxToken GetOutOrRefModifier()
        {
            // special case where argument == parameter
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(argument.Expression);
            if (symbolInfo.Symbol != null && symbolInfo.Symbol.Kind == SymbolKind.Parameter)
            {
                if (IsSameParameter(symbolInfo.Symbol as IParameterSymbol, parameters))
                {
                    return SyntaxFactory.Token(SyntaxKind.OutKeyword);
                }
            }

            BaseMethodDeclarationSyntax method = parameters.Select(p => p.AncestorAndSelf<BaseMethodDeclarationSyntax>()).FirstOrDefault(m => m.Body != null);
            if (method == null)
            {
                return SyntaxFactory.Token(SyntaxKind.RefKeyword);
            }

            DataFlowAnalysis dataFlow = semanticModel.AnalyzeDataFlow(method.Body);
            if (ContainSameParameter(dataFlow.ReadInside, parameters))
            {
                return SyntaxFactory.Token(SyntaxKind.RefKeyword);
            }

            return ContainSameParameter(dataFlow.AlwaysAssigned, parameters) ? SyntaxFactory.Token(SyntaxKind.OutKeyword) : SyntaxFactory.Token(SyntaxKind.RefKeyword);
        }

        private static bool ContainSameParameter(IEnumerable<ISymbol> symbols, IEnumerable<ParameterSyntax> parameters)
        {
            foreach (ISymbol symbol in symbols)
            {
                if (!(symbol is IParameterSymbol parameterSymbol))
                {
                    continue;
                }

                if (IsSameParameter(parameterSymbol, parameters))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameParameter(IParameterSymbol parameterSymbol, IEnumerable<ParameterSyntax> parameters)
        {
            IEnumerable<ParameterSyntax> parametersFromSymbol = parameterSymbol.Locations.Select(l => l.FindToken().AncestorAndSelf<ParameterSyntax>());
            if (parameters.Any(p => parametersFromSymbol.Any(p2 => p == p2)))
            {
                return true;
            }

            return false;
        }

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            SyntaxToken modifier = GetOutOrRefModifier();

            Dictionary<SyntaxNode, SyntaxNode> map = new Dictionary<SyntaxNode, SyntaxNode>
            {
                {
                    argument,
                    SyntaxFactory.Argument(argument.NameColon, modifier, argument.Expression)
                        .WithAdditionalAnnotations(Formatter.Annotation)
                }
            };

            foreach (ParameterSyntax parameter in parameters)
            {
                map.Add(parameter,
                        SyntaxFactory.Parameter(parameter.AttributeLists, parameter.Modifiers.Add(modifier), parameter.Type, parameter.Identifier, parameter.Default)
                            .WithAdditionalAnnotations(Formatter.Annotation));
            }

            SyntaxNode root = (SyntaxNode)await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode newRoot = root.ReplaceNodes(map.Keys, (o, n) => map[o]);

            return document.WithSyntaxRoot(newRoot);
        }

        public override string Title
        {
            get { return Resources.AddOutOrRefTitle; }
        }

    }
}
