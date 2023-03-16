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
        Collapse,
        /// <summary>
        /// Do not update node expansion (when we are generating a new UI model, we will use the default expansion).
        /// This option is used when we generate a UI model using a newly created Document Symbol data model.
        /// </summary>
        NoChange,
        /// <summary>
        /// Apply current tree view expansion to nodes (if the caret is moved around, we want to preserve existing
        /// expanded/collapsed node states as the underlying data model has not been updated).
        /// </summary>
        CurrentExpansion
    }
}
