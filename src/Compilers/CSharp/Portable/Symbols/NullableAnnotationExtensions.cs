// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

// https://github.com/dotnet/roslyn/issues/34962 IDE005 "Fix formatting" does a poor job with a switch expression as the body of an expression-bodied method
#pragma warning disable IDE0055

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
        public static NullableAnnotation Join(this NullableAnnotation a, NullableAnnotation b) => (a < b) ? b : a;

        /// <summary>
        /// Meet two nullable annotations for computing the nullable annotation of a type parameter from upper bounds.
        /// This uses the contravariant merging rules. (NotAnnotated wins over Oblivious which wins over Annotated)
        /// </summary>
        public static NullableAnnotation Meet(this NullableAnnotation a, NullableAnnotation b) => (a < b) ? a : b;

        /// <summary>
        /// Return the nullable annotation to use when two annotations are expected to be "compatible", which means
        /// they could be the same. These are the "invariant" merging rules. (NotAnnotated wins over Annotated which wins over Oblivious)
        /// </summary>
        public static NullableAnnotation EnsureCompatible(this NullableAnnotation a, NullableAnnotation b) =>
            (a, b) switch
            {
                (NullableAnnotation.Oblivious, _) => b,
                (_, NullableAnnotation.Oblivious) => a,
                _ => a < b ? a : b,
            };

        /// <summary>
        /// Merges nullability.
        /// </summary>
        public static NullableAnnotation MergeNullableAnnotation(this NullableAnnotation a, NullableAnnotation b, VarianceKind variance) =>
            variance switch
            {
                VarianceKind.In => a.Meet(b),
                VarianceKind.Out => a.Join(b),
                VarianceKind.None => a.EnsureCompatible(b),
                _ => throw ExceptionUtilities.UnexpectedValue(variance)
            };

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

        internal static CodeAnalysis.NullableAnnotation ToPublicAnnotation(this TypeWithAnnotations type) =>
            ToPublicAnnotation(type.Type, type.NullableAnnotation);

        private static CodeAnalysis.NullableAnnotation ToPublicAnnotation(TypeSymbol type, NullableAnnotation annotation) =>
            annotation switch
            {
                CSharp.NullableAnnotation.Annotated => CodeAnalysis.NullableAnnotation.Annotated,
                CSharp.NullableAnnotation.NotAnnotated => CodeAnalysis.NullableAnnotation.NotAnnotated,
                // A value type may be oblivious or not annotated depending on whether the type reference
                // is from source or metadata. (Binding using the #nullable context only when setting the annotation
                // to avoid checking IsValueType early.) The annotation is normalized here in the public API.
                CSharp.NullableAnnotation.Oblivious when  type.IsValueType => CodeAnalysis.NullableAnnotation.NotAnnotated,
                CSharp.NullableAnnotation.Oblivious => CodeAnalysis.NullableAnnotation.None,
                _ => throw ExceptionUtilities.UnexpectedValue(annotation)
            };

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
