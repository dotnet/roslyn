// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteDocumentSnapshot
{
    public TextDocument TextDocument { get; }
    public RemoteProjectSnapshot ProjectSnapshot { get; }

    private RazorCodeDocument? _codeDocument;
    private SourceGeneratedDocument? _generatedDocument;
    private SourceGeneratedDocument? _declGeneratedDocument;
    private bool _declGeneratedDocumentInitialized;

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

    public RemoteProjectSnapshot Project => ProjectSnapshot;

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

#if SONICDEV
    [System.Obsolete("PROTOTYPE(sonic): Call the overload that takes a bool to prove that you thought about which document to get")]
#endif
    public ValueTask<SourceGeneratedDocument> GetGeneratedDocumentAsync(CancellationToken cancellationToken)
    {
        return GetGeneratedDocumentInternalAsync(cancellationToken);
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

    /// <summary>
    /// Returns the decl-half generated document for this Razor document, or <see langword="null"/> when the
    /// source generator did not emit a decl-half for it. Caches the (possibly null) result.
    /// </summary>
#if SONICDEV
    [System.Obsolete("PROTOTYPE(sonic): Call the overload that takes a bool to prove that you thought about which document to get")]
#endif
    public ValueTask<SourceGeneratedDocument?> TryGetDeclGeneratedDocumentAsync(CancellationToken cancellationToken)
    {
        return TryGetDeclGeneratedDocumentInternalAsync(cancellationToken);
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

#if SONICDEV
    [System.Obsolete("PROTOTYPE(sonic): Call the overload that takes a bool to prove that you thought about which document to get")]
#endif
    public ValueTask<SyntaxTree> GetCSharpSyntaxTreeAsync(CancellationToken cancellationToken)
    {
        return GetCSharpSyntaxTreeAsync(declarationDocument: false, cancellationToken);
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
