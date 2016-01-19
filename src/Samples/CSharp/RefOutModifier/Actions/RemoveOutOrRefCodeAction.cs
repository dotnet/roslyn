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

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    internal class RemoveOutOrRefCodeAction : Microsoft.CodeAnalysis.CodeActions.CodeAction
    {
        private readonly Document document;
        private readonly SemanticModel semanticModel;
        private readonly ArgumentSyntax argument;
        private readonly IEnumerable<ParameterSyntax> parameters;

        public static bool Applicable(SemanticModel semanticModel, ArgumentSyntax argument, IEnumerable<ParameterSyntax> parameters)
        {
            var method = argument.AncestorAndSelf<BaseMethodDeclarationSyntax>();
            if (method == null ||
                method.Body == null)
            {
                return false;
            }

            if (argument.RefOrOutKeyword.Kind() == SyntaxKind.RefKeyword)
            {
                return true;
            }

            Debug.Assert(argument.RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword);

            var symbolInfo = semanticModel.GetSymbolInfo(argument.Expression);
            if (!(symbolInfo.Symbol != null && symbolInfo.Symbol.Kind == SymbolKind.Local))
            {
                return true;
            }

            // for local, make sure it is definitely assigned before removing "out" keyword
            var invocation = argument.AncestorAndSelf<InvocationExpressionSyntax>();
            if (invocation == null)
            {
                return false;
            }

            var range = GetStatementRangeForFlowAnalysis<StatementSyntax>(method.Body, TextSpan.FromBounds(method.Body.OpenBraceToken.Span.End, invocation.Span.Start));
            var dataFlow = semanticModel.AnalyzeDataFlow(range.Item1, range.Item2);
            foreach (var symbol in dataFlow.AlwaysAssigned)
            {
                if (symbolInfo.Symbol == symbol)
                {
                    return true;
                }
            }

            return false;
        }

        private static Tuple<T, T> GetStatementRangeForFlowAnalysis<T>(SyntaxNode node, TextSpan textSpan) where T : SyntaxNode
        {
            T firstStatement = null;
            T lastStatement = null;

            foreach (var stmt in node.DescendantNodesAndSelf().OfType<T>())
            {
                if (firstStatement == null && stmt.Span.Start >= textSpan.Start)
                {
                    firstStatement = stmt;
                }

                if (firstStatement != null && stmt.Span.End <= textSpan.End && stmt.Parent == firstStatement.Parent)
                {
                    lastStatement = stmt;
                }
            }

            if (firstStatement == null || lastStatement == null)
            {
                return null;
            }

            return new Tuple<T, T>(firstStatement, lastStatement);
        }

        public RemoveOutOrRefCodeAction(
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

        public override string Title
        {
            get { return Resources.RemoveOutOrRefTitle; }
        }

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var map = new Dictionary<SyntaxToken, SyntaxToken>();

            map.Add(this.argument.RefOrOutKeyword, default(SyntaxToken));

            var tokenBeforeArgumentModifier = this.argument.RefOrOutKeyword.GetPreviousToken(includeSkipped: true);
            map.Add(tokenBeforeArgumentModifier,
                    tokenBeforeArgumentModifier.MergeTrailingTrivia(this.argument.RefOrOutKeyword)
                                               .WithAdditionalAnnotations(Formatter.Annotation));

            foreach (var parameter in this.parameters)
            {
                var outOrRefModifier = parameter.Modifiers.FirstOrDefault(t => t.Kind() == SyntaxKind.OutKeyword || t.Kind() == SyntaxKind.RefKeyword);
                if (outOrRefModifier.Kind() == SyntaxKind.None)
                {
                    continue;
                }

                map.Add(outOrRefModifier, default(SyntaxToken));

                var tokenBeforeParameterModifier = outOrRefModifier.GetPreviousToken(includeSkipped: true);
                map.Add(tokenBeforeParameterModifier, tokenBeforeParameterModifier.MergeTrailingTrivia(outOrRefModifier)
                                                                                  .WithAdditionalAnnotations(Formatter.Annotation));
            }

            var root = (SyntaxNode)await this.document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceTokens(map.Keys, (o, n) => map[o]);

            return this.document.WithSyntaxRoot(newRoot);
        }
    }
}