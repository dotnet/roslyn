// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteProjectSnapshot : IProjectSnapshot
{
    public RemoteSolutionSnapshot SolutionSnapshot { get; }

    private readonly Project _project;
    private readonly Dictionary<TextDocument, RemoteDocumentSnapshot> _documentMap = [];

    public RemoteProjectSnapshot(Project project, RemoteSolutionSnapshot solutionSnapshot)
    {
        if (!project.ContainsRazorDocuments())
        {
            throw new ArgumentException(SR.Project_does_not_contain_any_Razor_documents, nameof(project));
        }

        _project = project;
        SolutionSnapshot = solutionSnapshot;
    }

    public IEnumerable<string> DocumentFilePaths
        => _project.AdditionalDocuments
            .Where(static d => d.IsRazorDocument())
            .Select(static d => d.FilePath.AssumeNotNull());

    public string FilePath => _project.FilePath.AssumeNotNull();

    public string IntermediateOutputPath => FilePathNormalizer.GetNormalizedDirectoryName(_project.CompilationOutputInfo.AssemblyPath);

    public string DisplayName => _project.Name;

    public Project Project => _project;

    public async ValueTask<TagHelperCollection> GetTagHelpersAsync(CancellationToken cancellationToken)
    {
        var generatorResult = await GeneratorRunResult.CreateAsync(throwIfNotFound: false, _project, cancellationToken).ConfigureAwait(false);

        return !generatorResult.IsDefault
            ? generatorResult.TagHelpers
            : [];
    }

    public RemoteDocumentSnapshot GetDocument(TextDocument document)
    {
        if (document.Project != _project)
        {
            throw new ArgumentException(SR.Document_does_not_belong_to_this_project, nameof(document));
        }

        if (!document.IsRazorDocument())
        {
            throw new ArgumentException(SR.Document_is_not_a_Razor_document);
        }

        return GetDocumentCore(document);
    }

    private RemoteDocumentSnapshot GetDocumentCore(TextDocument document)
    {
        lock (_documentMap)
        {
            if (!_documentMap.TryGetValue(document, out var snapshot))
            {
                snapshot = new RemoteDocumentSnapshot(document, this);
                _documentMap.Add(document, snapshot);
            }

            return snapshot;
        }
    }

    public bool ContainsDocument(string filePath)
    {
        if (!filePath.IsRazorFilePath())
        {
            throw new ArgumentException(SR.Format0_is_not_a_Razor_file_path(filePath), nameof(filePath));
        }

        var documentIds = _project.Solution.GetDocumentIdsWithFilePath(filePath);

        foreach (var documentId in documentIds)
        {
            if (_project.Id == documentId.ProjectId &&
                _project.ContainsAdditionalDocument(documentId))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetDocument(string filePath, [NotNullWhen(true)] out IDocumentSnapshot? document)
    {
        if (!filePath.IsRazorFilePath())
        {
            throw new ArgumentException(SR.Format0_is_not_a_Razor_file_path(filePath), nameof(filePath));
        }

        var documentIds = _project.Solution.GetDocumentIdsWithFilePath(filePath);

        foreach (var documentId in documentIds)
        {
            if (_project.Id == documentId.ProjectId &&
                _project.GetAdditionalDocument(documentId) is { } doc)
            {
                document = GetDocumentCore(doc);
                return true;
            }
        }

        document = null;
        return false;
    }

    internal async Task<RazorCodeDocument> GetRequiredCodeDocumentAsync(IDocumentSnapshot documentSnapshot, CancellationToken cancellationToken)
    {
        var generatorResult = await GeneratorRunResult.CreateAsync(throwIfNotFound: true, _project, cancellationToken).ConfigureAwait(false);

        Assumed.False(generatorResult.IsDefault);

        return generatorResult.GetRequiredCodeDocument(documentSnapshot.FilePath);
    }

    internal async Task<SourceGeneratedDocument> GetRequiredGeneratedDocumentAsync(IDocumentSnapshot documentSnapshot, CancellationToken cancellationToken)
    {
        var generatorResult = await GeneratorRunResult.CreateAsync(throwIfNotFound: true, _project, cancellationToken).ConfigureAwait(false);

        Assumed.False(generatorResult.IsDefault);

        return await generatorResult.GetRequiredSourceGeneratedDocumentForRazorFilePathAsync(documentSnapshot.FilePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RazorCodeDocument?> TryGetCodeDocumentForGeneratedDocumentAsync(RazorGeneratedDocumentIdentity identity, CancellationToken cancellationToken)
    {
        Debug.Assert(identity.DocumentId.ProjectId == _project.Id, "Generated document does not belong to this project.");
        var hintName = identity.HintName;

        return await TryGetCodeDocumentFromGeneratedHintNameAsync(hintName, cancellationToken).ConfigureAwait(false);
    }

    private async Task<RazorCodeDocument?> TryGetCodeDocumentFromGeneratedHintNameAsync(string generatedDocumentHintName, CancellationToken cancellationToken)
    {
        var generatorResult = await GeneratorRunResult.CreateAsync(throwIfNotFound: false, _project, cancellationToken).ConfigureAwait(false);
        if (generatorResult.IsDefault)
        {
            return null;
        }

        return generatorResult.GetRazorFilePathFromHintName(generatedDocumentHintName) is { } razorFilePath
            ? generatorResult.GetCodeDocument(razorFilePath)
            : null;
    }

    public async Task<TextDocument?> TryGetRazorDocumentForGeneratedDocumentAsync(RazorGeneratedDocumentIdentity identity, CancellationToken cancellationToken)
    {
        Debug.Assert(identity.DocumentId.ProjectId == _project.Id, "Generated document does not belong to this project.");
        var hintName = identity.HintName;

        var generatorResult = await GeneratorRunResult.CreateAsync(throwIfNotFound: false, _project, cancellationToken).ConfigureAwait(false);
        if (generatorResult.IsDefault)
        {
            return null;
        }

        return generatorResult.GetRazorFilePathFromHintName(hintName) is { } razorFilePath &&
            generatorResult.TryGetRazorDocument(razorFilePath, out var razorDocument)
                ? razorDocument
                : null;
    }
}
