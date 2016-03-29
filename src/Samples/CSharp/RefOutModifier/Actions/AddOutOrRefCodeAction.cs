// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Samples.CodeAction.AddOrRemoveRefOutModifier.Properties;

namespace Roslyn.Samples.CodeAction.AddOrRemoveRefOutModifier
{
    internal class AddOutOrRefCodeAction : Microsoft.CodeAnalysis.CodeActions.CodeAction
    {
        private readonly Document document;
        private readonly SemanticModel semanticModel;
        private readonly ArgumentSyntax argument;
        private readonly IEnumerable<ParameterSyntax> parameters;

        public static bool Applicable(SemanticModel semanticModel, ArgumentSyntax argument, IEnumerable<ParameterSyntax> parameters)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(argument.Expression);
            var symbol = symbolInfo.Symbol;

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

            var field = symbol as IFieldSymbol;
            if (field != null)
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
            var symbolInfo = semanticModel.GetSymbolInfo(argument.Expression);
            if (symbolInfo.Symbol != null && symbolInfo.Symbol.Kind == SymbolKind.Parameter)
            {
                if (IsSameParameter(symbolInfo.Symbol as IParameterSymbol, this.parameters))
                {
                    return SyntaxFactory.Token(SyntaxKind.OutKeyword);
                }
            }

            var method = this.parameters.Select(p => p.AncestorAndSelf<BaseMethodDeclarationSyntax>()).FirstOrDefault(m => m.Body != null);
            if (method == null)
            {
                return SyntaxFactory.Token(SyntaxKind.RefKeyword);
            }

            var dataFlow = this.semanticModel.AnalyzeDataFlow(method.Body);
            if (ContainSameParameter(dataFlow.ReadInside, this.parameters))
            {
                return SyntaxFactory.Token(SyntaxKind.RefKeyword);
            }

            return ContainSameParameter(dataFlow.AlwaysAssigned, this.parameters) ? SyntaxFactory.Token(SyntaxKind.OutKeyword) : SyntaxFactory.Token(SyntaxKind.RefKeyword);
        }

        private static bool ContainSameParameter(IEnumerable<ISymbol> symbols, IEnumerable<ParameterSyntax> parameters)
        {
            foreach (var symbol in symbols)
            {
                var parameterSymbol = symbol as IParameterSymbol;
                if (parameterSymbol == null)
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
            var parametersFromSymbol = parameterSymbol.Locations.Select(l => l.FindToken().AncestorAndSelf<ParameterSyntax>());
            if (parameters.Any(p => parametersFromSymbol.Any(p2 => p == p2)))
            {
                return true;
            }

            return false;
        }

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var modifier = GetOutOrRefModifier();

            var map = new Dictionary<SyntaxNode, SyntaxNode>();
            map.Add(this.argument,
                    SyntaxFactory.Argument(this.argument.NameColon, modifier, this.argument.Expression)
                        .WithAdditionalAnnotations(Formatter.Annotation));

            foreach (var parameter in this.parameters)
            {
                map.Add(parameter,
                        SyntaxFactory.Parameter(parameter.AttributeLists, parameter.Modifiers.Add(modifier), parameter.Type, parameter.Identifier, parameter.Default)
                            .WithAdditionalAnnotations(Formatter.Annotation));
            }

            var root = (SyntaxNode)await this.document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNodes(map.Keys, (o, n) => map[o]);

            return this.document.WithSyntaxRoot(newRoot);
        }

        public override string Title
        {
            get { return Resources.AddOutOrRefTitle; }
        }
    }
}
