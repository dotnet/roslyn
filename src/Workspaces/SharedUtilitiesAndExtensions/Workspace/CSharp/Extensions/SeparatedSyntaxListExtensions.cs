// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class SeparatedSyntaxListExtensions
{
    extension<T>(SeparatedSyntaxList<T> separatedList) where T : SyntaxNode
    {
        public SeparatedSyntaxList<T> AddRangeWithTrailingSeparator(
IEnumerable<T> nodes, SyntaxKind separator = SyntaxKind.CommaToken)
        {
            return separatedList.InsertRangeWithTrailingSeparator(separatedList.Count, nodes, separator);
        }

        public SeparatedSyntaxList<T> InsertRangeWithTrailingSeparator(
    int index, IEnumerable<T> nodes, SyntaxKind separator = SyntaxKind.CommaToken)
        {
            var newList = separatedList.InsertRange(index, nodes);
            if (index < separatedList.Count)
                return newList;

            return newList.Count == newList.SeparatorCount
                ? newList
                : SyntaxFactory.SeparatedList<T>(newList.GetWithSeparators().Add(SyntaxFactory.Token(separator)));
        }
    }
}
