// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives;

using static SyntaxFactory;

/// <summary>
/// Implements a code fix for all misplaced using statements.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MoveMisplacedUsingDirectives), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class MisplacedUsingDirectivesCodeFixProvider() : CodeFixProvider
{
    private static readonly SyntaxAnnotation s_usingPlacementCodeFixAnnotation = new(nameof(s_usingPlacementCodeFixAnnotation));

    /// <summary>
    /// A blanket warning that this codefix may change code so that it does not compile.
    /// </summary>
    private static readonly SyntaxAnnotation s_warningAnnotation = WarningAnnotation.Create(
        CSharpAnalyzersResources.Warning_colon_Moving_using_directives_may_change_code_meaning);

    public override ImmutableArray<string> FixableDiagnosticIds => [IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId];

    public override FixAllProvider GetFixAllProvider()
    {
        // Since we work on an entire document at a time fixing all contained diagnostics, the batch fixer should not have merge conflicts.
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;

        var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var compilationUnit = (CompilationUnitSyntax)syntaxRoot;

        var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        var simplifierOptions = new CSharpSimplifierOptions(configOptions);

        // Read the preferred placement option and verify if it can be applied to this code file. There are cases
        // where we will not be able to fix the diagnostic and the user will need to resolve it manually.
        var (placement, preferPreservation) = DeterminePlacement(compilationUnit, configOptions.GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement));
        if (preferPreservation)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(CodeAction.Create(
                    CSharpAnalyzersResources.Move_misplaced_using_directives,
                    cancellationToken => GetTransformedDocumentAsync(document, compilationUnit, placement, simplifierOptions, cancellationToken),
                    nameof(CSharpAnalyzersResources.Move_misplaced_using_directives)),
                diagnostic);
        }
    }

    internal static async Task<Document> TransformDocumentIfRequiredAsync(
        Document document,
        SimplifierOptions simplifierOptions,
        CodeStyleOption2<AddImportPlacement> importPlacementStyleOption,
        CancellationToken cancellationToken)
    {
        var compilationUnit = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var (placement, preferPreservation) = DeterminePlacement(compilationUnit, importPlacementStyleOption);
        if (preferPreservation)
            return document;

        // We are called from a diagnostic, but also for all new documents, so check if there are any usings at all
        // otherwise there is nothing to do.
        var allUsingDirectives = GetAllUsingDirectives(compilationUnit);
        if (allUsingDirectives.Length == 0)
            return document;

        return await GetTransformedDocumentAsync(
            document, compilationUnit, placement, simplifierOptions, cancellationToken).ConfigureAwait(false);
    }

    private static ImmutableArray<UsingDirectiveSyntax> GetAllUsingDirectives(CompilationUnitSyntax compilationUnit)
    {
        using var _ = ArrayBuilder<UsingDirectiveSyntax>.GetInstance(out var result);

        foreach (var usingDirective in compilationUnit.Usings)
        {
            // ignore global usings in the compilation unit, they cannot be moved.
            if (usingDirective.GlobalKeyword == default)
                result.Add(usingDirective);
        }

        Recurse(compilationUnit.Members);

        return result.ToImmutableAndClear();

        void Recurse(SyntaxList<MemberDeclarationSyntax> members)
        {
            foreach (var member in members)
            {
                if (member is BaseNamespaceDeclarationSyntax namespaceDeclaration)
                {
                    result.AddRange(namespaceDeclaration.Usings);
                    Recurse(namespaceDeclaration.Members);
                }
            }
        }
    }

    private static async Task<Document> GetTransformedDocumentAsync(
        Document document,
        CompilationUnitSyntax compilationUnit,
        AddImportPlacement placement,
        SimplifierOptions simplifierOptions,
        CancellationToken cancellationToken)
    {
        var bannerService = document.GetRequiredLanguageService<IFileBannerFactsService>();

        var allUsingDirectives = GetAllUsingDirectives(compilationUnit);

        // Expand usings so that they can be properly simplified after they are relocated.
        var compilationUnitWithExpandedUsings = await ExpandUsingDirectivesAsync(
            document, compilationUnit, allUsingDirectives, cancellationToken).ConfigureAwait(false);

        // Remove the file header from the compilation unit so that we do not lose it when making changes to usings.
        var (compilationUnitWithoutHeader, fileHeader) = RemoveFileHeader(compilationUnitWithExpandedUsings, bannerService);

        var newCompilationUnit = placement == AddImportPlacement.InsideNamespace
            ? MoveUsingsInsideNamespace(compilationUnitWithoutHeader)
            : MoveUsingsOutsideNamespaces(compilationUnitWithoutHeader, ignoringAliases: placement == AddImportPlacement.OutsideNamespaceIgnoringAliases);

        // Re-attach the header now that using have been moved and LeadingTrivia is no longer being altered.
        var newCompilationUnitWithHeader = AddFileHeader(newCompilationUnit, fileHeader);
        var newDocument = document.WithSyntaxRoot(newCompilationUnitWithHeader);

        // Simplify usings now that they have been moved and are in the proper context.
#if CODE_STYLE
#pragma warning disable RS0030 // Do not used banned APIs (ReduceAsync with SimplifierOptions isn't public)
        return await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, optionSet: null, cancellationToken).ConfigureAwait(false);
