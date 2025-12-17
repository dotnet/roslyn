// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Member safety under updated memory safety rules (<see cref="ModuleSymbol.UseUpdatedMemorySafetyRules"/>).
/// </summary>
internal enum CallerUnsafeMode
{
    /// <summary>
    /// The member is not considered unsafe under the updated memory safety rules.
    /// </summary>
    None,

    /// <summary>
    /// The member is implicitly considered unsafe because it contains pointers in its signature.
    /// This member should not have the <see cref="AttributeDescription.RequiresUnsafeAttribute"/> emitted.
    /// </summary>
    Implicit,

    /// <summary>
    /// The member is explicitly marked as <see langword="unsafe"/> under the updated memory safety rules.
    /// This member should have the <see cref="AttributeDescription.RequiresUnsafeAttribute"/> emitted.
    /// </summary>
    Explicit,
}
