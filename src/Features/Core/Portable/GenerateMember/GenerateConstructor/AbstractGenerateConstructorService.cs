// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax> :
        IGenerateConstructorService
        where TService : AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
        where TArgumentSyntax : SyntaxNode
        where TAttributeArgumentSyntax : SyntaxNode
    {
        protected abstract bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType);
        protected abstract bool IsSimpleNameGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool IsConstructorInitializerGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);

        protected abstract bool TryInitializeSimpleNameGenerationState(SemanticDocument document, SyntaxNode simpleName, CancellationToken cancellationToken, out SyntaxToken token, out ImmutableArray<TArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn);
        protected abstract bool TryInitializeConstructorInitializerGeneration(SemanticDocument document, SyntaxNode constructorInitializer, CancellationToken cancellationToken, out SyntaxToken token, out ImmutableArray<TArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn);
        protected abstract bool TryInitializeSimpleAttributeNameGenerationState(SemanticDocument document, SyntaxNode simpleName, CancellationToken cancellationToken, out SyntaxToken token, out ImmutableArray<TArgumentSyntax> arguments, out ImmutableArray<TAttributeArgumentSyntax> attributeArguments, out INamedTypeSymbol typeToGenerateIn);
        protected abstract ImmutableArray<ParameterName> GenerateParameterNames(SemanticModel semanticModel, IEnumerable<TArgumentSyntax> arguments, IList<string> reservedNames, NamingRule parameterNamingRule, CancellationToken cancellationToken);

        protected abstract string GenerateNameForArgument(SemanticModel semanticModel, TArgumentSyntax argument, CancellationToken cancellationToken);
        protected abstract RefKind GetRefKind(TArgumentSyntax argument);
        protected abstract bool IsNamedArgument(TArgumentSyntax argument);
        protected abstract ITypeSymbol GetArgumentType(SemanticModel semanticModel, TArgumentSyntax argument, CancellationToken cancellationToken);

        protected abstract bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType);

        protected abstract IMethodSymbol GetDelegatingConstructor(State state, SemanticDocument document, int argumentCount, INamedTypeSymbol namedType, ISet<IMethodSymbol> candidates, CancellationToken cancellationToken);
        protected abstract IMethodSymbol GetCurrentConstructor(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken);
        protected abstract IMethodSymbol GetDelegatedConstructor(SemanticModel semanticModel, IMethodSymbol constructor, CancellationToken cancellationToken);

        protected virtual string GenerateNameForArgument(SemanticModel semanticModel, TAttributeArgumentSyntax argument, CancellationToken cancellationToken) => null;
        protected virtual ImmutableArray<ParameterName> GenerateParameterNames(SemanticModel semanticModel, IEnumerable<TAttributeArgumentSyntax> arguments, IList<string> reservedNames, NamingRule parameternamingRule, CancellationToken cancellationToken) => default;
        protected virtual ITypeSymbol GetAttributeArgumentType(SemanticModel semanticModel, TAttributeArgumentSyntax argument, CancellationToken cancellationToken) => null;

        protected bool CanDelegeteThisConstructor(State state, SemanticDocument document, IMethodSymbol delegatedConstructor, CancellationToken cancellationToken = default)
        {
            var currentConstructor = GetCurrentConstructor(document.SemanticModel, state.Token, cancellationToken);
            if (currentConstructor.Equals(delegatedConstructor))
                return false;

            // We need ensure that delegating constructor won't cause circular dependency.
            // The chain of dependency can not exceed the number for constructors
            var constructorsCount = delegatedConstructor.ContainingType.InstanceConstructors.Length;
            for (var i = 0; i < constructorsCount; i++)
            {
                delegatedConstructor = GetDelegatedConstructor(document.SemanticModel, delegatedConstructor, cancellationToken);
                if (delegatedConstructor == null)
                    return true;

                if (delegatedConstructor.Equals(currentConstructor))
                    return false;
            }

            return false;
        }

        public async Task<ImmutableArray<CodeAction>> GenerateConstructorAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateConstructor, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var state = await State.GenerateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
                if (state != null)
                {
                    using var _ = ArrayBuilder<CodeAction>.GetInstance(out var result);

                    // If we have any fields we'd like to generate, offer a code action to do that.
                    if (state.ParameterToNewFieldMap.Count > 0)
                    {
                        result.Add(new MyCodeAction(
                            string.Format(FeaturesResources.Generate_constructor_in_0_with_fields, state.TypeToGenerateIn.Name),
                            c => state.GetChangedDocumentAsync(document, withFields: true, withProperties: false, c)));
                    }

                    // Same with a version that generates properties instead.
                    if (state.ParameterToNewPropertyMap.Count > 0)
                    {
                        result.Add(new MyCodeAction(
                            string.Format(FeaturesResources.Generate_constructor_in_0_with_properties, state.TypeToGenerateIn.Name),
                            c => state.GetChangedDocumentAsync(document, withFields: false, withProperties: true, c)));
                    }

                    // Always offer to just generate the constructor and nothing else.
                    result.Add(new MyCodeAction(
                        string.Format(FeaturesResources.Generate_constructor_in_0, state.TypeToGenerateIn.Name),
                        c => state.GetChangedDocumentAsync(document, withFields: false, withProperties: false, c)));

                    return result.ToImmutable();
                }
            }

            return ImmutableArray<CodeAction>.Empty;
        }

        protected static bool IsSymbolAccessible(ISymbol symbol, SemanticDocument document)
        {
            if (symbol == null)
            {
                return false;
            }

            if (symbol.Kind == SymbolKind.Property)
            {
                if (!IsSymbolAccessible(((IPropertySymbol)symbol).SetMethod, document))
                {
                    return false;
                }
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

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
