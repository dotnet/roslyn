// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

/// <summary>
/// For finding references to implicit constructor initializers (i.e. the implicit "base()" call when non is
/// explicitly supplied).
/// </summary>
internal sealed class ImplicitConstructorInitializerSymbolReferenceFinder : ExplicitOrImplicitConstructorInitializerSymbolReferenceFinder
{
    public static readonly ImplicitConstructorInitializerSymbolReferenceFinder Instance = new();

    private ImplicitConstructorInitializerSymbolReferenceFinder()
    {
    }

    /// <summary>
    /// Only use this finder if the constructor is one that can be called without parameters (and thus a base class
    /// could be calling it implicitly).
    /// </summary>
    protected override bool CanFind(IMethodSymbol symbol)
        => base.CanFind(symbol) && symbol.Parameters.All(p => p.IsOptional || p.IsParams);

    protected override bool CheckIndex(Document document, string name, SyntaxTreeIndex index)
    {
        if (index.ContainsImplicitBaseConstructorInitializer)
        {
            // if we have `partial class C { public C() { } }` we have to assume it might be a match, as the base
            // type reference might be in a another part of the partial in another file.
            if (index.ContainsPartialClass)
                return true;

            // Otherwise, if it doesn't have any partial types, ensure that the base type name is referenced in the
            // same file.  e.g. `class C : B { public C() { } }`.   This allows us to greatly filter down the
            // number of matches, presuming that most inheriting types in a project are not themselves partial.
            if (index.ProbablyContainsIdentifier(name))
                return true;
        }

        return false;
    }

    protected sealed override void FindReferencesInDocument<TData>(
        IMethodSymbol methodSymbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = state.SyntaxFacts;
        var constructorNodes = state.Cache.GetConstructorDeclarations(cancellationToken);
        foreach (var constructorNode in constructorNodes)
        {
            if (!syntaxFacts.HasImplicitBaseConstructorInitializer(constructorNode))
                continue;

            // Looks like a suitable match.  A constructor with an implicit base constructor initializer. See if the
            // constructor's base type is the type that contains the constructor we're looking for.
            if (state.SemanticModel.GetDeclaredSymbol(constructorNode, cancellationToken) is IMethodSymbol constructor &&
                SymbolFinder.OriginalSymbolsMatch(state.Solution, methodSymbol.ContainingType, constructor.ContainingType.BaseType))
            {
                processResult(new(constructorNode, new(
                    state.Document,
                    alias: null,
                    constructor.Locations.First(loc => loc.SourceTree == constructorNode.SyntaxTree && constructorNode.Span.IntersectsWith(loc.SourceSpan)),
                    isImplicit: true,
                    new(valueUsageInfoOpt: null, TypeOrNamespaceUsageInfo.ObjectCreation),
                    additionalProperties: [],
                    CandidateReason.None)),
                    processResultData);
            }
        }
    }
}
