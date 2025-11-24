// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal static partial class ExtensionMethodImportCompletionHelper
{
    private sealed partial class SymbolComputer
    {
        private static bool CheckConstraints(ITypeSymbol receiverTypeSymbol, ITypeParameterSymbol typeParameter)
        {
            // An extension on a method type parameter.  These often have constraints on them.  Ensure that the
            // receiver feels at least plausibly usable as the argument.
            if (!SatisfiesBaseTypeConstraint(receiverTypeSymbol, typeParameter))
                return false;

            if (!SatisfiesInterfaceConstraint(receiverTypeSymbol, typeParameter))
                return false;

            // Note: we could add more checks here.  Like class/struct/new()/unmanaged/etc. constraints
            return true;
        }

        private static bool SatisfiesInterfaceConstraint(ITypeSymbol receiverTypeSymbol, ITypeParameterSymbol typeParameter)
            => CheckConstraints(receiverTypeSymbol, typeParameter, TypeKind.Interface, static type => type.GetAllInterfacesIncludingThis());

        private static bool SatisfiesBaseTypeConstraint(ITypeSymbol receiverTypeSymbol, ITypeParameterSymbol typeParameter)
            => CheckConstraints(receiverTypeSymbol, typeParameter, TypeKind.Class, static type => type.GetBaseTypesAndThis());

        private static IEnumerable<ITypeSymbol> GetAllTypeParameterConstraintTypes(
            ITypeParameterSymbol typeParameter,
            TypeKind typeKind,
            Func<ITypeSymbol, IEnumerable<ITypeSymbol>> getInheritanceTypes)
        {
            using var _ = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var typeParameterStack);
            typeParameterStack.Push(typeParameter);

            while (typeParameterStack.TryPop(out var currentTypeParameter))
            {
                foreach (var constraintType in currentTypeParameter.ConstraintTypes)
                {
                    // Type parameter is constrained to another type parameter, add that other parameter to the list
                    // to check after this one.
                    if (constraintType is ITypeParameterSymbol toCheck)
                    {
                        typeParameterStack.Push(toCheck);
                        continue;
                    }

                    if (constraintType.TypeKind == typeKind)
                    {
                        foreach (var baseType in GetAllTypes(constraintType, typeKind, getInheritanceTypes))
                            yield return baseType;
                    }
                }
            }
        }

        private static bool CheckConstraints(
            ITypeSymbol receiverTypeSymbol,
            ITypeParameterSymbol typeParameter,
            TypeKind constraintTypeKind,
            Func<ITypeSymbol, IEnumerable<ITypeSymbol>> getInheritanceTypes)
        {
            using var _ = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var typeParameterStack);
            typeParameterStack.Push(typeParameter);

            while (typeParameterStack.TryPop(out var currentTypeParameter))
            {
                foreach (var constraintType in currentTypeParameter.ConstraintTypes)
                {
                    // Type parameter is constrained to another type parameter, add that other parameter to the list
                    // to check after this one.
                    if (constraintType is ITypeParameterSymbol toCheck)
                    {
                        typeParameterStack.Push(toCheck);
                        continue;
                    }

                    if (constraintType.TypeKind == constraintTypeKind)
                    {
                        var originalConstraintType = constraintType.OriginalDefinition;
                        foreach (var type in GetAllTypes(receiverTypeSymbol, constraintTypeKind, getInheritanceTypes))
                        {
                            if (type.OriginalDefinition.Equals(originalConstraintType))
                                return true;
                        }

                        // Receiver type didn't derive from (and wasn't) the constraint type.
                        return false;
                    }
                }
            }

            return true;
        }

        private static IEnumerable<ITypeSymbol> GetAllTypes(
            ITypeSymbol type, TypeKind typeKind, Func<ITypeSymbol, IEnumerable<ITypeSymbol>> getInheritanceTypes)
        {
            if (type is ITypeParameterSymbol typeParameter)
            {
                // We have a a type parameter.  We have to walk through its constraints (which may be other type parameters)
                // to find all the named types in its inheritance hierarchy.
                return GetAllTypeParameterConstraintTypes(typeParameter, typeKind, getInheritanceTypes);
            }
            else
            {
                // We have a named type.  Just get the inheritance types directly from it.
                return getInheritanceTypes(type);
            }
        }
    }
}
