// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
using EditorMap = System.Collections.Immutable.ImmutableDictionary<Microsoft.CodeAnalysis.SyntaxTree, Microsoft.CodeAnalysis.Editing.DocumentEditor>;
using SyntaxMap = System.Collections.Immutable.ImmutableDictionary<Microsoft.CodeAnalysis.ISymbol, Microsoft.CodeAnalysis.SyntaxNode[]>;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal abstract class AbstractMemberPullerWithQuickAction
    {
        /// <summary>
        ///  This method is used to check whether the selected member overrides the member in destination.
        ///  It just checks the members directly declared in the destination.
        /// </summary>
        protected abstract bool IsSelectedMemberDeclarationAlreadyInDestination(INamedTypeSymbol destination, ISymbol symbol);

        internal CodeAction TryComputeRefactoring(
            Document document,
            ISymbol selectedMember,
            INamedTypeSymbol destinationType)
        {
            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(destinationType, ImmutableArray.Create(selectedMember));
            if (result.PullUpOperationCausesError ||
                IsSelectedMemberDeclarationAlreadyInDestination(destinationType, selectedMember))
            {
                return default;
            }

            return TryGetCodeAction(result, document);
        }

        internal CodeAction TryGetCodeAction(
            PullMembersUpAnalysisResult result,
            Document contextDocument)
        {
            if (result.Destination.TypeKind == TypeKind.Interface)
            {
                return new DocumentChangeAction(
                    string.Format(FeaturesResources.Add_to_0, result.Destination),
                    cancellationToken => PullMembersIntoInterfaceAsync(result, contextDocument, cancellationToken));
            }
            else if (result.Destination.TypeKind == TypeKind.Class)
            {
                return new SolutionChangeAction(
                    string.Format(FeaturesResources.Add_to_0, result.Destination),
                    cancellationToken => PullMembersUpAsync(result, contextDocument, cancellationToken));
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(result.Destination);
            }
        }

        private async Task<Solution> PullMembersUpAsync(
            PullMembersUpAnalysisResult result,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var solution = contextDocument.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var destinationNodeSyntax = await codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(
                contextDocument.Project.Solution, result.Destination, default, cancellationToken).ConfigureAwait(false);
            var (editorMap, syntaxMap) = await InitializeEditorMapsAndSyntaxMapAsync(result, destinationNodeSyntax, solutionEditor, solution, cancellationToken).ConfigureAwait(false);
            return PullMembersIntoClass(
                result, codeGenerationService, destinationNodeSyntax,
                solutionEditor, solution, editorMap, syntaxMap);
        }

        private IMethodSymbol FilterGetterOrSetter(IMethodSymbol getterOrSetter)
        {
            return getterOrSetter?.DeclaredAccessibility == Accessibility.Public ? getterOrSetter : null;
        }

        private async Task<Document> PullMembersIntoInterfaceAsync(
            PullMembersUpAnalysisResult result,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var symbolsToPullUp = result.MemberAnalysisResults.
                SelectAsArray(analysisResult =>
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
            var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
            return await codeGenerationService.AddMembersAsync(
                contextDocument.Project.Solution, result.Destination, symbolsToPullUp, options: options, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private Solution PullMembersIntoClass(
            PullMembersUpAnalysisResult result,
            ICodeGenerationService codeGenerationService,
            SyntaxNode destinationSyntaxNode,
            SolutionEditor solutionEditor,
            Solution solution,
            ImmutableDictionary<SyntaxTree, DocumentEditor> editorMap,
            ImmutableDictionary<ISymbol, SyntaxNode[]> syntaxMap)
        {
            // Add members to destination
            var pullUpMembersSymbols = result.MemberAnalysisResults.SelectAsArray(memberResult => memberResult.Member);
            var options = new CodeGenerationOptions(reuseSyntax: true, generateMethodBodies: false);
            var membersAddedNode = codeGenerationService.AddMembers(destinationSyntaxNode, pullUpMembersSymbols, options: options);
            var destinationEditor = editorMap[destinationSyntaxNode.SyntaxTree];
            destinationEditor.ReplaceNode(destinationSyntaxNode, (syntaxNode, generator) => membersAddedNode);

            // Remove the original members since we are pulling members into class
            foreach (var analysisResult in result.MemberAnalysisResults)
            {
                foreach (var syntax in syntaxMap[analysisResult.Member])
                {
                    var originalMemberEditor = editorMap[syntax.SyntaxTree];
                    originalMemberEditor.RemoveNode(originalMemberEditor.Generator.GetDeclaration(syntax));
                }
            }

            return solutionEditor.GetChangedSolution();
        }

        private async Task<(EditorMap editorMap, SyntaxMap syntaxMap)> InitializeEditorMapsAndSyntaxMapAsync(
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
            var syntaxMapBuilder = ImmutableDictionary.CreateBuilder<ISymbol, SyntaxNode[]>();

            foreach (var memberAnalysisResult in result.MemberAnalysisResults)
            {
                var tasks = memberAnalysisResult.Member.DeclaringSyntaxReferences.SelectAsArray(@ref => @ref.GetSyntaxAsync(cancellationToken));
                var allSyntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
                await AddEditorsToEditorMapBuilderAsync(editorMapBuilder, allSyntaxes, solutionEditor, solution).ConfigureAwait(false);
                syntaxMapBuilder.Add(memberAnalysisResult.Member, allSyntaxes);
            }

            await AddEditorsToEditorMapBuilderAsync(
                editorMapBuilder,
                new[] { destinationSyntaxNode },
                solutionEditor,
                solution).ConfigureAwait(false);
            return (editorMapBuilder.ToImmutableDictionary(), syntaxMapBuilder.ToImmutableDictionary());
        }

        private async Task AddEditorsToEditorMapBuilderAsync(
            ImmutableDictionary<SyntaxTree, DocumentEditor>.Builder mapBuilder,
            SyntaxNode[] syntaxNodes,
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
