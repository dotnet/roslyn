// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
