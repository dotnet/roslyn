// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

[Export(typeof(IMetadataAsSourceFileService)), Shared]
internal sealed class MetadataAsSourceFileService : IMetadataAsSourceFileService
{
    internal const string MetadataAsSource = nameof(MetadataAsSource);

    /// <summary>
    /// Set of providers that can be used to generate source for a symbol (for example, by decompiling, or by
    /// extracting it from a pdb).
    /// </summary>
    private readonly Lazy<ImmutableArray<Lazy<IMetadataAsSourceFileProvider, MetadataAsSourceFileProviderMetadata>>> _providers;

    /// <summary>
    /// Workspace created the first time we generate any metadata for any symbol.
    /// </summary>
    private MetadataAsSourceWorkspace? _workspace;

    /// <summary>
    /// A lock to ensure we initialize <see cref="_workspace"/> and cleanup stale data only once.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(initialCount: 1);

    /// <summary>
    /// Stores the original text loader for documents that have been opened in the workspace.
    /// When the document is closed, we set the text loader back to the original loader.
    /// Concurrent access is guaranteed by callers on the UI thread.
    /// </summary>
    private readonly Dictionary<DocumentId, TextLoader> _openedDocumentReloaders = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MetadataAsSourceFileService(
        [ImportMany] IEnumerable<Lazy<IMetadataAsSourceFileProvider, MetadataAsSourceFileProviderMetadata>> providers)
    {
        _providers = new(() => [.. ExtensionOrderer.Order(providers)]);
    }

