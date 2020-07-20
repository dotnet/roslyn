// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// Callback the host (VS) passes to the OOP service to allow it to send batch notifications about todo comments.
    /// </summary>
    internal interface ITodoCommentsListener
    {
        Task ReportTodoCommentDataAsync(DocumentId documentId, ImmutableArray<TodoCommentData> data, CancellationToken cancellationToken);
    }
}
