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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Samples.CodeAction.AddOrRemoveRefOutModifier
{
    internal class ApplicableActionFinder
    {
        private readonly Document document;
        private readonly CancellationToken cancellationToken;
        private readonly int position;

        public ApplicableActionFinder(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            this.document = document;
            this.position = position;
            this.cancellationToken = cancellationToken;
        }

        public async Task<Tuple<TextSpan, Microsoft.CodeAnalysis.CodeActions.CodeAction>> GetSpanAndActionAsync()
        {
            var semanticModel = await document.GetSemanticModelAsync(this.cancellationToken).ConfigureAwait(false);
            var tree = (SyntaxTree)await document.GetSyntaxTreeAsync(this.cancellationToken).ConfigureAwait(false);
            if (!tree.OnArgumentOrParameter(this.position))
            {
                return null;
            }

            var root = await tree.GetRootAsync(this.cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(this.position);
            var action = await GetActionAsync(semanticModel, tree, token).ConfigureAwait(false);
            if (action == null)
            {
                return null;
            }

            return Tuple.Create(token.Span, action);
        }

        private async Task<Microsoft.CodeAnalysis.CodeActions.CodeAction> GetActionAsync(SemanticModel semanticModel, SyntaxTree tree, SyntaxToken token)
        {
            var methodSymbol = GetMethodDefinitionSymbol(semanticModel, token);
            if (methodSymbol == null || methodSymbol.Locations.Any(l => l.IsInMetadata))
            {
                // can't find method definition defined in source
                return null;
            }

            // adding or deleting ref/out on anonymous method/lambda not supported
            if (methodSymbol.MethodKind == MethodKind.AnonymousFunction)
            {
                return null;
            }

            var argumentAndParameters = await GetArgumentAndParametersAsync(semanticModel, methodSymbol, token).ConfigureAwait(false);
            if (argumentAndParameters == null)
            {
                return null;
            }

            // currently only support everything in one file
            var nodes = (new SyntaxNode[] { argumentAndParameters.Item1 }).Concat(argumentAndParameters.Item2);
            if (this.document.Project.GetContainingDocuments(nodes, this.cancellationToken).Count() != 1)
            {
                return null;
            }

            if (tree.OnArgumentOrParameterWithoutRefOut(this.position))
            {
                return AddOutOrRefCodeAction.Applicable(semanticModel, argumentAndParameters.Item1, argumentAndParameters.Item2) ?
                    new AddOutOrRefCodeAction(this.document, semanticModel, argumentAndParameters.Item1, argumentAndParameters.Item2) : null;
            }
            else
            {
                return RemoveOutOrRefCodeAction.Applicable(semanticModel, argumentAndParameters.Item1, argumentAndParameters.Item2) ?
                    new RemoveOutOrRefCodeAction(this.document, semanticModel, argumentAndParameters.Item1, argumentAndParameters.Item2) : null;
            }
        }

        private async Task<Tuple<ArgumentSyntax, IEnumerable<ParameterSyntax>>> GetArgumentAndParametersAsync(
            SemanticModel semanticModel,
            IMethodSymbol methodSymbol,
            SyntaxToken token)
        {
            var parameterInfo = GetParameterInfo(semanticModel, methodSymbol, token);
            if (parameterInfo == null)
            {
                return null;
            }

            var argument = await GetArgumentAsync(methodSymbol, parameterInfo.Item1, parameterInfo.Item2.First()).ConfigureAwait(false);
            if (argument == null)
            {
                return null;
            }

            return Tuple.Create(argument, parameterInfo.Item2);
        }

        private async Task<ArgumentSyntax> GetArgumentAsync(IMethodSymbol methodSymbol, int parameterIndex, ParameterSyntax parameter)
        {
            var solution = this.document.Project.Solution;
            var result = await SymbolFinder.FindReferencesAsync(methodSymbol, solution, cancellationToken: this.cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            // sample only supports having only one reference and from same project
            if (result.Count() != 1 ||
                result.Single().Locations.Any(l => !l.Location.IsInSource))
            {
                return null;
            }

            var invocation = result.Single()
                                   .Locations
                                   .Cast<Location>()
                                   .Select(l => l.FindToken().AncestorAndSelf<InvocationExpressionSyntax>())
                                   .Single();

            var parameterName = parameter.Identifier.ValueText;
            var list = new List<ArgumentSyntax>();

            for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                // position based
                var argument = invocation.ArgumentList.Arguments[i];
                if (argument.NameColon == null && i == parameterIndex)
                {
                    return argument;
                }

                // named parameter
                if (argument.NameColon != null)
                {
                    var namedArgument = invocation.ArgumentList
                                                  .Arguments
                                                  .Where(a => a.NameColon != null)
                                                  .FirstOrDefault(a => a.NameColon.Name.Identifier.ValueText == parameterName);
                    if (namedArgument == null)
                    {
                        return null;
                    }

                    return namedArgument;
                }
            }

            return null;
        }

        private Tuple<int, IEnumerable<ParameterSyntax>> GetParameterInfo(
            SemanticModel semanticModel,
            IMethodSymbol methodSymbol,
            SyntaxToken token)
        {
            int parameterIndex = GetParameterIndex(methodSymbol, token);
            if (parameterIndex < 0)
            {
                return null;
            }

            // find all parameter syntax for the index
            var parameters = methodSymbol.Locations
                                         .Select(l => l.FindToken().AncestorAndSelf<BaseMethodDeclarationSyntax>())
                                         .Select(n => n.ParameterList.Parameters[parameterIndex]);

            if (parameters.Count() > 1)
            {
                parameters = parameters.Reverse();
            }

            return Tuple.Create(parameterIndex, parameters);
        }

        private int GetParameterIndex(IMethodSymbol methodSymbol, SyntaxToken token)
        {
            var argument = token.AncestorAndSelf<ArgumentSyntax>();
            if (argument != null)
            {
                // name parameter?
                if (argument.NameColon != null)
                {
                    var symbol = methodSymbol.Parameters.FirstOrDefault(p => p.Name == argument.NameColon.Name.Identifier.ValueText);
                    if (symbol == null)
                    {
                        // named parameter is used but can't find one?
                        return -1;
                    }

                    return symbol.Ordinal;
                }

                // positional argument
                var list = argument.Parent as ArgumentListSyntax;
                for (int i = 0; i < list.Arguments.Count; i++)
                {
                    var arg = list.Arguments[i];

                    // malformed call
                    if (arg.NameColon != null)
                    {
                        return -1;
                    }

                    if (arg == argument)
                    {
                        return i;
                    }
                }

                return -1;
            }

            var parameter = token.AncestorAndSelf<ParameterSyntax>();
            if (parameter != null)
            {
                var parameterList = parameter.AncestorAndSelf<ParameterListSyntax>();
                return parameterList.Parameters.IndexOf(parameter);
            }

            return -1;
        }

        private IMethodSymbol GetMethodDefinitionSymbol(SemanticModel semanticModel, SyntaxToken token)
        {
            var argument = token.AncestorAndSelf<ArgumentSyntax>();
            if (argument != null)
            {
                var invocation = argument.AncestorAndSelf<InvocationExpressionSyntax>();
                if (invocation == null)
                {
                    return null;
                }

                return semanticModel.GetSymbolInfo(invocation, this.cancellationToken).Symbol as IMethodSymbol;
            }

            var parameter = token.AncestorAndSelf<ParameterSyntax>();
            if (parameter != null)
            {
                var parameterList = parameter.AncestorAndSelf<ParameterListSyntax>();
                if (parameterList == null)
                {
                    // doesn't support lambda
                    return null;
                }

                var definitionNode = parameterList.Parent;
                return semanticModel.GetDeclaredSymbol(definitionNode, this.cancellationToken) as IMethodSymbol;
            }

            return null;
        }
    }
}
