// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Roslyn.Test.Utilities;
using Roslyn.Text.Adornments;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class AssertExtensions
{
    internal static void AssertExpectedClassification(
        this ClassifiedTextRun run,
        string expectedText,
        string expectedClassificationType,
        ClassifiedTextRunStyle expectedClassificationStyle = ClassifiedTextRunStyle.Plain)
    {
        Assert.Equal(expectedText, run.Text);
        Assert.Equal(expectedClassificationType, run.ClassificationTypeName);
        Assert.Equal(expectedClassificationStyle, run.Style);
    }

    public static async Task AssertWorkspaceEditAsync(this WorkspaceEdit workspaceEdit, Solution solution, IEnumerable<(Uri fileUri, string contents)> expectedChanges, CancellationToken cancellationToken)
    {
        var changes = Assert.NotNull(workspaceEdit.DocumentChanges);

        foreach (var change in Flatten(changes))
        {
            if (change.TryGetFirst(out var textDocumentEdit))
            {
                var uri = textDocumentEdit.TextDocument.DocumentUri.GetRequiredParsedUri();
                var documentId = solution.GetDocumentIdsWithFilePath(RazorUri.GetDocumentFilePathFromUri(uri)).Single();
                var document = solution.GetDocument(documentId) ?? solution.GetAdditionalDocument(documentId);
                Assert.NotNull(document);
                var text = await document.GetTextAsync(cancellationToken);

                text = text.WithChanges(textDocumentEdit.Edits.Select(e => text.GetTextChange((TextEdit)e)));

                solution = document is Document
                    ? solution.WithDocumentText(document.Id, text)
                    : solution.WithAdditionalDocumentText(document.Id, text);
            }
            else if (change.TryGetSecond(out var createFile))
            {
                var uri = createFile.DocumentUri.GetRequiredParsedUri();
                var documentId = DocumentId.CreateNewId(solution.ProjectIds.Single());
                var filePath = createFile.DocumentUri.GetRequiredParsedUri().GetDocumentFilePath();
                var documentInfo = DocumentInfo.Create(documentId, Path.GetFileName(filePath), filePath: filePath);
                solution = solution.AddDocument(documentInfo);
            }
            else if (change.TryGetThird(out var renameFile))
            {
                var (oldUri, newUri) = (renameFile.OldDocumentUri.GetRequiredParsedUri(), renameFile.NewDocumentUri.GetRequiredParsedUri());
                var documentId = solution.GetDocumentIdsWithFilePath(RazorUri.GetDocumentFilePathFromUri(oldUri)).Single();
                var document = solution.GetDocument(documentId) ?? solution.GetAdditionalDocument(documentId);
                Assert.NotNull(document);
                if (document is Document)
                {
                    solution = solution.WithDocumentFilePath(document.Id, newUri.GetDocumentFilePath());
                }
                else
                {
                    var filePath = newUri.GetDocumentFilePath();
                    var text = await document.GetTextAsync(cancellationToken);
                    solution = document.Project
                        .RemoveAdditionalDocument(document.Id)
                        .AddAdditionalDocument(Path.GetFileName(filePath), text, filePath: filePath).Project.Solution;
                }
            }
            else
            {
                Assert.Fail($"Don't know how to process a {change.Value?.GetType().Name}.");
            }
        }

        foreach (var (uri, contents) in expectedChanges)
        {
            var document = solution.GetTextDocuments(uri).First();
            var text = await document.GetTextAsync(cancellationToken);
            AssertEx.EqualOrDiff(contents, text.ToString());
        }

        static IEnumerable<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>> Flatten(SumType<TextDocumentEdit[], SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]> documentChanges)
        {
            if (documentChanges.TryGetFirst(out var textDocumentEdits))
            {
                foreach (var edit in textDocumentEdits)
                {
                    yield return edit;
                }
            }
            else if (documentChanges.TryGetSecond(out var changes))
            {
                foreach (var change in changes)
                {
                    yield return change;
                }
            }
        }
    }
}
