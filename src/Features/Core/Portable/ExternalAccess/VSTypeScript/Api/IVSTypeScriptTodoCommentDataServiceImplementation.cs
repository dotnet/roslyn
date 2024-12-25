// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.TaskList;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal readonly struct VSTypeScriptTaskListItem(VSTypeScriptTaskListItemDescriptorWrapper descriptor, string message, int position)
{
    public VSTypeScriptTaskListItemDescriptorWrapper Descriptor { get; } = descriptor;
    public string Message { get; } = message;
    public int Position { get; } = position;
}

internal readonly struct VSTypeScriptTaskListItemDescriptorWrapper
{
    internal readonly TaskListItemDescriptor Descriptor;

    public string Text => Descriptor.Text;

    internal VSTypeScriptTaskListItemDescriptorWrapper(TaskListItemDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    public static ImmutableArray<VSTypeScriptTaskListItemDescriptorWrapper> Parse(ImmutableArray<string> items)
        => TaskListItemDescriptor.Parse(items).SelectAsArray(d => new VSTypeScriptTaskListItemDescriptorWrapper(d));
}

internal interface IVSTypeScriptTaskListServiceImplementation
{
    Task<ImmutableArray<VSTypeScriptTaskListItem>> GetTaskListItemsAsync(
        Document document, ImmutableArray<VSTypeScriptTaskListItemDescriptorWrapper> value, CancellationToken cancellationToken);
}
