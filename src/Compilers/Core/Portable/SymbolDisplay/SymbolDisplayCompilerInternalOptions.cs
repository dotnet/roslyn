// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

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
        /// Display `System.[U]IntPtr` instead of `n[u]int`.
        /// </summary>
        UseNativeIntegerUnderlyingType = 1 << 6,

        /// <summary>
        /// Separate out nested types from containing types using <c>+</c> instead of <c>.</c> (dot).
        /// </summary>
        UsePlusForNestedTypes = 1 << 7,

        /// <summary>
        /// Display `MyType@File.cs` instead of `MyType`.
        /// </summary>
        IncludeContainingFileForFileTypes = 1 << 8,

        /// <summary>
        /// Does not include parameter name if the parameter is displayed on its own
        /// (i.e., not as part of a method, delegate, or indexer).
        /// </summary>
        ExcludeParameterNameIfStandalone = 1 << 9,
    }
}
