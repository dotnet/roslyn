// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteDocumentSnapshot : IDocumentSnapshot
{
    public TextDocument TextDocument { get; }
    public RemoteProjectSnapshot ProjectSnapshot { get; }

    private RazorCodeDocument? _codeDocument;
    private SourceGeneratedDocument? _generatedDocument;

    public RemoteDocumentSnapshot(TextDocument textDocument, RemoteProjectSnapshot projectSnapshot)
    {
        if (!textDocument.IsRazorDocument())
        {
            throw new ArgumentException(SR.Document_is_not_a_Razor_document);
        }

        TextDocument = textDocument;
        ProjectSnapshot = projectSnapshot;
    }

    public RazorFileKind FileKind => FileKinds.GetFileKindFromPath(FilePath);
    public string FilePath => TextDocument.FilePath.AssumeNotNull();
    public string TargetPath => TextDocument.FilePath.AssumeNotNull();

    public IProjectSnapshot Project => ProjectSnapshot;

    public int Version => -999; // We don't expect to use this in cohosting, but plenty of existing code logs it's value

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        return TryGetText(out var result)
            ? new(result)
            : new(TextDocument.GetTextAsync(cancellationToken));
    }

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
    {
        return TryGetTextVersion(out var result)
            ? new(result)
            : new(TextDocument.GetTextVersionAsync(cancellationToken));
    }

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
        => TextDocument.TryGetText(out result);

    public bool TryGetTextVersion(out VersionStamp result)
        => TextDocument.TryGetTextVersion(out result);

    public bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? result)
        => (result = _codeDocument) is not null;

    public async ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken)
    {
        if (_codeDocument is not null)
        {
            return _codeDocument;
        }

        var document = await ProjectSnapshot.GetRequiredCodeDocumentAsync(this, cancellationToken).ConfigureAwait(false);
        return InterlockedOperations.Initialize(ref _codeDocument, document);
    }

    public IDocumentSnapshot WithText(SourceText text)
    {
        var id = TextDocument.Id;
        var newDocument = TextDocument.Project.Solution
            .WithAdditionalDocumentText(id, text)
            .GetAdditionalDocument(id)
            .AssumeNotNull();

        var snapshotManager = ProjectSnapshot.SolutionSnapshot.SnapshotManager;
        return snapshotManager.GetSnapshot(newDocument);
    }

    public async ValueTask<SourceGeneratedDocument> GetGeneratedDocumentAsync(CancellationToken cancellationToken)
    {
        if (_generatedDocument is not null)
        {
            return _generatedDocument;
        }

        var generatedDocument = await ProjectSnapshot.GetRequiredGeneratedDocumentAsync(this, cancellationToken).ConfigureAwait(false);
        return InterlockedOperations.Initialize(ref _generatedDocument, generatedDocument);
    }

    public ValueTask<SyntaxTree> GetCSharpSyntaxTreeAsync(CancellationToken cancellationToken)
    {
        var document = _generatedDocument;
        if (document is not null &&
            document.TryGetSyntaxTree(out var tree))
        {
            return new(tree.AssumeNotNull());
        }

        return GetCSharpSyntaxTreeCoreAsync(document, cancellationToken);

        async ValueTask<SyntaxTree> GetCSharpSyntaxTreeCoreAsync(Document? document, CancellationToken cancellationToken)
        {
            document ??= await GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return tree.AssumeNotNull();
        }
    }
}
