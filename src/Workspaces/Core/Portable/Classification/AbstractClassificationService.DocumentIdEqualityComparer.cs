// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract partial class AbstractClassificationService
    {
        private class DocumentIdEqualityComparer : IEqualityComparer<Document>
        {
            public static readonly IEqualityComparer<Document> Instance = new DocumentIdEqualityComparer();

            public bool Equals([AllowNull] Document x, [AllowNull] Document y)
                => x?.Id == y?.Id;

            public int GetHashCode([DisallowNull] Document obj)
                => obj?.Id.GetHashCode() ?? 0;
        }
    }
}
