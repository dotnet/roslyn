// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.ComponentModel;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Enumeration for possible kinds of type symbols.
    /// </summary>
    public enum TypeKind : byte
    {
        /// <summary>
        /// Type's kind is undefined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Type is an array type.
        /// </summary>
        Array = 1,

        /// <summary>
        /// Type is a class.
        /// </summary>
        Class = 2,

        /// <summary>
        /// Type is a delegate.
        /// </summary>
        Delegate = 3,

        /// <summary>
        /// Type is dynamic.
        /// </summary>
        Dynamic = 4,

        /// <summary>
        /// Type is an enumeration.
        /// </summary>
        Enum = 5,

        /// <summary>
        /// Type is an error type.
        /// </summary>
        Error = 6,

        /// <summary>
        /// Type is an interface.
        /// </summary>
        Interface = 7,

        /// <summary>
        /// Type is a module.
        /// </summary>
        Module = 8,

        /// <summary>
        /// Type is a pointer.
        /// </summary>
        Pointer = 9,

        /// <summary>
        /// Type is a C# struct or VB Structure
        /// </summary>
        Struct = 10,

        /// <summary>
        /// Type is a C# struct or VB Structure
        /// </summary>
        Structure = 10,

        /// <summary>
        /// Type is a type parameter.
        /// </summary>
        TypeParameter = 11,

        /// <summary>
        /// Type is an interactive submission.
        /// </summary>
        Submission = 12,
    }
}
