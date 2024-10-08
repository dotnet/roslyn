// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.AddImport;

/// <summary>
/// Specifies the desired placement of added imports.
/// </summary>
internal enum AddImportPlacement
{
    /// <summary>
    /// Place imports inside the namespace definition.
    /// </summary>
    InsideNamespace,

    /// <summary>
    /// Place imports outside the namespace definition.
    /// </summary>
    OutsideNamespace
}
