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
        public CodeAction TryComputeCodeAction(
            Document document,
            ISymbol selectedMember,
            INamedTypeSymbol destination)
        {
            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(destination, ImmutableArray.Create((member: selectedMember, makeAbstract: false)));
            if (result.PullUpOperationCausesError ||
                IsSelectedMemberDeclarationAlreadyInDestination(selectedMember, destination))
            {
                return default;
            }

            return new SolutionChangeAction(
                string.Format(FeaturesResources.Add_to_0, result.Destination),
                cancellationToken => PullMembersUpAsync(document, result, cancellationToken));
        }

        /// <summary>
        /// Return the changed solution if all changes in result are applied.
        /// </summary>
        /// <param name="result">Contains the members to pull up and all the fix operations</param>>
        internal async Task<Solution> PullMembersUpAsync(
            Document document,
            PullMembersUpAnalysisResult result,
            CancellationToken cancellationToken)
        {
            if (result.Destination.TypeKind == TypeKind.Interface)
            {
                return await PullMembersIntoInterfaceAsync(document, result, cancellationToken).ConfigureAwait(false);
            }
            else if (result.Destination.TypeKind == TypeKind.Class)
            {
                return await PullMembersIntoClassAsync(document, result, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(result.Destination);
            }
        }

        private IMethodSymbol FilterGetterOrSetter(IMethodSymbol getterOrSetter)
        {
            // Filter the non-public getter/setter since the propery is going to pull up to an interface
            return getterOrSetter?.DeclaredAccessibility == Accessibility.Public ? getterOrSetter : null;
        }

        private IMethodSymbol CreatePublicGetterAndSetter(IPropertySymbol containingProperty, IMethodSymbol getterOrSetter)
        {
            // Create a public getter/setter since user is trying to pull a non-public property to an interface.
            // If getterOrSetter is null, it means this property doesn't have a getter/setter, so just don't generate it.
            return getterOrSetter == null
                ? getterOrSetter
                : CodeGenerationSymbolFactory.CreateMethodSymbol(getterOrSetter, accessibility: Accessibility.Public);
        }

        private async Task<Solution> PullMembersIntoInterfaceAsync(
            Document document,
            PullMembersUpAnalysisResult result,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var codeGenerationService = document.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var destinationSyntaxNode = await codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(
                document.Project.Solution, result.Destination, default, cancellationToken).ConfigureAwait(false);
            var (editorMap, syntaxMap) = await InitializeEditorMapsAndSyntaxMapAsync(result, solution, solutionEditor, destinationSyntaxNode, cancellationToken).ConfigureAwait(false);
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
                                getMethod: CreatePublicGetterAndSetter(propertySymbol, propertySymbol.GetMethod),
                                setMethod: CreatePublicGetterAndSetter(propertySymbol, propertySymbol.SetMethod));
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

            // Change original members
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
                                codeGenerationService, originalMemberEditor,
                                syntax, analysisResult.Member);
                        }
                    }
                }
            }

            return solutionEditor.GetChangedSolution();
        }

        private void ChangeMemberToPublicAndNonStatic(
            ICodeGenerationService codeGenerationService,
            DocumentEditor editor,
            SyntaxNode memberSyntax,
            ISymbol member)
        {
            var modifiers = DeclarationModifiers.From(member).WithIsStatic(false);
            if (member is IEventSymbol eventSymbol)
            {
                ChangeEventToPublicAndNonStatic(
                    codeGenerationService,
                    editor,
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
            ICodeGenerationService codeGenerationService,
            DocumentEditor editor,
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

        private async Task<Solution> PullMembersIntoClassAsync(
            Document document,
            PullMembersUpAnalysisResult result,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var codeGenerationService = document.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var destinationSyntaxNode = await codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(
                document.Project.Solution, result.Destination, default, cancellationToken).ConfigureAwait(false);
            var (editorMap, syntaxMap) = await InitializeEditorMapsAndSyntaxMapAsync(result, solution, solutionEditor, destinationSyntaxNode, cancellationToken).ConfigureAwait(false);
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
            Solution solution,
            SolutionEditor solutionEditor,
            SyntaxNode destinationSyntaxNode,
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
                await AddEditorsToEditorMapBuilderAsync(solution, solutionEditor, editorMapBuilder, allSyntaxes).ConfigureAwait(false);
                syntaxMapBuilder.Add(memberAnalysisResult.Member, allSyntaxes);
            }

            await AddEditorsToEditorMapBuilderAsync(
                solution,
                solutionEditor,
                editorMapBuilder,
                new[] { destinationSyntaxNode }).ConfigureAwait(false);
            return (editorMapBuilder.ToImmutableDictionary(), syntaxMapBuilder.ToImmutableDictionary());
        }

        private async Task AddEditorsToEditorMapBuilderAsync(
            Solution solution,
            SolutionEditor solutionEditor,
            ImmutableDictionary<SyntaxTree, DocumentEditor>.Builder mapBuilder,
            SyntaxNode[] syntaxNodes)
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
        private bool IsSelectedMemberDeclarationAlreadyInDestination(ISymbol selectedMember, INamedTypeSymbol destination)
        {
            if (destination.TypeKind == TypeKind.Interface)
            {
                return IsSelectedMemberDeclarationAlreadyInDestinationInterface(selectedMember, destination);
            }
            else
            {
                return IsSelectedMemberDeclarationAlreadyInDestinationClass(selectedMember, destination);
            }
        }

        private bool IsSelectedMemberDeclarationAlreadyInDestinationClass(ISymbol selectedMember, INamedTypeSymbol destination)
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
            ISymbol selectedMember, INamedTypeSymbol destination)
        {
            foreach (var interfaceMember in destination.GetMembers())
            {
                var implementationOfMember = selectedMember.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
                if (SymbolEquivalenceComparer.Instance.Equals(selectedMember, implementationOfMember?.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
