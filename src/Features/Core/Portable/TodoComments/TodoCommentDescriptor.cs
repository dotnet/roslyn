// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// Description of a TODO comment type to find in a user's comments.
    /// </summary>
    internal readonly struct TodoCommentDescriptor
    {
        public string Text { get; }
        public int Priority { get; }

        public TodoCommentDescriptor(string text, int priority)
        {
            Text = text;
            Priority = priority;
        }

        public static ImmutableArray<TodoCommentDescriptor> Parse(ImmutableArray<string> items)
        {
            using var _ = ArrayBuilder<TodoCommentDescriptor>.GetInstance(out var result);

            foreach (var item in items)
            {
                if (item.Split(':') is [var token, var priorityString] &&
                    !string.IsNullOrWhiteSpace(token) &&
                    int.TryParse(priorityString, NumberStyles.None, CultureInfo.InvariantCulture, out var priority))
                {
                    result.Add(new TodoCommentDescriptor(token, priority));
                }
            }

            return result.ToImmutable();
        }
    }
}
