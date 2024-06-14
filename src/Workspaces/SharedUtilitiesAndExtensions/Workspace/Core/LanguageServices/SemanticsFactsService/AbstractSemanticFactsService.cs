// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.LanguageService;

internal abstract partial class AbstractSemanticFactsService : ISemanticFacts
{
    public abstract ISyntaxFacts SyntaxFacts { get; }
    public abstract IBlockFacts BlockFacts { get; }

    protected abstract ISemanticFacts SemanticFacts { get; }

    protected abstract SyntaxToken ToIdentifierToken(string identifier);

    // local name can be same as field or property. but that will hide
    // those and can cause semantic change later in some context.
    // so to be safe, we consider field and property in scope when
    // creating unique name for local
    private static readonly Func<ISymbol, bool> s_LocalNameFilter = s =>
        s.Kind is SymbolKind.Local or
                  SymbolKind.Parameter or
                  SymbolKind.RangeVariable or
                  SymbolKind.Field or
                  SymbolKind.Property ||
        s is { Kind: SymbolKind.NamedType, IsStatic: true };

    public SyntaxToken GenerateUniqueName(
        SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt,
        string baseName, CancellationToken cancellationToken)
    {
        return GenerateUniqueName(
            semanticModel, location, containerOpt, baseName, filter: null, usedNames: null, cancellationToken);
    }

    public SyntaxToken GenerateUniqueName(
        SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt,
        string baseName, IEnumerable<string> usedNames, CancellationToken cancellationToken)
    {
        return GenerateUniqueName(
            semanticModel, location, containerOpt, baseName, filter: null, usedNames, cancellationToken);
    }

    public SyntaxToken GenerateUniqueLocalName(
        SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt,
        string baseName, CancellationToken cancellationToken)
    {
        return GenerateUniqueName(
            semanticModel, location, containerOpt, baseName, s_LocalNameFilter, usedNames: [], cancellationToken);
    }

    public SyntaxToken GenerateUniqueLocalName(
        SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt,
        string baseName, IEnumerable<string> usedNames, CancellationToken cancellationToken)
    {
        return GenerateUniqueName(
            semanticModel, location, containerOpt, baseName, s_LocalNameFilter, usedNames: usedNames, cancellationToken);
    }

    public SyntaxToken GenerateUniqueName(
        SemanticModel semanticModel,
        SyntaxNode location, SyntaxNode containerOpt,
        string baseName, Func<ISymbol, bool> filter,
        IEnumerable<string> usedNames, CancellationToken cancellationToken)
    {
        var container = containerOpt ?? location.AncestorsAndSelf().FirstOrDefault(
            a => BlockFacts.IsExecutableBlock(a) || SyntaxFacts.IsParameterList(a) || SyntaxFacts.IsMethodBody(a));

        var candidates = GetCollidableSymbols(semanticModel, location, container, cancellationToken);
        var filteredCandidates = filter != null ? candidates.Where(filter) : candidates;

        return GenerateUniqueName(baseName, filteredCandidates.Select(s => s.Name).Concat(usedNames));
    }

    /// <summary>
    /// Retrieves all symbols that could collide with a symbol at the specified location.
    /// A symbol can possibly collide with the location if it is available to that location and/or
    /// could cause a compiler error if its name is re-used at that location.
    /// </summary>
    protected virtual IEnumerable<ISymbol> GetCollidableSymbols(SemanticModel semanticModel, SyntaxNode location, SyntaxNode container, CancellationToken cancellationToken)
        => semanticModel.LookupSymbols(location.SpanStart).Concat(semanticModel.GetExistingSymbols(container, cancellationToken));

    public SyntaxToken GenerateUniqueName(string baseName, IEnumerable<string> usedNames)
    {
        return this.ToIdentifierToken(
            NameGenerator.EnsureUniqueness(
                baseName, usedNames, this.SyntaxFacts.IsCaseSensitive));
    }

#nullable enable

    protected static IMethodSymbol? FindDisposeMethod(Compilation compilation, ITypeSymbol? type, bool isAsync)
    {
        if (type is null)
            return null;

        var methodToLookFor = isAsync
            ? GetDisposeMethod(typeof(IAsyncDisposable).FullName!, nameof(IAsyncDisposable.DisposeAsync))
            : GetDisposeMethod(typeof(IDisposable).FullName!, nameof(IDisposable.Dispose));
        if (methodToLookFor is null)
            return null;

        var impl = type.FindImplementationForInterfaceMember(methodToLookFor) ?? methodToLookFor;
        return impl as IMethodSymbol;

        IMethodSymbol? GetDisposeMethod(string typeName, string methodName)
        {
            var disposableType = compilation.GetBestTypeByMetadataName(typeName);
            return disposableType?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => m.Parameters.Length == 0 && m.Name == methodName);
        }
    }

