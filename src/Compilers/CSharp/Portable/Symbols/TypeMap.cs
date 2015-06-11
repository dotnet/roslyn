// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Utility class for substituting actual type arguments for formal generic type parameters.
    /// </summary>
    internal sealed class TypeMap : AbstractTypeParameterMap
    {
        // Only when the caller passes allowAlpha=true do we tolerate substituted (alpha-renamed) type parameters as keys
        internal TypeMap(ImmutableArray<TypeParameterSymbol> from, ImmutableArray<TypeSymbol> to, bool allowAlpha = false)
            : base(ConstructMapping(from, to))
        {
            // mapping contents are read-only hereafter
            Debug.Assert(allowAlpha || !from.Any(tp => tp is SubstitutedTypeParameterSymbol));
        }

        // Only when the caller passes allowAlpha=true do we tolerate substituted (alpha-renamed) type parameters as keys
        internal TypeMap(ImmutableArray<TypeParameterSymbol> from, ImmutableArray<TypeParameterSymbol> to, bool allowAlpha = false)
            : this(from, StaticCast<TypeSymbol>.From(to), allowAlpha)
        {
            // mapping contents are read-only hereafter
        }

        internal TypeMap(SmallDictionary<TypeParameterSymbol, TypeSymbol> mapping)
            : base(new SmallDictionary<TypeParameterSymbol, TypeSymbol>(mapping, ReferenceEqualityComparer.Instance))
        {
            // mapping contents are read-only hereafter
        }

        private static SmallDictionary<TypeParameterSymbol, TypeSymbol> ForType(NamedTypeSymbol containingType)
        {
            var substituted = containingType as SubstitutedNamedTypeSymbol;
            return (object)substituted != null ?
                new SmallDictionary<TypeParameterSymbol, TypeSymbol>(substituted.TypeSubstitution.Mapping, ReferenceEqualityComparer.Instance) :
                new SmallDictionary<TypeParameterSymbol, TypeSymbol>(ReferenceEqualityComparer.Instance);
        }
        internal TypeMap(NamedTypeSymbol containingType, ImmutableArray<TypeParameterSymbol> typeParameters, ImmutableArray<TypeSymbol> typeArguments)
            : base(ForType(containingType))
        {
            for (int i = 0; i < typeParameters.Length; i++)
            {
                TypeParameterSymbol tp = typeParameters[i];
                TypeSymbol ta = typeArguments[i];
                if (!ReferenceEquals(tp, ta))
                {
                    Mapping.Add(tp, ta);
                }
            }
        }

        private static readonly SmallDictionary<TypeParameterSymbol, TypeSymbol> s_emptyDictionary =
            new SmallDictionary<TypeParameterSymbol, TypeSymbol>(ReferenceEqualityComparer.Instance);

        private TypeMap()
            : base(s_emptyDictionary)
        {
            Debug.Assert(s_emptyDictionary.IsEmpty());
        }

        private static readonly TypeMap s_emptyTypeMap = new TypeMap();
        public static TypeMap Empty
        {
            get
            {
                Debug.Assert(s_emptyTypeMap.Mapping.IsEmpty());
                return s_emptyTypeMap;
            }
        }

        private TypeMap WithAlphaRename(ImmutableArray<TypeParameterSymbol> oldTypeParameters, Symbol newOwner, out ImmutableArray<TypeParameterSymbol> newTypeParameters)
        {
            if (oldTypeParameters.Length == 0)
            {
                newTypeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
                return this;
            }

            // Note: the below assertion doesn't hold while rewriting async lambdas defined inside generic methods.
            // The async rewriter adds a synthesized struct inside the lambda frame and construct a typemap from
            // the lambda frame's substituted type parameters.
            // Debug.Assert(!oldTypeParameters.Any(tp => tp is SubstitutedTypeParameterSymbol));

            // warning: we expose result to the SubstitutedTypeParameterSymbol constructor, below, even before it's all filled in.
            TypeMap result = new TypeMap(this.Mapping);
            ArrayBuilder<TypeParameterSymbol> newTypeParametersBuilder = ArrayBuilder<TypeParameterSymbol>.GetInstance();

            // The case where it is "synthesized" is when we're creating type parameters for a synthesized (generic)
            // class or method for a lambda appearing in a generic method.
            bool synthesized = !ReferenceEquals(oldTypeParameters[0].ContainingSymbol.OriginalDefinition, newOwner.OriginalDefinition);

            int ordinal = 0;
            foreach (var tp in oldTypeParameters)
            {
                var newTp = synthesized ?
                    new SynthesizedSubstitutedTypeParameterSymbol(newOwner, result, tp, ordinal) :
                    new SubstitutedTypeParameterSymbol(newOwner, result, tp, ordinal);
                result.Mapping.Add(tp, newTp);
                newTypeParametersBuilder.Add(newTp);
                ordinal++;
            }

            newTypeParameters = newTypeParametersBuilder.ToImmutableAndFree();
            return result;
        }

        internal TypeMap WithAlphaRename(NamedTypeSymbol oldOwner, NamedTypeSymbol newOwner, out ImmutableArray<TypeParameterSymbol> newTypeParameters)
        {
            Debug.Assert(oldOwner.ConstructedFrom == oldOwner);
            return WithAlphaRename(oldOwner.OriginalDefinition.TypeParameters, newOwner, out newTypeParameters);
        }

        internal TypeMap WithAlphaRename(MethodSymbol oldOwner, Symbol newOwner, out ImmutableArray<TypeParameterSymbol> newTypeParameters)
        {
            Debug.Assert(oldOwner.ConstructedFrom == oldOwner);
            return WithAlphaRename(oldOwner.OriginalDefinition.TypeParameters, newOwner, out newTypeParameters);
        }

        internal TypeMap WithConcatAlphaRename(MethodSymbol oldOwner, Symbol newOwner, out ImmutableArray<TypeParameterSymbol> newTypeParameters, MethodSymbol stopAt = null)
        {
            Debug.Assert(oldOwner.ConstructedFrom == oldOwner);

            // Build the array up backwards, then reverse it.
            // The following example goes through the do-loop in order M3, M2, M1
            // but the type parameters have to be <T1, T2, T3, T4>
            // void M1<T1>() {
            //   void M2<T2, T3>() {
            //     void M3<T4>() {
            //     }
            //   }
            // }
            // However, if stopAt is M1, then the type parameters would be <T2, T3, T4>
            // That is, stopAt's type parameters are excluded - the parameters are in the range (stopAt, oldOwner]
            // A null stopAt means "include everything"
            var parameters = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            while (oldOwner != null && oldOwner != stopAt)
            {
                var currentParameters = oldOwner.OriginalDefinition.TypeParameters;

                for (int i = currentParameters.Length - 1; i >= 0; i--)
                {
                    parameters.Add(currentParameters[i]);
                }

                oldOwner = oldOwner.ContainingSymbol as MethodSymbol;
            }
            parameters.ReverseContents();

            // Ensure that if stopAt was provided, it actually was in the chain and we stopped at it.
            // If not provided, both should be null (if stopAt != null && oldOwner == null, then it wasn't in the chain)
            Debug.Assert(stopAt == oldOwner);

            return WithAlphaRename(parameters.ToImmutableAndFree(), newOwner, out newTypeParameters);
        }

        private static SmallDictionary<TypeParameterSymbol, TypeSymbol> ConstructMapping(ImmutableArray<TypeParameterSymbol> from, ImmutableArray<TypeSymbol> to)
        {
            SmallDictionary<TypeParameterSymbol, TypeSymbol> mapping = new SmallDictionary<TypeParameterSymbol, TypeSymbol>(ReferenceEqualityComparer.Instance);

            Debug.Assert(from.Length == to.Length);

            for (int i = 0; i < from.Length; i++)
            {
                TypeParameterSymbol tp = from[i];
                TypeSymbol ta = to[i];
                if (!ReferenceEquals(tp, ta))
                {
                    mapping.Add(tp, ta);
                }
            }

            return mapping;
        }
    }
}
