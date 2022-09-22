// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.TaskList;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// Description of a TODO comment type to find in a user's comments.
    /// </summary>
    [DataContract]
    [Obsolete($"Use {nameof(TaskListItemDescriptor)} instead")]
    internal readonly struct TodoCommentDescriptor
    {
        [DataMember(Order = 0)]
        public string Text { get; }
        [DataMember(Order = 1)]
        public int Priority { get; }

        public TodoCommentDescriptor(string text, int priority)
        {
            Text = text;
            Priority = priority;
        }

        public static ImmutableArray<TodoCommentDescriptor> Parse(ImmutableArray<string> items)
            => TaskListItemDescriptor.Parse(items).SelectAsArray(d => new TodoCommentDescriptor(d.Text, d.Priority));
    }
}
