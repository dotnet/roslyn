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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    using SymbolKeyAndLineNumberArray = ImmutableArray<(SymbolKey, int lineNumber)>;

    internal abstract partial class AbstractInheritanceMarginService : IInheritanceMarginService
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
            bool includeGlobalImports,
            CancellationToken cancellationToken)
        {
            var (remappedProject, symbolKeyAndLineNumbers) = await GetMemberSymbolKeysAsync(document, spanToSearch, cancellationToken).ConfigureAwait(false);

            // if we didn't remap the symbol to another project (e.g. remapping from a metadata-as-source symbol back to
            // the originating project), then we're in teh same project and we should try to get global import
            // information to display.
            if (remappedProject != document.Project)
                includeGlobalImports = false;

            if (!includeGlobalImports && symbolKeyAndLineNumbers.IsEmpty)
                return ImmutableArray<InheritanceMarginItem>.Empty;

            return await GetInheritanceMemberItemAsync(
                remappedProject,
                documentForGlobalImports: includeGlobalImports ? document : null,
                spanToSearch,
                symbolKeyAndLineNumbers,
                cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<(Project remapped, SymbolKeyAndLineNumberArray symbolKeyAndLineNumbers)> GetMemberSymbolKeysAsync(
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

                if (project != null)
                    return (project, builder.ToImmutable());
            }

            return (document.Project, SymbolKeyAndLineNumberArray.Empty);
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
