// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;
using System.Collections.Immutable;

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
            return x.DisplayText == y.DisplayText && CompletionHelper.TagsEqual(x, y);
        }

        public int GetHashCode(CompletionItem obj)
        {
            return Hash.Combine(obj.DisplayText.GetHashCode(), obj.Tags.Length);
        }
    }
}
