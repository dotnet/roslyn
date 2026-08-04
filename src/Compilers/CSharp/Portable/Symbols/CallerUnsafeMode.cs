// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Member safety as defined by the unsafe evolution feature (<see cref="MessageID.IDS_FeatureUnsafeEvolution"/>).
/// </summary>
internal enum CallerUnsafeMode
{
    /// <summary>
    /// The member is not considered caller-unsafe.
    /// </summary>
    None,

    /// <summary>
    /// The member is implicitly considered caller-unsafe because it contains pointers in its signature.
    /// This state is valid even under the legacy memory safety rules to avoid a dip caused by pointers being safe regardless of memory safety rules.
    /// </summary>
    Implicit,

    /// <summary>
    /// The member is explicitly marked as <see langword="unsafe"/> under the updated memory safety rules (<see cref="ModuleSymbol.UseUpdatedMemorySafetyRules"/>).
    /// </summary>
    Explicit,
}
