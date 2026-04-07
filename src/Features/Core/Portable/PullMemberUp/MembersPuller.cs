// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;

internal static class MembersPuller
{
    /// <summary>
    /// Annotation used to mark imports that we move over, so that we can remove these imports if they are unnecessary
    /// (and so we don't remove any other unnecessary imports)
    /// </summary>
    private static readonly SyntaxAnnotation s_removableImportAnnotation = new("PullMemberRemovableImport");
    private static readonly SyntaxAnnotation s_destinationNodeAnnotation = new("DestinationNode");

    public static CodeAction TryComputeCodeAction(
        Document document,
        ImmutableArray<ISymbol> selectedMembers,
        INamedTypeSymbol destination)
    {
        var result = PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(destination,
            selectedMembers.SelectAsArray(m => (member: m, makeAbstract: false)));

        if (result.PullUpOperationNeedsToDoExtraChanges ||
            selectedMembers.Any(IsSelectedMemberDeclarationAlreadyInDestination, destination))
        {
            return null;
        }

        var title = result.Destination.Name;
        return SolutionChangeAction.Create(
            title,
            cancellationToken => PullMembersUpAsync(document, result, cancellationToken),
            title);
    }

    public static Task<Solution> PullMembersUpAsync(
        Document document,
        PullMembersUpOptions pullMembersUpOptions,
        CancellationToken cancellationToken)
    {
        return pullMembersUpOptions.Destination.TypeKind switch
        {
            TypeKind.Interface => PullMembersIntoInterfaceAsync(document, pullMembersUpOptions, cancellationToken),
            // We can treat VB modules as a static class
            TypeKind.Class or TypeKind.Module => PullMembersIntoClassAsync(document, pullMembersUpOptions, cancellationToken),
            _ => throw ExceptionUtilities.UnexpectedValue(pullMembersUpOptions.Destination),
        };
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
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var solutionEditor = new SolutionEditor(solution);
        var codeGenerationService = document.Project.Services.GetRequiredService<ICodeGenerationService>();
        var destinationSyntaxNode = codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclaration(
            solution, pullMemberUpOptions.Destination, location: null, cancellationToken);
        var symbolToDeclarationsMap = await InitializeSymbolToDeclarationsMapAsync(pullMemberUpOptions, cancellationToken).ConfigureAwait(false);
        var symbolsToPullUp = pullMemberUpOptions.MemberAnalysisResults.SelectAsArray(GetSymbolsToPullUp);

        var destinationEditor = await solutionEditor.GetDocumentEditorAsync(
            solution.GetDocumentId(destinationSyntaxNode.SyntaxTree),
            cancellationToken).ConfigureAwait(false);

        // Add members to interface
        var context = new CodeGenerationContext(
            generateMethodBodies: false,
            generateMembers: false);

        var info = await destinationEditor.OriginalDocument.GetCodeGenerationInfoAsync(context, cancellationToken).ConfigureAwait(false);
        var destinationWithMembersAdded = info.Service.AddMembers(destinationSyntaxNode, symbolsToPullUp, info, cancellationToken);

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
                            declaration, analysisResult.Member,
                            info, cancellationToken);
                    }
                }
            }
        }

        return solutionEditor.GetChangedSolution();
    }

    private static ISymbol GetSymbolsToPullUp(MemberAnalysisResult analysisResult)
    {
        var member = analysisResult.Member;
        // We don't support generating static interface members, so we need to update to non-static before generating.
        var modifier = DeclarationModifiers.From(member).WithIsStatic(false).WithPartial(false);
        if (member is IPropertySymbol propertySymbol)
        {
            // Property is treated differently since we need to make sure it gives right accessor symbol to ICodeGenerationService,
            // otherwise ICodeGenerationService won't give the expected declaration.
            if (analysisResult.ChangeOriginalToPublic)
            {
                // We are pulling a non-public property, change its getter/setter to public and itself to be public.
                return CodeGenerationSymbolFactory.CreatePropertySymbol(
                    propertySymbol,
                    accessibility: Accessibility.Public,
                    modifiers: modifier,
                    getMethod: MakePublicAccessor(propertySymbol.GetMethod),
                    setMethod: MakePublicAccessor(propertySymbol.SetMethod));
            }
            else
            {
                // We are pulling a public property, filter the non-public getter/setter.
                return CodeGenerationSymbolFactory.CreatePropertySymbol(
                    propertySymbol,
                    modifiers: modifier,
                    getMethod: FilterOutNonPublicAccessor(propertySymbol.GetMethod),
                    setMethod: FilterOutNonPublicAccessor(propertySymbol.SetMethod));
            }
        }
        else if (member is IMethodSymbol methodSymbol)
        {
            return CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, modifiers: modifier);
        }
        else if (member is IEventSymbol eventSymbol)
        {
            return CodeGenerationSymbolFactory.CreateEventSymbol(eventSymbol, modifiers: modifier);
        }
        else if (member is IFieldSymbol fieldSymbol)
        {
            return CodeGenerationSymbolFactory.CreateFieldSymbol(fieldSymbol, modifiers: modifier);
        }

        // ICodeGenerationService will give the right result if it is method or event
        return member;
    }

    private static void ChangeMemberToPublicAndNonStatic(
        ICodeGenerationService codeGenerationService,
        DocumentEditor editor,
        SyntaxNode memberDeclaration,
        ISymbol member,
        CodeGenerationContextInfo info,
        CancellationToken cancellationToken)
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
                modifiers,
                info,
                cancellationToken);
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
        DeclarationModifiers modifiers,
        CodeGenerationContextInfo info,
        CancellationToken cancellationToken)
    {
        var declaration = editor.Generator.GetDeclaration(eventDeclaration);
        var isEventHasExplicitAddOrRemoveMethod =
            (eventSymbol.AddMethod != null && !eventSymbol.AddMethod.IsImplicitlyDeclared) ||
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

            var eventGenerationInfo = info.WithContext(new CodeGenerationContext(generateMethodBodies: false));

            var publicAndNonStaticSyntax = codeGenerationService.CreateEventDeclaration(publicAndNonStaticSymbol, CodeGenerationDestination.ClassType, eventGenerationInfo, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var solutionEditor = new SolutionEditor(solution);
        var codeGenerationService = document.Project.Services.GetRequiredService<ICodeGenerationService>();

        var destinationSyntaxNode = codeGenerationService.FindMostRelevantNameSpaceOrTypeDeclaration(
            solution, result.Destination, location: null, cancellationToken);

        var destinationEditor = await solutionEditor.GetDocumentEditorAsync(
            solution.GetDocumentId(destinationSyntaxNode.SyntaxTree),
            cancellationToken).ConfigureAwait(false);

        var symbolToDeclarations = await InitializeSymbolToDeclarationsMapAsync(result, cancellationToken).ConfigureAwait(false);

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

        var context = new CodeGenerationContext(
            reuseSyntax: true,
            generateMethodBodies: false);

        var options = await destinationEditor.OriginalDocument.GetCleanCodeGenerationOptionsAsync(cancellationToken).ConfigureAwait(false);
        var info = codeGenerationService.GetInfo(context, options.GenerationOptions, destinationEditor.OriginalDocument.Project.ParseOptions);

        var newDestination = codeGenerationService
            .AddMembers(destinationSyntaxNode, pullUpMembersSymbols, info, cancellationToken)
            .WithAdditionalAnnotations(s_destinationNodeAnnotation);
        using var _ = PooledHashSet<SyntaxNode>.GetInstance(out var sourceImports);

        var syntaxFacts = destinationEditor.OriginalDocument.GetRequiredLanguageService<ISyntaxFactsService>();

        // Remove some original members since we are pulling members into class.
        // Note: If the user chooses to make the member abstract, then the original member will be changed to an override,
        // and it will pull an abstract declaration up to the destination.
        // But if the member is abstract itself, it will still be removed.
        foreach (var analysisResult in result.MemberAnalysisResults)
        {
            var resultNamespace = analysisResult.Member.ContainingNamespace;
            if (!resultNamespace.IsGlobalNamespace)
            {
                sourceImports.Add(
                    destinationEditor.Generator.NamespaceImportDeclaration(
                        resultNamespace.ToDisplayString(SymbolDisplayFormats.NameFormat))
                    .WithAdditionalAnnotations(s_removableImportAnnotation));
            }

            foreach (var syntax in symbolToDeclarations[analysisResult.Member])
            {
                var originalMemberEditor = await solutionEditor.GetDocumentEditorAsync(
                    solution.GetDocumentId(syntax.SyntaxTree),
                    cancellationToken).ConfigureAwait(false);

                sourceImports.AddRange(GetImports(syntax, syntaxFacts)
                    .Select(import => import
                        .WithoutLeadingTrivia()
                        .WithTrailingTrivia(originalMemberEditor.Generator.ElasticCarriageReturnLineFeed)
                        .WithAdditionalAnnotations(s_removableImportAnnotation)));

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
        if (!result.Destination.IsAbstract &&
            result.MemberAnalysisResults.Any(static analysis => analysis.Member.IsAbstract || analysis.MakeMemberDeclarationAbstract))
        {
            var modifiers = DeclarationModifiers.From(result.Destination).WithIsAbstract(true);
            newDestination = destinationEditor.Generator.WithModifiers(newDestination, modifiers);
        }

        destinationEditor.ReplaceNode(destinationSyntaxNode, newDestination);

        // add imports by moving all source imports to destination container, then taking out unneccessary
        // imports that we just added (marked by our annotation).
        var addImportsService = destinationEditor.OriginalDocument.GetRequiredLanguageService<IAddImportsService>();

        var destinationTrivia = GetLeadingTriviaBeforeFirstMember(destinationEditor.OriginalRoot, syntaxFacts);

        destinationEditor.ReplaceNode(destinationEditor.OriginalRoot, (root, _) =>
            RemoveLeadingTriviaBeforeFirstMember(root, syntaxFacts));

        destinationEditor.ReplaceNode(destinationEditor.OriginalRoot, (node, generator) => addImportsService.AddImports(
            destinationEditor.SemanticModel,
            node,
            node.GetAnnotatedNodes(s_destinationNodeAnnotation).FirstOrDefault(),
            sourceImports,
            generator,
            options.CleanupOptions.AddImportOptions,
            cancellationToken));

        var removeImportsService = destinationEditor.OriginalDocument.GetRequiredLanguageService<IRemoveUnnecessaryImportsService>();
        var destinationDocument = await removeImportsService.RemoveUnnecessaryImportsAsync(
            destinationEditor.GetChangedDocument(),
            node => node.HasAnnotation(s_removableImportAnnotation),
            cancellationToken).ConfigureAwait(false);

        // Format whitespace trivia within the import statements we pull up
        destinationDocument = await Formatter.FormatAsync(destinationDocument, s_removableImportAnnotation, options.CleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);

        var destinationRoot = AddLeadingTriviaBeforeFirstMember(
            await destinationDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false),
            syntaxFacts,
            destinationTrivia);

        destinationEditor.ReplaceNode(destinationEditor.OriginalRoot, destinationRoot);

        return solutionEditor.GetChangedSolution();
    }

    /// <summary>
    /// In the case where we have leading whitespace in front of the first member and there are no imports, adding imports
    /// moves that trivia to above the import (and sometimes removes it entirely if the import is later removed). 
    /// So, we want to cache the trivia before, delete it, then add it back in after the imports are added.
    /// </summary>
    private static SyntaxTriviaList GetLeadingTriviaBeforeFirstMember(SyntaxNode root, ISyntaxFactsService syntaxFacts)
    {
        var members = syntaxFacts.GetMembersOfCompilationUnit(root);
        // guaranteed to have at least one member, as we need a base class
        var firstMember = members.First();
        return firstMember.GetLeadingTrivia();
    }

    private static SyntaxNode RemoveLeadingTriviaBeforeFirstMember(SyntaxNode root, ISyntaxFactsService syntaxFacts)
    {
        var members = syntaxFacts.GetMembersOfCompilationUnit(root);
        // guaranteed to have at least one member, as we need a base class
        var firstMember = members.First();
        return root.ReplaceNode(firstMember, firstMember.WithoutLeadingTrivia());
    }

    private static SyntaxNode AddLeadingTriviaBeforeFirstMember(SyntaxNode root, ISyntaxFactsService syntaxFacts, SyntaxTriviaList trivia)
    {
        var members = syntaxFacts.GetMembersOfCompilationUnit(root);
        // guaranteed to have at least one member, as we need a base class
        var firstMember = members.First();
        return root.ReplaceNode(firstMember, firstMember.WithLeadingTrivia(trivia));
    }

    /// <summary>
    /// Get all import statements in scope for this syntax by traversing up the tree and searching in containing namespaces and compilation units.
    /// </summary>
    /// <param name="start">The node to start traversing up from</param>
    /// <returns>All the import/using directives found along the traversal</returns>
    private static ImmutableArray<SyntaxNode> GetImports(SyntaxNode start, ISyntaxFactsService syntaxFacts)
    {
        return start.AncestorsAndSelf()
            .Where(node => node is ICompilationUnitSyntax || syntaxFacts.IsBaseNamespaceDeclaration(node))
            .SelectManyAsArray(node => node is ICompilationUnitSyntax
                ? syntaxFacts.GetImportsOfCompilationUnit(node)
                : syntaxFacts.GetImportsOfBaseNamespaceDeclaration(node));
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
        CancellationToken cancellationToken)
    {
        // One member may have multiple syntax nodes (e.g partial method).
        // Create a map from ISymbol to SyntaxNode find them more easily.
        var symbolToDeclarationsBuilder = ImmutableDictionary.CreateBuilder<ISymbol, ImmutableArray<SyntaxNode>>();

        foreach (var memberAnalysisResult in result.MemberAnalysisResults)
        {
            var tasks = memberAnalysisResult.Member.DeclaringSyntaxReferences.SelectAsArray(@ref => @ref.GetSyntaxAsync(cancellationToken));
            var allSyntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
            symbolToDeclarationsBuilder.Add(memberAnalysisResult.Member, [.. allSyntaxes]);
        }

        return symbolToDeclarationsBuilder.ToImmutableDictionary();
    }

    /// <summary>
    ///  This method is used to check whether the selected member overrides the member in destination.
    ///  It just checks the members directly declared in the destination.
    /// </summary>
    private static bool IsSelectedMemberDeclarationAlreadyInDestination(ISymbol selectedMember, INamedTypeSymbol destination)
    {
        return destination.TypeKind == TypeKind.Interface
            ? IsSelectedMemberDeclarationAlreadyInDestinationInterface(selectedMember, destination)
            : IsSelectedMemberDeclarationAlreadyInDestinationClass(selectedMember, destination);
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
            for (var symbol = selectedMember; symbol != null; symbol = symbol.GetOverriddenMember())
                overrideMembersSet.Add(symbol);

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
