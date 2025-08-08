// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;

internal abstract partial class AbstractGenerateConstructorService<TService, TExpressionSyntax> :
    IGenerateConstructorService
    where TService : AbstractGenerateConstructorService<TService, TExpressionSyntax>
    where TExpressionSyntax : SyntaxNode
{
    protected abstract bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType);
    protected abstract bool IsSimpleNameGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);
    protected abstract bool IsConstructorInitializerGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);
    protected abstract bool IsImplicitObjectCreation(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);

    protected abstract bool TryInitializeImplicitObjectCreation(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken, out SyntaxToken token, out ImmutableArray<Argument<TExpressionSyntax>> arguments, [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn);
    protected abstract bool TryInitializeSimpleNameGenerationState(SemanticDocument document, SyntaxNode simpleName, CancellationToken cancellationToken, out SyntaxToken token, out ImmutableArray<Argument<TExpressionSyntax>> arguments, [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn);
    protected abstract bool TryInitializeConstructorInitializerGeneration(SemanticDocument document, SyntaxNode constructorInitializer, CancellationToken cancellationToken, out SyntaxToken token, out ImmutableArray<Argument<TExpressionSyntax>> arguments, [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn);
    protected abstract bool TryInitializeSimpleAttributeNameGenerationState(SemanticDocument document, SyntaxNode simpleName, CancellationToken cancellationToken, out SyntaxToken token, out ImmutableArray<Argument<TExpressionSyntax>> arguments, [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn);

    protected abstract ITypeSymbol GetArgumentType(SemanticModel semanticModel, Argument<TExpressionSyntax> argument, CancellationToken cancellationToken);
    protected abstract string GenerateNameForExpression(SemanticModel semanticModel, TExpressionSyntax expression, CancellationToken cancellationToken);

    protected abstract bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType);

    protected abstract IMethodSymbol? GetCurrentConstructor(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken);
    protected abstract IMethodSymbol? GetDelegatedConstructor(SemanticModel semanticModel, IMethodSymbol constructor, CancellationToken cancellationToken);

    protected bool WillCauseConstructorCycle(State state, SemanticDocument document, IMethodSymbol delegatedConstructor, CancellationToken cancellationToken)
    {
        // Check if we're in a constructor.  If not, then we can always have our new constructor delegate to
        // another, as it can't cause a cycle.
        var currentConstructor = GetCurrentConstructor(document.SemanticModel, state.Token, cancellationToken);
        if (currentConstructor == null)
            return false;

        // If we're delegating to the constructor we're currently in, that would cause a cycle.
        if (currentConstructor.Equals(delegatedConstructor))
            return true;

        // Delegating to a constructor in the base type can't cause a cycle
        if (!delegatedConstructor.ContainingType.Equals(currentConstructor.ContainingType))
            return false;

        // We need ensure that delegating constructor won't cause circular dependency.
        // The chain of dependency can not exceed the number for constructors
        var constructorsCount = delegatedConstructor.ContainingType.InstanceConstructors.Length;
        var currentDelegatedConstructor = delegatedConstructor;
        for (var i = 0; i < constructorsCount; i++)
        {
            currentDelegatedConstructor = GetDelegatedConstructor(document.SemanticModel, currentDelegatedConstructor, cancellationToken);
            if (currentDelegatedConstructor == null)
                return false;

            if (currentDelegatedConstructor.Equals(currentConstructor))
                return true;
        }

        return true;
    }

    public async Task<ImmutableArray<CodeAction>> GenerateConstructorAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateConstructor, cancellationToken))
        {
            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var state = await State.GenerateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
            if (state != null)
            {
                Contract.ThrowIfNull(state.TypeToGenerateIn);

                using var result = TemporaryArray<CodeAction>.Empty;

                // If we have any fields we'd like to generate, offer a code action to do that.
                if (state.ParameterToNewFieldMap.Count > 0)
                {
                    result.Add(CodeAction.Create(
                        string.Format(CodeFixesResources.Generate_constructor_in_0_with_fields, state.TypeToGenerateIn.Name),
                        c => state.GetChangedDocumentAsync(document, withFields: true, withProperties: false, c),
                        nameof(CodeFixesResources.Generate_constructor_in_0_with_fields) + "_" + state.TypeToGenerateIn.Name));
                }

                // Same with a version that generates properties instead.
                if (state.ParameterToNewPropertyMap.Count > 0)
                {
                    result.Add(CodeAction.Create(
                        string.Format(CodeFixesResources.Generate_constructor_in_0_with_properties, state.TypeToGenerateIn.Name),
                        c => state.GetChangedDocumentAsync(document, withFields: false, withProperties: true, c),
                        nameof(CodeFixesResources.Generate_constructor_in_0_with_properties) + "_" + state.TypeToGenerateIn.Name));
                }

                // Always offer to just generate the constructor and nothing else.
                result.Add(CodeAction.Create(
                    string.Format(CodeFixesResources.Generate_constructor_in_0, state.TypeToGenerateIn.Name),
                    c => state.GetChangedDocumentAsync(document, withFields: false, withProperties: false, c),
                    nameof(CodeFixesResources.Generate_constructor_in_0) + "_" + state.TypeToGenerateIn.Name));

                return result.ToImmutableAndClear();
            }
        }

        return [];
    }

    protected static bool IsSymbolAccessible(ISymbol? symbol, SemanticDocument document)
    {
        if (symbol == null)
        {
            return false;
        }

        if (symbol is IPropertySymbol property &&
            !IsSymbolAccessible(property.SetMethod, document))
        {
            return false;
        }

        // Public and protected constructors are accessible.  Internal constructors are
        // accessible if we have friend access.  We can't call the normal accessibility
        // checkers since they will think that a protected constructor isn't accessible
        // (since we don't have the destination type that would have access to them yet).
        switch (symbol.DeclaredAccessibility)
        {
            case Accessibility.ProtectedOrInternal:
            case Accessibility.Protected:
            case Accessibility.Public:
                return true;
            case Accessibility.ProtectedAndInternal:
            case Accessibility.Internal:
                return document.SemanticModel.Compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(
                    symbol.ContainingAssembly);

            default:
                return false;
        }
    }

    protected string GenerateNameForArgument(SemanticModel semanticModel, Argument<TExpressionSyntax> argument, CancellationToken cancellationToken)
    {
        // If it named argument then we use the name provided.
        if (argument.IsNamed)
            return argument.Name;

        if (argument.Expression is null)
            return ITypeSymbolExtensions.DefaultParameterName;

        var name = this.GenerateNameForExpression(semanticModel, argument.Expression, cancellationToken);
        return string.IsNullOrEmpty(name) ? ITypeSymbolExtensions.DefaultParameterName : name;
    }

    private ImmutableArray<ParameterName> GenerateParameterNames(
        SemanticDocument document, IEnumerable<Argument<TExpressionSyntax>> arguments, IList<string> reservedNames, NamingRule parameterNamingRule, CancellationToken cancellationToken)
    {
        reservedNames ??= SpecializedCollections.EmptyList<string>();

        // We can't change the names of named parameters.  Any other names we're flexible on.
        var isFixed = reservedNames.Select(s => true).Concat(
            arguments.Select(a => a.IsNamed)).ToImmutableArray();

        var parameterNames = reservedNames.Concat(
            arguments.Select(a => this.GenerateNameForArgument(document.SemanticModel, a, cancellationToken))).ToImmutableArray();

        var syntaxFacts = document.Document.GetRequiredLanguageService<ISyntaxFactsService>();
        var comparer = syntaxFacts.StringComparer;
        return [.. NameGenerator.EnsureUniqueness(parameterNames, isFixed, canUse: s => !reservedNames.Any(n => comparer.Equals(s, n)))
            .Select((name, index) => new ParameterName(name, isFixed[index], parameterNamingRule))
            .Skip(reservedNames.Count)];
    }
}
