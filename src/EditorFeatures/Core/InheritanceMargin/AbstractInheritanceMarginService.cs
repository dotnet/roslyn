// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.InheritanceMargin.InheritanceMarginServiceHelper;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal abstract class AbstractInheritanceMarginService : IInheritanceMarginService
    {
        /// <summary>
        /// Given the syntax nodes to search,
        /// get all the method, event, property and type declaration syntax nodes.
        /// </summary>
        protected abstract ImmutableArray<SyntaxNode> GetMembers(IEnumerable<SyntaxNode> nodesToSearch);

        /// <summary>
        /// Get the token that represents declaration node.
        /// e.g. Identifier for method/property/event and this keyword for indexer.
        /// </summary>
        protected abstract SyntaxToken GetDeclarationToken(SyntaxNode declarationNode);

        protected abstract string GlobalImportsTitle { get; }

        public async ValueTask<ImmutableArray<InheritanceMarginItem>> GetInheritanceMemberItemsAsync(
            Document document,
            TextSpan spanToSearch,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<InheritanceMarginItem>.GetInstance(out var items);

            await AddInheritedImportsAsync(document, spanToSearch, items, cancellationToken).ConfigureAwait(false);
            await AddInheritanceMemberItemsAsync(document, spanToSearch, items, cancellationToken).ConfigureAwait(false);

            return items.ToImmutable();
        }

        private async Task AddInheritedImportsAsync(
            Document document,
            TextSpan spanToSearch,
            ArrayBuilder<InheritanceMarginItem> items,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var imports = syntaxFacts.GetImportsOfCompilationUnit(root);

            // Place the imports item on the start of the first import in the file.  Or, if there is no import, then on
            // the first line.
            var spanStart = imports.Count > 0 ? imports[0].SpanStart : 0;

            // if that location doesn't intersect with the lines of interest, immediately bail out.
            if (!spanToSearch.IntersectsWith(spanStart))
                return;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var scopes = semanticModel.GetImportScopes(position: 0, cancellationToken);

            // If we have global imports they would only be in the last scope in the scopes array.  All other scopes
            // correspond to inner scopes for either the compilation unit or namespace.
            var lastScope = scopes.LastOrDefault();
            if (lastScope == null)
                return;

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
                return;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var lineNumber = text.Lines.GetLineFromPosition(spanStart).LineNumber;

            foreach (var group in nonLocalImports.GroupBy(i => i.DeclaringSyntaxReference?.SyntaxTree))
            {
                var groupSyntaxTree = group.Key;
                if (groupSyntaxTree is null)
                {
                    using var _ = ArrayBuilder<InheritanceTargetItem>.GetInstance(out var targetItems);

                    foreach (var import in group)
                    {
                        var item = DefinitionItem.CreateNonNavigableItem(ImmutableArray<string>.Empty, ImmutableArray<TaggedText>.Empty);
                        targetItems.Add(new InheritanceTargetItem(
                            InheritanceRelationship.InheritedImport, item.Detach(), Glyph.None, import.NamespaceOrType.ToDisplayString()));
                    }

                    items.Add(new InheritanceMarginItem(
                        lineNumber, this.GlobalImportsTitle, ImmutableArray.Create(new TaggedText(TextTags.Text, this.GlobalImportsTitle)),
                        Glyph.Namespace, isOrdered: true, targetItems.ToImmutable()));
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
                            InheritanceRelationship.InheritedImport, item.Detach(), Glyph.None, import.NamespaceOrType.ToDisplayString()));
                    }

                    var filePath = groupSyntaxTree.FilePath;
                    var fileName = filePath == null ? null : IOUtilities.PerformIO(() => Path.GetFileName(filePath)) ?? filePath;
                    var taggedText = new TaggedText(TextTags.Text, string.Format(EditorFeaturesResources.Directives_from_0, fileName));

                    items.Add(new InheritanceMarginItem(
                        lineNumber, this.GlobalImportsTitle, ImmutableArray.Create(taggedText), Glyph.Namespace, isOrdered: true, targetItems.ToImmutable()));
                }
            }
        }

        private async ValueTask AddInheritanceMemberItemsAsync(
            Document document,
            TextSpan spanToSearch,
            ArrayBuilder<InheritanceMarginItem> items,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var allDeclarationNodes = GetMembers(root.DescendantNodes(spanToSearch));
            if (allDeclarationNodes.IsEmpty)
                return;

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var mappingService = document.Project.Solution.Workspace.Services.GetRequiredService<ISymbolMappingService>();
            using var _ = ArrayBuilder<(SymbolKey symbolKey, int lineNumber)>.GetInstance(out var builder);

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
                builder.Add((mappingResult.Symbol.GetSymbolKey(cancellationToken), sourceText.Lines.GetLineFromPosition(GetDeclarationToken(memberDeclarationNode).SpanStart).LineNumber));
            }

            var symbolKeyAndLineNumbers = builder.ToImmutable();
            if (symbolKeyAndLineNumbers.IsEmpty || project == null)
                return;

            var solution = project.Solution;
            var serializedInheritanceMarginItems = await GetInheritanceMemberItemAsync(
                solution,
                project.Id,
                symbolKeyAndLineNumbers,
                cancellationToken).ConfigureAwait(false);

            foreach (var item in serializedInheritanceMarginItems)
                items.Add(await InheritanceMarginItem.ConvertAsync(solution, item, cancellationToken).ConfigureAwait(false));
        }

        private static bool CanHaveInheritanceTarget(ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedType)
            {
                return !symbol.IsStatic && namedType.TypeKind is TypeKind.Interface or TypeKind.Class or TypeKind.Struct;
            }

            if (symbol is IEventSymbol or IPropertySymbol
                or IMethodSymbol
                {
                    MethodKind: MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.UserDefinedOperator or MethodKind.Conversion
                })
            {
                return true;
            }

            return false;
        }
    }
}
