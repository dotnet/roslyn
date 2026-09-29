// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteDocumentSnapshot
{
    private RazorCodeDocument? _codeDocument;
    private SourceGeneratedDocument? _generatedDocument;
    private SourceGeneratedDocument? _declGeneratedDocument;
    private bool _declGeneratedDocumentInitialized;

    public TextDocument TextDocument { get; }
    public RemoteProjectSnapshot ProjectSnapshot { get; }

    public DocumentUri Uri
    {
        get
        {
            field ??= TextDocument.GetURI();
            return field;
        }
    }

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

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        return TextDocument.TryGetText(out var result)
            ? new(result)
            : new(TextDocument.GetTextAsync(cancellationToken));
    }

    public async ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken)
    {
        if (_codeDocument is not null)
        {
            return _codeDocument;
        }

        var document = await ProjectSnapshot.GetRequiredCodeDocumentAsync(this, cancellationToken).ConfigureAwait(false);
        return InterlockedOperations.Initialize(ref _codeDocument, document);
    }

    public RemoteDocumentSnapshot WithText(SourceText text)
    {
        var id = TextDocument.Id;
        var newDocument = TextDocument.Project.Solution
            .WithAdditionalDocumentText(id, text)
            .GetAdditionalDocument(id)
            .AssumeNotNull();

        var snapshotManager = ProjectSnapshot.SolutionSnapshot.SnapshotManager;
        return snapshotManager.GetSnapshot(newDocument);
    }

    public async ValueTask<SourceGeneratedDocument?> TryGetGeneratedDocumentAsync(bool declarationDocument, CancellationToken cancellationToken)
    {
        return declarationDocument
            ? await TryGetDeclGeneratedDocumentInternalAsync(cancellationToken).ConfigureAwait(false)
            : await GetGeneratedDocumentInternalAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<SourceGeneratedDocument> GetGeneratedDocumentAsync(bool declarationDocument, CancellationToken cancellationToken)
    {
        return declarationDocument
            ? (await TryGetDeclGeneratedDocumentInternalAsync(cancellationToken).ConfigureAwait(false)).AssumeNotNull()
            : await GetGeneratedDocumentInternalAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<SourceGeneratedDocument> GetGeneratedDocumentInternalAsync(CancellationToken cancellationToken)
    {
        if (_generatedDocument is not null)
        {
            return _generatedDocument;
        }

        var generatedDocument = await ProjectSnapshot.GetRequiredGeneratedDocumentAsync(this, cancellationToken).ConfigureAwait(false);
        return InterlockedOperations.Initialize(ref _generatedDocument, generatedDocument);
    }

    private async ValueTask<SourceGeneratedDocument?> TryGetDeclGeneratedDocumentInternalAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _declGeneratedDocumentInitialized))
        {
            return _declGeneratedDocument;
        }

        var declDocument = await ProjectSnapshot.TryGetDeclGeneratedDocumentAsync(this, cancellationToken).ConfigureAwait(false);

        _declGeneratedDocument = declDocument;
        Volatile.Write(ref _declGeneratedDocumentInitialized, true);
        return declDocument;
    }

    public ValueTask<SyntaxTree> GetCSharpSyntaxTreeAsync(bool declarationDocument, CancellationToken cancellationToken)
    {
        var document = declarationDocument ? _declGeneratedDocument : _generatedDocument;
        if (document is not null &&
            document.TryGetSyntaxTree(out var tree))
        {
            return new(tree.AssumeNotNull());
        }

        return GetCSharpSyntaxTreeCoreAsync(document, cancellationToken);

        async ValueTask<SyntaxTree> GetCSharpSyntaxTreeCoreAsync(Document? document, CancellationToken cancellationToken)
        {
            document ??= await GetGeneratedDocumentAsync(declarationDocument, cancellationToken).ConfigureAwait(false);

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return tree.AssumeNotNull();
        }
    }
}
