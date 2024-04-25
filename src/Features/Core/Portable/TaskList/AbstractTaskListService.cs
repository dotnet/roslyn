// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.TaskList;

internal abstract class AbstractTaskListService : ITaskListService
{
    protected abstract bool PreprocessorHasComment(SyntaxTrivia trivia);
    protected abstract bool IsSingleLineComment(SyntaxTrivia trivia);
    protected abstract bool IsMultilineComment(SyntaxTrivia trivia);
    protected abstract bool IsIdentifierCharacter(char ch);

    protected abstract string GetNormalizedText(string message);
    protected abstract int GetCommentStartingIndex(string message);
    protected abstract void AppendTaskListItems(ImmutableArray<TaskListItemDescriptor> descriptors, SyntacticDocument document, SyntaxTrivia trivia, ArrayBuilder<TaskListItem> items);

    public async Task<ImmutableArray<TaskListItem>> GetTaskListItemsAsync(
        Document document,
        ImmutableArray<TaskListItemDescriptor> descriptors,
        CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var result = await client.TryInvokeAsync<IRemoteTaskListService, ImmutableArray<TaskListItem>>(
                document.Project,
                (service, checksum, cancellationToken) => service.GetTaskListItemsAsync(checksum, document.Id, descriptors, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.HasValue)
                return [];

            return result.Value;
        }

        return await GetTaskListItemsInProcessAsync(document, descriptors, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<TaskListItem>> GetTaskListItemsInProcessAsync(
        Document document,
        ImmutableArray<TaskListItemDescriptor> descriptors,
        CancellationToken cancellationToken)
    {
        if (descriptors.IsEmpty)
            return [];

        cancellationToken.ThrowIfCancellationRequested();

        // strongly hold onto text and tree
        var syntaxDoc = await SyntacticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // reuse list
        using var _ = ArrayBuilder<TaskListItem>.GetInstance(out var items);

        foreach (var trivia in syntaxDoc.Root.DescendantTrivia())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ContainsComments(trivia))
                continue;

            AppendTaskListItems(descriptors, syntaxDoc, trivia, items);
        }

        return items.ToImmutableAndClear();
    }

    private bool ContainsComments(SyntaxTrivia trivia)
        => PreprocessorHasComment(trivia) || IsSingleLineComment(trivia) || IsMultilineComment(trivia);

    protected void AppendTaskListItemsOnSingleLine(
        ImmutableArray<TaskListItemDescriptor> descriptors,
        SyntacticDocument document,
        string message, int start,
        ArrayBuilder<TaskListItem> items)
    {
        var index = GetCommentStartingIndex(message);
        if (index >= message.Length)
            return;

        var normalized = GetNormalizedText(message);
        foreach (var commentDescriptor in descriptors)
        {
            var token = commentDescriptor.Text;
            if (string.Compare(
                    normalized, index, token, indexB: 0,
                    length: token.Length, comparisonType: StringComparison.OrdinalIgnoreCase) != 0)
            {
                continue;
            }

            if (message.Length > index + token.Length && IsIdentifierCharacter(message[index + token.Length]))
                // they wrote something like:
                // todoboo
                // instead of
                // todo
                continue;

            var trimmedMessage = message[index..];
            var position = start + index;

            // Go through SyntaxTree so that any `#line` remapping is picked up
            var location = document.SyntaxTree.GetLocation(new TextSpan(position, 0));

            items.Add(new TaskListItem(
                commentDescriptor.Priority, trimmedMessage, document.Document.Id, location.GetLineSpan(), location.GetMappedLineSpan()));
        }
    }

    protected void ProcessMultilineComment(
        ImmutableArray<TaskListItemDescriptor> commentDescriptors,
        SyntacticDocument document,
        SyntaxTrivia trivia, int postfixLength,
        ArrayBuilder<TaskListItem> items)
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
            var message = postfixLength == 0 ? fullString : fullString[..(fullSpan.Length - postfixLength)];
            AppendTaskListItemsOnSingleLine(commentDescriptors, document, message, fullSpan.Start, items);
            return;
        }

        // multiline 
        var startMessage = text.ToString(TextSpan.FromBounds(fullSpan.Start, startLine.End));
        AppendTaskListItemsOnSingleLine(commentDescriptors, document, startMessage, fullSpan.Start, items);

        for (var lineNumber = startLine.LineNumber + 1; lineNumber < endLine.LineNumber; lineNumber++)
        {
            var line = text.Lines[lineNumber];
            var message = line.ToString();

            AppendTaskListItemsOnSingleLine(commentDescriptors, document, message, line.Start, items);
        }

        var length = fullSpan.End - endLine.Start;
        if (length >= postfixLength)
            length -= postfixLength;

        var endMessage = text.ToString(new TextSpan(endLine.Start, length));
        AppendTaskListItemsOnSingleLine(commentDescriptors, document, endMessage, endLine.Start, items);
    }
}
