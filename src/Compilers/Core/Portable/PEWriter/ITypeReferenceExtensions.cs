// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Text;

namespace Microsoft.Cci
{
    internal static class ITypeReferenceExtensions
    {
        internal static void GetConsolidatedTypeArguments(this ITypeReference typeReference, ArrayBuilder<ITypeReference> consolidatedTypeArguments, EmitContext context)
        {
            INestedTypeReference? nestedTypeReference = typeReference.AsNestedTypeReference;
            nestedTypeReference?.GetContainingType(context).GetConsolidatedTypeArguments(consolidatedTypeArguments, context);

            IGenericTypeInstanceReference? genTypeInstance = typeReference.AsGenericTypeInstanceReference;
            if (genTypeInstance != null)
            {
                consolidatedTypeArguments.AddRange(genTypeInstance.GetGenericArguments(context));
            }
        }

        internal static ITypeReference GetUninstantiatedGenericType(this ITypeReference typeReference, EmitContext context)
        {
            IGenericTypeInstanceReference? genericTypeInstanceReference = typeReference.AsGenericTypeInstanceReference;
            if (genericTypeInstanceReference != null)
            {
                return genericTypeInstanceReference.GetGenericType(context);
            }

            ISpecializedNestedTypeReference? specializedNestedType = typeReference.AsSpecializedNestedTypeReference;
            if (specializedNestedType != null)
            {
                return specializedNestedType.GetUnspecializedVersion(context);
            }

            return typeReference;
        }

        internal static bool IsTypeSpecification(this ITypeReference typeReference)
        {
            INestedTypeReference? nestedTypeReference = typeReference.AsNestedTypeReference;
            if (nestedTypeReference != null)
            {
                return nestedTypeReference.AsSpecializedNestedTypeReference != null ||
                    nestedTypeReference.AsGenericTypeInstanceReference != null;
            }

            return typeReference.AsNamespaceTypeReference == null;
        }
    }
}
