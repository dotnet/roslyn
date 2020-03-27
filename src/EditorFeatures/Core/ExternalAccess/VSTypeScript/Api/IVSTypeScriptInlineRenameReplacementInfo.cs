// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api

{
    internal interface IVSTypeScriptInlineRenameReplacementInfo
    {
        /// <summary>
        /// The solution obtained after resolving all conflicts.
        /// </summary>
        Solution NewSolution { get; }

        /// <summary>
        /// Whether or not the replacement text entered by the user is valid.
        /// </summary>
        bool ReplacementTextValid { get; }

        /// <summary>
        /// The documents that need to be updated.
        /// </summary>
        IEnumerable<DocumentId> DocumentIds { get; }

        /// <summary>
        /// Returns all the replacements that need to be performed for the specified document.
        /// </summary>
        IEnumerable<VSTypeScriptInlineRenameReplacementWrapper> GetReplacements(DocumentId documentId);
    }
}
