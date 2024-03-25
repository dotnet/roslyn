// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.TaskList;

[DataContract]
internal readonly record struct TaskListOptions
{
    private static readonly ImmutableArray<string> s_defaultDescriptors = ["HACK:2", "TODO:2", "UNDONE:2", "UnresolvedMergeConflict:3"];

    [DataMember]
    public ImmutableArray<string> Descriptors { get; init; } = s_defaultDescriptors;

    [DataMember]
    public bool ComputeForClosedFiles { get; init; } = true;

    public TaskListOptions()
    {
    }

    public static readonly TaskListOptions Default = new();
}
