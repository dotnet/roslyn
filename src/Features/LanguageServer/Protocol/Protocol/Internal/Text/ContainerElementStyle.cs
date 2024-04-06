// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Text.Adornments
{
    //
    // Summary:
    //     The layout style for a Microsoft.VisualStudio.Text.Adornments.ContainerElement.
    [Flags]
    internal enum ContainerElementStyle
    {
        //
        // Summary:
        //     Contents are end-to-end, and wrapped when the control becomes too wide.
        Wrapped = 0x0,
        //
        // Summary:
        //     Contents are stacked vertically.
        Stacked = 0x1,
        //
        // Summary:
        //     Additional padding above and below content.
        VerticalPadding = 0x2
    }
}