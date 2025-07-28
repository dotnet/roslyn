// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.TaskList;

internal static class TaskListOptionsStorage
{
    public static readonly Option2<ImmutableArray<string>> Descriptors = new("dotnet_task_list_storage_descriptors", TaskListOptions.Default.Descriptors);
    public static readonly Option2<bool> ComputeTaskListItemsForClosedFiles = new("dotnet_compute_task_list_items_for_closed_files", defaultValue: true);

    extension(IGlobalOptionService globalOptions)
    {
        public TaskListOptions GetTaskListOptions()
        => new()
        {
            Descriptors = globalOptions.GetOption(Descriptors),
            ComputeForClosedFiles = globalOptions.GetOption(ComputeTaskListItemsForClosedFiles)
        };
    }
}
