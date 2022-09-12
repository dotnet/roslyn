// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.TaskList
{
    /// <summary>
    /// Description of a TODO comment type to find in a user's comments.
    /// </summary>
    [DataContract]
    internal readonly struct TaskListItemDescriptor
    {
        [DataMember(Order = 0)]
        public string Text { get; }
        [DataMember(Order = 1)]
        public int Priority { get; }

        public TaskListItemDescriptor(string text, int priority)
        {
            Text = text;
            Priority = priority;
        }

        public static ImmutableArray<TaskListItemDescriptor> Parse(ImmutableArray<string> items)
        {
            using var _ = ArrayBuilder<TaskListItemDescriptor>.GetInstance(out var result);

            foreach (var item in items)
            {
                if (item.Split(':') is [var token, var priorityString] &&
                    !string.IsNullOrWhiteSpace(token) &&
                    int.TryParse(priorityString, NumberStyles.None, CultureInfo.InvariantCulture, out var priority))
                {
                    result.Add(new TaskListItemDescriptor(token, priority));
                }
            }

            return result.ToImmutable();
        }
    }
}
