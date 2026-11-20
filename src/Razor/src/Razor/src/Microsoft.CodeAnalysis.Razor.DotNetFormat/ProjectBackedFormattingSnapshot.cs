// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Remote.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

// Adapts a Razor AdditionalDocument to the shared formatting snapshot contract. Generation stays on Roslyn's normal
// Project pipeline so dotnet format shares the project's incremental generator state instead of creating a GeneratorDriver.
internal sealed class ProjectBackedFormattingSnapshot : IDocumentSnapshot
{
    private RazorCodeDocument? _codeDocument;
    private RazorProjectGeneratorRunResult? _generatorRunResult;
    private readonly TextDocument _document;

    private ProjectBackedFormattingSnapshot(
        TextDocument document,
        RazorProjectGeneratorRunResult? generatorRunResult,
        RazorCodeDocument? codeDocument)
    {
        _document = document;
        _generatorRunResult = generatorRunResult;
        _codeDocument = codeDocument;
    }

    public RazorCodeDocument CodeDocument
        => _codeDocument ?? throw new InvalidOperationException("The Razor code document has not been generated.");

    public string FilePath
        => _document.FilePath ?? throw new InvalidOperationException("The Razor document does not have a file path.");

    public RazorFileKind FileKind => FileKinds.GetFileKindFromPath(FilePath);

    public async ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken)
    {
        if (_codeDocument is null)
        {
            _generatorRunResult = await RazorProjectGeneratorRunResult.TryCreateAsync(_document.Project, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"The Razor source generator was no longer available for project '{_document.Project.Name}'.");

            _codeDocument = _generatorRunResult.GetRequiredCodeDocument(FilePath);
        }

        return _codeDocument;
    }

    public async ValueTask<SyntaxTree> GetCSharpSyntaxTreeAsync(
        bool declarationDocument,
        CancellationToken cancellationToken)
    {
        _generatorRunResult ??= await RazorProjectGeneratorRunResult.TryCreateAsync(_document.Project, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"The Razor source generator was no longer available for project '{_document.Project.Name}'.");

        var generatedDocument = await _generatorRunResult.GetRequiredSourceGeneratedDocumentAsync(
            FilePath,
            declarationDocument,
            cancellationToken).ConfigureAwait(false);

        return await generatedDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Generated document '{generatedDocument.Name}' did not have a syntax tree.");
    }

    public IDocumentSnapshot WithText(SourceText text)
    {
        // Replacing the AdditionalDocument in a new Solution snapshot invalidates the relevant incremental generator
        // inputs. Clear the cached run result so the next request observes outputs for this updated project snapshot.
        var updatedDocument = _document.Project.Solution
            .WithAdditionalDocumentText(_document.Id, text)
            .GetAdditionalDocument(_document.Id)
            ?? throw new InvalidOperationException($"Could not update Razor document '{FilePath}'.");

        return new ProjectBackedFormattingSnapshot(
            updatedDocument,
            generatorRunResult: null,
            codeDocument: null);
    }

    public static async Task<ProjectBackedFormattingSnapshot?> TryCreateAsync(
        TextDocument document,
        CancellationToken cancellationToken)
    {
        var generatorResult = await RazorProjectGeneratorRunResult.TryCreateAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (generatorResult is null)
        {
            return null;
        }

        var filePath = document.FilePath
            ?? throw new InvalidOperationException("The Razor document does not have a file path.");
        var codeDocument = generatorResult.GetRequiredCodeDocument(filePath);

        return new(document, generatorResult, codeDocument);
    }
}
