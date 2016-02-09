// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

#if SRM
namespace System.Reflection.Metadata.Decoding
#else
namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
#if SRM && FUTURE
    public
#endif
    internal interface IConstructedTypeProvider<TType> : ISZArrayTypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a generic instantiation of the given generic type with the given type arguments.
        /// </summary>
        TType GetGenericInstance(TType genericType, ImmutableArray<TType> typeArguments);

        /// <summary>
        /// Gets the type symbol for a generalized array of the given element type and shape. 
        /// </summary>
        TType GetArrayType(TType elementType, ArrayShape shape);

        /// <summary>
        /// Gets the type symbol for a managed pointer to the given element type.
        /// </summary>
        TType GetByReferenceType(TType elementType);

        /// <summary>
        /// Gets the type symbol for an unmanaged pointer to the given element ty
        /// </summary>
        TType GetPointerType(TType elementType);
    }
}
