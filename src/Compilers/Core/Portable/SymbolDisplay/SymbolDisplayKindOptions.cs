// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies which kind keywords should be included when displaying symbols.
    /// </summary>
    [Flags]
    public enum SymbolDisplayKindOptions
    {
        /// <summary>
        /// Omits all kind keywords.
        /// </summary>
        None = 0,

        /// <summary>
        /// Includes the <c>namespace</c> keyword before namespaces.
        /// For example, "namespace System", rather than "System".
        /// </summary>
        IncludeNamespaceKeyword = 1 << 0,

        /// <summary>
        /// Includes the type keyword before types.
        /// For example, "class C" in C# or "Structure S" in Visual Basic.
        /// </summary>
        IncludeTypeKeyword = 1 << 1,

        /// <summary>
        /// Include the member keyword before members (if one exists).
        /// For example, "event D E" in C# or "Function MyFun()" in Visual Basic.
        /// </summary>
        IncludeMemberKeyword = 1 << 2,
    }
}
