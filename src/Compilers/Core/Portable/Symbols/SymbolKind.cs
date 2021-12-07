// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Roslyn.Utilities;

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

        /// <summary>
        /// Symbol represents a function pointer type
        /// </summary>
        FunctionPointerType = 20,
    }

    internal static class SymbolKindInternal
    {
        /// <summary>
        /// Internal Symbol representing the inferred signature of
        /// a lambda expression or method group.
        /// </summary>
        internal const SymbolKind FunctionType = (SymbolKind)255;

#if DEBUG
        static SymbolKindInternal()
        {
            Debug.Assert(!EnumUtilities.ContainsValue(FunctionType));
        }
#endif
    }
}
