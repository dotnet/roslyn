// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal partial class SemanticClassifier
    {
        /// <summary>
        /// <see cref="IEqualityComparer{T}"/> for <see cref="Document"/>s that considers two <see cref="Document"/>s
        /// equal if they have the same <see cref="TextDocument.Id"/>.  Used so we can keep track of a list of <see
        /// cref="Document"/>s to compute classified spans for, only paying attention to the last version of the <see
        /// cref="Document"/> we encounter.
        /// </summary>
        private class DocumentByIdEqualityComparer : IEqualityComparer<Document>
        {
            public static readonly IEqualityComparer<Document> Instance = new DocumentByIdEqualityComparer();

            public bool Equals(Document x, Document y)
                => x?.Id == y?.Id;

            public int GetHashCode(Document obj)
                => obj?.Id.GetHashCode() ?? 0;
        }
    }
}
