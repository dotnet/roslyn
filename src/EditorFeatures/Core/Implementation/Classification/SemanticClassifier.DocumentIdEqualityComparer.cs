// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal partial class SemanticClassifier
    {
        private class DocumentIdEqualityComparer : IEqualityComparer<Document>
        {
            public static readonly IEqualityComparer<Document> Instance = new DocumentIdEqualityComparer();

            public bool Equals(Document x, Document y)
                => x?.Id == y?.Id;

            public int GetHashCode(Document obj)
                => obj?.Id.GetHashCode() ?? 0;
        }
    }
}