#pragma warning restore
#else
        return await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, simplifierOptions, cancellationToken).ConfigureAwait(false);
#endif
    }

    private static async Task<CompilationUnitSyntax> ExpandUsingDirectivesAsync(
        Document document, CompilationUnitSyntax compilationUnit, ImmutableArray<UsingDirectiveSyntax> allUsingDirectives, CancellationToken cancellationToken)
    {
        // Create a map between the original node and the future expanded node.
        var expandUsingDirectiveTasks = allUsingDirectives.ToDictionary(
            usingDirective => (SyntaxNode)usingDirective,
            usingDirective => ExpandUsingDirectiveAsync(document, usingDirective, cancellationToken));

        // Wait for all using directives to be expanded
        await Task.WhenAll(expandUsingDirectiveTasks.Values).ConfigureAwait(false);

        // Replace using directives with their expanded version.
        return compilationUnit.ReplaceNodes(
            expandUsingDirectiveTasks.Keys,
            (node, _) => expandUsingDirectiveTasks[node].Result);
    }

    private static async Task<SyntaxNode> ExpandUsingDirectiveAsync(Document document, UsingDirectiveSyntax usingDirective, CancellationToken cancellationToken)
    {
        var newType = await Simplifier.ExpandAsync(usingDirective.NamespaceOrType, document, cancellationToken: cancellationToken).ConfigureAwait(false);
        return usingDirective.WithNamespaceOrType(newType);
    }

    private static CompilationUnitSyntax MoveUsingsInsideNamespace(CompilationUnitSyntax compilationUnit)
    {
        // Get the compilation unit usings and set them up to format when moved.
        var usingsToAdd = compilationUnit.Usings
            .Where(u => u.GlobalKeyword == default)
            .Select(d => d.WithAdditionalAnnotations(Formatter.Annotation, s_warningAnnotation));

        // Remove usings and fix leading trivia for compilation unit.
        var compilationUnitWithoutUsings = compilationUnit.WithUsings([.. compilationUnit.Usings.Where(u => u.GlobalKeyword != default)]);
        var compilationUnitWithoutBlankLine = compilationUnitWithoutUsings.Usings.Count == 0
            ? RemoveLeadingBlankLinesFromFirstMember(compilationUnitWithoutUsings)
            : compilationUnitWithoutUsings;

        // Fix the leading trivia for the namespace declaration.
        var namespaceDeclaration = (BaseNamespaceDeclarationSyntax)compilationUnitWithoutBlankLine.Members[0];
        var namespaceDeclarationWithBlankLine = EnsureLeadingBlankLineBeforeFirstMember(namespaceDeclaration);

        // Update the namespace declaration with the usings from the compilation unit.
        var newUsings = namespaceDeclarationWithBlankLine.Usings.InsertRange(0, usingsToAdd);
        var namespaceDeclarationWithUsings = namespaceDeclarationWithBlankLine.WithUsings(newUsings);

        // Update the compilation unit with the new namespace declaration 
        return compilationUnitWithoutBlankLine.ReplaceNode(namespaceDeclaration, namespaceDeclarationWithUsings);
    }

    private static CompilationUnitSyntax MoveUsingsOutsideNamespaces(
        CompilationUnitSyntax compilationUnit, bool ignoringAliases)
    {
        var namespaceDeclarations = compilationUnit.Members.OfType<BaseNamespaceDeclarationSyntax>();
        var namespaceDeclarationMap = namespaceDeclarations.ToDictionary(
            namespaceDeclaration => namespaceDeclaration,
            namespaceDeclaration => RemoveUsingsFromNamespace(namespaceDeclaration, ignoringAliases));

        // Replace the namespace declarations in the compilation with the ones without using directives.
        var compilationUnitWithReplacedNamespaces = compilationUnit.ReplaceNodes(
            namespaceDeclarations, (node, _) => namespaceDeclarationMap[node].namespaceWithoutUsings);

        // Get the using directives from the namespaces and set them up to format when moved.
        var usingsToAdd = namespaceDeclarationMap.Values.SelectMany(result => result.usingsFromNamespace)
            .Select(directive => directive.WithAdditionalAnnotations(Formatter.Annotation, s_warningAnnotation));

        var (deduplicatedUsings, orphanedTrivia) = RemoveDuplicateUsings(compilationUnit.Usings, [.. usingsToAdd]);

        // Update the compilation unit with the usings from the namespace declaration.
        var compilationUnitWithUsings = compilationUnitWithReplacedNamespaces.WithUsings([
            .. compilationUnitWithReplacedNamespaces.Usings,
            .. deduplicatedUsings]);

        // Fix the leading trivia for the compilation unit. 
        var compilationUnitWithSeparatorLine = EnsureLeadingBlankLineBeforeFirstMember(compilationUnitWithUsings);

        if (!orphanedTrivia.Any())
        {
            return compilationUnitWithSeparatorLine;
        }

        // Add leading trivia that was orphaned from removing duplicate using directives to the first member in the compilation unit.
        var firstMember = compilationUnitWithSeparatorLine.Members[0];
        return compilationUnitWithSeparatorLine.ReplaceNode(firstMember, firstMember.WithPrependedLeadingTrivia(orphanedTrivia));
    }

    /// <summary>
    /// Moves preprocessor directives (like #if/#endif) that surround using directives along with the usings.
    /// If the first using has an #if directive, finds the corresponding #endif and attaches it to the last using.
    /// </summary>
    private static (ImmutableArray<UsingDirectiveSyntax> adjustedUsings, BaseNamespaceDeclarationSyntax adjustedContainer) MovePreprocessorDirectivesWithUsings(
        ImmutableArray<UsingDirectiveSyntax> usings, BaseNamespaceDeclarationSyntax container)
    {
        if (usings.IsEmpty)
            return (usings, container);

        // Check if the first using has an #if directive in its leading trivia
        var firstUsing = usings[0];
        var leadingTrivia = firstUsing.GetLeadingTrivia();
        var hasIfDirective = leadingTrivia.Any(t => t.IsKind(SyntaxKind.IfDirectiveTrivia));

        if (!hasIfDirective)
            return (usings, container);

        // Find the #endif directive. It should be in the leading trivia of the first member after the usings.
        var members = container.Members;
        if (members.Count == 0)
            return (usings, container);

        var firstMember = members[0];
        var firstMemberLeadingTrivia = firstMember.GetLeadingTrivia();

        // Find the first #endif directive in the member's leading trivia
        var endIfIndex = -1;
        for (int i = 0; i < firstMemberLeadingTrivia.Count; i++)
        {
            if (firstMemberLeadingTrivia[i].IsKind(SyntaxKind.EndIfDirectiveTrivia))
            {
                endIfIndex = i;
                break;
            }
        }

        if (endIfIndex == -1)
            return (usings, container);

        // Extract the #endif and any trivia up to and including the first newline after it
        var endIfTrivia = firstMemberLeadingTrivia[endIfIndex];
        var triviaToMove = new List<SyntaxTrivia> { endIfTrivia };

        // Include the newline after the #endif
        if (endIfIndex + 1 < firstMemberLeadingTrivia.Count &&
            firstMemberLeadingTrivia[endIfIndex + 1].IsKind(SyntaxKind.EndOfLineTrivia))
        {
            triviaToMove.Add(firstMemberLeadingTrivia[endIfIndex + 1]);
        }

        // Remove the #endif and its trailing newline from the first member
        var newFirstMemberLeadingTrivia = firstMemberLeadingTrivia.ToList();
        for (int i = triviaToMove.Count - 1; i >= 0; i--)
        {
            newFirstMemberLeadingTrivia.RemoveAt(endIfIndex);
        }

        var newFirstMember = firstMember.WithLeadingTrivia(newFirstMemberLeadingTrivia);
        var newContainer = container.ReplaceNode(firstMember, newFirstMember);

        // Attach the #endif to the last using's trailing trivia
        var lastUsing = usings[^1];
        var lastUsingTrailingTrivia = lastUsing.GetTrailingTrivia();
        var newLastUsingTrailingTrivia = lastUsingTrailingTrivia.AddRange(triviaToMove);
        var newLastUsing = lastUsing.WithTrailingTrivia(newLastUsingTrailingTrivia);

        // Replace the last using in the array
        var adjustedUsings = usings.SetItem(usings.Length - 1, newLastUsing);

        return (adjustedUsings, newContainer);
    }

    private static (BaseNamespaceDeclarationSyntax namespaceWithoutUsings, ImmutableArray<UsingDirectiveSyntax> usingsFromNamespace) RemoveUsingsFromNamespace(
        BaseNamespaceDeclarationSyntax usingContainer, bool ignoringAliases)
    {
        var namespaceDeclarations = usingContainer.Members.OfType<BaseNamespaceDeclarationSyntax>();
        var namespaceDeclarationMap = namespaceDeclarations.ToDictionary(
            namespaceDeclaration => namespaceDeclaration,
            namespaceDeclaration => RemoveUsingsFromNamespace(namespaceDeclaration, ignoringAliases));

        // Get the using directives from the namespaces.
        var usingsFromNamespaces = namespaceDeclarationMap.Values.SelectMany(result => result.usingsFromNamespace);

        // Get usings from the current container level only
        var localUsings = ignoringAliases
            ? usingContainer.Usings.Where(u => u.Alias is null)
            : usingContainer.Usings;

        // Replace the namespace declarations in the compilation with the ones without using directives.
        var namespaceDeclarationWithReplacedNamespaces = usingContainer.ReplaceNodes(
            namespaceDeclarations, (node, _) => namespaceDeclarationMap[node].namespaceWithoutUsings);

        // Handle preprocessor directives that surround the usings at this container level
        var (adjustedLocalUsings, adjustedContainer) = MovePreprocessorDirectivesWithUsings(
            localUsings.ToImmutableArray(), namespaceDeclarationWithReplacedNamespaces);

        // Combine adjusted local usings with usings from nested namespaces
        var allUsings = adjustedLocalUsings.Concat(usingsFromNamespaces).ToImmutableArray();

        // Remove usings and fix leading trivia for namespace declaration.
        var namespaceDeclarationWithoutUsings = adjustedContainer
            .WithUsings(ignoringAliases
                ? List(adjustedContainer.Usings.Where(u => u.Alias != null))
                : default);

        var namespaceDeclarationWithoutBlankLine = namespaceDeclarationWithoutUsings.Usings.Count == 0
            ? RemoveLeadingBlankLinesFromFirstMember(namespaceDeclarationWithoutUsings)
            : namespaceDeclarationWithoutUsings;

        return (namespaceDeclarationWithoutBlankLine, allUsings);
    }

    private static (IEnumerable<UsingDirectiveSyntax> deduplicatedUsings, IEnumerable<SyntaxTrivia> orphanedTrivia) RemoveDuplicateUsings(
        IEnumerable<UsingDirectiveSyntax> existingUsings,
        ImmutableArray<UsingDirectiveSyntax> usingsToAdd)
    {
        var seenUsings = existingUsings.ToList();

        var deduplicatedUsingsBuilder = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();
        var orphanedTrivia = Enumerable.Empty<SyntaxTrivia>();

        foreach (var usingDirective in usingsToAdd)
        {
            // Check is the node is a duplicate.
            if (seenUsings.Any(seenUsingDirective => seenUsingDirective.IsEquivalentTo(usingDirective, topLevel: false)))
            {
                // If there was trivia from the duplicate node, check if any of the trivia is necessary to keep.
                var leadingTrivia = usingDirective.GetLeadingTrivia();
                if (leadingTrivia.Any(trivia => !trivia.IsWhitespaceOrEndOfLine()))
                {
                    // Capture the meaningful trivia so we can prepend it to the next kept node.
                    orphanedTrivia = orphanedTrivia.Concat(leadingTrivia);
                }
            }
            else
            {
                seenUsings.Add(usingDirective);

                // Add any orphaned trivia to this node.
                deduplicatedUsingsBuilder.Add(usingDirective.WithPrependedLeadingTrivia(orphanedTrivia));
                orphanedTrivia = [];
            }
        }

        return (deduplicatedUsingsBuilder.ToImmutable(), orphanedTrivia);
    }

    private static SyntaxList<MemberDeclarationSyntax> GetMembers(SyntaxNode node)
        => node switch
        {
            CompilationUnitSyntax compilationUnit => compilationUnit.Members,
            BaseNamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Members,
            _ => throw ExceptionUtilities.UnexpectedValue(node)
        };

    private static TSyntaxNode RemoveLeadingBlankLinesFromFirstMember<TSyntaxNode>(TSyntaxNode node) where TSyntaxNode : SyntaxNode
    {
        var members = GetMembers(node);
        if (members is not [var firstMember, ..])
            return node;

        var firstMemberTrivia = firstMember.GetLeadingTrivia();

        // If there is no leading trivia, then return the node as it is.
        if (firstMemberTrivia.Count == 0)
            return node;

        var newTrivia = SplitIntoLines(firstMemberTrivia)
            .SkipWhile(trivia => trivia.All(t => t.IsWhitespaceOrEndOfLine()) && trivia.Last().IsKind(SyntaxKind.EndOfLineTrivia))
            .SelectMany(t => t);

        var newFirstMember = firstMember.WithLeadingTrivia(newTrivia);
        return node.ReplaceNode(firstMember, newFirstMember);
    }

    private static IEnumerable<IEnumerable<SyntaxTrivia>> SplitIntoLines(SyntaxTriviaList triviaList)
    {
        var index = 0;
        for (var i = 0; i < triviaList.Count; i++)
        {
            if (triviaList[i].IsEndOfLine())
            {
                yield return triviaList.TakeRange(index, i);
                index = i + 1;
            }
        }

        if (index < triviaList.Count)
        {
            yield return triviaList.TakeRange(index, triviaList.Count - 1);
        }
    }

    private static TSyntaxNode EnsureLeadingBlankLineBeforeFirstMember<TSyntaxNode>(TSyntaxNode node) where TSyntaxNode : SyntaxNode
    {
        var members = GetMembers(node);
        if (members is not [var firstMember, ..])
            return node;

        var firstMemberTrivia = firstMember.GetLeadingTrivia();

        // If the first member already contains a leading new line then, this will already break up the usings from these members.
        if (firstMemberTrivia is [(kind: SyntaxKind.EndOfLineTrivia), ..])
            return node;

        var newFirstMember = firstMember.WithLeadingTrivia(firstMemberTrivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed));
        return node.ReplaceNode(firstMember, newFirstMember);
    }

    private static (AddImportPlacement placement, bool preferPreservation) DeterminePlacement(CompilationUnitSyntax compilationUnit, CodeStyleOption2<AddImportPlacement> styleOption)
    {
        var placement = styleOption.Value;
        var preferPreservation = styleOption.Notification == NotificationOption2.None;

        if (preferPreservation || placement != AddImportPlacement.InsideNamespace)
            return (placement, preferPreservation);

        // Determine if we can safely apply the InsideNamespace preference.

        // Do not offer a code fix when there are multiple namespaces in the source file. When there are
        // nested namespaces it is not clear if inner usings can be moved outwards without causing 
        // collisions. Also, when moving usings inwards it is complex to determine which namespaces they
        // should be moved into.

        // Only move using declarations inside the namespace when
        // - There are no global attributes
        // - There are no type definitions outside of the single top level namespace
        // - There is only a single namespace declared at the top level
        var forcePreservation = compilationUnit.AttributeLists.Any()
            || compilationUnit.Members.Count > 1
            || !HasOneNamespace(compilationUnit);

        return (AddImportPlacement.InsideNamespace, forcePreservation);
    }

    private static bool HasOneNamespace(CompilationUnitSyntax compilationUnit)
    {
        // Find all the NamespaceDeclarations
        var allNamespaces = compilationUnit
            .DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .OfType<BaseNamespaceDeclarationSyntax>();

        // To determine if there are multiple namespaces we only need to look for at least two.
        return allNamespaces.Take(2).Count() == 1;
    }

    private static (CompilationUnitSyntax compilationUnitWithoutHeader, ImmutableArray<SyntaxTrivia> header) RemoveFileHeader(
        CompilationUnitSyntax syntaxRoot, IFileBannerFactsService bannerService)
    {
        var fileHeader = bannerService.GetFileBanner(syntaxRoot);
        var leadingTrivia = syntaxRoot.GetLeadingTrivia();

        for (var i = fileHeader.Length - 1; i >= 0; i--)
        {
            leadingTrivia = leadingTrivia.RemoveAt(i);
        }

        var newCompilationUnit = syntaxRoot.WithLeadingTrivia(leadingTrivia);

        return (newCompilationUnit, fileHeader);
    }

    private static CompilationUnitSyntax AddFileHeader(CompilationUnitSyntax compilationUnit, ImmutableArray<SyntaxTrivia> fileHeader)
    {
        if (fileHeader.IsEmpty)
        {
            return compilationUnit;
        }

        // Add leading trivia to the first token.
        var firstToken = compilationUnit.GetFirstToken(includeZeroWidth: true);
        var newLeadingTrivia = firstToken.LeadingTrivia.InsertRange(0, fileHeader);
        var newFirstToken = firstToken.WithLeadingTrivia(newLeadingTrivia);

        return compilationUnit.ReplaceToken(firstToken, newFirstToken);
    }
}
