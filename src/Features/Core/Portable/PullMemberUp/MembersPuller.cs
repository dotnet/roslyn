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

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal static class MembersPuller
    {
        /// <summary>
        /// Return the CodeAction to pull <paramref name="selectedMember"/> up to destinationType. If the pulling will cause error, it will return null.
        /// </summary>
        public static CodeAction TryComputeCodeAction(
            Document document,
            ISymbol selectedMember,
            INamedTypeSymbol destination)
        {
            var result = PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(destination, ImmutableArray.Create((member: selectedMember, makeAbstract: false)));
            if (result.PullUpOperationNeedsToDoExtraChanges ||
                IsSelectedMemberDeclarationAlreadyInDestination(selectedMember, destination))
            {
                return null;
            }

            return new SolutionChangeAction(
                string.Format(FeaturesResources.Pull_0_up_to_1, selectedMember.Name, result.Destination.Name),
                cancellationToken => PullMembersUpAsync(document, result, cancellationToken));
        }

        /// <summary>
        /// Return the changed solution if all changes in pullMembersUpOptions are applied.
        /// </summary>
        /// <param name="pullMembersUpOptions">Contains the members to pull up and all the fix operations</param>>
        public static Task<Solution> PullMembersUpAsync(
            Document document,
            PullMembersUpOptions pullMembersUpOptions,
            CancellationToken cancellationToken)
        {
            if (pullMembersUpOptions.Destination.TypeKind == TypeKind.Interface)
            {
                return PullMembersIntoInterfaceAsync(document, pullMembersUpOptions, document.Project.Solution, cancellationToken);
            }
            else if (pullMembersUpOptions.Destination.TypeKind == TypeKind.Class)
            {
                return PullMembersIntoClassAsync(document, pullMembersUpOptions, document.Project.Solution, cancellationToken);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(pullMembersUpOptions.Destination);
            }
        }

        private static IMethodSymbol FilterOutNonPublicAccessor(IMethodSymbol getterOrSetter)
        {
            // We are pulling a public property, it could have a public getter/setter but 
            // the other getter/setter is not.
            // In this scenario, only the public getter/setter
            // will be add to the destination interface.
            return getterOrSetter?.DeclaredAccessibility == Accessibility.Public ? getterOrSetter : null;
        }

        private static IMethodSymbol MakePublicAccessor(IMethodSymbol getterOrSetter)
        {
            // Create a public getter/setter since user is trying to pull a non-public property to an interface.
            // If getterOrSetter is null, it means this property doesn't have a getter/setter, so just don't generate it.
            return getterOrSetter == null
                ? getterOrSetter
                : CodeGenerationSymbolFactory.CreateMethodSymbol(getterOrSetter, accessibility: Accessibility.Public);
        }

        private static async Task<Solution> PullMembersIntoInterfaceAsync(
            Document document,
            PullMembersUpOptions pullMemberUpOptions,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var solutionEditor = new SolutionEditor(solution);
            var codeGenerationService = document.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var destinationSyntaxNode = await codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(
                solution, pullMemberUpOptions.Destination, options: null, cancellationToken).ConfigureAwait(false);
            var symbolToDeclarationsMap = await InitializeSymbolToDeclarationsMapAsync(pullMemberUpOptions, solution, solutionEditor, destinationSyntaxNode, cancellationToken).ConfigureAwait(false);
            var symbolsToPullUp = pullMemberUpOptions.MemberAnalysisResults.
                SelectAsArray(analysisResult => GetSymbolsToPullUp(analysisResult));

            // Add members to interface
            var codeGenerationOptions = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
            var destinationWithMembersAdded = codeGenerationService.AddMembers(destinationSyntaxNode, symbolsToPullUp, options: codeGenerationOptions, cancellationToken: cancellationToken);
            var destinationEditor = await solutionEditor.GetDocumentEditorAsync(
                solution.GetDocumentId(destinationSyntaxNode.SyntaxTree),
                cancellationToken).ConfigureAwait(false);
            destinationEditor.ReplaceNode(destinationSyntaxNode, (syntaxNode, generator) => destinationWithMembersAdded);

            // Change original members
            foreach (var analysisResult in pullMemberUpOptions.MemberAnalysisResults)
            {
                foreach (var declaration in symbolToDeclarationsMap[analysisResult.Member])
                {
                    var originalMemberEditor = await solutionEditor.GetDocumentEditorAsync(
                        solution.GetDocumentId(declaration.SyntaxTree),
                        cancellationToken).ConfigureAwait(false);

                    if (analysisResult.Member.ContainingType.TypeKind == TypeKind.Interface)
                    {
                        // If we are pulling member from interface to interface, the original member should be removed.
                        // Also don't need to worry about other changes since the member is removed
                        originalMemberEditor.RemoveNode(originalMemberEditor.Generator.GetDeclaration(declaration));
                    }
                    else
                    {
                        if (analysisResult.ChangeOriginalToNonStatic || analysisResult.ChangeOriginalToPublic)
                        {
                            ChangeMemberToPublicAndNonStatic(
                                codeGenerationService, originalMemberEditor,
                                declaration, analysisResult.Member);
                        }
                    }
                }
            }

            return solutionEditor.GetChangedSolution();
        }

        private static ISymbol GetSymbolsToPullUp(MemberAnalysisResult analysisResult)
        {
            if (analysisResult.Member is IPropertySymbol propertySymbol)
            {
                // Property is treated differently since we need to make sure it gives right accessor symbol to ICodeGenerationService,
                // otherwise ICodeGenerationService won't give the expected declaration.
                if (analysisResult.ChangeOriginalToPublic)
                {
                    // We are pulling a non-public property, change its getter/setter to public and itself to be public.
                    return CodeGenerationSymbolFactory.CreatePropertySymbol(
                        propertySymbol,
                        accessibility: Accessibility.Public,
                        getMethod: MakePublicAccessor(propertySymbol.GetMethod),
                        setMethod: MakePublicAccessor(propertySymbol.SetMethod));
                }
                else
                {
                    // We are pulling a public property, filter the non-public getter/setter.
                    return CodeGenerationSymbolFactory.CreatePropertySymbol(
                        propertySymbol,
                        getMethod: FilterOutNonPublicAccessor(propertySymbol.GetMethod),
                        setMethod: FilterOutNonPublicAccessor(propertySymbol.SetMethod));
                }
            }
            else
            {
                // ICodeGenerationService will give the right result if it is method or event
                return analysisResult.Member;
            }
        }

        private static void ChangeMemberToPublicAndNonStatic(
            ICodeGenerationService codeGenerationService,
            DocumentEditor editor,
            SyntaxNode memberDeclaration,
            ISymbol member)
        {
            var modifiers = DeclarationModifiers.From(member).WithIsStatic(false);
            // Event is different since several events may be declared in one line.
            if (member is IEventSymbol eventSymbol)
            {
                ChangeEventToPublicAndNonStatic(
                    codeGenerationService,
                    editor,
                    eventSymbol,
                    memberDeclaration,
                    modifiers);
            }
            else
            {
                editor.SetAccessibility(memberDeclaration, Accessibility.Public);
                editor.SetModifiers(memberDeclaration, modifiers);
            }
        }

        private static void ChangeEventToPublicAndNonStatic(
            ICodeGenerationService codeGenerationService,
            DocumentEditor editor,
            IEventSymbol eventSymbol,
            SyntaxNode eventDeclaration,
            DeclarationModifiers modifiers)
        {
            var declaration = editor.Generator.GetDeclaration(eventDeclaration);
            var isEventHasExplicitAddOrRemoveMethod =
                (eventSymbol is { AddMethod: { IsImplicitlyDeclared: false } }) ||
                (eventSymbol.RemoveMethod != null && !eventSymbol.RemoveMethod.IsImplicitlyDeclared);
            // There are three situations here:
            // 1. Single Event.
            // 2. Several events exist in one declaration.
            // 3. Event has add or remove method(user declared).
            // For situation 1, declaration is EventFieldDeclaration, eventDeclaration is variableDeclaration.
            // For situation 2, declaration and eventDeclaration are both EventDeclaration, which are same.
            // For situation 3, it is same as situation 2, but has add or remove method.
            if (declaration.Equals(eventDeclaration) && !isEventHasExplicitAddOrRemoveMethod)
            {
                // Several events are declared in same line
                var publicAndNonStaticSymbol = CodeGenerationSymbolFactory.CreateEventSymbol(
                    eventSymbol,
                    accessibility: Accessibility.Public,
                    modifiers: modifiers);
                var options = new CodeGenerationOptions(generateMethodBodies: false);
                var publicAndNonStaticSyntax = codeGenerationService.CreateEventDeclaration(publicAndNonStaticSymbol, destination: CodeGenerationDestination.ClassType, options: options);
                // Insert a new declaration and remove the original declaration
                editor.InsertAfter(declaration, publicAndNonStaticSyntax);
                editor.RemoveNode(eventDeclaration);
            }
            else
            {
                // Handle both single event and event has add or remove method
                editor.SetAccessibility(declaration, Accessibility.Public);
                editor.SetModifiers(declaration, modifiers);
            }
        }

        private static async Task<Solution> PullMembersIntoClassAsync(
            Document document,
            PullMembersUpOptions result,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var solutionEditor = new SolutionEditor(solution);
            var codeGenerationService = document.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var destinationSyntaxNode = await codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(
                solution, result.Destination, options: null, cancellationToken).ConfigureAwait(false);
            var symbolToDeclarations = await InitializeSymbolToDeclarationsMapAsync(result, solution, solutionEditor, destinationSyntaxNode, cancellationToken).ConfigureAwait(false);
            // Add members to destination
            var pullUpMembersSymbols = result.MemberAnalysisResults.SelectAsArray(
                memberResult =>
                {
                    if (memberResult.MakeMemberDeclarationAbstract && !memberResult.Member.IsKind(SymbolKind.Field))
                    {
                        // Change the member to abstract if user choose to make them abstract
                        return MakeAbstractVersion(memberResult.Member);
                    }
                    else
                    {
                        return memberResult.Member;
                    }
                });
            var options = new CodeGenerationOptions(reuseSyntax: true, generateMethodBodies: false);
            var newDestination = codeGenerationService.AddMembers(destinationSyntaxNode, pullUpMembersSymbols, options: options);

            // Remove some original members since we are pulling members into class.
            // Note: If the user chooses to make the member abstract, then the original member will be changed to an override,
            // and it will pull an abstract declaration up to the destination.
            // But if the member is abstract itself, it will still be removed.
            foreach (var analysisResult in result.MemberAnalysisResults)
            {
                foreach (var syntax in symbolToDeclarations[analysisResult.Member])
                {
                    var originalMemberEditor = await solutionEditor.GetDocumentEditorAsync(
                        solution.GetDocumentId(syntax.SyntaxTree),
                        cancellationToken).ConfigureAwait(false);

                    if (!analysisResult.MakeMemberDeclarationAbstract || analysisResult.Member.IsAbstract)
                    {
                        originalMemberEditor.RemoveNode(originalMemberEditor.Generator.GetDeclaration(syntax));
                    }
                    else
                    {
                        var declarationSyntax = originalMemberEditor.Generator.GetDeclaration(syntax);
                        originalMemberEditor.ReplaceNode(declarationSyntax, (node, generator) => generator.WithModifiers(node, DeclarationModifiers.Override));
                    }
                }
            }

            // Change the destination to abstract class if needed.
            var destinationEditor = await solutionEditor.GetDocumentEditorAsync(
                solution.GetDocumentId(destinationSyntaxNode.SyntaxTree),
                cancellationToken).ConfigureAwait(false);
            if (!result.Destination.IsAbstract &&
                result.MemberAnalysisResults.Any(analysis => analysis.Member.IsAbstract || analysis.MakeMemberDeclarationAbstract))
            {
                var modifiers = DeclarationModifiers.From(result.Destination).WithIsAbstract(true);
                newDestination = destinationEditor.Generator.WithModifiers(newDestination, modifiers);
            }

            destinationEditor.ReplaceNode(destinationSyntaxNode, (syntaxNode, generator) => newDestination);
            return solutionEditor.GetChangedSolution();
        }

        private static ISymbol MakeAbstractVersion(ISymbol member)
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
                return CodeGenerationSymbolFactory.CreatePropertySymbol(propertySymbol, modifiers: modifier, getMethod: propertySymbol.GetMethod, setMethod: propertySymbol.SetMethod);
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

        private static async Task<ImmutableDictionary<ISymbol, ImmutableArray<SyntaxNode>>> InitializeSymbolToDeclarationsMapAsync(
            PullMembersUpOptions result,
            Solution solution,
            SolutionEditor solutionEditor,
            SyntaxNode destinationSyntaxNode,
            CancellationToken cancellationToken)
        {
            // One member may have multiple syntax nodes (e.g partial method).
            // Create a map from ISymbol to SyntaxNode find them more easily.
            var symbolToDeclarationsBuilder = ImmutableDictionary.CreateBuilder<ISymbol, ImmutableArray<SyntaxNode>>();

            foreach (var memberAnalysisResult in result.MemberAnalysisResults)
            {
                var tasks = memberAnalysisResult.Member.DeclaringSyntaxReferences.SelectAsArray(@ref => @ref.GetSyntaxAsync(cancellationToken));
                var allSyntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
                symbolToDeclarationsBuilder.Add(memberAnalysisResult.Member, allSyntaxes.ToImmutableArray());
            }

            return symbolToDeclarationsBuilder.ToImmutableDictionary();
        }

        /// <summary>
        ///  This method is used to check whether the selected member overrides the member in destination.
        ///  It just checks the members directly declared in the destination.
        /// </summary>
        private static bool IsSelectedMemberDeclarationAlreadyInDestination(ISymbol selectedMember, INamedTypeSymbol destination)
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

        private static bool IsSelectedMemberDeclarationAlreadyInDestinationClass(ISymbol selectedMember, INamedTypeSymbol destination)
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

        private static bool IsSelectedMemberDeclarationAlreadyInDestinationInterface(
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
