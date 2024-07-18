// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AliasAmbiguousType;

internal abstract class AbstractAliasAmbiguousTypeCodeFixProvider : CodeFixProvider
{
    protected abstract string GetTextPreviewOfChange(string aliasName, ITypeSymbol typeSymbol);

    public override FixAllProvider? GetFixAllProvider() => null;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Innermost: We are looking for an IdentifierName. IdentifierName is sometimes at the same span as its parent (e.g. SimpleBaseTypeSyntax).
        var diagnosticNode = root.FindNode(context.Span, getInnermostNodeForTie: true);
        if (!syntaxFacts.IsIdentifierName(diagnosticNode))
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbolInfo = semanticModel.GetSymbolInfo(diagnosticNode, cancellationToken);
        if (!SymbolCandidatesContainsSupportedSymbols(symbolInfo))
            return;

        var addImportService = document.GetRequiredLanguageService<IAddImportsService>();
        var syntaxGenerator = document.GetRequiredLanguageService<SyntaxGenerator>();
        var compilation = semanticModel.Compilation;

        var placementOption = await document.GetAddImportPlacementOptionsAsync(addImportService, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actions);
        foreach (var symbol in Sort(symbolInfo.CandidateSymbols.Cast<ITypeSymbol>(), placementOption.PlaceSystemNamespaceFirst))
        {
            var typeName = symbol.Name;
            var title = GetTextPreviewOfChange(typeName, symbol);

            actions.Add(CodeAction.Create(
                title,
                cancellationToken =>
                {
                    var aliasDirective = syntaxGenerator.AliasImportDeclaration(typeName, symbol);
                    var newRoot = addImportService.AddImport(compilation, root, diagnosticNode, aliasDirective, syntaxGenerator, placementOption, cancellationToken);
                    return Task.FromResult(document.WithSyntaxRoot(newRoot));
                },
                title));
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                string.Format(CodeFixesResources.Alias_ambiguous_type_0, diagnosticNode.ToString()),
                actions.ToImmutable(),
                isInlinable: true),
            context.Diagnostics.First());
    }

    private static IEnumerable<ITypeSymbol> Sort(IEnumerable<ITypeSymbol> types, bool sortSystemFirst)
    {
        // get all the name portions of the fully-qualified-names of the types in 'types'.
        // cache these in this local dictionary so we only have to compute them once.
        var typeToNameSegments = new Dictionary<ITypeSymbol, ImmutableArray<string>>();

        return types.OrderBy((t1, t2) =>
        {
            var t1NameSegments = GetNameSegments(t1);
            var t2NameSegments = GetNameSegments(t2);

            // compare all the name segments the two types have in common.
            for (int i = 0, n = Math.Min(t1NameSegments.Length, t2NameSegments.Length); i < n; i++)
            {
                var t1NameSegment = t1NameSegments[i];
                var t2NameSegment = t2NameSegments[i];

                // if we're on the first name segment, ensure we sort 'System' properly if the user
                // prefers them coming first.
                var comparer = i == 0 && sortSystemFirst ? SortSystemFirstComparer.Instance : StringComparer.Ordinal;

                var diff = comparer.Compare(t1NameSegment, t2NameSegment);
                if (diff != 0)
                    return diff;
            }

            // if all the names matched up to this point, then the type with the shorter number of segments comes first.
            return t1NameSegments.Length - t2NameSegments.Length;
        });

        ImmutableArray<string> GetNameSegments(ITypeSymbol symbol)
        {
            return typeToNameSegments.GetOrAdd(symbol, static symbol =>
            {
                using var result = TemporaryArray<string>.Empty;

                for (ISymbol current = symbol; current != null; current = current.ContainingSymbol)
                {
                    if (string.IsNullOrEmpty(current.Name))
                        break;

                    result.Add(current.Name);
                }

                // We walked upwards to get the name segments.  So reverse teh order here so it goes from outer-most to
                // inner-most names.
                result.ReverseContents();
                return result.ToImmutableAndClear();
            });
        }
    }

    private static bool SymbolCandidatesContainsSupportedSymbols(SymbolInfo symbolInfo)
        => symbolInfo.CandidateReason == CandidateReason.Ambiguous &&
           // Arity: Aliases can only name closed constructed types. (See also proposal https://github.com/dotnet/csharplang/issues/1239)
           // Aliasing as a closed constructed type is possible but would require to remove the type arguments from the diagnosed node.
           // It is unlikely that the user wants that and so generic types are not supported.
           symbolInfo.CandidateSymbols.All(symbol => symbol.IsKind(SymbolKind.NamedType) &&
                                                     symbol.GetArity() == 0);

    private sealed class SortSystemFirstComparer : IComparer<string>
    {
        public static readonly IComparer<string> Instance = new SortSystemFirstComparer();

        public int Compare(string? x, string? y)
        {
            var xIsSystem = x == nameof(System);
            var yIsSystem = y == nameof(System);

            if (xIsSystem && yIsSystem)
                return 0;

            if (xIsSystem)
                return -1;

            if (yIsSystem)
                return 1;

            return StringComparer.Ordinal.Compare(x, y);
        }
    }
}
