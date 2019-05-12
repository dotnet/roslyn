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
        /// This uses the covariant merging rules.
        /// </summary>
        public static NullableAnnotation Join(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a.IsAnnotated() || b.IsAnnotated())
                return NullableAnnotation.Annotated;
            return (a < b) ? a : b;
        }

        /// <summary>
        /// Meet two nullable annotations for computing the nullable annotation of a type parameter from upper bounds.
        /// This uses the contravariant merging rules.
        /// </summary>
        public static NullableAnnotation Meet(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a.IsNotAnnotated() || b.IsNotAnnotated())
                return NullableAnnotation.NotAnnotated;
            return (a < b) ? a : b;
        }

        /// <summary>
        /// Check that two nullable annotations are "compatible", which means they could be the same. Return the
        /// nullable annotation to be used as a result.  This uses the invariant merging rules.
        /// </summary>
        public static NullableAnnotation EnsureCompatible(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a.IsOblivious())
                return b;
            if (b.IsOblivious())
                return a;
            return (a < b) ? a : b;
        }

        /// <summary>
        /// Merges nullability.
        /// </summary>
        public static NullableAnnotation MergeNullableAnnotation(this NullableAnnotation a, NullableAnnotation b, VarianceKind variance)
        {
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
        public const byte NotAnnotatedAttributeValue = (byte)NullableAnnotation.NotAnnotated;

        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableAnnotation.Annotated"/>.
        /// </summary>
        public const byte AnnotatedAttributeValue = (byte)NullableAnnotation.Annotated;

        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableAnnotation.Oblivious"/>.
        /// </summary>
        public const byte ObliviousAttributeValue = (byte)NullableAnnotation.Oblivious;

        internal static NullabilityInfo ToNullabilityInfo(this CodeAnalysis.NullableAnnotation annotation, TypeSymbol type)
        {
            if (annotation == CodeAnalysis.NullableAnnotation.NotApplicable)
            {
                return default;
            }

            CSharp.NullableAnnotation internalAnnotation = annotation.ToInternalAnnotation();
            return internalAnnotation.ToNullabilityInfo(type);
        }

        internal static NullabilityInfo ToNullabilityInfo(this NullableAnnotation annotation, TypeSymbol type)
        {
            var flowState = TypeWithAnnotations.Create(type, annotation).ToTypeWithState().State;
            return new NullabilityInfo(annotation.ToPublicAnnotation(), flowState.ToPublicFlowState());
        }

        internal static CodeAnalysis.NullableAnnotation ToPublicAnnotation(this CSharp.NullableAnnotation annotation) =>
            annotation switch
            {
                CSharp.NullableAnnotation.Annotated => CodeAnalysis.NullableAnnotation.Annotated,
                CSharp.NullableAnnotation.NotAnnotated => CodeAnalysis.NullableAnnotation.NotAnnotated,
                CSharp.NullableAnnotation.Oblivious => CodeAnalysis.NullableAnnotation.Disabled,
                _ => throw ExceptionUtilities.UnexpectedValue(annotation)
            };

        internal static CSharp.NullableAnnotation ToInternalAnnotation(this CodeAnalysis.NullableAnnotation annotation) =>
            annotation switch
            {
                CodeAnalysis.NullableAnnotation.NotApplicable => CSharp.NullableAnnotation.Oblivious,
                CodeAnalysis.NullableAnnotation.Disabled => CSharp.NullableAnnotation.Oblivious,
                CodeAnalysis.NullableAnnotation.NotAnnotated => CSharp.NullableAnnotation.NotAnnotated,
                CodeAnalysis.NullableAnnotation.Annotated => CSharp.NullableAnnotation.Annotated,
                _ => throw ExceptionUtilities.UnexpectedValue(annotation)
            };
    }
}
