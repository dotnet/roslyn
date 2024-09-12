// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.TaskList;

internal enum TaskListItemPriority
{
    Low,
    Medium,
    High,
}

/// <summary>
/// Description of a TODO comment type to find in a user's comments.
/// </summary>
[DataContract]
internal readonly struct TaskListItemDescriptor(string text, TaskListItemPriority priority)
{
    [DataMember(Order = 0)]
    public string Text { get; } = text;
    [DataMember(Order = 1)]
    public TaskListItemPriority Priority { get; } = priority;

    public static ImmutableArray<TaskListItemDescriptor> Parse(ImmutableArray<string> items)
    {
        using var _ = ArrayBuilder<TaskListItemDescriptor>.GetInstance(out var result);

        foreach (var item in items)
        {
            if (item.Split(':') is [var token, var priorityString] &&
                !string.IsNullOrWhiteSpace(token) &&
                int.TryParse(priorityString, NumberStyles.None, CultureInfo.InvariantCulture, out var encoded))
            {
                // From:
                // https://devdiv.visualstudio.com/DevDiv/_git/VS?path=/src/env/ErrorList/Pkg/Shims/TaskListOptions.cs&version=GBmain&line=133&lineEnd=134&lineStartColumn=1&lineEndColumn=98&lineStyle=plain&_a=contents
#if false
                // I've no idea why the strange conversion (legacy mapping from __VSERRORCATEGORY?).
                private static int EncodedValueFromPriority(CommentTaskPriority p) { return 3 - (int)p; }
                private static CommentTaskPriority PriorityFromEncodedValue(int v) { return (CommentTaskPriority)(3 - v); }
#endif
                // In other words, the actual VS enum here goes from high-to-low priority, but the values are
                // encoded low-to-high. So we undo this conversion to map from the encoded priority values to what
                // they represent.
                var priority = encoded switch
                {
                    1 => TaskListItemPriority.Low,
                    2 => TaskListItemPriority.Medium,
                    3 => TaskListItemPriority.High,
                    _ => TaskListItemPriority.Medium,
                };
                result.Add(new TaskListItemDescriptor(token, priority));
            }
        }

        return result.ToImmutable();
    }
}
