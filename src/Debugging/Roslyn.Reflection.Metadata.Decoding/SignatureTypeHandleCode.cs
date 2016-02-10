// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

#if SRM
namespace System.Reflection.Metadata.Decoding
#else
namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
#if SRM && FUTURE
    public
#endif
    internal enum SignatureTypeHandleCode : byte
    {
        /// <summary>
        /// It is not known in the current context if the type reference or definition is a class or value type.
        /// This will be the case when <see cref="SignatureDecoderOptions.DifferentiateClassAndValueTypes"/> is not specified.
        /// </summary>
        Unresolved = 0,

        /// <summary>
        /// The type definition or reference refers to a class.
        /// </summary>
        Class = 0x12, // TODO: CorElementType.ELEMENT_TYPE_CLASS,

        /// <summary>
        /// The type definition or reference refers to a value type.
        /// </summary>
        ValueType = 0x11, // TODO: CorElementType.ELEMENT_TYPE_VALUETYPE,
    }
}
