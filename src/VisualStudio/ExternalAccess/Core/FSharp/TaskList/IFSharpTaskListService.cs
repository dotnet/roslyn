// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.TaskList;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.TaskList;
#endif

internal interface IFSharpTaskListService
{
    Task<ImmutableArray<FSharpTaskListItem>> GetTaskListItemsAsync(Document document, ImmutableArray<FSharpTaskListDescriptor> descriptors, CancellationToken cancellationToken);
}
