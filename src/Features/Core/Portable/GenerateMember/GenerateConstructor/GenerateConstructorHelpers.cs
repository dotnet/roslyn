// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal static class GenerateConstructorHelpers
    {
        public static bool CanDelegateTo<TExpressionSyntax>(
            SemanticDocument document,
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<TExpressionSyntax> expressions,
            IMethodSymbol constructor)
            where TExpressionSyntax : SyntaxNode
        {
            // Look for constructors in this specified type that are:
            // 1. Accessible.  We obviously need our constructor to be able to call that other constructor.
            // 2. Won't cause a cycle.  i.e. if we're generating a new constructor from an existing constructor,
            //    then we don't want it calling back into us.
            // 3. Are compatible with the parameters we're generating for this constructor.  Compatible means there
            //    exists an implicit conversion from the new constructor's parameter types to the existing
            //    constructor's parameter types.
            var semanticFacts = document.Document.GetRequiredLanguageService<ISemanticFactsService>();
            var semanticModel = document.SemanticModel;
            var compilation = semanticModel.Compilation;

            return constructor.Parameters.Length == parameters.Length &&
                   constructor.Parameters.SequenceEqual(parameters, (p1, p2) => p1.RefKind == p2.RefKind) &&
                   IsSymbolAccessible(compilation, constructor) &&
                   IsCompatible(semanticFacts, semanticModel, constructor, expressions);
        }

        private static bool IsSymbolAccessible(Compilation compilation, ISymbol symbol)
        {
            if (symbol == null)
                return false;

            if (symbol is IPropertySymbol { SetMethod: { } setMethod } property &&
                !IsSymbolAccessible(compilation, setMethod))
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
                    return compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(symbol.ContainingAssembly);

                default:
                    return false;
            }
        }

        private static bool IsCompatible<TExpressionSyntax>(
            ISemanticFactsService semanticFacts,
            SemanticModel semanticModel,
            IMethodSymbol constructor,
            ImmutableArray<TExpressionSyntax> expressions)
            where TExpressionSyntax : SyntaxNode
        {
            Debug.Assert(constructor.Parameters.Length == expressions.Length);

            // Resolve the constructor into our semantic model's compilation; if the constructor we're looking at is from
            // another project with a different language.
            var constructorInCompilation = (IMethodSymbol?)SymbolKey.Create(constructor).Resolve(semanticModel.Compilation).Symbol;
            Contract.ThrowIfNull(constructorInCompilation);

            for (var i = 0; i < constructorInCompilation.Parameters.Length; i++)
            {
                var constructorParameter = constructorInCompilation.Parameters[i];
                if (constructorParameter == null)
                    return false;

                var conversion = semanticFacts.ClassifyConversion(semanticModel, expressions[i], constructorParameter.Type);
                if (!conversion.IsIdentity && !conversion.IsImplicit)
                    return false;
            }

            return true;
        }
    }
}
