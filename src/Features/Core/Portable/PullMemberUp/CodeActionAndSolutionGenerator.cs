// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.PullMemberUp
{
    internal class CodeActionAndSolutionGenerator
    {
        private async Task<Solution> GetSolutionAsync(
            AnalysisResult result,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var solution = contextDocument.Project.Solution;
            var targetNodeSyntax = await codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(solution, result.Target).ConfigureAwait(false);

            if (targetNodeSyntax != null)
            {
                var solutionEditor = new SolutionEditor(solution);
                var (editorMap, syntaxMap) = await InitializeEditorMapsAndSyntaxMap(result, targetNodeSyntax, solutionEditor, solution, cancellationToken).ConfigureAwait(false);
                return GenerateTargetingClassSolution(
                    result, codeGenerationService, targetNodeSyntax,
                    solutionEditor, solution, editorMap, syntaxMap);
            }
            else
            {
                return default;
            }
        }

        internal CodeAction GetCodeAction(
            AnalysisResult result,
            Document contextDocument,
            string title)
        {
            if (result.Target.TypeKind == TypeKind.Interface)
            {
                var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
                return CreateAddMembersToInterfaceAction(result, contextDocument, codeGenerationService, title);
            }
            else if (result.Target.TypeKind == TypeKind.Class)
            {
                return new SolutionChangeAction(
                    title,
                    async cancellationToken =>
                    {
                        var changedSolution = await GetSolutionAsync(result, contextDocument, cancellationToken).ConfigureAwait(false);
                        return changedSolution;
                    });
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(result.Target);
            }
        }

        private CodeAction CreateAddMembersToInterfaceAction(
            AnalysisResult result,
            Document contextDocument,
            ICodeGenerationService codeGenerationService,
            string title)
        {
            var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
            var symbolsToPullUp = result.MembersAnalysisResults.
                Select(analysisResult =>
                {
                    if (analysisResult.Member is IPropertySymbol propertySymbol)
                    {
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

            return new DocumentChangeAction(
                title,
                async cancellationToken => await codeGenerationService.AddMembersAsync(
                    contextDocument.Project.Solution, result.Target, symbolsToPullUp, options: options, cancellationToken: cancellationToken));
        }

        private IMethodSymbol FilterGetterOrSetter(IMethodSymbol getterOrSetter)
        {
            return getterOrSetter?.DeclaredAccessibility == Accessibility.Public ? getterOrSetter : null;
        }

        private SyntaxNode GetAbstractTargetNode(SyntaxNode node, ISymbol symbol, Solution solution)
        {
            var generator = SyntaxGenerator.GetGenerator(solution.GetDocument(node.SyntaxTree));
            var modifiers = DeclarationModifiers.From(symbol).WithIsAbstract(true);
            return generator.WithModifiers(node, modifiers);
        }

        private Solution GenerateTargetingClassSolution(
            AnalysisResult result,
            ICodeGenerationService codeGenerationService,
            SyntaxNode targetNodeSyntax,
            SolutionEditor solutionEditor,
            Solution solution,
            ImmutableDictionary<DocumentId, DocumentEditor> editorMap,
            ImmutableDictionary<ISymbol, IEnumerable<SyntaxNode>> syntaxMap)
        {
            AddMembersToClass(result, codeGenerationService, targetNodeSyntax, solution, editorMap);
            RemoveOriginalMembers(result, solution, editorMap, syntaxMap);
            return solutionEditor.GetChangedSolution();
        }

        private void AddMembersToClass(
            AnalysisResult result,
            ICodeGenerationService codeGenerationService,
            SyntaxNode targetNodeSyntax,
            Solution solution,
            ImmutableDictionary<DocumentId, DocumentEditor> editorMap)
        {
            var pullUpMembersSymbols = result.MembersAnalysisResults.Where(analysis => !analysis.MakeAbstract).
                Select(selection => selection.Member);

            var options = new CodeGenerationOptions(reuseSyntax: true, generateMethodBodies: false);
            var membersAddedNode = codeGenerationService.AddMembers(targetNodeSyntax, pullUpMembersSymbols, options: options);

            var editor = editorMap[solution.GetDocumentId(targetNodeSyntax.SyntaxTree)];
            editor.ReplaceNode(targetNodeSyntax, membersAddedNode);
        }

        private void RemoveOriginalMembers(
            AnalysisResult result,
            Solution solution,
            ImmutableDictionary<DocumentId, DocumentEditor> editorMap,
            ImmutableDictionary<ISymbol, IEnumerable<SyntaxNode>> syntaxMap)
        {
            // If member is marked as make abstract, then don't remove it, just add an abstract declaration to target
            foreach (var analysisResult in result.MembersAnalysisResults.Where(analysisResult => !analysisResult.MakeAbstract))
            {
                foreach (var syntax in syntaxMap[analysisResult.Member])
                {
                    RemoveNode(editorMap[solution.GetDocumentId(syntax.SyntaxTree)], syntax);
                }
            }
        }

        private async Task<(ImmutableDictionary<DocumentId, DocumentEditor>, ImmutableDictionary<ISymbol, IEnumerable<SyntaxNode>>)> InitializeEditorMapsAndSyntaxMap(
            AnalysisResult result,
            SyntaxNode targetSyntaxNode,
            SolutionEditor solutionEditor,
            Solution solution,
            CancellationToken cancellationToken)
        {
            // Members and target may come from different documents, and some of them may also share the same document.
            // So EditorMap is used to save and group all the editors will be used.
            var editorMapBuilder = ImmutableDictionary.CreateBuilder<DocumentId, DocumentEditor>();
            // One member may have multiple syntaxNodes (e.g partial method).
            // SyntaxMap is used to find the syntaxNodes need to be changed more easily.
            var syntaxMapBuilder = ImmutableDictionary.CreateBuilder<ISymbol, IEnumerable<SyntaxNode>>();
            var membersNeedToBeChanged = result.MembersAnalysisResults;
            if (result.Target.TypeKind == TypeKind.Interface)
            {
                membersNeedToBeChanged = result.MembersAnalysisResults.
                    Where(analysisResult => analysisResult.ChangeOriginToNonStatic || analysisResult.ChangeOriginToPublic);
            }

            foreach (var memberAnalysisResult in membersNeedToBeChanged)
            {
                var tasks = memberAnalysisResult.Member.DeclaringSyntaxReferences.Select(async @ref => await @ref.GetSyntaxAsync(cancellationToken));
                var allSyntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
                await AddEditorsToEditorMapBuilder(editorMapBuilder, allSyntaxes, solutionEditor, solution);
                syntaxMapBuilder.Add(memberAnalysisResult.Member, allSyntaxes.ToList());
            }

            if (result.ChangeTargetAbstract)
            {
                // When the target is needed to change to abstract, we need the syntax.
                syntaxMapBuilder.Add(result.Target, new SyntaxNode[] { targetSyntaxNode });
                await AddEditorsToEditorMapBuilder(editorMapBuilder, new SyntaxNode[] { targetSyntaxNode }, solutionEditor, solution);
            }
            else
            {
                await AddEditorsToEditorMapBuilder(editorMapBuilder, new SyntaxNode[] { targetSyntaxNode }, solutionEditor, solution);
            }

            return (editorMapBuilder.ToImmutableDictionary(), syntaxMapBuilder.ToImmutableDictionary());
        }

        private async Task AddEditorsToEditorMapBuilder(
            ImmutableDictionary<DocumentId, DocumentEditor>.Builder mapBuilder,
            IEnumerable<SyntaxNode> syntaxNodes,
            SolutionEditor solutionEditor,
            Solution solution)
        {
            foreach (var syntax in syntaxNodes)
            {
                if (!mapBuilder.ContainsKey(solution.GetDocumentId(syntax.SyntaxTree)))
                {
                    var id = solution.GetDocumentId(syntax.SyntaxTree);
                    var editor = await solutionEditor.GetDocumentEditorAsync(id).ConfigureAwait(false);
                    mapBuilder.Add(solution.GetDocumentId(syntax.SyntaxTree), editor);
                }
            }
        }

        private void RemoveNode(DocumentEditor editor, SyntaxNode node)
        {
            editor.RemoveNode(editor.Generator.GetDeclaration(node));
        }
    }
}
