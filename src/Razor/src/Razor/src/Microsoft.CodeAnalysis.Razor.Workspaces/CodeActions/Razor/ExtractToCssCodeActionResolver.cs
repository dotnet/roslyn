// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class ExtractToCssCodeActionResolver(
    LanguageServerFeatureOptions languageServerFeatureOptions,
    IFileSystem fileSystem) : IRazorCodeActionResolver
{
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions = languageServerFeatureOptions;
    private readonly IFileSystem _fileSystem = fileSystem;

    public string Action => LanguageServerConstants.CodeActions.ExtractToCss;

    public async Task<WorkspaceEdit?> ResolveAsync(DocumentContext documentContext, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var actionParams = data.Deserialize<ExtractToCssCodeActionParams>();
        if (actionParams is null)
        {
            return null;
        }

        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var cssFilePath = $"{FilePathNormalizer.Normalize(documentContext.Uri.GetAbsoluteOrUNCPath())}.css";
        var cssFileUri = LspFactory.CreateFilePathUri(cssFilePath, _languageServerFeatureOptions);

        var text = await documentContext.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);

        var cssContent = text.ToString(new TextSpan(actionParams.ExtractStart, actionParams.ExtractEnd - actionParams.ExtractStart)).Trim();
        var removeRange = codeDocument.Source.Text.GetRange(actionParams.RemoveStart, actionParams.RemoveEnd);

        var codeDocumentIdentifier = new OptionalVersionedTextDocumentIdentifier { DocumentUri = new(documentContext.Uri) };
        var cssDocumentIdentifier = new OptionalVersionedTextDocumentIdentifier { DocumentUri = new(cssFileUri) };

        using var changes = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>(capacity: 3);

        // First, an edit to remove the script tag and its contents.
        changes.Add(new TextDocumentEdit
        {
            TextDocument = codeDocumentIdentifier,
            Edits = [LspFactory.CreateTextEdit(removeRange, string.Empty)]
        });

        if (_fileSystem.FileExists(cssFilePath))
        {
            // CSS file already exists, insert the content at the end.
            GetLastLineNumberAndLength(cssFilePath, out var lastLineNumber, out var lastLineLength);

            changes.Add(new TextDocumentEdit
            {
                TextDocument = cssDocumentIdentifier,
                Edits = [LspFactory.CreateTextEdit(
                    position: (lastLineNumber, lastLineLength),
                    newText: lastLineNumber == 0 && lastLineLength == 0
                        ? cssContent
                        : Environment.NewLine + Environment.NewLine + cssContent)]
            });
        }
        else
        {
            // No CSS file, create it and fill it in
            changes.Add(new CreateFile { DocumentUri = cssDocumentIdentifier.DocumentUri });
            changes.Add(new TextDocumentEdit
            {
                TextDocument = cssDocumentIdentifier,
                Edits = [LspFactory.CreateTextEdit(position: (0, 0), cssContent)]
            });
        }

        return new WorkspaceEdit
        {
            DocumentChanges = changes.ToArray(),
        };
    }

    private void GetLastLineNumberAndLength(string cssFilePath, out int lastLineNumber, out int lastLineLength)
    {
        using var stream = _fileSystem.OpenReadStream(cssFilePath);
        GetLastLineNumberAndLength(stream, bufferSize: 4096, out lastLineNumber, out lastLineLength);
    }

    private static void GetLastLineNumberAndLength(Stream stream, int bufferSize, out int lastLineNumber, out int lastLineLength)
    {
        lastLineNumber = 0;
        lastLineLength = 0;

        using var _ = ArrayPool<char>.Shared.GetPooledArray(bufferSize, out var buffer);
        using var reader = new StreamReader(stream);

        var currLineLength = 0;
        var currLineNumber = 0;

        int charsRead;
        while ((charsRead = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            var chunk = buffer.AsSpan(0, charsRead);
            while (true)
            {
                // Since we're only concerned with the last line length, we don't need to worry about \r\n. Strictly speaking,
                // we're incorrectly counting the \r in the line length, but since the last line can't end with a \n (since that
                // starts a new line) it doesn't actually change the output of the method.
                var index = chunk.IndexOf('\n');
                if (index == -1)
                {
                    currLineLength += chunk.Length;
                    break;
                }

                currLineNumber++;
                currLineLength = 0;
                chunk = chunk[(index + 1)..];
            }
        }

        lastLineNumber = currLineNumber;
        lastLineLength = currLineLength;
    }

    internal readonly struct TestAccessor
    {
        public static void GetLastLineNumberAndLength(Stream stream, int bufferSize, out int lastLineNumber, out int lastLineLength)
        {
            ExtractToCssCodeActionResolver.GetLastLineNumberAndLength(stream, bufferSize, out lastLineNumber, out lastLineLength);
        }
    }
}
