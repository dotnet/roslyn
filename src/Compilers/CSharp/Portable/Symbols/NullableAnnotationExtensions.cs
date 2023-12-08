// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class NullableAnnotationExtensions
    {
        public static bool IsAnnotated(this NullableAnnotation annotation) => annotation == NullableAnnotation.Annotated;

        public static bool IsNotAnnotated(this NullableAnnotation annotation) => annotation == NullableAnnotation.NotAnnotated;

        public static bool IsOblivious(this NullableAnnotation annotation) => annotation == NullableAnnotation.Oblivious;

        /// <summary>
        /// Join nullable annotations from the set of lower bounds for fixing a type parameter.
        /// This uses the covariant merging rules. (Annotated wins over Oblivious which wins over NotAnnotated)
        /// </summary>
        public static NullableAnnotation Join(this NullableAnnotation a, NullableAnnotation b)
        {
            Debug.Assert(a != NullableAnnotation.Ignored);
            Debug.Assert(b != NullableAnnotation.Ignored);
            return (a < b) ? b : a;
        }

        /// <summary>
        /// Meet two nullable annotations for computing the nullable annotation of a type parameter from upper bounds.
        /// This uses the contravariant merging rules. (NotAnnotated wins over Oblivious which wins over Annotated)
        /// </summary>
        public static NullableAnnotation Meet(this NullableAnnotation a, NullableAnnotation b)
        {
            Debug.Assert(a != NullableAnnotation.Ignored);
            Debug.Assert(b != NullableAnnotation.Ignored);
            return (a < b) ? a : b;
        }

        /// <summary>
        /// Return the nullable annotation to use when two annotations are expected to be "compatible", which means
        /// they could be the same. These are the "invariant" merging rules. (NotAnnotated wins over Annotated which wins over Oblivious)
        /// </summary>
        public static NullableAnnotation EnsureCompatible(this NullableAnnotation a, NullableAnnotation b)
        {
            Debug.Assert(a != NullableAnnotation.Ignored);
            Debug.Assert(b != NullableAnnotation.Ignored);
            return (a, b) switch
            {
                (NullableAnnotation.Oblivious, _) => b,
                (_, NullableAnnotation.Oblivious) => a,
                _ => a < b ? a : b,
            };
        }

        /// <summary>
        /// Merges nullability.
        /// </summary>
        public static NullableAnnotation MergeNullableAnnotation(this NullableAnnotation a, NullableAnnotation b, VarianceKind variance)
        {
            Debug.Assert(a != NullableAnnotation.Ignored);
            Debug.Assert(b != NullableAnnotation.Ignored);
            return variance switch
            {
                VarianceKind.In => a.Meet(b),
                VarianceKind.Out => a.Join(b),
                VarianceKind.None => a.EnsureCompatible(b),
                _ => throw ExceptionUtilities.UnexpectedValue(variance)
            };
        }

        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableAnnotation.NotAnnotated"/>.
        /// </summary>
        public const byte NotAnnotatedAttributeValue = 1;

        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableAnnotation.Annotated"/>.
        /// </summary>
        public const byte AnnotatedAttributeValue = 2;

        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableAnnotation.Oblivious"/>.
        /// </summary>
        public const byte ObliviousAttributeValue = 0;

        internal static NullabilityInfo ToNullabilityInfo(this CodeAnalysis.NullableAnnotation annotation, TypeSymbol type)
        {
            if (annotation == CodeAnalysis.NullableAnnotation.None)
            {
                return default;
            }

            CSharp.NullableAnnotation internalAnnotation = annotation.ToInternalAnnotation();
            return internalAnnotation.ToNullabilityInfo(type);
        }

        internal static NullabilityInfo ToNullabilityInfo(this NullableAnnotation annotation, TypeSymbol type)
        {
            var flowState = TypeWithAnnotations.Create(type, annotation).ToTypeWithState().State;
            return new NullabilityInfo(ToPublicAnnotation(type, annotation), flowState.ToPublicFlowState());
        }

        internal static ITypeSymbol GetPublicSymbol(this TypeWithAnnotations type)
        {
            return type.Type?.GetITypeSymbol(type.ToPublicAnnotation());
        }

        internal static ImmutableArray<ITypeSymbol> GetPublicSymbols(this ImmutableArray<TypeWithAnnotations> types)
        {
            return types.SelectAsArray(t => t.GetPublicSymbol());
        }

        internal static CodeAnalysis.NullableAnnotation ToPublicAnnotation(this TypeWithAnnotations type) =>
            ToPublicAnnotation(type.Type, type.NullableAnnotation);

        internal static ImmutableArray<CodeAnalysis.NullableAnnotation> ToPublicAnnotations(this ImmutableArray<TypeWithAnnotations> types) =>
            types.SelectAsArray(t => t.ToPublicAnnotation());

#nullable enable

        internal static CodeAnalysis.NullableAnnotation ToPublicAnnotation(TypeSymbol? type, NullableAnnotation annotation)
        {
            Debug.Assert(annotation != NullableAnnotation.Ignored);
            return annotation switch
            {
                NullableAnnotation.Annotated => CodeAnalysis.NullableAnnotation.Annotated,
                NullableAnnotation.NotAnnotated => CodeAnalysis.NullableAnnotation.NotAnnotated,

                // A value type may be oblivious or not annotated depending on whether the type reference
                // is from source or metadata. (Binding using the #nullable context only when setting the annotation
                // to avoid checking IsValueType early.) The annotation is normalized here in the public API.
                NullableAnnotation.Oblivious when type?.IsValueType == true => CodeAnalysis.NullableAnnotation.NotAnnotated,
                NullableAnnotation.Oblivious => CodeAnalysis.NullableAnnotation.None,

                NullableAnnotation.Ignored => CodeAnalysis.NullableAnnotation.None,

                _ => throw ExceptionUtilities.UnexpectedValue(annotation)
            };
        }

#nullable disable

        internal static CSharp.NullableAnnotation ToInternalAnnotation(this CodeAnalysis.NullableAnnotation annotation) =>
            annotation switch
            {
                CodeAnalysis.NullableAnnotation.None => CSharp.NullableAnnotation.Oblivious,
                CodeAnalysis.NullableAnnotation.NotAnnotated => CSharp.NullableAnnotation.NotAnnotated,
                CodeAnalysis.NullableAnnotation.Annotated => CSharp.NullableAnnotation.Annotated,
                _ => throw ExceptionUtilities.UnexpectedValue(annotation)
            };
    }
}
