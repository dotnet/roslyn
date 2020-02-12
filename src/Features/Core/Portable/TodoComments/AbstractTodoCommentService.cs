﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.TodoComments
{
    internal abstract class AbstractTodoCommentService : ITodoCommentService
    {
        // we hold onto workspace to make sure given input (Document) belong to right workspace.
        // since remote host is from workspace service, different workspace can have different expectation
        // on remote host, so we need to make sure given input always belong to right workspace where
        // the session belong to.
        private readonly Workspace _workspace;

        protected AbstractTodoCommentService(Workspace workspace)
        {
            _workspace = workspace;
        }

        protected abstract bool PreprocessorHasComment(SyntaxTrivia trivia);
        protected abstract bool IsSingleLineComment(SyntaxTrivia trivia);
        protected abstract bool IsMultilineComment(SyntaxTrivia trivia);
        protected abstract bool IsIdentifierCharacter(char ch);

        protected abstract string GetNormalizedText(string message);
        protected abstract int GetCommentStartingIndex(string message);
        protected abstract void AppendTodoComments(IList<TodoCommentDescriptor> commentDescriptors, SyntacticDocument document, SyntaxTrivia trivia, List<TodoComment> todoList);

        public async Task<IList<TodoComment>> GetTodoCommentsAsync(Document document, IList<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken)
        {
            // make sure given input is right one
            Contract.ThrowIfFalse(_workspace == document.Project.Solution.Workspace);

            // run todo scanner on remote host. 
            // we only run closed files to make open document to have better responsiveness. 
            // also we cache everything related to open files anyway, no saving by running
            // them in remote host
            if (!document.IsOpen())
            {
                var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var result = await client.TryRunRemoteAsync<IList<TodoComment>>(
                        WellKnownServiceHubServices.CodeAnalysisService,
                        nameof(IRemoteTodoCommentService.GetTodoCommentsAsync),
                        document.Project.Solution,
                        new object[] { document.Id, commentDescriptors },
                        callbackTarget: null,
                        cancellationToken).ConfigureAwait(false);

                    if (result.HasValue)
                    {
                        return result.Value;
                    }
                }
            }

            return await GetTodoCommentsInCurrentProcessAsync(document, commentDescriptors, cancellationToken).ConfigureAwait(false);
        }

        private async Task<IList<TodoComment>> GetTodoCommentsInCurrentProcessAsync(
            Document document, IList<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // strongly hold onto text and tree
            var syntaxDoc = await SyntacticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // reuse list
            var todoList = new List<TodoComment>();

            foreach (var trivia in syntaxDoc.Root.DescendantTrivia())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!ContainsComments(trivia))
                {
                    continue;
                }

                AppendTodoComments(commentDescriptors, syntaxDoc, trivia, todoList);
            }

            return todoList;
        }

        private bool ContainsComments(SyntaxTrivia trivia)
        {
            return PreprocessorHasComment(trivia) || IsSingleLineComment(trivia) || IsMultilineComment(trivia);
        }

        protected void AppendTodoCommentInfoFromSingleLine(IList<TodoCommentDescriptor> commentDescriptors, SyntacticDocument document, string message, int start, List<TodoComment> todoList)
        {
            var index = GetCommentStartingIndex(message);
            if (index >= message.Length)
            {
                return;
            }

            var normalized = GetNormalizedText(message);
            foreach (var commentDescriptor in commentDescriptors)
            {
                var token = commentDescriptor.Text;
                if (string.Compare(
                        normalized, index, token, indexB: 0,
                        length: token.Length, comparisonType: StringComparison.OrdinalIgnoreCase) != 0)
                {
                    continue;
                }

                if ((message.Length > index + token.Length) && IsIdentifierCharacter(message[index + token.Length]))
                {
                    // they wrote something like:
                    // todoboo
                    // instead of
                    // todo
                    continue;
                }

                todoList.Add(new TodoComment(commentDescriptor, message.Substring(index), start + index));
            }
        }

        protected void ProcessMultilineComment(IList<TodoCommentDescriptor> commentDescriptors, SyntacticDocument document, SyntaxTrivia trivia, int postfixLength, List<TodoComment> todoList)
        {
            // this is okay since we know it is already alive
            var text = document.Text;

            var fullSpan = trivia.FullSpan;
            var fullString = trivia.ToFullString();

            var startLine = text.Lines.GetLineFromPosition(fullSpan.Start);
            var endLine = text.Lines.GetLineFromPosition(fullSpan.End);

            // single line multiline comments
            if (startLine.LineNumber == endLine.LineNumber)
            {
                var message = postfixLength == 0 ? fullString : fullString.Substring(0, fullSpan.Length - postfixLength);
                AppendTodoCommentInfoFromSingleLine(commentDescriptors, document, message, fullSpan.Start, todoList);
                return;
            }

            // multiline 
            var startMessage = text.ToString(TextSpan.FromBounds(fullSpan.Start, startLine.End));
            AppendTodoCommentInfoFromSingleLine(commentDescriptors, document, startMessage, fullSpan.Start, todoList);

            for (var lineNumber = startLine.LineNumber + 1; lineNumber < endLine.LineNumber; lineNumber++)
            {
                var line = text.Lines[lineNumber];
                var message = line.ToString();

                AppendTodoCommentInfoFromSingleLine(commentDescriptors, document, message, line.Start, todoList);
            }

            var length = fullSpan.End - endLine.Start;
            if (length >= postfixLength)
            {
                length -= postfixLength;
            }

            var endMessage = text.ToString(new TextSpan(endLine.Start, length));
            AppendTodoCommentInfoFromSingleLine(commentDescriptors, document, endMessage, endLine.Start, todoList);
        }
    }
}
