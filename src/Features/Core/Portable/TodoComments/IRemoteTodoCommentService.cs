// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// interface exist to strongly type todo comment remote service
    /// </summary>
    internal interface IRemoteTodoCommentService
    {
        Task<IReadOnlyList<TodoComment>> GetTodoCommentsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, IReadOnlyList<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken);
    }
}
