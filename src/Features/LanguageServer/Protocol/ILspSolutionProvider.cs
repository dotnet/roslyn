// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal interface ILspSolutionProvider
    {
        /// <summary>
        /// Finds the workspace and solution containing the specified document URI
        /// and returns the documents in that context.
        /// </summary>
        /// <param name="documentUri">the document's file path URI.</param>
        /// <returns>the documents in the correct workspace and solution context</returns>
        ImmutableArray<Document> GetDocuments(Uri documentUri);

        /// <summary>
        /// Return the latest solution from the main workspace that we know about.
        /// </summary>
        Solution GetCurrentSolutionForMainWorkspace();
    }
}
