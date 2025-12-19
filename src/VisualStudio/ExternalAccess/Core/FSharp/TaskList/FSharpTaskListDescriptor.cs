// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.TaskList;

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.FSharp.TaskList;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.TaskList;
#endif

internal readonly struct FSharpTaskListDescriptor
{
    internal readonly TaskListItemDescriptor Descriptor;

    public string Text => Descriptor.Text;

    internal FSharpTaskListDescriptor(TaskListItemDescriptor descriptor)
    {
        Descriptor = descriptor;
    }
}
