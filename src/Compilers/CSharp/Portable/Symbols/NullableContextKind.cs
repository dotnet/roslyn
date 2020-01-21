// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Used by symbol implementations (source and metadata) to represent the value
    /// that was mapped from, or will be mapped to a [NullableContext] attribute.
    /// </summary>
    internal enum NullableContextKind : byte
    {
        /// <summary>
        /// Uninitialized state
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// No [NullableContext] attribute
        /// </summary>
        None,

        /// <summary>
        /// [NullableContext(0)]
        /// </summary>
        Oblivious,

        /// <summary>
        /// [NullableContext(1)]
        /// </summary>
        NotAnnotated,

        /// <summary>
        /// [NullableContext(2)]
        /// </summary>
        Annotated,
    }

    internal static class NullableContextExtensions
    {
        internal static bool TryGetByte(this NullableContextKind kind, out byte? value)
        {
            switch (kind)
            {
                case NullableContextKind.Unknown:
                    value = null;
                    return false;
                case NullableContextKind.None:
                    value = null;
                    return true;
                case NullableContextKind.Oblivious:
                    value = NullableAnnotationExtensions.ObliviousAttributeValue;
                    return true;
                case NullableContextKind.NotAnnotated:
                    value = NullableAnnotationExtensions.NotAnnotatedAttributeValue;
                    return true;
                case NullableContextKind.Annotated:
                    value = NullableAnnotationExtensions.AnnotatedAttributeValue;
                    return true;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        internal static NullableContextKind ToNullableContextFlags(this byte? value)
        {
            switch (value)
            {
                case null:
                    return NullableContextKind.None;
                case NullableAnnotationExtensions.ObliviousAttributeValue:
                    return NullableContextKind.Oblivious;
                case NullableAnnotationExtensions.NotAnnotatedAttributeValue:
                    return NullableContextKind.NotAnnotated;
                case NullableAnnotationExtensions.AnnotatedAttributeValue:
                    return NullableContextKind.Annotated;
                default:
                    throw ExceptionUtilities.UnexpectedValue(value);
            }
        }
    }
}
