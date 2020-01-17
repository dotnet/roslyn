// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the possible kinds of symbols.
    /// </summary>
    public enum SymbolKind
    {
        /// <summary>
        /// Symbol is an alias.
        /// </summary>
        Alias = 0,

        /// <summary>
        /// Symbol is an array type.
        /// </summary>
        ArrayType = 1,

        /// <summary>
        /// Symbol is an assembly.
        /// </summary>
        Assembly = 2,

        /// <summary>
        /// Symbol is a dynamic type.
        /// </summary>
        DynamicType = 3,

        /// <summary>
        /// Symbol that represents an error 
        /// </summary>
        ErrorType = 4,

        /// <summary>
        /// Symbol is an Event.
        /// </summary>
        Event = 5,

        /// <summary>
        /// Symbol is a field.
        /// </summary>
        Field = 6,

        /// <summary>
        /// Symbol is a label.
        /// </summary>
        Label = 7,

        /// <summary>
        /// Symbol is a local.
        /// </summary>
        Local = 8,

        /// <summary>
        /// Symbol is a method.
        /// </summary>
        Method = 9,

        /// <summary>
        /// Symbol is a netmodule.
        /// </summary>
        NetModule = 10,

        /// <summary>
        /// Symbol is a named type (e.g. class).
        /// </summary>
        NamedType = 11,

        /// <summary>
        /// Symbol is a namespace.
        /// </summary>
        Namespace = 12,

        /// <summary>
        /// Symbol is a parameter.
        /// </summary>
        Parameter = 13,

        /// <summary>
        /// Symbol is a pointer type.
        /// </summary>
        PointerType = 14,

        /// <summary>
        /// Symbol is a property.
        /// </summary>
        Property = 15,

        /// <summary>
        /// Symbol is a range variable of a query expression.
        /// </summary>
        RangeVariable = 16,

        /// <summary>
        /// Symbol is a type parameter.
        /// </summary>
        TypeParameter = 17,

        /// <summary>
        /// Symbol is a preprocessing/conditional compilation constant.
        /// </summary>
        Preprocessing = 18,

        /// <summary>
        /// Symbol represents a value that is discarded, e.g. in M(out _)
        /// </summary>
        Discard = 19,
    }
}
