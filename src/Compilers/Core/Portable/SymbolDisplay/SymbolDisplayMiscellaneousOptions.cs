// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies miscellaneous options about the format of symbol descriptions.
    /// </summary>
    [Flags]
    public enum SymbolDisplayMiscellaneousOptions
    {
        /// <summary>
        /// Specifies that no miscellaneous options should be applied.
        /// </summary>
        None = 0,

        /// <summary>
        /// Uses keywords for predefined types. 
        /// For example, "int" instead of "System.Int32" in C#
        /// or "Integer" instead of "System.Integer" in Visual Basic.
        /// </summary>
        UseSpecialTypes = 1 << 0,

        /// <summary>
        /// Escapes identifiers that are also keywords.
        /// For example, "@true" instead of "true" in C# or
        /// "[True]" instead of "True" in Visual Basic.
        /// </summary>
        EscapeKeywordIdentifiers = 1 << 1,

        /// <summary>
        /// Displays asterisks between commas in multi-dimensional arrays.
        /// For example, "int[][*,*]" instead of "int[][,]" in C# or
        /// "Integer()(*,*)" instead of "Integer()(*,*) in Visual Basic.
        /// </summary>
        UseAsterisksInMultiDimensionalArrays = 1 << 2,

        /// <summary>
        /// Displays "?" for erroneous types that lack names (perhaps due to faulty metadata).
        /// </summary>
        UseErrorTypeSymbolName = 1 << 3,

        /// <summary>
        /// Displays attributes names without the "Attribute" suffix, if possible.
        /// <para>
        /// Has no effect outside <see cref="ISymbol.ToMinimalDisplayString"/> and only applies
        /// if the context location is one where an attribute ca be referenced without the suffix.
        /// </para>
        /// </summary>
        RemoveAttributeSuffix = 1 << 4,

        /// <summary>
        /// Displays <see cref="Nullable{T}"/> as a normal generic type, rather than with
        /// the special question mark syntax.
        /// </summary>
        ExpandNullable = 1 << 5,

        /// <summary>
        /// Append '?' to nullable reference types.
        /// </summary>
        IncludeNullableReferenceTypeModifier = 1 << 6,

        /// <summary>
        /// Allow the use of <c>default</c> instead of <c>default(T)</c> where applicable.
        /// </summary>
        AllowDefaultLiteral = 1 << 7,

        /// <summary>
        /// Append '!' to non-nullable reference types.
        /// </summary>
        IncludeNotNullableReferenceTypeModifier = 1 << 8,

        /// <summary>
        /// Insert a tuple into the display parts as a single part instead of multiple parts (similar
        /// to how anonymous types are inserted).
        /// </summary>
        CollapseTupleTypes = 1 << 9,

        /// <summary>
        /// Displays <see cref="System.ValueTuple"/> as a normal generic type, rather than with the special
        /// parenthetical syntax (e.g. <code>ValueTuple&lt;int, string&gt;</code> instead of <code>(int, string)</code>)
        /// </summary>
        ExpandValueTuple = 1 << 10,
    }
}
