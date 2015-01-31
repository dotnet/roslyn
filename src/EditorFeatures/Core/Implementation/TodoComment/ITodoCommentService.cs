// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Description of a TODO comment type to find in a user's comments.
    /// </summary>
    internal struct TodoCommentDescriptor
    {
        public string Text { get; private set; }
        public int Priority { get; private set; }

        public TodoCommentDescriptor(string text, int priority) : this()
        {
            this.Text = text;
            this.Priority = priority;
        }
    }

    /// <summary>
    /// A TODO comment that has been found within the user's code.
    /// </summary>
    internal struct TodoComment
    {
        public TodoCommentDescriptor Descriptor { get; private set; }
        public string Message { get; private set; }
        public int Position { get; private set; }

        public TodoComment(TodoCommentDescriptor descriptor, string message, int position) : this()
        {
            this.Descriptor = descriptor;
            this.Message = message;
            this.Position = position;
        }
    }

    internal interface ITodoCommentService : ILanguageService
    {
        Task<IList<TodoComment>> GetTodoCommentsAsync(Document document, ImmutableArray<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken);
    }
}
