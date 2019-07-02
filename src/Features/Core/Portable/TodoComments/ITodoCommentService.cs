// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// Description of a TODO comment type to find in a user's comments.
    /// </summary>
    internal readonly struct TodoCommentDescriptor
    {
        public string Text { get; }
        public int Priority { get; }

        public TodoCommentDescriptor(string text, int priority) : this()
        {
            Text = text;
            Priority = priority;
        }
    }

    /// <summary>
    /// A TODO comment that has been found within the user's code.
    /// </summary>
    internal readonly struct TodoComment
    {
        public TodoCommentDescriptor Descriptor { get; }
        public string Message { get; }
        public int Position { get; }

        public TodoComment(TodoCommentDescriptor descriptor, string message, int position) : this()
        {
            Descriptor = descriptor;
            Message = message;
            Position = position;
        }
    }

    internal interface ITodoCommentService : ILanguageService
    {
        Task<IList<TodoComment>> GetTodoCommentsAsync(Document document, IList<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken);
    }
}
