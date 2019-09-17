// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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

        public static bool CanMakeLocalFunctionStatic(ImmutableArray<ISymbol> captures)
            => captures.Length > 0 && !captures.Any(s => s.IsThisParameter());

        public static async Task<Document> MakeLocalFunctionStaticAsync(
            Document document,
            SemanticModel semanticModel,
            LocalFunctionStatementSyntax localFunction,
            ImmutableArray<ISymbol> captures,
            CancellationToken cancellationToken)
        {
            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
            var syntaxEditor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            await MakeLocalFunctionStaticAsync(document, semanticModel, localFunction, captures, syntaxEditor, cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(syntaxEditor.GetChangedRoot());
        }

        public static async Task MakeLocalFunctionStaticAsync(
            Document document,
            SemanticModel semanticModel,
            LocalFunctionStatementSyntax localFunction,
            ImmutableArray<ISymbol> captures,
            SyntaxEditor syntaxEditor,
            CancellationToken cancellationToken)
        {
            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
            var localFunctionSymbol = semanticModel.GetDeclaredSymbol(localFunction, cancellationToken);
            var documentImmutableSet = ImmutableHashSet.Create(document);

            // Finds all the call sites of the local function
            var referencedSymbols = await SymbolFinder.FindReferencesAsync(
                localFunctionSymbol, document.Project.Solution, documentImmutableSet, cancellationToken).ConfigureAwait(false);

            // Now we need to find all the refereces to the local function that we might need to fix.
            var shouldWarn = false;
            using var builderDisposer = ArrayBuilder<InvocationExpressionSyntax>.GetInstance(out var invocations);

            foreach (var referencedSymbol in referencedSymbols)
            {
                foreach (var location in referencedSymbol.Locations)
                {
                    // We limited the search scope to the single document, 
                    // so all reference should be in the same tree.
                    var referenceNode = root.FindNode(location.Location.SourceSpan);
                    if (!(referenceNode is IdentifierNameSyntax identifierNode))
                    {
                        // Unexpected scenario, skip and warn.
                        shouldWarn = true;
                        continue;
                    }

                    if (identifierNode.Parent is InvocationExpressionSyntax invocation)
                    {
                        invocations.Add(invocation);
                    }
                    else
                    {
                        // We won't be able to fix non-invocation references, 
                        // e.g. creating a delegate. 
                        shouldWarn = true;
                    }
                }
            }

            var parameterAndCapturedSymbols = CreateParameterSymbols(captures);

            // Fix all invocations by passing in additional arguments.
            foreach (var invocation in invocations)
            {
                syntaxEditor.ReplaceNode(
                    invocation,
                    (node, generator) =>
                    {
                        var currentInvocation = (InvocationExpressionSyntax)node;
                        var seenNamedArgument = currentInvocation.ArgumentList.Arguments.Any(a => a.NameColon != null);
                        var seenDefaultArgumentValue = currentInvocation.ArgumentList.Arguments.Count < localFunction.ParameterList.Parameters.Count;

                        var newArguments = parameterAndCapturedSymbols.Select(
                            p => generator.Argument(
                                seenNamedArgument || seenDefaultArgumentValue ? p.symbol.Name : null,
                                p.symbol.RefKind,
                                p.capture.Name.ToIdentifierName()) as ArgumentSyntax);

                        var newArgList = currentInvocation.ArgumentList.WithArguments(currentInvocation.ArgumentList.Arguments.AddRange(newArguments));
                        return currentInvocation.WithArgumentList(newArgList);
                    });
            }

            // In case any of the captured variable isn't camel-cased,
            // we need to change the referenced name inside local function to use the new parameter's name.
            foreach (var (parameter, capture) in parameterAndCapturedSymbols)
            {
                if (parameter.Name == capture.Name)
                {
                    continue;
                }

                var referencedCaptureSymbols = await SymbolFinder.FindReferencesAsync(
                    capture, document.Project.Solution, documentImmutableSet, cancellationToken).ConfigureAwait(false);

                foreach (var referencedSymbol in referencedCaptureSymbols)
                {
                    foreach (var location in referencedSymbol.Locations)
                    {
                        var referenceSpan = location.Location.SourceSpan;
                        if (!localFunction.FullSpan.Contains(referenceSpan))
                        {
                            continue;
                        }

                        var referenceNode = root.FindNode(referenceSpan);
                        if (referenceNode is IdentifierNameSyntax identifierNode)
                        {
                            syntaxEditor.ReplaceNode(
                                identifierNode,
                                (node, generator) => generator.IdentifierName(parameter.Name.ToIdentifierToken()).WithTriviaFrom(node));
                        }
                    }
                }
            }

            // Updates the local function declaration with variables passed in as parameters
            syntaxEditor.ReplaceNode(
                localFunction,
                (node, generator) =>
                {
                    var localFunctionWithNewParameters = CodeGenerator.AddParameterDeclarations(
                        node,
                        parameterAndCapturedSymbols.SelectAsArray(p => p.symbol),
                        document.Project.Solution.Workspace);

                    if (shouldWarn)
                    {
                        var annotation = WarningAnnotation.Create(CSharpFeaturesResources.Warning_colon_Adding_parameters_to_local_function_declaration_may_produce_invalid_code);
                        localFunctionWithNewParameters = localFunctionWithNewParameters.WithAdditionalAnnotations(annotation);
                    }

                    return AddStaticModifier(localFunctionWithNewParameters, CSharpSyntaxGenerator.Instance);
                });
        }

        public static SyntaxNode AddStaticModifier(SyntaxNode localFunction, SyntaxGenerator generator)
            => generator.WithModifiers(
                localFunction,
                generator.GetModifiers(localFunction).WithIsStatic(true));

        /// <summary>
        /// Creates a new parameter symbol paired with the original captured symbol for each captured variables.
        /// </summary>
        private static ImmutableArray<(IParameterSymbol symbol, ISymbol capture)> CreateParameterSymbols(ImmutableArray<ISymbol> captures)
        {
            var parameters = ArrayBuilder<(IParameterSymbol, ISymbol)>.GetInstance(captures.Length);

            foreach (var symbol in captures)
            {
                parameters.Add((CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default,
                    refKind: RefKind.None,
                    isParams: false,
                    type: symbol.GetSymbolType(),
                    name: symbol.Name.ToCamelCase()), symbol));
            }

            return parameters.ToImmutableAndFree();
        }
    }
}
