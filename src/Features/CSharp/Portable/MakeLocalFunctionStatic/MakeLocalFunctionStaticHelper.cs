// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    internal static class MakeLocalFunctionStaticHelper
    {
        public static bool IsStaticLocalFunctionSupported(SyntaxTree tree)
            => tree.Options is CSharpParseOptions csharpOption && csharpOption.LanguageVersion >= LanguageVersion.CSharp8;

        public static bool TryGetCaputuredSymbols(LocalFunctionStatementSyntax localFunction, SemanticModel semanticModel, out ImmutableArray<ISymbol> captures)
        {
            var dataFlow = semanticModel.AnalyzeDataFlow(localFunction);
            captures = dataFlow.CapturedInside;

            return dataFlow.Succeeded;
        }

        public static async Task<Document> MakeLocalFunctionStaticAsync(
            LocalFunctionStatementSyntax localFunction,
            ImmutableArray<ISymbol> captures,
            SemanticModel semanticModel,
            Document document,
            CancellationToken cancellationToken)
        {
            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;

            var localFunctionSymbol = semanticModel.GetDeclaredSymbol(localFunction, cancellationToken);
            var parameters = CreateParameterSymbols(captures);

            // Finds all the call sites of the local function
            var referencedSymbols = await SymbolFinder.FindReferencesAsync(
                localFunctionSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);

            // Now we need to find all the refereces to the local function that we might need to fix.
            var shouldWarn = false;
            var invocationReferences = new List<InvocationExpressionSyntax>();

            foreach (var referencedSymbol in referencedSymbols)
            {
                foreach (var location in referencedSymbol.Locations)
                {
                    // Since this is a local function, all reference must be in the same tree.
                    var referenceNode = root.FindNode(location.Location.SourceSpan);
                    if (!(referenceNode is IdentifierNameSyntax identifierNode))
                    {
                        continue;
                    }

                    if (identifierNode.Parent is InvocationExpressionSyntax invocation)
                    {
                        invocationReferences.Add(invocation);
                    }
                    else
                    {
                        // We won't be able to fix non-invocation references, 
                        // e.g. creating a delegate. 
                        shouldWarn = true;
                    }
                }
            }

            var nodeToTrack = new List<SyntaxNode>(invocationReferences) { localFunction };
            root = root.TrackNodes(nodeToTrack);

            // Fix all invocations by passing in additional arguments.
            foreach (var invocation in invocationReferences.OrderByDescending(n => n.Span.Start))
            {
                var currentInvocation = root.GetCurrentNode(invocation);

                var seenNamedArgument = currentInvocation.ArgumentList.Arguments.Any(a => a.NameColon != null);
                var seenDefaultArgumentValue = currentInvocation.ArgumentList.Arguments.Count < localFunction.ParameterList.Parameters.Count;

                var newArguments = parameters.Select(
                    p => CSharpSyntaxGenerator.Instance.Argument(
                        seenNamedArgument || seenDefaultArgumentValue ? p.Name : null,
                        p.RefKind,
                        p.Name.ToIdentifierName()) as ArgumentSyntax);

                var newArgList = currentInvocation.ArgumentList.WithArguments(currentInvocation.ArgumentList.Arguments.AddRange(newArguments));
                var newInvocation = currentInvocation.WithArgumentList(newArgList);

                root = root.ReplaceNode(currentInvocation, newInvocation);
            }

            // Updates the local function declaration with variables passed in as parameters
            localFunction = root.GetCurrentNode(localFunction);
            var localFunctionWithNewParameters = CodeGenerator.AddParameterDeclarations(
                localFunction,
                parameters,
                document.Project.Solution.Workspace);

            if (shouldWarn)
            {
                var annotation = WarningAnnotation.Create(CSharpFeaturesResources.Warning_colon_Adding_parameters_to_local_function_declaration_may_produce_invalid_code);
                localFunctionWithNewParameters = localFunctionWithNewParameters.WithAdditionalAnnotations(annotation);
            }

            var fixedLocalFunction = AddStaticModifier(localFunctionWithNewParameters, CSharpSyntaxGenerator.Instance);
            var fixedRoot = root.ReplaceNode(localFunction, fixedLocalFunction);

            return document.WithSyntaxRoot(fixedRoot);
        }

        public static SyntaxNode AddStaticModifier(SyntaxNode localFunction, SyntaxGenerator generator)
            => generator.WithModifiers(
                localFunction,
                generator.GetModifiers(localFunction).WithIsStatic(true));

        /// <summary>
        /// Creates a new parameter symbol for each captured variables.
        /// </summary>
        private static ImmutableArray<IParameterSymbol> CreateParameterSymbols(ImmutableArray<ISymbol> captures)
        {
            var parameters = ArrayBuilder<IParameterSymbol>.GetInstance(captures.Length);

            foreach (var symbol in captures)
            {
                parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default,
                    refKind: RefKind.None,
                    isParams: false,
                    type: symbol.GetSymbolType(),
                    name: symbol.Name.ToCamelCase()));
            }

            return parameters.ToImmutableAndFree();
        }
    }
}
