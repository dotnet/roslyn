// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.PullMemberUp
{
    internal class CodeActionAndSolutionGenerator
    {
        private async Task<Solution> PullMembersUpAsync(
            PullMembersUpAnalysisResult result,
            Document contextDocument,
            SyntaxNode destinationNodeSyntax,
            CancellationToken cancellationToken)
        {
            var solution = contextDocument.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var (editorMap, syntaxMap) = await InitializeEditorMapsAndSyntaxMapAsync(result, destinationNodeSyntax, solutionEditor, solution, cancellationToken).ConfigureAwait(false);
            return PullMembersIntoClass(
                result, codeGenerationService, destinationNodeSyntax,
                solutionEditor, solution, editorMap, syntaxMap);
        }

        internal async Task<CodeAction> TryGetCodeActionAsync(
            PullMembersUpAnalysisResult result,
            Document contextDocument,
            CancellationToken cancellationToken)
        { 
            var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var destinationNodeSyntax = await codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(
                contextDocument.Project.Solution, result.Destination, default, cancellationToken).ConfigureAwait(false);

            if (destinationNodeSyntax == null)
            {
                return null;
            }

            if (result.Destination.TypeKind == TypeKind.Interface)
            {
                return PullMembersIntoInterface(result, contextDocument, codeGenerationService);
            }
            else if (result.Destination.TypeKind == TypeKind.Class)
            {
                return new SolutionChangeAction(
                    string.Format(FeaturesResources.Add_to_0, result.Destination),
                    token => PullMembersUpAsync(result, contextDocument, destinationNodeSyntax, token));
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(result.Destination);
            }
        }

        private CodeAction PullMembersIntoInterface(
            PullMembersUpAnalysisResult result,
            Document contextDocument,
            ICodeGenerationService codeGenerationService)
        {
            return new DocumentChangeAction(
                string.Format(FeaturesResources.Add_to_0, result.Destination),
                cancellationToken =>
                {
                    var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
                    var symbolsToPullUp = result.MembersAnalysisResults.
                        Select(analysisResult =>
                        {
                            if (analysisResult.Member is IPropertySymbol propertySymbol)
                            {
                                // When it is a property, it could have a public getter/setter
                                // but other one is not public. In this scenario, only the public getter/setter
                                // will be add to the destination interface
                                return CodeGenerationSymbolFactory.CreatePropertySymbol(
                                    propertySymbol,
                                    getMethod: FilterGetterOrSetter(propertySymbol.GetMethod),
                                    setMethod: FilterGetterOrSetter(propertySymbol.SetMethod));
                            }
                            else
                            {
                                return analysisResult.Member;
                            }
                        });
                    return codeGenerationService.AddMembersAsync(
                        contextDocument.Project.Solution, result.Destination, symbolsToPullUp, options: options, cancellationToken: cancellationToken);
                });
        }

        private IMethodSymbol FilterGetterOrSetter(IMethodSymbol getterOrSetter)
        {
            return getterOrSetter?.DeclaredAccessibility == Accessibility.Public ? getterOrSetter : null;
        }

        private Solution PullMembersIntoClass(
            PullMembersUpAnalysisResult result,
            ICodeGenerationService codeGenerationService,
            SyntaxNode destinationNodeSyntax,
            SolutionEditor solutionEditor,
            Solution solution,
            ImmutableDictionary<SyntaxTree, DocumentEditor> editorMap,
            ImmutableDictionary<ISymbol, ImmutableArray<SyntaxNode>> syntaxMap)
        {
            // Add members to destination
            var pullUpMembersSymbols = result.MembersAnalysisResults.Select(memberResult => memberResult.Member);
            var options = new CodeGenerationOptions(reuseSyntax: true, generateMethodBodies: false);
            var membersAddedNode = codeGenerationService.AddMembers(destinationNodeSyntax, pullUpMembersSymbols, options: options);
            var destinationEditor = editorMap[destinationNodeSyntax.SyntaxTree];
            destinationEditor.ReplaceNode(destinationNodeSyntax, (syntaxNode, generator) => membersAddedNode);

            // Remove the original members since we are pulling members into class
            foreach (var analysisResult in result.MembersAnalysisResults)
            {
                foreach (var syntax in syntaxMap[analysisResult.Member])
                {
                    var originalMemberEditor = editorMap[syntax.SyntaxTree];
                    originalMemberEditor.RemoveNode(originalMemberEditor.Generator.GetDeclaration(syntax));
                }
            }

            return solutionEditor.GetChangedSolution();
        }

        private async Task<(ImmutableDictionary<SyntaxTree, DocumentEditor> editorMap, ImmutableDictionary<ISymbol, ImmutableArray<SyntaxNode>> syntaxMap)> InitializeEditorMapsAndSyntaxMapAsync(
            PullMembersUpAnalysisResult result,
            SyntaxNode destinationSyntaxNode,
            SolutionEditor solutionEditor,
            Solution solution,
            CancellationToken cancellationToken)
        {
            // Members and destination may come from different documents,
            // EditorMap is used to save and group all the editors will be used.
            var editorMapBuilder = ImmutableDictionary.CreateBuilder<SyntaxTree, DocumentEditor>();
            // One member may have multiple syntaxNodes (e.g partial method).
            // SyntaxMap is used to find the syntaxNodes need to be changed more easily.
            var syntaxMapBuilder = ImmutableDictionary.CreateBuilder<ISymbol, ImmutableArray<SyntaxNode>>();

            foreach (var memberAnalysisResult in result.MembersAnalysisResults)
            {
                var tasks = memberAnalysisResult.Member.DeclaringSyntaxReferences.SelectAsArray(@ref => @ref.GetSyntaxAsync(cancellationToken));
                var allSyntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
                await AddEditorsToEditorMapBuilderAsync(editorMapBuilder, allSyntaxes.ToImmutableArray(), solutionEditor, solution);
                syntaxMapBuilder.Add(memberAnalysisResult.Member, allSyntaxes.ToImmutableArray());
            }

            await AddEditorsToEditorMapBuilderAsync(
                editorMapBuilder,
                ImmutableArray.Create(destinationSyntaxNode),
                solutionEditor,
                solution);
            return (editorMapBuilder.ToImmutableDictionary(), syntaxMapBuilder.ToImmutableDictionary());
        }

        private async Task AddEditorsToEditorMapBuilderAsync(
            ImmutableDictionary<SyntaxTree, DocumentEditor>.Builder mapBuilder,
            ImmutableArray<SyntaxNode> syntaxNodes,
            SolutionEditor solutionEditor,
            Solution solution)
        {
            foreach (var syntax in syntaxNodes)
            {
                if (!mapBuilder.ContainsKey(syntax.SyntaxTree))
                {
                    var editor = await solutionEditor.GetDocumentEditorAsync(solution.GetDocumentId(syntax.SyntaxTree)).ConfigureAwait(false);
                    mapBuilder.Add(syntax.SyntaxTree, editor);
                }
            }
        }
    }
}
