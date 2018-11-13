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
        internal async Task<Solution> GetSolutionAsync(
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
                if (result.Target.TypeKind == TypeKind.Interface)
                {
                    return GenerateTargetingInterfaceSolution(
                        result, codeGenerationService, targetNodeSyntax,
                        solutionEditor, solution, editorMap, syntaxMap);
                }
                else
                {
                    return GenerateTargetingClassSolution(
                        result, codeGenerationService, targetNodeSyntax,
                        solutionEditor, solution, editorMap, syntaxMap);
                }
            }
            else
            {
                return default;
            }
        }

        internal CodeAction GetCodeActionAsync(
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
            if (result.ChangeTargetAbstract)
            {
                var abstractTarget = GetAbstractTargetNode(targetNodeSyntax, result.Target, solution);
                AddMembersToClass(result, codeGenerationService, abstractTarget, targetNodeSyntax, solution, editorMap);
            }
            else
            {
                AddMembersToClass(result, codeGenerationService, targetNodeSyntax, targetNodeSyntax, solution, editorMap);
            }

            RemoveOriginalMembers(result, solution, editorMap, syntaxMap);
            return solutionEditor.GetChangedSolution();
        }

        private void AddMembersToClass(
            AnalysisResult result,
            ICodeGenerationService codeGenerationService,
            SyntaxNode nodeToReplaceTarget,
            SyntaxNode targetNodeSyntax,
            Solution solution,
            ImmutableDictionary<DocumentId, DocumentEditor> editorMap)
        {
            var abstractMembersSymbol = result.MembersAnalysisResults.Where(analysis => analysis.MakeAbstract).
                Select(analysis => GetAbstractMemberSymbol(analysis.Member));

            var pullUpMembersSymbols = result.MembersAnalysisResults.Where(analysis => !analysis.MakeAbstract).
                Select(selection => selection.Member).Concat(abstractMembersSymbol);

            var options = new CodeGenerationOptions(reuseSyntax: true, generateMethodBodies: false);
            var membersAddedNode = codeGenerationService.AddMembers(nodeToReplaceTarget, pullUpMembersSymbols, options: options);

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
                throw ExceptionUtilities.UnexpectedValue(symbol);
            }
        }

        private Solution GenerateTargetingInterfaceSolution(
            AnalysisResult result,
            ICodeGenerationService codeGenerationService,
            SyntaxNode targetNodeSyntax,
            SolutionEditor solutionEditor,
            Solution solution,
            ImmutableDictionary<DocumentId, DocumentEditor> editorMap,
            ImmutableDictionary<ISymbol, IEnumerable<SyntaxNode>> syntaxMap)
        {
            AddMembersToInterface(result, codeGenerationService, editorMap[solution.GetDocumentId(targetNodeSyntax.SyntaxTree)], targetNodeSyntax);
            ChangedOriginalMembers(result, codeGenerationService, solution, editorMap, syntaxMap);
            return solutionEditor.GetChangedSolution();
        }

        private void ChangedOriginalMembers(
            AnalysisResult result,
            ICodeGenerationService codeGenerationService,
            Solution solution,
            ImmutableDictionary<DocumentId, DocumentEditor> editorMap,
            ImmutableDictionary<ISymbol, IEnumerable<SyntaxNode>> syntaxMap)
        {
            var membersNeedToChange = result.MembersAnalysisResults.
                Where(member => member.ChangeOriginToNonStatic || member.ChangeOriginToPublic).
                Select(analysisResult => analysisResult.Member);
            foreach (var symbol in membersNeedToChange)
            {
                foreach (var syntax in syntaxMap[symbol])
                {
                    ChangeMemberToPublicAndNonStatic(
                        editorMap[solution.GetDocumentId(syntax.SyntaxTree)],
                        codeGenerationService, symbol, syntax);
                }
            }
        }

        private void AddMembersToInterface(
            AnalysisResult result,
            ICodeGenerationService codeGenerationService,
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

            var options = new CodeGenerationOptions(generateMethodBodies: false);
            var targetWithMembersAdded = codeGenerationService.AddMembers(targetNodeSyntax, symbolsToPullUp, options: options);
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
        
        private void RemoveNode(DocumentEditor editor, SyntaxNode node)
        {
            editor.RemoveNode(editor.Generator.GetDeclaration(node));
        }

        private void ChangeMemberToPublicAndNonStatic(
            DocumentEditor editor,
            ICodeGenerationService codeGenerationService,
            ISymbol symbol,
            SyntaxNode memberSyntax)
        {
            var modifiers = DeclarationModifiers.From(symbol).WithIsStatic(false);
            if (symbol is IEventSymbol eventSymbol)
            {
                ChangeEventToPublicAndNonStatic(
                    editor,
                    codeGenerationService,
                    eventSymbol,
                    memberSyntax,
                    modifiers);
            }
            else
            {
                editor.SetAccessibility(memberSyntax, Accessibility.Public);
                editor.SetModifiers(memberSyntax, modifiers);
            }
        }

        private void ChangeEventToPublicAndNonStatic(
            DocumentEditor editor,
            ICodeGenerationService codeGenerationService,
            IEventSymbol eventSymbol,
            SyntaxNode memberSyntax,
            DeclarationModifiers modifiers)
        {
            var declaration = editor.Generator.GetDeclaration(memberSyntax);
            if (declaration.Equals(memberSyntax))
            {
                if ((eventSymbol.AddMethod != null && !eventSymbol.AddMethod.IsImplicitlyDeclared) ||
                    (eventSymbol.RemoveMethod != null && !eventSymbol.RemoveMethod.IsImplicitlyDeclared))
                {
                    // One events with add or remove method
                    editor.SetAccessibility(declaration, Accessibility.Public);
                    editor.SetModifiers(declaration, modifiers);
                    return;
                }
                else
                {
                    // Several events are declared same line
                    var publicAndNonStaticSymbol = CodeGenerationSymbolFactory.CreateEventSymbol(
                        eventSymbol,
                        accessibility: Accessibility.Public,
                        modifiers: modifiers);
                    var options = new CodeGenerationOptions(generateMethodBodies: true);
                    var publicAndNonStaticSyntax = codeGenerationService.CreateEventDeclaration(publicAndNonStaticSymbol, destination: CodeGenerationDestination.ClassType, options: options);
                    // Insert a new declaration and remove the orginal declaration
                    editor.InsertAfter(declaration, publicAndNonStaticSyntax);
                    editor.RemoveNode(memberSyntax);
                }
            }
            else
            {
                editor.SetAccessibility(declaration, Accessibility.Public);
                editor.SetModifiers(declaration, modifiers);
            }
        }
    }
}
