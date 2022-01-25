// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal sealed class UnionCompletionItemComparer : IEqualityComparer<CompletionItem>
    {
        public static readonly UnionCompletionItemComparer Instance = new();

        private UnionCompletionItemComparer()
        {
        }

        public bool Equals(CompletionItem? x, CompletionItem? y)
            => ReferenceEquals(x, y) ||
               x is not null && y is not null && x.DisplayText == y.DisplayText && x.Tags.SequenceEqual(y.Tags);

        public int GetHashCode(CompletionItem obj)
            => Hash.Combine(obj.DisplayText.GetHashCode(), obj.Tags.Length);
    }
}
