// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Describes the expansion option to be applied whenever a new Document Symbol UI model is created.
    /// </summary>
    internal enum ExpansionOption
    {
        /// <summary>
        /// Expand all nodes.
        /// </summary>
        Expand,
        /// <summary>
        /// Collapse all nodes.
        /// </summary>
        Collapse
    }
}
