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
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.PullMemberUp
{
    internal class CodeActionAndSolutionGenerator
    {
        private async Task<Solution> GetUpdateSolutionAsync(
            AnalysisResult result,
            Document contextDocument,
            SyntaxNode targetNodeSyntax,
            CancellationToken cancellationToken)
        {
            var solution = contextDocument.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var (editorMap, syntaxMap) = await InitializeEditorMapsAndSyntaxMapAsync(result, targetNodeSyntax, solutionEditor, solution, cancellationToken).ConfigureAwait(false);
            return PullMembersIntoClass(
                result, codeGenerationService, targetNodeSyntax,
                solutionEditor, solution, editorMap, syntaxMap);
        }

        internal async Task<CodeAction> TryGetCodeActionAsync(
            AnalysisResult result,
            Document contextDocument,
            string title)
        { 
            var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var targetNodeSyntax = await codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(
                contextDocument.Project.Solution, result._target).ConfigureAwait(false);

            if (targetNodeSyntax != null)
            {
                if (result._target.TypeKind == TypeKind.Interface)
                {
                    return PullMembersIntoInterface(result, contextDocument, codeGenerationService, title);
                }
                else if (result._target.TypeKind == TypeKind.Class)
                {
                    return new SolutionChangeAction(
                        title,
                        cancellationToken => GetUpdateSolutionAsync(result, contextDocument, targetNodeSyntax, cancellationToken));
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(result._target);
                }
            }
            else
            {
                return null;
            }
        }

        private CodeAction PullMembersIntoInterface(
            AnalysisResult result,
            Document contextDocument,
            ICodeGenerationService codeGenerationService,
            string title)
        {
            var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
            var symbolsToPullUp = result._membersAnalysisResults.
                Select(analysisResult =>
                {
                    if (analysisResult._member is IPropertySymbol propertySymbol)
                    {
                        // When it is a property, it could have a public getter/setter
                        // but other one is not public. In this scenario, only the public getter/setter
                        // will be add the target interface
                        return CodeGenerationSymbolFactory.CreatePropertySymbol(
                                propertySymbol,
                                getMethod: FilterGetterOrSetter(propertySymbol.GetMethod),
                                setMethod: FilterGetterOrSetter(propertySymbol.SetMethod));
                    }
                    else
                    {
                        return analysisResult._member;
                    }
                });

            return new DocumentChangeAction(
                title,
                cancellationToken => codeGenerationService.AddMembersAsync(
                    contextDocument.Project.Solution, result._target, symbolsToPullUp, options: options, cancellationToken: cancellationToken));
        }

        private IMethodSymbol FilterGetterOrSetter(IMethodSymbol getterOrSetter)
        {
            return getterOrSetter?.DeclaredAccessibility == Accessibility.Public ? getterOrSetter : null;
        }

        private Solution PullMembersIntoClass(
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
            var pullUpMembersSymbols = result._membersAnalysisResults.Select(memberResult => memberResult._member);

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
            foreach (var analysisResult in result._membersAnalysisResults)
            {
                foreach (var syntax in syntaxMap[analysisResult._member])
                {
                    RemoveDeclaration(editorMap[solution.GetDocumentId(syntax.SyntaxTree)], syntax);
                }
            }
        }

        private async Task<(ImmutableDictionary<DocumentId, DocumentEditor>, ImmutableDictionary<ISymbol, IEnumerable<SyntaxNode>>)> InitializeEditorMapsAndSyntaxMapAsync(
            AnalysisResult result,
            SyntaxNode targetSyntaxNode,
            SolutionEditor solutionEditor,
            Solution solution,
            CancellationToken cancellationToken)
        {
            // Members and target may come from different documents,
            // So EditorMap is used to save and group all the editors will be used.
            var editorMapBuilder = ImmutableDictionary.CreateBuilder<DocumentId, DocumentEditor>();
            // One member may have multiple syntaxNodes (e.g partial method).
            // SyntaxMap is used to find the syntaxNodes need to be changed more easily.
            var syntaxMapBuilder = ImmutableDictionary.CreateBuilder<ISymbol, IEnumerable<SyntaxNode>>();
            var membersNeedToBeChanged = result._membersAnalysisResults;

            foreach (var memberAnalysisResult in membersNeedToBeChanged)
            {
                var tasks = memberAnalysisResult._member.DeclaringSyntaxReferences.Select(@ref => @ref.GetSyntaxAsync(cancellationToken));
                var allSyntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
                await AddEditorsToEditorMapBuilderAsync(editorMapBuilder, allSyntaxes, solutionEditor, solution);
                syntaxMapBuilder.Add(memberAnalysisResult._member, allSyntaxes.ToImmutableArray());
            }

            await AddEditorsToEditorMapBuilderAsync(editorMapBuilder, new SyntaxNode[] { targetSyntaxNode }, solutionEditor, solution);
            return (editorMapBuilder.ToImmutableDictionary(), syntaxMapBuilder.ToImmutableDictionary());
        }

        private async Task AddEditorsToEditorMapBuilderAsync(
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

        private void RemoveDeclaration(DocumentEditor editor, SyntaxNode node)
        {
            editor.RemoveNode(editor.Generator.GetDeclaration(node));
        }
    }
}
