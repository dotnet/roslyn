// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;

internal enum StringCopyPasteContentKind
{
    /// <summary>
    /// When text content is copied.
    /// </summary>
    Text,

    /// <summary>
    /// When an interpolation is copied.
    /// </summary>
    Interpolation,
}
