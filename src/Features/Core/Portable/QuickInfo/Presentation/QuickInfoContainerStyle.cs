// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.QuickInfo.Presentation;

/// <summary>
/// The layout style for a <see cref="QuickInfoContainerElement"/>.
/// </summary>
[Flags]
internal enum QuickInfoContainerStyle
{
    /// <summary>
    /// Contents are end-to-end, and wrapped when the control becomes too wide.
    /// </summary>
    Wrapped = 0,

    /// <summary>
    /// Contents are stacked vertically.
    /// </summary>
    Stacked = 1 << 0,

    /// <summary>
    /// Additional padding above and below content.
    /// </summary>
    VerticalPadding = 1 << 1
}