#nullable disable

    #region ISemanticFacts implementation

    public bool SupportsImplicitInterfaceImplementation => SemanticFacts.SupportsImplicitInterfaceImplementation;

    public bool SupportsParameterizedProperties => SemanticFacts.SupportsParameterizedProperties;

    public bool ExposesAnonymousFunctionParameterNames => SemanticFacts.ExposesAnonymousFunctionParameterNames;

    public bool IsWrittenTo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        => SemanticFacts.IsWrittenTo(semanticModel, node, cancellationToken);

    public bool IsOnlyWrittenTo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        => SemanticFacts.IsOnlyWrittenTo(semanticModel, node, cancellationToken);

    public bool IsInOutContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        => SemanticFacts.IsInOutContext(semanticModel, node, cancellationToken);

    public bool IsInRefContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        => SemanticFacts.IsInRefContext(semanticModel, node, cancellationToken);

    public bool IsInInContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        => SemanticFacts.IsInInContext(semanticModel, node, cancellationToken);

    public bool CanReplaceWithRValue(SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
        => SemanticFacts.CanReplaceWithRValue(semanticModel, expression, cancellationToken);

    public ISymbol GetDeclaredSymbol(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        => SemanticFacts.GetDeclaredSymbol(semanticModel, token, cancellationToken);

    public bool LastEnumValueHasInitializer(INamedTypeSymbol namedTypeSymbol)
        => SemanticFacts.LastEnumValueHasInitializer(namedTypeSymbol);

    public bool TryGetSpeculativeSemanticModel(SemanticModel oldSemanticModel, SyntaxNode oldNode, SyntaxNode newNode, out SemanticModel speculativeModel)
        => SemanticFacts.TryGetSpeculativeSemanticModel(oldSemanticModel, oldNode, newNode, out speculativeModel);

    public ImmutableHashSet<string> GetAliasNameSet(SemanticModel model, CancellationToken cancellationToken)
        => SemanticFacts.GetAliasNameSet(model, cancellationToken);

    public ForEachSymbols GetForEachSymbols(SemanticModel semanticModel, SyntaxNode forEachStatement)
        => SemanticFacts.GetForEachSymbols(semanticModel, forEachStatement);

    public SymbolInfo GetCollectionInitializerSymbolInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        => SemanticFacts.GetCollectionInitializerSymbolInfo(semanticModel, node, cancellationToken);

    public IMethodSymbol GetGetAwaiterMethod(SemanticModel semanticModel, SyntaxNode node)
        => SemanticFacts.GetGetAwaiterMethod(semanticModel, node);

    public ImmutableArray<IMethodSymbol> GetDeconstructionAssignmentMethods(SemanticModel semanticModel, SyntaxNode node)
        => SemanticFacts.GetDeconstructionAssignmentMethods(semanticModel, node);

    public ImmutableArray<IMethodSymbol> GetDeconstructionForEachMethods(SemanticModel semanticModel, SyntaxNode node)
        => SemanticFacts.GetDeconstructionForEachMethods(semanticModel, node);

    public bool IsPartial(INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
        => SemanticFacts.IsPartial(typeSymbol, cancellationToken);

    public IEnumerable<ISymbol> GetDeclaredSymbols(SemanticModel semanticModel, SyntaxNode memberDeclaration, CancellationToken cancellationToken)
        => SemanticFacts.GetDeclaredSymbols(semanticModel, memberDeclaration, cancellationToken);

    public IParameterSymbol FindParameterForArgument(SemanticModel semanticModel, SyntaxNode argumentNode, bool allowUncertainCandidates, bool allowParams, CancellationToken cancellationToken)
        => SemanticFacts.FindParameterForArgument(semanticModel, argumentNode, allowUncertainCandidates, allowParams, cancellationToken);

    public IParameterSymbol FindParameterForAttributeArgument(SemanticModel semanticModel, SyntaxNode argumentNode, bool allowUncertainCandidates, bool allowParams, CancellationToken cancellationToken)
        => SemanticFacts.FindParameterForAttributeArgument(semanticModel, argumentNode, allowUncertainCandidates, allowParams, cancellationToken);

    public ISymbol FindFieldOrPropertyForArgument(SemanticModel semanticModel, SyntaxNode argumentNode, CancellationToken cancellationToken)
        => SemanticFacts.FindFieldOrPropertyForArgument(semanticModel, argumentNode, cancellationToken);

    public ISymbol FindFieldOrPropertyForAttributeArgument(SemanticModel semanticModel, SyntaxNode argumentNode, CancellationToken cancellationToken)
        => SemanticFacts.FindFieldOrPropertyForAttributeArgument(semanticModel, argumentNode, cancellationToken);

    public ImmutableArray<ISymbol> GetBestOrAllSymbols(SemanticModel semanticModel, SyntaxNode node, SyntaxToken token, CancellationToken cancellationToken)
        => SemanticFacts.GetBestOrAllSymbols(semanticModel, node, token, cancellationToken);

    public bool IsInsideNameOfExpression(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        => SemanticFacts.IsInsideNameOfExpression(semanticModel, node, cancellationToken);

    public ImmutableArray<IMethodSymbol> GetLocalFunctionSymbols(Compilation compilation, ISymbol symbol, CancellationToken cancellationToken)
        => SemanticFacts.GetLocalFunctionSymbols(compilation, symbol, cancellationToken);

    public bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol expressionTypeOpt, CancellationToken cancellationToken)
        => SemanticFacts.IsInExpressionTree(semanticModel, node, expressionTypeOpt, cancellationToken);

    public string GenerateNameForExpression(SemanticModel semanticModel, SyntaxNode expression, bool capitalize, CancellationToken cancellationToken)
        => SemanticFacts.GenerateNameForExpression(semanticModel, expression, capitalize, cancellationToken);

#if !CODE_STYLE

    public Task<ISymbol> GetInterceptorSymbolAsync(Document document, int position, CancellationToken cancellationToken)
        => SemanticFacts.GetInterceptorSymbolAsync(document, position, cancellationToken);

#endif

    #endregion
}
