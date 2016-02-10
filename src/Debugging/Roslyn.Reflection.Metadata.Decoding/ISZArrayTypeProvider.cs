// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if SRM
namespace System.Reflection.Metadata.Decoding
#else
namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
#if SRM && FUTURE
    public
#endif
    internal interface ISZArrayTypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a single-dimensional array with zero lower bounds of the given element type.
        /// </summary>
        TType GetSZArrayType(TType elementType);
    }
}
