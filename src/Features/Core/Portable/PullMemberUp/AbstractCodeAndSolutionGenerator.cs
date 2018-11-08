// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.PullMemberUp
{
    internal abstract class AbstractCodeAndSolutionGenerator : IPullMemberUpActionAndSolutionGenerator
    {
        private Dictionary<DocumentId, DocumentEditor> EditorMap { get; set; }

        private Dictionary<ISymbol, List<SyntaxNode>> SyntaxMap { get; set; }

        private ICodeGenerationService CodeGenerationService { get; set; }

        public async Task<Solution> GetSolutionAsync(
            AnalysisResult result,
            Document contextDocument,
            CancellationToken cancellationToken = default)
        {
            CodeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var solution = contextDocument.Project.Solution;
            var targetNodeSyntax = await CodeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(solution, result.Target).ConfigureAwait(false);

            if (targetNodeSyntax != null)
            {
                var solutionEditor = new SolutionEditor(solution);
                await InitializeEditorsAndSyntaxes(result, targetNodeSyntax, solutionEditor, solution, cancellationToken).ConfigureAwait(false);
                if (result.Target.TypeKind == TypeKind.Interface)
                {
                    return GenerateTargetingInterfaceSolution(result, targetNodeSyntax, solutionEditor, solution);
                }
                else if (result.Target.TypeKind == TypeKind.Class)
                {
                    return GenerateTargetingClassSolution(result, targetNodeSyntax, solutionEditor, solution);
                }
                else
                {
                    throw new ArgumentException($"{nameof(result.Target)} should be interface or class");
                }
            }
            else
            {
                return default;
            }
        }

        public CodeAction GetCodeActionAsync(
            AnalysisResult result,
            Document contextDocument,
            string title)
        {
            if (!result.IsValid)
            {
                return default;
            }

            CodeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            if (result.Target.TypeKind == TypeKind.Interface)
            {
                return CreateAddMembersToInterfaceAction(result, contextDocument, title);
            }
            else if (result.Target.TypeKind == TypeKind.Class)
            {
                return new SolutionChangeAction(
                    title,
                    async _ =>
                    {
                        var changedSolution = await GetSolutionAsync(result, contextDocument).ConfigureAwait(false);
                        return changedSolution;
                    });
            }
            else
            {
                throw new ArgumentException($"{nameof(result.Target)} should be interface or class");
            }
        }

        private CodeAction CreateAddMembersToInterfaceAction(
            AnalysisResult result,
            Document contextDocument,
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
                async cancellationToken => await CodeGenerationService.AddMembersAsync(
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
            SyntaxNode targetNodeSyntax,
            SolutionEditor solutionEditor,
            Solution solution)
        {
            if (result.ChangeTargetAbstract)
            {
                var abstractTarget = GetAbstractTargetNode(targetNodeSyntax, result.Target, solution);
                AddMembersToClass(result, abstractTarget, targetNodeSyntax, solution);
            }
            else
            {
                AddMembersToClass(result, targetNodeSyntax, targetNodeSyntax, solution);
            }

            RemoveOriginalMembers(result, solution);
            return solutionEditor.GetChangedSolution();
        }

        private void AddMembersToClass(
            AnalysisResult result,
            SyntaxNode nodeToReplaceTarget,
            SyntaxNode targetNodeSyntax,
            Solution solution)
        {
            var abstractMembersSymbol = result.MembersAnalysisResults.Where(analysis => analysis.MakeAbstract).
                Select(analysis => GetAbstractMemberSymbol(analysis.Member));

            var pullUpMembersSymbols = result.MembersAnalysisResults.Where(analysis => !analysis.MakeAbstract).
                Select(selection => selection.Member).Concat(abstractMembersSymbol);

            var options = new CodeGenerationOptions(generateMembers: false, generateMethodBodies: false, reuseSyntax: true);
            var membersAddedNode = CodeGenerationService.AddMembers(nodeToReplaceTarget, pullUpMembersSymbols, options: options);

            var editor = EditorMap[solution.GetDocumentId(targetNodeSyntax.SyntaxTree)];
            editor.ReplaceNode(targetNodeSyntax, membersAddedNode);
        }

        private void ChangeTargetToAbstract(
            IEnumerable<SyntaxNode> targetAllSyntaxes,
            INamedTypeSymbol targetSymbol,
            Solution solution)
        {
            var declaration = DeclarationModifiers.From(targetSymbol).WithIsAbstract(true);
            foreach (var syntax in targetAllSyntaxes)
            {
                var editor = EditorMap[solution.GetDocumentId(syntax.SyntaxTree)];
                editor.SetModifiers(syntax, declaration);
            }
        }

        private void RemoveOriginalMembers(AnalysisResult result, Solution solution)
        {
            // If member is marked as make abstract, then don't remove it, just add an abstract declaration to target
            foreach (var analysisResult in result.MembersAnalysisResults.Where(analysisResult => !analysisResult.MakeAbstract))
            {
                foreach (var syntax in SyntaxMap[analysisResult.Member])
                {
                    RemoveNode(EditorMap[solution.GetDocumentId(syntax.SyntaxTree)], syntax, analysisResult.Member);
                }
            }
        }
        
        private async Task InitializeEditorsAndSyntaxes(
            AnalysisResult result,
            SyntaxNode targetSyntaxNode,
            SolutionEditor solutionEditor,
            Solution solution,
            CancellationToken cancellationToken)
        {
            EditorMap = new Dictionary<DocumentId, DocumentEditor>();
            SyntaxMap = new Dictionary<ISymbol, List<SyntaxNode>>();
            var membersNeedToBeChanged = result.MembersAnalysisResults;
            if (result.Target.TypeKind == TypeKind.Interface)
            {
                membersNeedToBeChanged = result.MembersAnalysisResults.
                Where(analysisResult => analysisResult.ChangeOriginToNonStatic || analysisResult.ChangeOriginToPublic);
            }

            foreach (var analysisResult in membersNeedToBeChanged)
            {
                var tasks = analysisResult.Member.DeclaringSyntaxReferences.Select(async @ref => await @ref.GetSyntaxAsync(cancellationToken));
                var allSyntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
                await AddEditorsToDict(allSyntaxes, solutionEditor, solution);
                SyntaxMap.Add(analysisResult.Member, allSyntaxes.ToList());
            }

            if (result.ChangeTargetAbstract)
            {
                var tasks = result.Target.DeclaringSyntaxReferences.Select(async @ref => await @ref.GetSyntaxAsync(cancellationToken));
                var allSyntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
                await AddEditorsToDict(allSyntaxes, solutionEditor, solution);
                SyntaxMap.Add(result.Target, allSyntaxes.ToList());
            }
            else
            {
                await AddEditorsToDict(new SyntaxNode[] { targetSyntaxNode }, solutionEditor, solution);
            }
        }

        private async Task AddEditorsToDict(
            IEnumerable<SyntaxNode> syntaxNodes,
            SolutionEditor solutionEditor,
            Solution solution)
        {
            foreach (var syntax in syntaxNodes)
            {
                if (!EditorMap.ContainsKey(solution.GetDocumentId(syntax.SyntaxTree)))
                {
                    var id = solution.GetDocumentId(syntax.SyntaxTree);
                    var editor = await solutionEditor.GetDocumentEditorAsync(id);
                    EditorMap.Add(solution.GetDocumentId(syntax.SyntaxTree), editor);
                }
            }
        }

        private ISymbol GetAbstractMemberSymbol(ISymbol symbol)
        {
            if (symbol.IsAbstract)
            {
                return symbol;
            }

            var modifier = DeclarationModifiers.From(symbol).WithIsAbstract(true);
            if (symbol is IMethodSymbol methodSymbol)
            {
                return CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, modifiers: modifier);
            }
            else if (symbol is IPropertySymbol propertySymbol)
            {
                return CodeGenerationSymbolFactory.CreatePropertySymbol(propertySymbol, modifiers: modifier);
            }
            else if (symbol is IEventSymbol eventSymbol)
            {
                return CodeGenerationSymbolFactory.CreateEventSymbol(eventSymbol, modifiers: modifier);
            }
            else
            {
                throw new ArgumentException($"{nameof(symbol)} should be method, property, event, indexer");
            }
        }

        private Solution GenerateTargetingInterfaceSolution(
            AnalysisResult result,
            SyntaxNode targetNodeSyntax,
            SolutionEditor solutionEditor,
            Solution solution)
        {
            AddMembersToInterface(result, EditorMap[solution.GetDocumentId(targetNodeSyntax.SyntaxTree)], targetNodeSyntax);
            ChangedOriginalMembers(result, solution);
            return solutionEditor.GetChangedSolution();
        }

        private void ChangedOriginalMembers(AnalysisResult result, Solution solution)
        {
            var membersNeedToChange = result.MembersAnalysisResults.Where(member => member.ChangeOriginToNonStatic || member.ChangeOriginToPublic).Select(analysisResult => analysisResult.Member);
            foreach (var symbol in membersNeedToChange)
            {
                foreach (var syntax in SyntaxMap[symbol])
                {
                    ChangeMemberToPublicAndNonStatic(EditorMap[solution.GetDocumentId(syntax.SyntaxTree)], symbol, syntax, FindContainingTypeNode(syntax, solution.GetDocument(syntax.SyntaxTree)), CodeGenerationService);
                }
            }
        }

        private SyntaxNode FindContainingTypeNode(SyntaxNode node, Document document)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            return generator.GetDeclaration(node, DeclarationKind.Class) ?? generator.GetDeclaration(node, DeclarationKind.Interface);
        }

        private void AddMembersToInterface(
            AnalysisResult result,
            DocumentEditor editor,
            SyntaxNode targetNodeSyntax)
        {
            var symbolsToPullUp = result.MembersAnalysisResults.
                Select(analysisResult =>
                {
                    if (analysisResult.Member is IPropertySymbol propertySymbol)
                    {
                        return CodeGenerationSymbolFactory.CreatePropertySymbol(
                                propertySymbol,
                                accessibility: Accessibility.Public,
                                getMethod: CreatePublicGetterAndSetter(propertySymbol.GetMethod, propertySymbol),
                                setMethod: CreatePublicGetterAndSetter(propertySymbol.SetMethod, propertySymbol));
                    }
                    else
                    {
                        return analysisResult.Member;
                    }
                });

            var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
            var targetWithMembersAdded = CodeGenerationService.AddMembers(targetNodeSyntax, symbolsToPullUp, options: options);
            editor.ReplaceNode(targetNodeSyntax, targetWithMembersAdded);
        }

        private IMethodSymbol CreatePublicGetterAndSetter(IMethodSymbol setterOrGetter, IPropertySymbol containingProperty)
        {
            if (setterOrGetter == null || setterOrGetter.DeclaredAccessibility == Accessibility.Public)
            {
                return setterOrGetter;
            }

            if (containingProperty.DeclaredAccessibility == Accessibility.Public)
            {
                return setterOrGetter.DeclaredAccessibility == Accessibility.Public ? setterOrGetter : null;
            }

            return CodeGenerationSymbolFactory.CreateMethodSymbol(setterOrGetter, accessibility: Accessibility.Public);
        }

        protected abstract void ChangeMemberToPublicAndNonStatic(DocumentEditor editor, ISymbol symbol, SyntaxNode node, SyntaxNode containingTypeNode, ICodeGenerationService codeGenerationService);

        protected abstract void RemoveNode(DocumentEditor editor, SyntaxNode node, ISymbol symbol);
    }
}
