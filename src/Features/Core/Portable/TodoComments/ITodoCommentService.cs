﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.TodoComments
{
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

        internal TodoCommentData CreateSerializableData(
            Document document, SourceText text, SyntaxTree tree)
        {
            // make sure given position is within valid text range.
            var textSpan = new TextSpan(Math.Min(text.Length, Math.Max(0, this.Position)), 0);

            var location = tree.GetLocation(textSpan);
            var originalLineInfo = location.GetLineSpan();
            var mappedLineInfo = location.GetMappedLineSpan();

            return new TodoCommentData
            {
                Priority = this.Descriptor.Priority,
                Message = this.Message,
                DocumentId = document.Id,
                OriginalLine = originalLineInfo.StartLinePosition.Line,
                OriginalColumn = originalLineInfo.StartLinePosition.Character,
                OriginalFilePath = document.FilePath,
                MappedLine = mappedLineInfo.StartLinePosition.Line,
                MappedColumn = mappedLineInfo.StartLinePosition.Character,
                MappedFilePath = mappedLineInfo.GetMappedFilePathIfExist(),
            };
        }
    }

    internal interface ITodoCommentService : ILanguageService
    {
        Task<ImmutableArray<TodoComment>> GetTodoCommentsAsync(Document document, ImmutableArray<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken);
    }
}
