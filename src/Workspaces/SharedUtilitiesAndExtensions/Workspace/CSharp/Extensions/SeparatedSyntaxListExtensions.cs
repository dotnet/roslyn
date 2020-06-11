// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SeparatedSyntaxListExtensions
    {
        public static SeparatedSyntaxList<T> InsertRangeWithTrailingSeparator<T>(
            this SeparatedSyntaxList<T> separatedList, int index, IEnumerable<T> nodes, SyntaxKind separator)
            where T : SyntaxNode
        {
            var newList = separatedList.InsertRange(index, nodes);
            if (index < separatedList.Count)
                return newList;

            var nodesAndTokens = newList.GetWithSeparators();
            if (!nodesAndTokens.Last().IsNode)
                return newList;

            return SyntaxFactory.SeparatedList<T>(nodesAndTokens.Add(SyntaxFactory.Token(separator)));
        }
    }
}
