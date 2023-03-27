// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.TaskList;

/// <summary>
/// Serialization type used to pass information to/from OOP and VS.
/// </summary>
[DataContract]
internal readonly record struct TaskListItem(
    [property: DataMember(Order = 0)] TaskListItemPriority Priority,
    [property: DataMember(Order = 1)] string Message,
    [property: DataMember(Order = 2)] DocumentId DocumentId,
    [property: DataMember(Order = 3)] FileLinePositionSpan Span,
    [property: DataMember(Order = 4)] FileLinePositionSpan MappedSpan);
