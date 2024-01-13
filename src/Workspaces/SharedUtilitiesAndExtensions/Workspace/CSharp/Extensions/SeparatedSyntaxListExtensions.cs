// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class SeparatedSyntaxListExtensions
{
    public static SeparatedSyntaxList<T> AddRangeWithTrailingSeparator<T>(
        this SeparatedSyntaxList<T> separatedList, IEnumerable<T> nodes, SyntaxKind separator = SyntaxKind.CommaToken)
        where T : SyntaxNode
    {
        return separatedList.InsertRangeWithTrailingSeparator(separatedList.Count, nodes, separator);
    }

    public static SeparatedSyntaxList<T> InsertRangeWithTrailingSeparator<T>(
        this SeparatedSyntaxList<T> separatedList, int index, IEnumerable<T> nodes, SyntaxKind separator = SyntaxKind.CommaToken)
        where T : SyntaxNode
    {
        var newList = separatedList.InsertRange(index, nodes);
        if (index < separatedList.Count)
            return newList;

        return newList.Count == newList.SeparatorCount
            ? newList
            : SyntaxFactory.SeparatedList<T>(newList.GetWithSeparators().Add(SyntaxFactory.Token(separator)));
    }
}
