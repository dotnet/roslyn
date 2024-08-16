// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed partial class CSharpSemanticFacts : ISemanticFacts
{
    internal static readonly CSharpSemanticFacts Instance = new();

    private CSharpSemanticFacts()
    {
    }

    public ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

    public bool SupportsImplicitInterfaceImplementation => true;

    public bool ExposesAnonymousFunctionParameterNames => false;

    public bool IsWrittenTo(SemanticModel semanticModel, [NotNullWhen(true)] SyntaxNode? node, CancellationToken cancellationToken)
        => (node as ExpressionSyntax).IsWrittenTo(semanticModel, cancellationToken);

    public bool IsOnlyWrittenTo(SemanticModel semanticModel, [NotNullWhen(true)] SyntaxNode? node, CancellationToken cancellationToken)
        => (node as ExpressionSyntax).IsOnlyWrittenTo();

    public bool IsInOutContext(SemanticModel semanticModel, [NotNullWhen(true)] SyntaxNode? node, CancellationToken cancellationToken)
        => (node as ExpressionSyntax).IsInOutContext();

    public bool IsInRefContext(SemanticModel semanticModel, [NotNullWhen(true)] SyntaxNode? node, CancellationToken cancellationToken)
        => (node as ExpressionSyntax).IsInRefContext();

    public bool IsInInContext(SemanticModel semanticModel, [NotNullWhen(true)] SyntaxNode? node, CancellationToken cancellationToken)
        => (node as ExpressionSyntax).IsInInContext();

    public bool CanReplaceWithRValue(SemanticModel semanticModel, [NotNullWhen(true)] SyntaxNode? expression, CancellationToken cancellationToken)
        => (expression as ExpressionSyntax).CanReplaceWithRValue(semanticModel, cancellationToken);

    public ISymbol? GetDeclaredSymbol(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
    {
        var location = token.GetLocation();

        foreach (var ancestor in token.GetAncestors<SyntaxNode>())
        {
            var symbol = semanticModel.GetDeclaredSymbol(ancestor, cancellationToken);
            if (symbol != null)
            {
                if (symbol is IMethodSymbol { MethodKind: MethodKind.Conversion })
                {
                    // The token may be part of a larger name (for example, `int` in `public static operator int[](Goo g);`.
                    // So check if the symbol's location encompasses the span of the token we're asking about.
                    if (symbol.Locations.Any(static (loc, location) => loc.SourceTree == location.SourceTree && loc.SourceSpan.Contains(location.SourceSpan), location))
                        return symbol;
                }
                else
                {
                    // For any other symbols, we only care if the name directly matches the span of the token
                    if (symbol.Locations.Contains(location))
                        return symbol;
                }

                // We found some symbol, but it defined something else. We're not going to have a higher node defining _another_ symbol with this token, so we can stop now.
                return null;
            }

            // If we hit an executable statement syntax and didn't find anything yet, we can just stop now -- anything higher would be a member declaration which won't be defined by something inside a statement.
            if (CSharpSyntaxFacts.Instance.IsExecutableStatement(ancestor))
                return null;
        }

        return null;
    }

    public bool LastEnumValueHasInitializer(INamedTypeSymbol namedTypeSymbol)
    {
        var enumDecl = namedTypeSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<EnumDeclarationSyntax>().FirstOrDefault();
        if (enumDecl != null)
        {
            var lastMember = enumDecl.Members.LastOrDefault();
            if (lastMember != null)
            {
                return lastMember.EqualsValue != null;
            }
        }

        return false;
    }

    public bool SupportsParameterizedProperties => false;

    public bool TryGetSpeculativeSemanticModel(
        SemanticModel oldSemanticModel,
        SyntaxNode oldNode,
        SyntaxNode newNode,
        [NotNullWhen(true)] out SemanticModel? speculativeModel)
    {
        Debug.Assert(oldNode.Kind() == newNode.Kind());

        var model = oldSemanticModel;
        if (oldNode is not BaseMethodDeclarationSyntax oldMethod || newNode is not BaseMethodDeclarationSyntax newMethod || oldMethod.Body == null)
        {
            speculativeModel = null;
            return false;
        }

        return model.TryGetSpeculativeSemanticModelForMethodBody(oldMethod.Body.OpenBraceToken.Span.End, newMethod, out speculativeModel);
    }

    public ImmutableHashSet<string> GetAliasNameSet(SemanticModel model, CancellationToken cancellationToken)
    {
        var original = model.GetOriginalSemanticModel();
        if (!original.SyntaxTree.HasCompilationUnitRoot)
        {
            return [];
        }

        var root = original.SyntaxTree.GetCompilationUnitRoot(cancellationToken);
        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);

        AppendAliasNames(root.Usings, builder);
        AppendAliasNames(root.Members.OfType<BaseNamespaceDeclarationSyntax>(), builder, cancellationToken);

        return builder.ToImmutable();
    }

    private static void AppendAliasNames(SyntaxList<UsingDirectiveSyntax> usings, ImmutableHashSet<string>.Builder builder)
    {
        foreach (var @using in usings)
        {
            if (@using.Alias == null || @using.Alias.Name == null)
            {
                continue;
            }

            @using.Alias.Name.Identifier.ValueText.AppendToAliasNameSet(builder);
        }
    }

    private static void AppendAliasNames(IEnumerable<BaseNamespaceDeclarationSyntax> namespaces, ImmutableHashSet<string>.Builder builder, CancellationToken cancellationToken)
    {
        foreach (var @namespace in namespaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AppendAliasNames(@namespace.Usings, builder);
            AppendAliasNames(@namespace.Members.OfType<BaseNamespaceDeclarationSyntax>(), builder, cancellationToken);
        }
    }

    public ForEachSymbols GetForEachSymbols(SemanticModel semanticModel, SyntaxNode forEachStatement)
    {
        if (forEachStatement is CommonForEachStatementSyntax csforEachStatement)
        {
            var info = semanticModel.GetForEachStatementInfo(csforEachStatement);
            return new ForEachSymbols(
                info.GetEnumeratorMethod,
                info.MoveNextMethod,
                info.CurrentProperty,
                info.DisposeMethod,
                info.ElementType);
        }
        else
        {
            return default;
        }
    }

    public SymbolInfo GetCollectionInitializerSymbolInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        => semanticModel.GetCollectionInitializerSymbolInfo((ExpressionSyntax)node, cancellationToken);

    public IMethodSymbol? GetGetAwaiterMethod(SemanticModel semanticModel, SyntaxNode node)
    {
        if (node is AwaitExpressionSyntax awaitExpression)
        {
            var info = semanticModel.GetAwaitExpressionInfo(awaitExpression);
            return info.GetAwaiterMethod;
        }

        return null;
    }

    public ImmutableArray<IMethodSymbol> GetDeconstructionAssignmentMethods(SemanticModel semanticModel, SyntaxNode node)
    {
        if (node is AssignmentExpressionSyntax assignment && assignment.IsDeconstruction())
        {
            using var builder = TemporaryArray<IMethodSymbol>.Empty;
            FlattenDeconstructionMethods(semanticModel.GetDeconstructionInfo(assignment), ref builder.AsRef());
            return builder.ToImmutableAndClear();
        }

        return [];
    }

    public ImmutableArray<IMethodSymbol> GetDeconstructionForEachMethods(SemanticModel semanticModel, SyntaxNode node)
    {
        if (node is ForEachVariableStatementSyntax @foreach)
        {
            using var builder = TemporaryArray<IMethodSymbol>.Empty;
            FlattenDeconstructionMethods(semanticModel.GetDeconstructionInfo(@foreach), ref builder.AsRef());
            return builder.ToImmutableAndClear();
        }

        return [];
    }

    private static void FlattenDeconstructionMethods(DeconstructionInfo deconstruction, ref TemporaryArray<IMethodSymbol> builder)
    {
        var method = deconstruction.Method;
        builder.AddIfNotNull(method);

        foreach (var nested in deconstruction.Nested)
            FlattenDeconstructionMethods(nested, ref builder);
    }

    public bool IsPartial(INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
    {
        var syntaxRefs = typeSymbol.DeclaringSyntaxReferences;
        foreach (var syntaxRef in syntaxRefs)
        {
            var node = syntaxRef.GetSyntax(cancellationToken);
            if (node is BaseTypeDeclarationSyntax { Modifiers: { } modifiers } &&
                modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<ISymbol> GetDeclaredSymbols(
        SemanticModel semanticModel, SyntaxNode memberDeclaration, CancellationToken cancellationToken)
    {
        switch (memberDeclaration)
        {
            case FieldDeclarationSyntax field:
                return field.Declaration.Variables.Select(
                    v => semanticModel.GetRequiredDeclaredSymbol(v, cancellationToken));

            case EventFieldDeclarationSyntax eventField:
                return eventField.Declaration.Variables.Select(
                    v => semanticModel.GetRequiredDeclaredSymbol(v, cancellationToken));

            default:
                return [semanticModel.GetRequiredDeclaredSymbol(memberDeclaration, cancellationToken)];
        }
    }

    public IParameterSymbol? FindParameterForArgument(SemanticModel semanticModel, SyntaxNode argument, bool allowUncertainCandidates, bool allowParams, CancellationToken cancellationToken)
        => ((ArgumentSyntax)argument).DetermineParameter(semanticModel, allowUncertainCandidates, allowParams, cancellationToken);

    public IParameterSymbol? FindParameterForAttributeArgument(SemanticModel semanticModel, SyntaxNode argument, bool allowUncertainCandidates, bool allowParams, CancellationToken cancellationToken)
        => ((AttributeArgumentSyntax)argument).DetermineParameter(semanticModel, allowUncertainCandidates, allowParams, cancellationToken);

    // Normal arguments can't reference fields/properties in c#
    public ISymbol? FindFieldOrPropertyForArgument(SemanticModel semanticModel, SyntaxNode argument, CancellationToken cancellationToken)
        => null;

    public ISymbol? FindFieldOrPropertyForAttributeArgument(SemanticModel semanticModel, SyntaxNode argument, CancellationToken cancellationToken)
        => argument is AttributeArgumentSyntax { NameEquals.Name: var name }
            ? semanticModel.GetSymbolInfo(name, cancellationToken).GetAnySymbol()
            : null;

    public ImmutableArray<ISymbol> GetBestOrAllSymbols(SemanticModel semanticModel, SyntaxNode? node, SyntaxToken token, CancellationToken cancellationToken)
    {
        if (node == null)
            return [];

        return node switch
        {
            AssignmentExpressionSyntax _ when token.Kind() == SyntaxKind.EqualsToken => GetDeconstructionAssignmentMethods(semanticModel, node).As<ISymbol>(),
            ForEachVariableStatementSyntax _ when token.Kind() == SyntaxKind.InKeyword => GetDeconstructionForEachMethods(semanticModel, node).As<ISymbol>(),
            FunctionPointerUnmanagedCallingConventionSyntax syntax => GetCallingConventionSymbols(semanticModel, syntax),
            _ => GetSymbolInfo(semanticModel, node, token, cancellationToken),
        };

        static ImmutableArray<ISymbol> GetCallingConventionSymbols(SemanticModel model, FunctionPointerUnmanagedCallingConventionSyntax syntax)
        {
            var type = model.Compilation.TryGetCallingConventionSymbol(syntax.Name.ValueText);
            return type is null ? [] : ImmutableArray.Create<ISymbol>(type);
        }
    }

    /// <summary>
    /// Returns the best symbols found that the provided token binds to.  This is similar to <see
    /// cref="ModelExtensions.GetSymbolInfo(SemanticModel, SyntaxNode, CancellationToken)"/>, but sometimes employs
    /// heuristics to provide a better result for tokens that users conceptually think bind to things, but which the
    /// compiler does not necessarily return results for.
    /// </summary>
    private static ImmutableArray<ISymbol> GetSymbolInfo(SemanticModel semanticModel, SyntaxNode node, SyntaxToken token, CancellationToken cancellationToken)
    {
        switch (node)
        {
            case OrderByClauseSyntax orderByClauseSyntax:
                if (token.Kind() == SyntaxKind.CommaToken)
                {
                    // Returning SymbolInfo for a comma token is the last resort
                    // in an order by clause if no other tokens to bind to a are present.
                    // See also the proposal at https://github.com/dotnet/roslyn/issues/23394
                    var separators = orderByClauseSyntax.Orderings.GetSeparators().ToImmutableList();
                    var index = separators.IndexOf(token);
                    if (index >= 0 && (index + 1) < orderByClauseSyntax.Orderings.Count)
                    {
                        var ordering = orderByClauseSyntax.Orderings[index + 1];
                        if (ordering.AscendingOrDescendingKeyword.Kind() == SyntaxKind.None)
                            return semanticModel.GetSymbolInfo(ordering, cancellationToken).GetBestOrAllSymbols();
                    }
                }
                else if (orderByClauseSyntax.Orderings[0].AscendingOrDescendingKeyword.Kind() == SyntaxKind.None)
                {
                    // The first ordering is displayed on the "orderby" keyword itself if there isn't a 
                    // ascending/descending keyword.
                    return semanticModel.GetSymbolInfo(orderByClauseSyntax.Orderings[0], cancellationToken).GetBestOrAllSymbols();
                }

                return default;
            case QueryClauseSyntax queryClauseSyntax:
                var queryInfo = semanticModel.GetQueryClauseInfo(queryClauseSyntax, cancellationToken);
                var hasCastInfo = queryInfo.CastInfo.Symbol != null;
                var hasOperationInfo = queryInfo.OperationInfo.Symbol != null;

                if (hasCastInfo && hasOperationInfo)
                {
                    // In some cases a single clause binds to more than one method. In those cases 
                    // the tokens in the clause determine which of the two SymbolInfos are returned.
                    // See also the proposal at https://github.com/dotnet/roslyn/issues/23394
                    return token.IsKind(SyntaxKind.InKeyword) ? queryInfo.CastInfo.GetBestOrAllSymbols() : queryInfo.OperationInfo.GetBestOrAllSymbols();
                }

                if (hasCastInfo)
                    return queryInfo.CastInfo.GetBestOrAllSymbols();

                return queryInfo.OperationInfo.GetBestOrAllSymbols();
            case IdentifierNameSyntax { Parent: PrimaryConstructorBaseTypeSyntax baseType }:
                return semanticModel.GetSymbolInfo(baseType, cancellationToken).GetBestOrAllSymbols();
        }

        //Only in the orderby clause a comma can bind to a symbol.
        if (token.IsKind(SyntaxKind.CommaToken))
            return [];

        // If we're on 'var' then asking for the symbol-info will get us the symbol *without* nullability
        // information. Check for that, and try to return the type with nullability info if it has it.
        if (node is IdentifierNameSyntax { IsVar: true })
        {
            var symbol = semanticModel.GetSymbolInfo(node, cancellationToken).GetAnySymbol();
            var type = semanticModel.GetTypeInfo(node, cancellationToken).Type;
            if (type != null &&
                type.Equals(symbol, SymbolEqualityComparer.Default) &&
                !type.Equals(symbol, SymbolEqualityComparer.IncludeNullability))
            {
                return ImmutableArray.Create<ISymbol>(type);
            }
        }

        return semanticModel.GetSymbolInfo(node, cancellationToken).GetBestOrAllSymbols();
    }

    public bool IsInsideNameOfExpression(SemanticModel semanticModel, [NotNullWhen(true)] SyntaxNode? node, CancellationToken cancellationToken)
        => (node as ExpressionSyntax).IsInsideNameOfExpression(semanticModel, cancellationToken);

    public ImmutableArray<IMethodSymbol> GetLocalFunctionSymbols(Compilation compilation, ISymbol symbol, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var builder);
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            var node = syntaxReference.GetSyntax(cancellationToken);

            foreach (var localFunction in node.DescendantNodes().Where(CSharpSyntaxFacts.Instance.IsLocalFunctionStatement))
            {
                var localFunctionSymbol = semanticModel.GetDeclaredSymbol(localFunction, cancellationToken);
                if (localFunctionSymbol is IMethodSymbol methodSymbol)
                {
                    builder.Add(methodSymbol);
                }
            }
        }

        return builder.ToImmutableAndClear();
    }

    public bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, [NotNullWhen(true)] INamedTypeSymbol? expressionType, CancellationToken cancellationToken)
        => node.IsInExpressionTree(semanticModel, expressionType, cancellationToken);

    public string GenerateNameForExpression(SemanticModel semanticModel, SyntaxNode expression, bool capitalize, CancellationToken cancellationToken)
        => semanticModel.GenerateNameForExpression((ExpressionSyntax)expression, capitalize, cancellationToken);

#if !CODE_STYLE

    public async Task<ISymbol?> GetInterceptorSymbolAsync(Document document, int position, CancellationToken cancellationToken)
    {
        // Have to be on an invocation name.

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root.FullSpan.Contains(position))
            return null;

        var token = root.FindToken(position);
        if (!token.IsKind(SyntaxKind.IdentifierToken) ||
            token.Parent is not SimpleNameSyntax simpleName)
        {
            return null;
        }

        // Supported syntax points for interception in v1 are:
        //
        // Goo()
        // X.Goo()
        // X?.Goo()
        var expression = simpleName.Parent switch
        {
            MemberAccessExpressionSyntax memberAccess when memberAccess.Name == simpleName => memberAccess,
            MemberBindingExpressionSyntax memberBinding when memberBinding.Name == simpleName => memberBinding,
            _ => (ExpressionSyntax)simpleName,
        };

        if (expression.Parent is not InvocationExpressionSyntax)
            return null;

        var contentHash = await document.GetContentHashAsync(cancellationToken).ConfigureAwait(false);
        var interceptsLocationData = new InterceptsLocationData(contentHash, simpleName.FullSpan.Start);

        // We only look for interceptors in generated source documents.  Interceptors cannot reasonably be written by
        // hand (as they involve embedded an encoded version of a file's content hash, position, and other debugging
        // information).  So the only realistic way to create them is by asking the compiler to create the attribute
        // using SemanticModel.GetInterceptableLocation as part of a generator.
        foreach (var generatedDocument in await document.Project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
        {
            var syntaxIndex = await generatedDocument.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);
            if (!syntaxIndex.TryGetInterceptsLocation(interceptsLocationData, out var methodDeclarationSpan))
                continue;

            var generatedRoot = await generatedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!generatedRoot.FullSpan.Contains(methodDeclarationSpan))
                continue;

            var methodDeclaration = generatedRoot.FindNode(methodDeclarationSpan);
            var semanticModel = await generatedDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
        }

        return null;
    }

#endif
}
