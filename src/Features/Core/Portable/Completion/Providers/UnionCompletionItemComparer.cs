// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal class UnionCompletionItemComparer : IEqualityComparer<CompletionItem>
    {
        public static UnionCompletionItemComparer Instance { get; } = new UnionCompletionItemComparer();

        private UnionCompletionItemComparer()
        {
        }

        public bool Equals(CompletionItem x, CompletionItem y)
        {
            return x.DisplayText == y.DisplayText &&
                (x.Tags == y.Tags || System.Linq.Enumerable.SequenceEqual(x.Tags, y.Tags));
        }

        public int GetHashCode(CompletionItem obj)
            => Hash.Combine(obj.DisplayText.GetHashCode(), obj.Tags.Length);
    }
}
