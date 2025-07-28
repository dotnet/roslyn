// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class SyntaxListExtensions
{
    extension<T>(SyntaxList<T> syntaxList) where T : SyntaxNode
    {
        public SyntaxList<T> RemoveRange(int index, int count)
        {
            var result = new List<T>(syntaxList);
            result.RemoveRange(index, count);
            return [.. result];
        }
    }

    extension<T>(SyntaxList<T> list) where T : SyntaxNode
    {
        public SyntaxList<T> Insert(int index, T item) => [.. list.Take(index).Concat(item).Concat(list.Skip(index))];
    }
}
