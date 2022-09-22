// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin.Finders;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    using SymbolAndLineNumberArray = ImmutableArray<(ISymbol symbol, int lineNumber)>;

    internal abstract partial class AbstractInheritanceMarginService
    {
        private static readonly SymbolDisplayFormat s_displayFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions:
                SymbolDisplayMemberOptions.IncludeContainingType |
                SymbolDisplayMemberOptions.IncludeExplicitInterface,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        private static async ValueTask<ImmutableArray<InheritanceMarginItem>> GetSymbolInheritanceChainItemsAsync(
            Project project,
            Document? document,
            SymbolAndLineNumberArray symbolAndLineNumbers,
            bool frozenPartialSemantics,
            CancellationToken cancellationToken)
        {
            // If we're starting from a document, use it to go to a frozen partial version of it to lower the amount of
            // work we need to do running source generators or producing skeleton references.
            if (document != null && frozenPartialSemantics)
            {
                document = document.WithFrozenPartialSemantics(cancellationToken);
                project = document.Project;
            }

            var solution = project.Solution;
            using var _ = ArrayBuilder<InheritanceMarginItem>.GetInstance(out var builder);
            foreach (var (symbol, lineNumber) in symbolAndLineNumbers)
            {
                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    await AddInheritanceMemberItemsForNamedTypeAsync(solution, namedTypeSymbol, lineNumber, builder, cancellationToken).ConfigureAwait(false);
                }

                if (symbol is IEventSymbol or IPropertySymbol or IMethodSymbol)
                {
                    await AddInheritanceMemberItemsForMembersAsync(solution, symbol, lineNumber, builder, cancellationToken).ConfigureAwait(false);
                }
            }

            return builder.ToImmutable();
        }

        private async ValueTask<(Project remapped, SymbolAndLineNumberArray symbolAndLineNumbers)> GetMemberSymbolsAsync(
            Document document,
            TextSpan spanToSearch,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var allDeclarationNodes = GetMembers(root.DescendantNodes(spanToSearch));
            if (!allDeclarationNodes.IsEmpty)
            {
                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var mappingService = document.Project.Solution.Services.GetRequiredService<ISymbolMappingService>();
                using var _ = ArrayBuilder<(ISymbol symbol, int lineNumber)>.GetInstance(out var builder);

                Project? project = null;

                foreach (var memberDeclarationNode in allDeclarationNodes)
                {
                    var member = semanticModel.GetDeclaredSymbol(memberDeclarationNode, cancellationToken);
                    if (member == null || !CanHaveInheritanceTarget(member))
                        continue;

                    // Use mapping service to find correct solution & symbol. (e.g. metadata symbol)
                    var mappingResult = await mappingService.MapSymbolAsync(document, member, cancellationToken).ConfigureAwait(false);
                    if (mappingResult == null)
                        continue;

                    // All the symbols here are declared in the same document, they should belong to the same project.
                    // So here it is enough to get the project once.
                    project ??= mappingResult.Project;
                    builder.Add((mappingResult.Symbol, sourceText.Lines.GetLineFromPosition(GetDeclarationToken(memberDeclarationNode).SpanStart).LineNumber));
                }

                if (project != null)
                    return (project, builder.ToImmutable());
            }

            return (document.Project, SymbolAndLineNumberArray.Empty);
        }

        private async Task<ImmutableArray<InheritanceMarginItem>> GetInheritanceMarginItemsInProcessAsync(
            Document document,
            TextSpan spanToSearch,
            bool includeGlobalImports,
            bool frozenPartialSemantics,
            CancellationToken cancellationToken)
        {
            var (remappedProject, symbolAndLineNumbers) = await GetMemberSymbolsAsync(document, spanToSearch, cancellationToken).ConfigureAwait(false);

            // if we didn't remap the symbol to another project (e.g. remapping from a metadata-as-source symbol back to
            // the originating project), then we're in teh same project and we should try to get global import
            // information to display.
            var remapped = remappedProject != document.Project;

            using var _ = ArrayBuilder<InheritanceMarginItem>.GetInstance(out var result);

            if (includeGlobalImports && !remapped)
                result.AddRange(await GetGlobalImportsItemsAsync(document, spanToSearch, frozenPartialSemantics: frozenPartialSemantics, cancellationToken).ConfigureAwait(false));

            if (!symbolAndLineNumbers.IsEmpty)
            {
                result.AddRange(await GetSymbolInheritanceChainItemsAsync(
                    remappedProject,
                    document: remapped ? null : document,
                    symbolAndLineNumbers,
                    frozenPartialSemantics: frozenPartialSemantics,
                    cancellationToken).ConfigureAwait(false));
            }

            return result.ToImmutable();
        }

        private async Task<ImmutableArray<InheritanceMarginItem>> GetGlobalImportsItemsAsync(
            Document document,
            TextSpan spanToSearch,
            bool frozenPartialSemantics,
            CancellationToken cancellationToken)
        {
            if (frozenPartialSemantics)
                document = document.WithFrozenPartialSemantics(cancellationToken);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var imports = syntaxFacts.GetImportsOfCompilationUnit(root);

            // Place the imports item on the start of the first import in the file.  Or, if there is no import, then on
            // the first line.
            var spanStart = imports.Count > 0 ? imports[0].SpanStart : 0;

            // if that location doesn't intersect with the lines of interest, immediately bail out.
            if (!spanToSearch.IntersectsWith(spanStart))
                return ImmutableArray<InheritanceMarginItem>.Empty;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var scopes = semanticModel.GetImportScopes(root.FullSpan.End, cancellationToken);

            // If we have global imports they would only be in the last scope in the scopes array.  All other scopes
            // correspond to inner scopes for either the compilation unit or namespace.
            var lastScope = scopes.LastOrDefault();
            if (lastScope == null)
                return ImmutableArray<InheritanceMarginItem>.Empty;

            // Pull in any project level imports, or imports from other files (e.g. global usings).
            var syntaxTree = semanticModel.SyntaxTree;
            var nonLocalImports = lastScope.Imports
                .WhereAsArray(i => i.DeclaringSyntaxReference?.SyntaxTree != syntaxTree)
                .Sort((i1, i2) =>
                {
                    return (i1.DeclaringSyntaxReference, i2.DeclaringSyntaxReference) switch
                    {
                        // Both are project level imports.  Sort by name of symbol imported.
                        (null, null) => i1.NamespaceOrType.ToDisplayString().CompareTo(i2.NamespaceOrType.ToDisplayString()),
                        // project level imports come first.
                        (null, not null) => -1,
                        (not null, null) => 1,
                        // both are from different files.  Sort by file path first, then location in file if same file path.
                        ({ SyntaxTree: var syntaxTree1, Span: var span1 }, { SyntaxTree: var syntaxTree2, Span: var span2 })
                            => syntaxTree1.FilePath != syntaxTree2.FilePath
                                ? StringComparer.OrdinalIgnoreCase.Compare(syntaxTree1.FilePath, syntaxTree2.FilePath)
                                : span1.CompareTo(span2),
                    };
                });

            if (nonLocalImports.Length == 0)
                return ImmutableArray<InheritanceMarginItem>.Empty;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var lineNumber = text.Lines.GetLineFromPosition(spanStart).LineNumber;

            var projectState = document.Project.State;
            var projectName = projectState.NameAndFlavor.name ?? projectState.Name;
            var languageGlyph = document.Project.Language switch
            {
                LanguageNames.CSharp => Glyph.CSharpFile,
                LanguageNames.VisualBasic => Glyph.BasicFile,
                _ => throw ExceptionUtilities.UnexpectedValue(document.Project.Language),
            };

            using var _1 = ArrayBuilder<InheritanceMarginItem>.GetInstance(out var items);

            foreach (var group in nonLocalImports.GroupBy(i => i.DeclaringSyntaxReference?.SyntaxTree))
            {
                var groupSyntaxTree = group.Key;
                if (groupSyntaxTree is null)
                {
                    using var _2 = ArrayBuilder<InheritanceTargetItem>.GetInstance(out var targetItems);

                    foreach (var import in group)
                    {
                        var item = DefinitionItem.CreateNonNavigableItem(ImmutableArray<string>.Empty, ImmutableArray<TaggedText>.Empty);
                        targetItems.Add(new InheritanceTargetItem(
                            InheritanceRelationship.InheritedImport, item.Detach(), Glyph.None, languageGlyph,
                            import.NamespaceOrType.ToDisplayString(), projectName));
                    }

                    items.Add(new InheritanceMarginItem(
                        lineNumber, this.GlobalImportsTitle, ImmutableArray.Create(new TaggedText(TextTags.Text, this.GlobalImportsTitle)),
                        Glyph.Namespace, targetItems.ToImmutable()));
                }
                else
                {
                    var destinationDocument = document.Project.Solution.GetDocument(groupSyntaxTree);
                    if (destinationDocument is null)
                        continue;

                    using var _ = ArrayBuilder<InheritanceTargetItem>.GetInstance(out var targetItems);

                    foreach (var import in group)
                    {
                        var item = DefinitionItem.Create(
                            ImmutableArray<string>.Empty, ImmutableArray<TaggedText>.Empty,
                            new DocumentSpan(destinationDocument, import.DeclaringSyntaxReference!.Span));
                        targetItems.Add(new InheritanceTargetItem(
                            InheritanceRelationship.InheritedImport, item.Detach(), Glyph.None, languageGlyph,
                            import.NamespaceOrType.ToDisplayString(), projectName));
                    }

                    var filePath = groupSyntaxTree.FilePath;
                    var fileName = filePath == null ? null : IOUtilities.PerformIO(() => Path.GetFileName(filePath)) ?? filePath;
                    var taggedText = new TaggedText(TextTags.Text, string.Format(FeaturesResources.Directives_from_0, fileName));

                    items.Add(new InheritanceMarginItem(
                        lineNumber, this.GlobalImportsTitle, ImmutableArray.Create(taggedText), Glyph.Namespace, targetItems.ToImmutable()));
                }
            }

            return items.ToImmutable();
        }

        private static async ValueTask AddInheritanceMemberItemsForNamedTypeAsync(
            Solution solution,
            INamedTypeSymbol namedTypeSymbol,
            int lineNumber,
            ArrayBuilder<InheritanceMarginItem> builder,
            CancellationToken cancellationToken)
        {
            var (baseTypeSymbolGroups, baseInterfaceSymbolGroups) = await BaseTypeSymbolsFinder.Instance
                .GetBaseTypeAndBaseInterfaceSymbolGroupsAsync(
                    namedTypeSymbol, solution, cancellationToken).ConfigureAwait(false);

            var derivedTypeSymbolGroups = await DerivedTypeSymbolsFinder.Instance.GetDerivedTypeSymbolGroupsAsync(
                namedTypeSymbol,
                solution,
                cancellationToken).ConfigureAwait(false);

            if (baseTypeSymbolGroups.Any() || baseInterfaceSymbolGroups.Any() || derivedTypeSymbolGroups.Any())
            {
                if (namedTypeSymbol.TypeKind == TypeKind.Interface)
                {
                    var item = await CreateInheritanceMemberItemForInterfaceAsync(
                        solution,
                        namedTypeSymbol,
                        lineNumber,
                        baseSymbolGroups: baseTypeSymbolGroups.Concat(baseInterfaceSymbolGroups),
                        derivedTypesSymbolGroups: derivedTypeSymbolGroups,
                        cancellationToken).ConfigureAwait(false);
                    builder.AddIfNotNull(item);
                }
                else
                {
                    Debug.Assert(namedTypeSymbol.TypeKind is TypeKind.Class or TypeKind.Struct);
                    if (baseTypeSymbolGroups.Any() || baseInterfaceSymbolGroups.Any() || derivedTypeSymbolGroups.Any())
                    {
                        var item = await CreateInheritanceItemForClassAndStructureAsync(
                            solution,
                            namedTypeSymbol,
                            lineNumber,
                            baseTypeSymbolGroups: baseTypeSymbolGroups,
                            implementedInterfacesSymbolGroups: baseInterfaceSymbolGroups,
                            derivedTypesSymbolGroups: derivedTypeSymbolGroups,
                            cancellationToken).ConfigureAwait(false);
                        builder.AddIfNotNull(item);
                    }
                }
            }
        }

        private static async ValueTask AddInheritanceMemberItemsForMembersAsync(
            Solution solution,
            ISymbol memberSymbol,
            int lineNumber,
            ArrayBuilder<InheritanceMarginItem> builder,
            CancellationToken cancellationToken)
        {
            if (memberSymbol.ContainingSymbol.IsInterfaceType())
            {
                // Go down the inheritance chain to find all the implementing targets.
                var implementingSymbolGroups = await ImplementingSymbolsFinder.Instance.GetImplementingSymbolsGroupAsync(memberSymbol, solution, cancellationToken).ConfigureAwait(false);
                if (implementingSymbolGroups.Any())
                {
                    var item = await CreateInheritanceMemberItemForInterfaceMemberAsync(solution,
                        memberSymbol,
                        lineNumber,
                        implementingMembers: implementingSymbolGroups,
                        cancellationToken).ConfigureAwait(false);
                    builder.AddIfNotNull(item);
                }
            }
            else
            {
                // Go down the inheritance chain to find all the overriding targets.
                var overridingSymbolGroups = await OverridingSymbolsFinder.Instance.GetOverridingSymbolsGroupAsync(memberSymbol, solution, cancellationToken).ConfigureAwait(false);

                // Go up the inheritance chain to find all overridden targets & implemented targets.
                var (implementedSymbolGroups, overriddenSymbolGroups) = await ImplementedSymbolAndOverriddenSymbolsFinder.Instance
                    .GetImplementedSymbolAndOverrriddenSymbolGroupsAsync(memberSymbol, solution, cancellationToken).ConfigureAwait(false);
                if (overridingSymbolGroups.Any() || overriddenSymbolGroups.Any() || implementedSymbolGroups.Any())
                {
                    var item = await CreateInheritanceMemberItemForClassOrStructMemberAsync(solution,
                        memberSymbol,
                        lineNumber,
                        implementedMemberSymbolGroups: implementedSymbolGroups,
                        overridingMemberSymbolGroups: overridingSymbolGroups,
                        overriddenMemberSymbolGroups: overriddenSymbolGroups,
                        cancellationToken).ConfigureAwait(false);
                    builder.AddIfNotNull(item);
                }
            }
        }

        private static async ValueTask<InheritanceMarginItem?> CreateInheritanceMemberItemForInterfaceAsync(
            Solution solution,
            INamedTypeSymbol interfaceSymbol,
            int lineNumber,
            ImmutableArray<SymbolGroup> baseSymbolGroups,
            ImmutableArray<SymbolGroup> derivedTypesSymbolGroups,
            CancellationToken cancellationToken)
        {
            var baseSymbolItems = await baseSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.InheritedInterface,
                    cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            var derivedTypeItems = await derivedTypesSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(solution,
                    symbolGroup,
                    InheritanceRelationship.ImplementingType,
                    cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            var nonNullBaseSymbolItems = GetNonNullTargetItems(baseSymbolItems);
            var nonNullDerivedTypeItems = GetNonNullTargetItems(derivedTypeItems);

            return InheritanceMarginItem.CreateOrdered(
                lineNumber,
                topLevelDisplayText: null,
                FindUsagesHelpers.GetDisplayParts(interfaceSymbol),
                interfaceSymbol.GetGlyph(),
                nonNullBaseSymbolItems.Concat(nonNullDerivedTypeItems));
        }

        private static async ValueTask<InheritanceMarginItem?> CreateInheritanceMemberItemForInterfaceMemberAsync(
            Solution solution,
            ISymbol memberSymbol,
            int lineNumber,
            ImmutableArray<SymbolGroup> implementingMembers,
            CancellationToken cancellationToken)
        {
            var implementedMemberItems = await implementingMembers
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.ImplementingMember,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var nonNullImplementedMemberItems = GetNonNullTargetItems(implementedMemberItems);
            return InheritanceMarginItem.CreateOrdered(
                lineNumber,
                topLevelDisplayText: null,
                FindUsagesHelpers.GetDisplayParts(memberSymbol),
                memberSymbol.GetGlyph(),
                nonNullImplementedMemberItems);
        }

        private static async ValueTask<InheritanceMarginItem?> CreateInheritanceItemForClassAndStructureAsync(
            Solution solution,
            INamedTypeSymbol memberSymbol,
            int lineNumber,
            ImmutableArray<SymbolGroup> baseTypeSymbolGroups,
            ImmutableArray<SymbolGroup> implementedInterfacesSymbolGroups,
            ImmutableArray<SymbolGroup> derivedTypesSymbolGroups,
            CancellationToken cancellationToken)
        {
            // If the target is an interface, it would be shown as 'Inherited interface',
            // and if it is an class/struct, it whould be shown as 'Base Type'
            var baseSymbolItems = await baseTypeSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.BaseType,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var implementedInterfaces = await implementedInterfacesSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.ImplementedInterface,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var derivedTypeItems = await derivedTypesSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(solution,
                    symbolGroup,
                    InheritanceRelationship.DerivedType,
                    cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            var nonNullBaseSymbolItems = GetNonNullTargetItems(baseSymbolItems);
            var nonNullDerivedTypeItems = GetNonNullTargetItems(derivedTypeItems);

            return InheritanceMarginItem.CreateOrdered(
                lineNumber,
                topLevelDisplayText: null,
                FindUsagesHelpers.GetDisplayParts(memberSymbol),
                memberSymbol.GetGlyph(),
                nonNullBaseSymbolItems.Concat(nonNullDerivedTypeItems));
        }

        private static async ValueTask<InheritanceMarginItem?> CreateInheritanceMemberItemForClassOrStructMemberAsync(
            Solution solution,
            ISymbol memberSymbol,
            int lineNumber,
            ImmutableArray<SymbolGroup> implementedMemberSymbolGroups,
            ImmutableArray<SymbolGroup> overridingMemberSymbolGroups,
            ImmutableArray<SymbolGroup> overriddenMemberSymbolGroups,
            CancellationToken cancellationToken)
        {
            var implementedMemberItems = await implementedMemberSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.ImplementedMember,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var overriddenMemberItems = await overriddenMemberSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.OverriddenMember,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var overridingMemberItems = await overridingMemberSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.OverridingMember,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var nonNullImplementedMemberItems = GetNonNullTargetItems(implementedMemberItems);
            var nonNullOverriddenMemberItems = GetNonNullTargetItems(overriddenMemberItems);
            var nonNullOverridingMemberItems = GetNonNullTargetItems(overridingMemberItems);

            return InheritanceMarginItem.CreateOrdered(
                lineNumber,
                topLevelDisplayText: null,
                FindUsagesHelpers.GetDisplayParts(memberSymbol),
                memberSymbol.GetGlyph(),
                nonNullImplementedMemberItems.Concat(nonNullOverriddenMemberItems, nonNullOverridingMemberItems));
        }

        private static async ValueTask<InheritanceTargetItem?> CreateInheritanceItemAsync(
            Solution solution,
            SymbolGroup symbolGroup,
            InheritanceRelationship inheritanceRelationship,
            CancellationToken cancellationToken)
        {
            var targetSymbol = symbolGroup.Symbols.First();
            var symbolInSource = await SymbolFinder.FindSourceDefinitionAsync(targetSymbol, solution, cancellationToken).ConfigureAwait(false);
            targetSymbol = symbolInSource ?? targetSymbol;

            // Right now the targets are not shown in a classified way.
            var definition = ToSlimDefinitionItem(targetSymbol, solution);
            if (definition == null)
            {
                return null;
            }

            var displayName = targetSymbol.ToDisplayString(s_displayFormat);

            var projectState = definition.SourceSpans.Length > 0
                ? definition.SourceSpans[0].Document.Project.State
                : null;

            var languageGlyph = targetSymbol.Language switch
            {
                LanguageNames.CSharp => Glyph.CSharpFile,
                LanguageNames.VisualBasic => Glyph.BasicFile,
                _ => throw ExceptionUtilities.UnexpectedValue(targetSymbol.Language),
            };

            return new InheritanceTargetItem(
                inheritanceRelationship,
                definition.Detach(),
                targetSymbol.GetGlyph(),
                languageGlyph,
                displayName,
                projectState?.NameAndFlavor.name ?? projectState?.Name);
        }

        /// <summary>
        /// For the <param name="memberSymbol"/>, get all the implementing symbols.
        /// </summary>
        internal static async Task<ImmutableArray<ISymbol>> GetImplementingSymbolsForTypeMemberAsync(
            Solution solution,
            ISymbol memberSymbol,
            CancellationToken cancellationToken)
        {
            if (memberSymbol is IMethodSymbol or IEventSymbol or IPropertySymbol
                && memberSymbol.ContainingSymbol.IsInterfaceType())
            {
                using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);
                // 1. Find all direct implementations for this member
                var implementationSymbols = await SymbolFinder.FindMemberImplementationsArrayAsync(
                    memberSymbol,
                    solution,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                builder.AddRange(implementationSymbols);

                // 2. Continue searching the overriden symbols. For example:
                // interface IBar { void Foo(); }
                // class Bar : IBar { public virtual void Foo() { } }
                // class Bar2 : IBar { public override void Foo() { } }
                // For 'IBar.Foo()', we need to find 'Bar2.Foo()'
                foreach (var implementationSymbol in implementationSymbols)
                {
                    builder.AddRange(await SymbolFinder.FindOverridesArrayAsync(implementationSymbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
                }

                return builder.ToImmutableArray();
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        /// <summary>
        /// Get overridden members the <param name="memberSymbol"/>.
        /// </summary>
        internal static ImmutableArray<ISymbol> GetOverriddenSymbols(ISymbol memberSymbol)
        {
            if (memberSymbol is INamedTypeSymbol)
            {
                return ImmutableArray<ISymbol>.Empty;
            }
            else
            {
                using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);
                for (var overriddenMember = memberSymbol.GetOverriddenMember();
                    overriddenMember != null;
                    overriddenMember = overriddenMember.GetOverriddenMember())
                {
                    builder.Add(overriddenMember.OriginalDefinition);
                }

                return builder.ToImmutableArray();
            }
        }

        /// <summary>
        /// Get the derived interfaces and derived classes for <param name="typeSymbol"/>.
        /// </summary>
        internal static async Task<ImmutableArray<INamedTypeSymbol>> GetDerivedTypesAndImplementationsAsync(
            Solution solution,
            INamedTypeSymbol typeSymbol,
            CancellationToken cancellationToken)
        {
            if (typeSymbol.IsInterfaceType())
            {
                var allDerivedInterfaces = await SymbolFinder.FindDerivedInterfacesArrayAsync(
                    typeSymbol,
                    solution,
                    transitive: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                var allImplementations = await SymbolFinder.FindImplementationsArrayAsync(
                    typeSymbol,
                    solution,
                    transitive: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return allDerivedInterfaces.Concat(allImplementations);
            }
            else
            {
                return await SymbolFinder.FindDerivedClassesArrayAsync(
                    typeSymbol,
                    solution,
                    transitive: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Create the DefinitionItem based on the numbers of locations for <paramref name="symbol"/>.
        /// If there is only one location, create the DefinitionItem contains only the documentSpan or symbolKey to save memory.
        /// Because in such case, when clicking InheritanceMarginGlpph, it will directly navigate to the symbol.
        /// Otherwise, create the full non-classified DefinitionItem. Because in such case we want to display all the locations to the user
        /// by reusing the FAR window.
        /// </summary>
        private static DefinitionItem? ToSlimDefinitionItem(ISymbol symbol, Solution solution)
        {
            var locations = symbol.Locations;
            if (locations.Length > 1)
            {
                return symbol.ToNonClassifiedDefinitionItem(
                    solution,
                    FindReferencesSearchOptions.Default with { UnidirectionalHierarchyCascade = true },
                    includeHiddenLocations: false);
            }

            if (locations.Length == 1)
            {
                var location = locations[0];
                if (location.IsInMetadata)
                {
                    return DefinitionItem.CreateMetadataDefinition(
                        tags: ImmutableArray<string>.Empty,
                        displayParts: ImmutableArray<TaggedText>.Empty,
                        nameDisplayParts: ImmutableArray<TaggedText>.Empty,
                        solution,
                        symbol);
                }
                else if (location.IsInSource && location.IsVisibleSourceLocation())
                {
                    var document = solution.GetDocument(location.SourceTree);
                    if (document != null)
                    {
                        var documentSpan = new DocumentSpan(document, location.SourceSpan);
                        return DefinitionItem.Create(
                            tags: ImmutableArray<string>.Empty,
                            displayParts: ImmutableArray<TaggedText>.Empty,
                            documentSpan,
                            nameDisplayParts: ImmutableArray<TaggedText>.Empty);
                    }
                }
            }

            return null;
        }

        private static ImmutableArray<InheritanceTargetItem> GetNonNullTargetItems(ImmutableArray<InheritanceTargetItem?> inheritanceTargetItems)
        {
            using var _ = ArrayBuilder<InheritanceTargetItem>.GetInstance(out var builder);
            foreach (var item in inheritanceTargetItems)
            {
                if (item.HasValue)
                {
                    builder.Add(item.Value);
                }
            }

            return builder.ToImmutable();
        }

        internal static bool IsNavigableSymbol(ISymbol symbol)
        {
            var locations = symbol.Locations;
            if (locations.Length == 1)
            {
                var location = locations[0];
                return location.IsInMetadata || (location.IsInSource && location.IsVisibleSourceLocation());
            }
            return !locations.IsEmpty;
        }
    }
}
