// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal static class GenerateConstructorHelpers
    {
        public static IMethodSymbol? FindConstructorToDelegateTo(
            Compilation compilation,
            INamedTypeSymbol typeToGenerateIn,
            bool includeBaseType,
            ImmutableArray<IParameterSymbol> allParameters,
            Func<IMethodSymbol, bool> canDelegateToConstructor)
        {
            for (var i = allParameters.Length; i >= 0; i--)
            {
                var parameters = allParameters.Take(i).ToImmutableArray();
                var result = FindConstructorToDelegateTo(compilation, parameters, typeToGenerateIn.InstanceConstructors, canDelegateToConstructor);
                if (result != null)
                    return result;

                if (includeBaseType && typeToGenerateIn.BaseType != null)
                {
                    result = FindConstructorToDelegateTo(compilation, parameters, typeToGenerateIn.BaseType.InstanceConstructors, canDelegateToConstructor);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        private static IMethodSymbol FindConstructorToDelegateTo(
            Compilation compilation,
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<IMethodSymbol> constructors,
            Func<IMethodSymbol, bool> canDelegateToConstructor)
        {
            // Look for constructors in this specified type that are:
            // 1. Non-implicit.  We don't want to add `: base()` as that's just redundant for subclasses and `:
            //    this()` won't even work as we won't have an implicit constructor once we add this new constructor.
            // 2. Accessible.  We obviously need our constructor to be able to call that other constructor.
            // 3. Won't cause a cycle.  i.e. if we're generating a new constructor from an existing constructor,
            //    then we don't want it calling back into us.
            // 4. Are compatible with the parameters we're generating for this constructor.  Compatible means there
            //    exists an implicit conversion from the new constructor's parameter types to the existing
            //    constructor's parameter types.
            var delegatedConstructor = constructors
                .Where(c => c.Parameters.Length == parameters.Length)
                .Where(c => c.Parameters.SequenceEqual(parameters, (p1, p2) => p1.RefKind == p2.RefKind))
                .Where(c => IsSymbolAccessible(compilation, c))
                .Where(c => !c.IsImplicitlyDeclared)
                .Where(canDelegateToConstructor)
                .Where(c => IsCompatible(compilation, c, parameters))
                .FirstOrDefault();

            return delegatedConstructor;
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

        private static bool IsCompatible(
            Compilation compilation,
            IMethodSymbol constructor,
            ImmutableArray<IParameterSymbol> parameters)
        {
            Debug.Assert(constructor.Parameters.Length == parameters.Length);

            for (var i = 0; i < constructor.Parameters.Length; i++)
            {
                var constructorParameter = constructor.Parameters[i];
                var conversion = compilation.ClassifyCommonConversion(parameters[i].Type, constructorParameter.Type);
                if (!conversion.IsIdentity && !conversion.IsImplicit)
                    return false;
            }

            return true;
        }

        public static IMethodSymbol? GetDelegatingConstructor(
            SemanticDocument document,
            SymbolInfo symbolInfo,
            ISet<IMethodSymbol> candidateInstanceConstructors,
            INamedTypeSymbol containingType,
            IList<ITypeSymbol> parameterTypes)
        {
            var symbol = symbolInfo.Symbol as IMethodSymbol;
            if (symbol == null && symbolInfo.CandidateSymbols.Length == 1)
            {
                // Even though the symbol info has a non-viable candidate symbol, we are trying 
                // to speculate a base constructor invocation from a different position then 
                // where the invocation to it would be generated. Passed in candidateInstanceConstructors 
                // actually represent all accessible and invocable constructor symbols. So, we allow 
                // candidate symbol for inaccessible OR not creatable candidate reason if it is in 
                // the given candidateInstanceConstructors.
                //
                // Note: if we get either of these cases, we ensure that we can at least convert 
                // the parameter types we have to the constructor parameter types.  This way we
                // don't accidentally think we delegate to a constructor in an abstract base class
                // when the parameter types don't match.
                if (symbolInfo.CandidateReason == CandidateReason.Inaccessible ||
                    (symbolInfo.CandidateReason == CandidateReason.NotCreatable && containingType.IsAbstract))
                {
                    var method = symbolInfo.CandidateSymbols.Single() as IMethodSymbol;
                    if (ParameterTypesMatch(document, parameterTypes, method))
                    {
                        symbol = method;
                    }
                }
            }

            if (symbol != null && candidateInstanceConstructors.Contains(symbol))
            {
                return symbol;
            }

            return null;
        }

        private static bool ParameterTypesMatch(SemanticDocument semanticDocument, IList<ITypeSymbol> parameterTypes, IMethodSymbol? method)
        {
            if (method == null)
            {
                return false;
            }

            if (parameterTypes.Count != method.Parameters.Length)
            {
                return false;
            }

            var compilation = semanticDocument.SemanticModel.Compilation;

            for (var i = 0; i < parameterTypes.Count; i++)
            {
                var type1 = parameterTypes[i];
                if (type1 != null)
                {
                    var type2 = method.Parameters[i].Type;

                    if (!compilation.HasImplicitConversion(fromType: type1, toType: type2))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
