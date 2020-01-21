// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        protected AbstractGenerateConstructorService()
        {
        }

        protected abstract bool IsSimpleNameGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool IsConstructorInitializerGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);

        protected abstract bool TryInitializeSimpleNameGenerationState(SemanticDocument document, SyntaxNode simpleName, CancellationToken cancellationToken, out SyntaxToken token, out ImmutableArray<TArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn);
        protected abstract bool TryInitializeConstructorInitializerGeneration(SemanticDocument document, SyntaxNode constructorInitializer, CancellationToken cancellationToken, out SyntaxToken token, out ImmutableArray<TArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn);
        protected abstract bool TryInitializeSimpleAttributeNameGenerationState(SemanticDocument document, SyntaxNode simpleName, CancellationToken cancellationToken, out SyntaxToken token, out ImmutableArray<TArgumentSyntax> arguments, out ImmutableArray<TAttributeArgumentSyntax> attributeArguments, out INamedTypeSymbol typeToGenerateIn);
        protected abstract ImmutableArray<ParameterName> GenerateParameterNames(SemanticModel semanticModel, IEnumerable<TArgumentSyntax> arguments, IList<string> reservedNames, NamingRule parameterNamingRule, CancellationToken cancellationToken);
        protected virtual ImmutableArray<ParameterName> GenerateParameterNames(SemanticModel semanticModel, IEnumerable<TAttributeArgumentSyntax> arguments, IList<string> reservedNames, NamingRule parameternamingRule, CancellationToken cancellationToken)
            => default;

        protected abstract string GenerateNameForArgument(SemanticModel semanticModel, TArgumentSyntax argument, CancellationToken cancellationToken);
        protected virtual string GenerateNameForArgument(SemanticModel semanticModel, TAttributeArgumentSyntax argument, CancellationToken cancellationToken) { return null; }
        protected abstract RefKind GetRefKind(TArgumentSyntax argument);
        protected abstract bool IsNamedArgument(TArgumentSyntax argument);
        protected abstract ITypeSymbol GetArgumentType(SemanticModel semanticModel, TArgumentSyntax argument, CancellationToken cancellationToken);
        protected virtual ITypeSymbol GetAttributeArgumentType(SemanticModel semanticModel, TAttributeArgumentSyntax argument, CancellationToken cancellationToken) { return null; }

        public async Task<ImmutableArray<CodeAction>> GenerateConstructorAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateConstructor, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var state = await State.GenerateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
                if (state != null)
                {
                    var result = ArrayBuilder<CodeAction>.GetInstance();
                    var codeAction = new GenerateConstructorCodeAction((TService)this, document, state, withFields: true);
                    result.Add(codeAction);

                    // First see the type of edit our regular code action would create.  If it 
                    // creates fields, then also offer to perform the code action without creating
                    // any fields.
                    var edit = await codeAction.GetEditAsync(cancellationToken).ConfigureAwait(false);
                    if (edit.addedFields)
                    {
                        result.Add(
                            new GenerateConstructorCodeAction((TService)this, document, state, withFields: false));
                    }

                    return result.ToImmutableAndFree();
                }
            }

            return ImmutableArray<CodeAction>.Empty;
        }

        protected static bool IsSymbolAccessible(
            ISymbol symbol, SemanticDocument document)
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
    }
}
