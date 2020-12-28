﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Experimentation
{
    internal enum ReSharperStatus
    {
        /// <summary>
        /// Disabled in the extension manager or not installed.
        /// </summary>
        NotInstalledOrDisabled,
        /// <summary>
        /// ReSharper is suspended. Package is loaded, but is not actually performing actions.
        /// </summary>
        Suspended,
        /// <summary>
        /// ReSharper is installed and enabled.
        /// </summary>
        Enabled
    }
}
