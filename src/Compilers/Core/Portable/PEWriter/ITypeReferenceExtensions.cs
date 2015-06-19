// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using System.Text;

namespace Microsoft.Cci
{
    internal static class ITypeReferenceExtensions
    {
        internal static void GetConsolidatedTypeArguments(this ITypeReference typeReference, ArrayBuilder<ITypeReference> consolidatedTypeArguments, EmitContext context)
        {
            INestedTypeReference nestedTypeReference = typeReference.AsNestedTypeReference;
            nestedTypeReference?.GetContainingType(context).GetConsolidatedTypeArguments(consolidatedTypeArguments, context);

            IGenericTypeInstanceReference genTypeInstance = typeReference.AsGenericTypeInstanceReference;
            if (genTypeInstance != null)
            {
                consolidatedTypeArguments.AddRange(genTypeInstance.GetGenericArguments(context));
            }
        }

        internal static ITypeReference GetUninstantiatedGenericType(this ITypeReference typeReference)
        {
            IGenericTypeInstanceReference genericTypeInstanceReference = typeReference.AsGenericTypeInstanceReference;
            if (genericTypeInstanceReference != null)
            {
                return genericTypeInstanceReference.GenericType;
            }

            ISpecializedNestedTypeReference specializedNestedType = typeReference.AsSpecializedNestedTypeReference;
            if (specializedNestedType != null)
            {
                return specializedNestedType.UnspecializedVersion;
            }

            return typeReference;
        }

        internal static bool IsTypeSpecification(this ITypeReference typeReference)
        {
            INestedTypeReference nestedTypeReference = typeReference.AsNestedTypeReference;
            if (nestedTypeReference != null)
            {
                return nestedTypeReference.AsSpecializedNestedTypeReference != null ||
                    nestedTypeReference.AsGenericTypeInstanceReference != null;
            }

            return typeReference.AsNamespaceTypeReference == null;
        }
    }
}