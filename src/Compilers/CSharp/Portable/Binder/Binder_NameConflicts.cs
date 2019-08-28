// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        private bool ValidateLambdaParameterNameConflictsInScope(Location location, string name, DiagnosticBag diagnostics)
        {
            return ValidateNameConflictsInScope(null, location, name, diagnostics);
        }

        internal bool ValidateDeclarationNameConflictsInScope(Symbol symbol, DiagnosticBag diagnostics)
        {
            Location location = GetLocation(symbol);
            return ValidateNameConflictsInScope(symbol, location, symbol.Name, diagnostics);
        }

        private static Location GetLocation(Symbol symbol)
        {
            var locations = symbol.Locations;
            return locations.Length != 0 ? locations[0] : symbol.ContainingSymbol.Locations[0];
        }

        internal void ValidateParameterNameConflicts(
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<ParameterSymbol> parameters,
            bool allowShadowingNames,
            DiagnosticBag diagnostics)
        {
            PooledHashSet<string> tpNames = null;
            if (!typeParameters.IsDefaultOrEmpty)
            {
                tpNames = PooledHashSet<string>.GetInstance();
                foreach (var tp in typeParameters)
                {
                    var name = tp.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    if (!tpNames.Add(name))
                    {
                        // Type parameter declaration name conflicts are detected elsewhere
                    }
                    else if (!allowShadowingNames)
                    {
                        ValidateDeclarationNameConflictsInScope(tp, diagnostics);
                    }
                }
            }

            PooledHashSet<string> pNames = null;
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

                    if (tpNames != null && tpNames.Contains(name))
                    {
                        // CS0412: 'X': a parameter or local variable cannot have the same name as a method type parameter
                        diagnostics.Add(ErrorCode.ERR_LocalSameNameAsTypeParam, GetLocation(p), name);
                    }

                    if (!pNames.Add(name))
                    {
                        // The parameter name '{0}' is a duplicate
                        diagnostics.Add(ErrorCode.ERR_DuplicateParamName, GetLocation(p), name);
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
        private bool ValidateNameConflictsInScope(Symbol symbol, Location location, string name, DiagnosticBag diagnostics)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            bool allowShadowing = Compilation.IsFeatureEnabled(MessageID.IDS_FeatureNameShadowingInNestedFunctions);

            for (Binder binder = this; binder != null; binder = binder.Next)
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
            }

            return false;
        }
    }
}
