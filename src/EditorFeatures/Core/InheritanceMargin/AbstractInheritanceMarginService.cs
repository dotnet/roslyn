// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal abstract partial class AbstractInheritanceMarginService : IInheritanceMarginService
    {
        public async Task<ImmutableArray<InheritanceMemberItem>> GetInheritanceInfoForLineAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var featureEnabled = options.GetOption(InheritanceMarginOptions.ShowInheritanceMargin);
            if (!featureEnabled)
            {
                return ImmutableArray<InheritanceMemberItem>.Empty;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var allDeclarationNodes = GetMembers(root);

            if (allDeclarationNodes.IsEmpty)
            {
                return ImmutableArray<InheritanceMemberItem>.Empty;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var lines = sourceText.Lines;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            using var _ = ArrayBuilder<InheritanceMemberItem>.GetInstance(out var builder);

            foreach (var memberDeclarationNode in allDeclarationNodes)
            {
                var member = semanticModel.GetDeclaredSymbol(memberDeclarationNode, cancellationToken);
                if (member != null)
                {
                    var mappingSymbolAndProject = await GetMappingSymbolAsync(
                        document,
                        member,
                        cancellationToken).ConfigureAwait(false);

                    if (mappingSymbolAndProject != null)
                    {
                        if (member is INamedTypeSymbol { TypeKind: not TypeKind.Error } namedTypeSymbol)
                        {
                            var baseTypes = GetImplementingSymbols(namedTypeSymbol);
                            var derivedTypes = await GetImplementedSymbolsAsync(
                                document,
                                namedTypeSymbol,
                                cancellationToken).ConfigureAwait(false);
                            if (!(baseTypes.IsEmpty && derivedTypes.IsEmpty))
                            {
                                var lineNumber = lines.GetLineFromPosition(memberDeclarationNode.SpanStart).LineNumber;
                                var item = await CreateInheritanceMemberInfoAsync(
                                    document,
                                    namedTypeSymbol,
                                    lineNumber,
                                    baseSymbols: baseTypes,
                                    derivedTypesSymbols: derivedTypes,
                                    cancellationToken).ConfigureAwait(false);
                                builder.Add(item);
                            }
                        }

                        if (member is IMethodSymbol or IEventSymbol or IPropertySymbol)
                        {
                            var overridenSymbols = await GetOverridenSymbolsAsync(document, member, cancellationToken).ConfigureAwait(false);
                            var overridingMembers = GetOverridingSymbols(member);
                            var implementedMembers = await GetImplementedSymbolsAsync(document, member, cancellationToken).ConfigureAwait(false);
                            var implementingMembers = GetImplementingSymbols(member);
                            if (!(overridenSymbols.IsEmpty
                                && !overridingMembers.IsEmpty
                                && !implementingMembers.IsEmpty
                                && !implementedMembers.IsEmpty))
                            {
                                var lineNumber = lines.GetLineFromPosition(memberDeclarationNode.SpanStart).LineNumber;
                                var item = await CreateInheritanceMemberInfoForMemberAsync(
                                    document,
                                    member,
                                    lineNumber,
                                    implementingMembers: implementingMembers,
                                    implementedMembers: implementedMembers,
                                    overridenMembers: overridenSymbols,
                                    overridingMembers: overridingMembers,
                                    cancellationToken).ConfigureAwait(false);
                                builder.Add(item);
                            }
                        }
                    }
                }
            }

            return builder.ToImmutable();
        }

        protected abstract ImmutableArray<SyntaxNode> GetMembers(SyntaxNode root);
    }
}
