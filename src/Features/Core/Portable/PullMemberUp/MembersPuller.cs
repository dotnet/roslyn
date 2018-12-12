// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
using EditorMap = System.Collections.Immutable.ImmutableDictionary<Microsoft.CodeAnalysis.SyntaxTree, Microsoft.CodeAnalysis.Editing.DocumentEditor>;
using SyntaxMap = System.Collections.Immutable.ImmutableDictionary<Microsoft.CodeAnalysis.ISymbol, Microsoft.CodeAnalysis.SyntaxNode[]>;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal class MembersPuller
    {
        internal static readonly MembersPuller Instance = new MembersPuller();

        private MembersPuller()
        {
        }

        /// <summary>
        /// Return the CodeAction to pull selectedMember up to destinationType. If the pulling will cause error,
        /// it will return null.
        /// </summary>
        internal CodeAction TryComputeCodeAction(
            Document document,
            ISymbol selectedMember,
            INamedTypeSymbol destinationType)
        {
            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(destinationType, ImmutableArray.Create((member: selectedMember, makeAbstract: false)));
            if (result.PullUpOperationCausesError ||
                IsSelectedMemberDeclarationAlreadyInDestination(destinationType, selectedMember))
            {
                return default;
            }

            return new SolutionChangeAction(
                string.Format(FeaturesResources.Add_to_0, result.Destination),
                cancellationToken => PullMembersUpAsync(result, document, cancellationToken));
        }

        /// <summary>
        /// Return the changed solution if all changes in result are applied.
        /// </summary>
        /// <param name="result">Contains the members to pull up and all the fix operations</param>>
        internal async Task<Solution> PullMembersUpAsync(
            PullMembersUpAnalysisResult result,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var solution = contextDocument.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var destinationSyntaxNode = await codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(
                contextDocument.Project.Solution, result.Destination, default, cancellationToken).ConfigureAwait(false);
            var (editorMap, syntaxMap) = await InitializeEditorMapsAndSyntaxMapAsync(result, destinationSyntaxNode, solutionEditor, solution, cancellationToken).ConfigureAwait(false);

            if (result.Destination.TypeKind == TypeKind.Interface)
            {
                return PullMembersIntoInterface(
                    result, codeGenerationService, destinationSyntaxNode,
                    solutionEditor, solution, editorMap, syntaxMap, cancellationToken);
            }
            else if (result.Destination.TypeKind == TypeKind.Class)
            {
                return PullMembersIntoClass(
                    result, codeGenerationService, destinationSyntaxNode,
                    solutionEditor, solution, editorMap, syntaxMap);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(result.Destination);
            }
        }

        private IMethodSymbol FilterGetterOrSetter(IMethodSymbol getterOrSetter)
        {
            return getterOrSetter?.DeclaredAccessibility == Accessibility.Public ? getterOrSetter : null;
        }

        private IMethodSymbol CreatePublicGetterAndSetter(IMethodSymbol getterOrSetter, IPropertySymbol containingProperty)
        {
            return getterOrSetter == null
                ? getterOrSetter
                : CodeGenerationSymbolFactory.CreateMethodSymbol(getterOrSetter, accessibility: Accessibility.Public);
        }

        private Solution PullMembersIntoInterface(
            PullMembersUpAnalysisResult result,
            ICodeGenerationService codeGenerationService,
            SyntaxNode destinationSyntaxNode,
            SolutionEditor solutionEditor,
            Solution solution,
            ImmutableDictionary<SyntaxTree, DocumentEditor> editorMap,
            ImmutableDictionary<ISymbol, SyntaxNode[]> syntaxMap,
            CancellationToken cancellationToken)
        {
            var symbolsToPullUp = result.MemberAnalysisResults.
                SelectAsArray(analysisResult =>
                {
                    if (analysisResult.Member is IPropertySymbol propertySymbol)
                    {
                        if (analysisResult.ChangeOriginalToPublic)
                        {
                            // We are pulling a non-public property, change its getter/setter to public and itself to be public.
                            return CodeGenerationSymbolFactory.CreatePropertySymbol(
                                propertySymbol,
                                accessibility: Accessibility.Public,
                                getMethod: CreatePublicGetterAndSetter(propertySymbol.GetMethod, propertySymbol),
                                setMethod: CreatePublicGetterAndSetter(propertySymbol.SetMethod, propertySymbol));
                        }
                        else
                        {
                            // We are pulling a public property, it could have a public getter/setter but 
                            // the other getter/setter is not.
                            // In this scenario, only the public getter/setter
                            // will be add to the destination interface.
                            return CodeGenerationSymbolFactory.CreatePropertySymbol(
                                propertySymbol,
                                getMethod: FilterGetterOrSetter(propertySymbol.GetMethod),
                                setMethod: FilterGetterOrSetter(propertySymbol.SetMethod));
                        }
                    }
                    else
                    {
                        return analysisResult.Member;
                    }
                });

            // Add members to interface
            var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
            var destinationWithMembersAdded = codeGenerationService.AddMembers(destinationSyntaxNode, symbolsToPullUp, options: options, cancellationToken: cancellationToken);
            var destinationEditor = editorMap[destinationSyntaxNode.SyntaxTree];
            destinationEditor.ReplaceNode(destinationSyntaxNode, (syntaxNode, generator) => destinationWithMembersAdded);

            // Change to original members
            foreach (var analysisResult in result.MemberAnalysisResults)
            {
                foreach (var syntax in syntaxMap[analysisResult.Member])
                {
                    var originalMemberEditor = editorMap[syntax.SyntaxTree];

                    if (analysisResult.Member.ContainingType.TypeKind == TypeKind.Interface)
                    {
                        // If we are pulling members from interface to interface, the original members should be removed.
                        // Also don't need to worry other changes since the member is removed
                        originalMemberEditor.RemoveNode(originalMemberEditor.Generator.GetDeclaration(syntax));
                    }
                    else
                    {
                        if (analysisResult.ChangeOriginalToNonStatic || analysisResult.ChangeOriginalToPublic)
                        {
                            ChangeMemberToPublicAndNonStatic(
                                originalMemberEditor, codeGenerationService,
                                analysisResult.Member, syntax);
                        }
                    }
                }
            }

            return solutionEditor.GetChangedSolution();
        }

        private void ChangeMemberToPublicAndNonStatic(
            DocumentEditor editor,
            ICodeGenerationService codeGenerationService,
            ISymbol member,
            SyntaxNode memberSyntax)
        {
            var modifiers = DeclarationModifiers.From(member).WithIsStatic(false);
            if (member is IEventSymbol eventSymbol)
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
            var pullUpMembersSymbols = result.MemberAnalysisResults.SelectAsArray(
                memberResult =>
                {
                    if (memberResult.MakeDeclarationAtDestinationAbstract)
                    {
                        // Change the member to abstract if user choose to make them abstract
                        return GetAbstractMemberSymbol(memberResult.Member);
                    }
                    else
                    {
                        return memberResult.Member;
                    }
                });
            var options = new CodeGenerationOptions(reuseSyntax: true, generateMethodBodies: false);
            var newDestination = codeGenerationService.AddMembers(destinationSyntaxNode, pullUpMembersSymbols, options: options);

            // Remove some original members since we are pulling members into class.
            // Note: If user chooses to make the member abstract, then the original member won't be touched,
            // It will just pull abstract declaration up to destination.
            // But if the member is abstract itself, it will still be removed.
            foreach (var analysisResult in result.MemberAnalysisResults)
            {
                if (!analysisResult.MakeDeclarationAtDestinationAbstract)
                {
                    foreach (var syntax in syntaxMap[analysisResult.Member])
                    {
                        var originalMemberEditor = editorMap[syntax.SyntaxTree];
                        originalMemberEditor.RemoveNode(originalMemberEditor.Generator.GetDeclaration(syntax));
                    }
                }
            }

            // Change the destination to abstract class if needed.
            var destinationEditor = editorMap[destinationSyntaxNode.SyntaxTree];
            if (!result.Destination.IsAbstract &&
                result.MemberAnalysisResults.Any(analysis => analysis.Member.IsAbstract || analysis.MakeDeclarationAtDestinationAbstract))
            {
                var modifiers = DeclarationModifiers.From(result.Destination).WithIsAbstract(true);
                newDestination = destinationEditor.Generator.WithModifiers(newDestination, modifiers);
            }

            destinationEditor.ReplaceNode(destinationSyntaxNode, (syntaxNode, generator) => newDestination);
            return solutionEditor.GetChangedSolution();
        }

        private ISymbol GetAbstractMemberSymbol(ISymbol member)
        {
            if (member.IsAbstract)
            {
                return member;
            }

            var modifier = DeclarationModifiers.From(member).WithIsAbstract(true);
            if (member is IMethodSymbol methodSymbol)
            {
                return CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, modifiers: modifier);
            }
            else if (member is IPropertySymbol propertySymbol)
            {
                return CodeGenerationSymbolFactory.CreatePropertySymbol(propertySymbol, modifiers: modifier);
            }
            else if (member is IEventSymbol eventSymbol)
            {
                return CodeGenerationSymbolFactory.CreateEventSymbol(eventSymbol, modifiers: modifier);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(member);
            }
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

        /// <summary>
        ///  This method is used to check whether the selected member overrides the member in destination.
        ///  It just checks the members directly declared in the destination.
        /// </summary>
        private bool IsSelectedMemberDeclarationAlreadyInDestination(INamedTypeSymbol destination, ISymbol symbol)
        {
            if (destination.TypeKind == TypeKind.Interface)
            {
                return IsSelectedMemberDeclarationAlreadyInDestinationInterface(destination, symbol);
            }
            else
            {
                return IsSelectedMemberDeclarationAlreadyInDestinationClass(destination, symbol);
            }
        }

        private bool IsSelectedMemberDeclarationAlreadyInDestinationClass(INamedTypeSymbol destination, ISymbol selectedMember)
        {
            if (selectedMember is IFieldSymbol fieldSymbol)
            {
                // If there is a member with same name in destination, pull the selected field will cause error,
                // so don't provide refactoring under this scenario
                return destination.GetMembers(fieldSymbol.Name).Any();
            }
            else
            {
                var overrideMembersSet = new HashSet<ISymbol>();
                for (var symbol = selectedMember; symbol != null; symbol = symbol.OverriddenMember())
                {
                    overrideMembersSet.Add(symbol);
                }
                
                // Since the destination and selectedMember may belong different language, so use SymbolEquivalenceComparer as comparer
                return overrideMembersSet.Intersect(destination.GetMembers(), SymbolEquivalenceComparer.Instance).Any();
            }
        }

        private bool IsSelectedMemberDeclarationAlreadyInDestinationInterface(
            INamedTypeSymbol destination,
            ISymbol selectedNode)
        {
            foreach (var interfaceMember in destination.GetMembers())
            {
                var implementationOfMember = selectedNode.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
                if (SymbolEquivalenceComparer.Instance.Equals(selectedNode, implementationOfMember?.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
