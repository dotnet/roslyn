// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        private bool ValidateLambdaParameterNameConflictsInScope(Location location, string name, BindingDiagnosticBag diagnostics)
        {
            return ValidateNameConflictsInScope(null, location, name, diagnostics);
        }

        internal bool ValidateDeclarationNameConflictsInScope(Symbol symbol, BindingDiagnosticBag diagnostics)
        {
            Location location = GetLocation(symbol);
            return ValidateNameConflictsInScope(symbol, location, symbol.Name, diagnostics);
        }

        private static Location GetLocation(Symbol symbol)
            => symbol.TryGetFirstLocation() ?? symbol.ContainingSymbol.GetFirstLocation();

        internal void ValidateParameterNameConflicts(
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<ParameterSymbol> parameters,
            bool allowShadowingNames,
            BindingDiagnosticBag diagnostics)
        {
            PooledDictionary<string, TypeParameterSymbol>? tpNames = null;
            if (!typeParameters.IsDefaultOrEmpty)
            {
                tpNames = PooledDictionary<string, TypeParameterSymbol>.GetInstance();
                foreach (var tp in typeParameters)
                {
                    var name = tp.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    if (!tpNames.TryAdd(name, tp))
                    {
                        // Type parameter declaration name conflicts are detected elsewhere
                    }
                    else if (!allowShadowingNames)
                    {
                        ValidateDeclarationNameConflictsInScope(tp, diagnostics);
                    }
                }
            }

            PooledHashSet<string>? pNames = null;
            if (!parameters.IsDefaultOrEmpty)
            {
                pNames = PooledHashSet<string>.GetInstance();
                foreach (var p in parameters)
                {
                    var name = p.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    if (tpNames != null && tpNames.TryGetValue(name, out TypeParameterSymbol? tp))
                    {
                        if (tp.ContainingSymbol is NamedTypeSymbol { IsExtension: true })
                        {
                            if (p.ContainingSymbol != (object)tp.ContainingSymbol) // Otherwise, SynthesizedExtensionMarker is going to report an error about this conflict
                            {
                                diagnostics.Add(ErrorCode.ERR_LocalSameNameAsExtensionTypeParameter, GetLocation(p), name);
                            }
                        }
                        else if (p.IsExtensionParameter())
                        {
                            diagnostics.Add(ErrorCode.ERR_TypeParameterSameNameAsExtensionParameter, tp.GetFirstLocationOrNone(), name);
                        }
                        else
                        {
                            // CS0412: 'X': a parameter or local variable cannot have the same name as a method type parameter
                            diagnostics.Add(ErrorCode.ERR_LocalSameNameAsTypeParam, GetLocation(p), name);
                        }
                    }

                    if (!pNames.Add(name))
                    {
                        if (parameters[0] is { ContainingSymbol: NamedTypeSymbol { IsExtension: true }, Name: var receiverName } && receiverName == name)
                        {
                            diagnostics.Add(ErrorCode.ERR_LocalSameNameAsExtensionParameter, GetLocation(p), name);
                        }
                        else
                        {
                            // The parameter name '{0}' is a duplicate
                            diagnostics.Add(ErrorCode.ERR_DuplicateParamName, GetLocation(p), name);
                        }
                    }
                    else if (!allowShadowingNames)
                    {
                        ValidateDeclarationNameConflictsInScope(p, diagnostics);
                    }
                }
            }

            tpNames?.Free();
            pNames?.Free();
        }

        /// <remarks>
        /// Don't call this one directly - call one of the helpers.
        /// </remarks>
        private bool ValidateNameConflictsInScope(Symbol? symbol, Location location, string name, BindingDiagnosticBag diagnostics)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            bool allowShadowing = Compilation.IsFeatureEnabled(MessageID.IDS_FeatureNameShadowingInNestedFunctions);

            for (Binder? binder = this; binder != null; binder = binder.Next)
            {
                // no local scopes enclose members
                if (binder is InContainerBinder)
                {
                    return false;
                }

                var scope = binder as LocalScopeBinder;
                if (scope?.EnsureSingleDefinition(symbol, name, location, diagnostics) == true)
                {
                    return true;
                }

                // If shadowing is enabled, avoid checking for conflicts outside of local functions or lambdas.
                if (allowShadowing && binder.IsNestedFunctionBinder)
                {
                    return false;
                }

                if (binder.IsLastBinderWithinMember())
                {
                    // Declarations within a member do not conflict with declarations outside.
                    return false;
                }
            }

            return false;
        }

        private bool IsLastBinderWithinMember()
        {
            var containingMemberOrLambda = this.ContainingMemberOrLambda;

            switch (containingMemberOrLambda?.Kind)
            {
                case null:
                case SymbolKind.NamedType:
                case SymbolKind.Namespace:
                    return true;
                default:
                    return containingMemberOrLambda.ContainingSymbol?.Kind == SymbolKind.NamedType &&
                           this.Next?.ContainingMemberOrLambda != containingMemberOrLambda;
            }
        }
    }
}
