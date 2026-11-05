// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
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
    public static async Task AssertWorkspaceEditAsync(this WorkspaceEdit workspaceEdit, Solution solution, IEnumerable<(DocumentUri fileUri, string contents)> expectedChanges, CancellationToken cancellationToken)
    {
        Assert.NotNull(workspaceEdit.DocumentChanges);
        var changes = workspaceEdit.DocumentChanges;

        foreach (var change in changes)
        {
            if (change is TextDocumentEdit textDocumentEdit)
            {
                var uri = textDocumentEdit.TextDocument.Uri;
                var document = (await solution.GetTextDocumentsAsync(uri, cancellationToken)).Single();
                Assert.NotNull(document);
                var text = await document.GetTextAsync(cancellationToken);

                text = text.WithChanges(textDocumentEdit.Edits.Select(e => text.GetTextChange((TextEdit)e.Value)));

                solution = document is Document
                    ? solution.WithDocumentText(document.Id, text)
                    : solution.WithAdditionalDocumentText(document.Id, text);
            }
            else if (change is CreateFile createFile)
            {
                var uri = createFile.Uri;
                var documentId = DocumentId.CreateNewId(solution.ProjectIds.Single());
                var filePath = createFile.Uri.GetDocumentFilePathFromUri();
                var documentInfo = DocumentInfo.Create(documentId, Path.GetFileName(filePath), filePath: filePath);
                solution = solution.AddDocument(documentInfo);
            }
            else if (change is RenameFile renameFile)
            {
                var (oldUri, newUri) = (renameFile.OldUri, renameFile.NewUri);
                var newFilePath = newUri.GetDocumentFilePathFromUri();
                var document = (await solution.GetTextDocumentsAsync(oldUri, cancellationToken)).Single();
                Assert.NotNull(document);
                if (document is Document)
                {
                    solution = solution.WithDocumentFilePath(document.Id, newFilePath);
                }
                else
                {
                    var text = await document.GetTextAsync(cancellationToken);
                    solution = document.Project
                        .RemoveAdditionalDocument(document.Id)
                        .AddAdditionalDocument(Path.GetFileName(newFilePath), text, filePath: newFilePath).Project.Solution;
                }
            }
            else
            {
                Assert.Fail($"Don't know how to process a {change.Value?.GetType().Name}.");
            }
        }

        foreach (var (uri, contents) in expectedChanges)
        {
            var document = (await solution.GetTextDocumentsAsync(uri, cancellationToken)).Single();
            var text = await document.GetTextAsync(cancellationToken);
            AssertEx.EqualOrDiff(contents, text.ToString());
        }
    }
}
