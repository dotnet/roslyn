// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
