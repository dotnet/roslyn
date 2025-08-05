// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class StringBuilderExtensions
{
    public static string ToStringAndClear(this StringBuilder builder)
    {
        var result = builder.ToString();
        builder.Clear();
        return result;
    }

    public static StringBuilder AppendJoinedValues<TNode>(
        this StringBuilder builder, string separator, SeparatedSyntaxList<TNode> values, Action<TNode, StringBuilder> append)
        where TNode : SyntaxNode
    {
        var first = true;
        foreach (var value in values)
        {
            if (!first)
                builder.Append(separator);

            first = false;
            append(value, builder);
        }

        return builder;
    }

    public static StringBuilder AppendJoinedValues<T>(this StringBuilder builder, string separator, ImmutableArray<T> values, Action<T, StringBuilder> append)
    {
        var first = true;
        foreach (var value in values)
        {
            if (!first)
                builder.Append(separator);

            first = false;
            append(value, builder);
        }

        return builder;
    }
}
