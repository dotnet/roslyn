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
    /// </summary>
    Implicit,

    /// <summary>
    /// The member is explicitly marked as <see langword="unsafe"/> under the updated memory safety rules.
    /// </summary>
    Explicit,

    /// <summary>
    /// The member is explicitly marked as <see langword="extern"/> under the updated memory safety rules.
    /// This can only appear for source symbols (after metadata roundtrip, this turns into <see cref="Explicit"/>).
    /// </summary>
    Extern,
}

internal static class CallerUnsafeModeExtensions
{
    public static bool NeedsRequiresUnsafeAttribute(this CallerUnsafeMode mode)
    {
        return mode is CallerUnsafeMode.Explicit or CallerUnsafeMode.Extern;
    }
}
