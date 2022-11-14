﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal readonly struct VSTypeScriptTaskListItem
{
    public VSTypeScriptTaskListItem(VSTypeScriptTaskListItemDescriptorWrapper descriptor, string message, int position)
    {
        Descriptor = descriptor;
        Message = message;
        Position = position;
    }

    public VSTypeScriptTaskListItemDescriptorWrapper Descriptor { get; }
    public string Message { get; }
    public int Position { get; }
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
