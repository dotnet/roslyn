// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class TriviaHelpers
{
    public static SyntaxTriviaList CreateTriviaListFromTo(SyntaxTriviaList triviaList, int startIndex, int endIndex)
    {
        if (startIndex > endIndex)
            return default;

        if (startIndex == 0 && endIndex == triviaList.Count)
            return triviaList;

        using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var builder);
        for (var i = startIndex; i <= endIndex; i++)
            builder.Add(triviaList[i]);

        return new SyntaxTriviaList(builder);
    }
}
