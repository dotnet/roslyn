// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    [Flags]
    internal enum SymbolDisplayCompilerInternalOptions
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// ".ctor" instead of "Goo"
        /// </summary>
        UseMetadataMethodNames = 1 << 0,

        /// <summary>
        /// "List`1" instead of "List&lt;T&gt;" ("List(of T)" in VB). Overrides GenericsOptions on
        /// types.
        /// </summary>
        UseArityForGenericTypes = 1 << 1,

        /// <summary>
        /// Append "[Missing]" to missing Metadata types (for testing).
        /// </summary>
        FlagMissingMetadataTypes = 1 << 2,

        /// <summary>
        /// Include the Script type when qualifying type names.
        /// </summary>
        IncludeScriptType = 1 << 3,

        /// <summary>
        /// Include custom modifiers (e.g. modopt([mscorlib]System.Runtime.CompilerServices.IsConst)) on
        /// the member (return) type and parameters.
        /// </summary>
        /// <remarks>
        /// CONSIDER: custom modifiers are part of the public API, so we might want to move this to SymbolDisplayMemberOptions.
        /// </remarks>
        IncludeCustomModifiers = 1 << 4,

        /// <summary>
        /// For a type written as "int[][,]" in C#, then
        ///   a) setting this option will produce "int[,][]", and
        ///   b) not setting this option will produce "int[][,]".
        /// </summary>
        ReverseArrayRankSpecifiers = 1 << 5,

        /// <summary>
        /// Append '!' to non-nullable reference types.
        /// Note this causes SymbolDisplay to pull on IsNullable and therefore NonNullTypes,
        /// so don't use this option in binding, in order to avoid cycles.
        /// </summary>
        IncludeNonNullableTypeModifier = 1 << 6,

        /// <summary>
        /// Display `System.ValueTuple` instead of tuple syntax `(...)`.
        /// </summary>
        UseValueTuple = 1 << 7,
    }
}