    public async Task<MetadataAsSourceFile> GetGeneratedFileAsync(
        Workspace sourceWorkspace,
        Project sourceProject,
        ISymbol symbol,
        bool signaturesOnly,
        MetadataAsSourceOptions options,
        CancellationToken cancellationToken)
    {
        if (sourceProject == null)
            throw new ArgumentNullException(nameof(sourceProject));

        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));

        if (symbol.Kind == SymbolKind.Namespace)
            throw new ArgumentException(FeaturesResources.symbol_cannot_be_a_namespace, nameof(symbol));

        symbol = symbol.GetOriginalUnreducedDefinition();

        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            IMetadataDocumentPersister persister;
            if (_workspace is null)
            {
                _workspace = new MetadataAsSourceWorkspace(this, sourceWorkspace.Services.HostServices);

                persister = _workspace.Services.GetRequiredService<IMetadataDocumentPersister>();
                // We're being initialized the first time.  Use this time to clean up any stale metadata-as-source files
                // from previous VS sessions.
                persister.CleanupGeneratedDocuments();
            }
            else
            {
                persister = _workspace.Services.GetRequiredService<IMetadataDocumentPersister>();
            }

            Contract.ThrowIfNull(_workspace);

            // We don't want to track telemetry for signatures only requests, only where we try to show source
            using var telemetryMessage = signaturesOnly ? null : new TelemetryMessage(cancellationToken);

            // todo - path.  we have first root path (metadatassource folder), then a top guid (created by this provider).
            // sub providers create their own subfolders under this guid with another guid. pdb uses projectid, decompiler uses new guid.
            // maybe have persister take in the parts to create either uri for temp path?  but somehow need this to be able to clean it up.
            // but seems possible - path is only accessed inthis method (in local cleanup).
            // Maybe persister should implement cleanup generated files???? Nice.

            foreach (var lazyProvider in _providers.Value)
            {
                var provider = lazyProvider.Value;
                var result = await provider.GetGeneratedFileAsync(_workspace, sourceWorkspace, sourceProject, symbol, signaturesOnly, options, telemetryMessage, persister, cancellationToken).ConfigureAwait(false);
                if (result is not null)
                    return result;
            }
        }

        // The decompilation provider can always return something
        throw ExceptionUtilities.Unreachable();
    }

    private static void AssertIsMainThread(MetadataAsSourceWorkspace workspace)
    {
        var threadingService = workspace.Services.GetRequiredService<IWorkspaceThreadingServiceProvider>().Service;
        Contract.ThrowIfFalse(threadingService.IsOnMainThread);
    }

    public bool TryAddDocumentToWorkspace(string filePath, SourceTextContainer sourceTextContainer, [NotNullWhen(true)] out DocumentId? documentId)
    {
        var workspace = _workspace;
        if (workspace is null)
        {
            // If we haven't even created a MetadataAsSource workspace yet, then this file definitely cannot be added to
            // it. This happens when the MiscWorkspace calls in to just see if it can attach this document to the
            // MetadataAsSource instead of itself.
            documentId = null;
            return false;
        }

        // There are no linked files in the MetadataAsSource workspace, so we can just use the first document id
        documentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).SingleOrDefault();
        if (documentId is null)
        {
            return false;
        }

        // Store a text loader for this document.  We'll need this when we close the document to avoid losing the text.
        // todo - loader not stored.  can we just use the current source text?
        // todo - should the workspace virtual text persister store this itself?
        // persister can write text and persist to workspace at same time.  virtual can persis only to workspace
        // todo - or have open / close be implemented by persister - do nothing for virtual.
        // todo - test verifies workspace text after close.
        var loader = TextLoader.From(TextAndVersion.Create(sourceTextContainer.CurrentText, VersionStamp.Default));
        _openedDocumentReloaders.Add(documentId, loader);

        workspace.OnDocumentOpened(documentId, sourceTextContainer);
        return true;
    }

    public bool TryRemoveDocumentFromWorkspace(string filePath)
    {
        var workspace = _workspace;
        if (workspace is null)
        {
            // If we haven't even created a MetadataAsSource workspace yet, then this file definitely cannot be removed
            // from it. This happens when the MiscWorkspace is hearing about a doc closing, and calls into the
            // MetadataAsSource system to see if it owns the file and should handle that event.
            return false;
        }

        // There are no linked files in the MetadataAsSource workspace, so we can just use the first document id
        var documentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
        if (documentId is null)
        {
            return false;
        }

        // In LSP, while calls to TryAddDocumentToWorkspace and TryRemoveDocumentFromWorkspace are handled
        // serially, it is possible that TryRemoveDocumentFromWorkspace called without TryAddDocumentToWorkspace first.
        // This can happen if the document is immediately closed after opening - only feature requests that force us
        // to materialize a solution will trigger TryAddDocumentToWorkspace, if none are made it is never called.
        // However TryRemoveDocumentFromWorkspace is always called on close.
        if (workspace.GetOpenDocumentIds().Contains(documentId))
        {
            var loader = _openedDocumentReloaders[documentId];
            workspace.OnDocumentClosed(documentId, loader);
        }

        return true;
    }

    public bool ShouldCollapseOnOpen(string? filePath, BlockStructureOptions blockStructureOptions)
    {
        if (filePath is null)
            return false;

        var workspace = _workspace;

        if (workspace == null)
        {
            try
            {
                throw new InvalidOperationException(
                    $"'{nameof(ShouldCollapseOnOpen)}' should only be called once outlining has already confirmed that '{filePath}' is from the {nameof(MetadataAsSourceWorkspace)}");
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
            }

            return false;
        }

        AssertIsMainThread(workspace);

        foreach (var provider in _providers.Value)
        {
            if (!provider.IsValueCreated)
                continue;

            if (provider.Value.ShouldCollapseOnOpen(workspace, filePath, blockStructureOptions))
                return true;
        }

        return false;
    }

    internal async Task<SymbolMappingResult?> MapSymbolAsync(Document document, SymbolKey symbolId, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(document.FilePath);

        Project? project = null;
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var provider in _providers.Value)
            {
                if (!provider.IsValueCreated)
                    continue;

                Contract.ThrowIfNull(_workspace);

                project = provider.Value.MapDocument(document);
                if (project is not null)
                    break;
            }
        }

        if (project is null)
            return null;

        var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var resolutionResult = symbolId.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken: cancellationToken);
        if (resolutionResult.Symbol == null)
            return null;

        return new SymbolMappingResult(project, resolutionResult.Symbol);
    }

    public bool IsNavigableMetadataSymbol(ISymbol symbol)
    {
        symbol = symbol.OriginalDefinition;

        if (!symbol.Locations.Any(static l => l.IsInMetadata))
        {
            return false;
        }

        switch (symbol.Kind)
        {
            case SymbolKind.Event:
            case SymbolKind.Field:
            case SymbolKind.Method:
            case SymbolKind.NamedType:
            case SymbolKind.Property:
            case SymbolKind.Parameter:
                return true;
        }

        return false;
    }

    public Workspace? TryGetWorkspace() => _workspace;
}
