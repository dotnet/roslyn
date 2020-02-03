// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// interface exist to strongly type todo comment remote service
    /// </summary>
    internal interface IRemoteTodoCommentService
    {
        Task<IList<TodoComment>> GetTodoCommentsAsync(DocumentId documentId, IList<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken);
    }
}
