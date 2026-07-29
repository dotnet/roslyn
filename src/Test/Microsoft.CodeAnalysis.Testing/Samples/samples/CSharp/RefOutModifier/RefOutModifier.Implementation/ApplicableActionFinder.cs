using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Samples.AddOrRemoveRefOutModifier
{
    internal class ApplicableActionFinder
    {
        private Document document;
        private readonly int position;
        private readonly CancellationToken cancellationToken;

        public ApplicableActionFinder(Document document, int position, CancellationToken cancellationToken)
        {
            this.document = document;
            this.position = position;
            this.cancellationToken = cancellationToken;
        }

        public async Task<(TextSpan, CodeAction)> GetSpanAndActionAsync()
        {
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            SyntaxTree tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (!tree.OnArgumentOrParameter(position))
            {
                return (default, null);
            }

            SyntaxNode root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxToken token = root.FindToken(position);
            CodeAction action = await GetActionAsync(semanticModel, tree, token).ConfigureAwait(false);
            if (action == null)
            {
                return (default, null);
            }

            return (token.Span, action);
        }

        private async Task<CodeAction> GetActionAsync(SemanticModel semanticModel, SyntaxTree tree, SyntaxToken token)
        {
            IMethodSymbol methodSymbol = GetMethodDefinitionSymbol(semanticModel, token);
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

            (ArgumentSyntax argument, IEnumerable<ParameterSyntax> parameters) = await GetArgumentAndParametersAsync(semanticModel, methodSymbol, token).ConfigureAwait(false);
            if (argument == null || parameters == null)
            {
                return null;
            }

            // currently only support everything in one file
            IEnumerable<SyntaxNode> nodes = (new SyntaxNode[] { argument }).Concat(parameters);
            if (document.Project.GetContainingDocuments(nodes, cancellationToken).Count() != 1)
            {
                return null;
            }

            if (tree.OnArgumentOrParameterWithoutRefOut(position))
            {
                return AddOutOrRefCodeAction.Applicable(semanticModel, argument, parameters)
                    ? new AddOutOrRefCodeAction(document, semanticModel, argument, parameters)
                    : null;
            }
            else
            {
                return RemoveOutOrRefCodeAction.Applicable(semanticModel, argument, parameters)
                    ? new RemoveOutOrRefCodeAction(document, semanticModel, argument, parameters)
                    : null;
            }
        }

        private async Task<(ArgumentSyntax, IEnumerable<ParameterSyntax>)> GetArgumentAndParametersAsync(
            SemanticModel semanticModel,
            IMethodSymbol methodSymbol,
            SyntaxToken token)
        {
            (int parameterIndex, IEnumerable<ParameterSyntax> parameters) = GetParameterInfo(semanticModel, methodSymbol, token);
            if (parameters == null)
            {
                return (null, null);
            }

            ArgumentSyntax argument = await GetArgumentAsync(methodSymbol, parameterIndex, parameters.First()).ConfigureAwait(false);
            if (argument == null)
            {
                return (null, null);
            }

            return (argument, parameters);
        }

        private async Task<ArgumentSyntax> GetArgumentAsync(IMethodSymbol methodSymbol, int parameterIndex, ParameterSyntax parameter)
        {
            Solution solution = document.Project.Solution;
            IEnumerable<ReferencedSymbol> result = await SymbolFinder.FindReferencesAsync(methodSymbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
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

            InvocationExpressionSyntax invocation = result.Single()
                                                          .Locations
                                                          .Cast<Location>()
                                                          .Select(l => l.FindToken().AncestorAndSelf<InvocationExpressionSyntax>())
                                                          .Single();

            string parameterName = parameter.Identifier.ValueText;
            List<ArgumentSyntax> list = new List<ArgumentSyntax>();

            for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                // position based
                ArgumentSyntax argument = invocation.ArgumentList.Arguments[i];
                if (argument.NameColon == null && i == parameterIndex)
                {
                    return argument;
                }

                // named parameter
                if (argument.NameColon != null)
                {
                    ArgumentSyntax namedArgument = invocation.ArgumentList
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

        private (int, IEnumerable<ParameterSyntax>) GetParameterInfo(
            SemanticModel semanticModel,
            IMethodSymbol methodSymbol,
            SyntaxToken token)
        {
            int parameterIndex = GetParameterIndex(methodSymbol, token);
            if (parameterIndex < 0)
            {
                return (default, null);
            }

            // find all parameter syntax for the index
            IEnumerable<ParameterSyntax> parameters = methodSymbol.Locations
                                                                  .Select(l => l.FindToken().AncestorAndSelf<BaseMethodDeclarationSyntax>())
                                                                  .Select(n => n.ParameterList.Parameters[parameterIndex]);

            if (parameters.Count() > 1)
            {
                parameters = parameters.Reverse();
            }

            return (parameterIndex, parameters);
        }

        private int GetParameterIndex(IMethodSymbol methodSymbol, SyntaxToken token)
        {
            ArgumentSyntax argument = token.AncestorAndSelf<ArgumentSyntax>();
            if (argument != null)
            {
                // name parameter?
                if (argument.NameColon != null)
                {
                    IParameterSymbol symbol = methodSymbol.Parameters.FirstOrDefault(p => p.Name == argument.NameColon.Name.Identifier.ValueText);
                    if (symbol == null)
                    {
                        // named parameter is used but can't find one?
                        return -1;
                    }

                    return symbol.Ordinal;
                }

                // positional argument
                ArgumentListSyntax list = argument.Parent as ArgumentListSyntax;
                for (int i = 0; i < list.Arguments.Count; i++)
                {
                    ArgumentSyntax arg = list.Arguments[i];

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

            ParameterSyntax parameter = token.AncestorAndSelf<ParameterSyntax>();
            if (parameter != null)
            {
                ParameterListSyntax parameterList = parameter.AncestorAndSelf<ParameterListSyntax>();
                return parameterList.Parameters.IndexOf(parameter);
            }

            return -1;
        }

        private IMethodSymbol GetMethodDefinitionSymbol(SemanticModel semanticModel, SyntaxToken token)
        {
            ArgumentSyntax argument = token.AncestorAndSelf<ArgumentSyntax>();
            if (argument != null)
            {
                InvocationExpressionSyntax invocation = argument.AncestorAndSelf<InvocationExpressionSyntax>();
                if (invocation == null)
                {
                    return null;
                }

                return semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
            }

            ParameterSyntax parameter = token.AncestorAndSelf<ParameterSyntax>();
            if (parameter != null)
            {
                ParameterListSyntax parameterList = parameter.AncestorAndSelf<ParameterListSyntax>();
                if (parameterList == null)
                {
                    // doesn't support lambda
                    return null;
                }

                SyntaxNode definitionNode = parameterList.Parent;
                return semanticModel.GetDeclaredSymbol(definitionNode, cancellationToken) as IMethodSymbol;
            }

            return null;
        }
    }
}
