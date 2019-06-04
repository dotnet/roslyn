// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Used by symbol implementations to represent the byte value of [NullableContext].
    /// </summary>
    [Flags]
    internal enum NullableContextFlags : byte
    {
        None = 0,
        Oblivious = 1,
        NotAnnotated = 2,
        Annotated = 3,
        Initialized = 4,
    }

    internal static class NullableContextExtensions
    {
        internal static bool TryGetByte(this NullableContextFlags flags, out byte? value)
        {
            value = (flags & ~NullableContextFlags.Initialized) switch
            {
                NullableContextFlags.None => (byte?)null,
                NullableContextFlags.Oblivious => NullableAnnotationExtensions.ObliviousAttributeValue,
                NullableContextFlags.NotAnnotated => NullableAnnotationExtensions.NotAnnotatedAttributeValue,
                NullableContextFlags.Annotated => NullableAnnotationExtensions.AnnotatedAttributeValue,
                _ => throw ExceptionUtilities.UnexpectedValue(flags)
            };
            return (flags & NullableContextFlags.Initialized) != 0;
        }

        internal static NullableContextFlags ToNullableContextFlags(this byte? value)
        {
            NullableContextFlags result = value switch
            {
                null => NullableContextFlags.None,
                NullableAnnotationExtensions.ObliviousAttributeValue => NullableContextFlags.Oblivious,
                NullableAnnotationExtensions.NotAnnotatedAttributeValue => NullableContextFlags.NotAnnotated,
                NullableAnnotationExtensions.AnnotatedAttributeValue => NullableContextFlags.Annotated,
                _ => throw ExceptionUtilities.UnexpectedValue(value)
            };
            return result | NullableContextFlags.Initialized;
        }
    }
}
