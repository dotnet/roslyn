// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
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

    private readonly Lazy<FileSystemMetadataDocumentPersister> _fileSystemMetadataDocumentPersister = new(() => new FileSystemMetadataDocumentPersister());
    private readonly Lazy<VirtualMetadataDocumentPersister> _virtualMetadataDocumentPersister = new(() => new VirtualMetadataDocumentPersister());

    /// <summary>
    /// Workspace created the first time we generate any metadata for any symbol.
    /// </summary>
    private MetadataAsSourceWorkspace? _workspace;

    /// <summary>
    /// A lock to ensure we initialize <see cref="_workspace"/> and cleanup stale data only once.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(initialCount: 1);

    /// <summary>
    /// Accessed both in <see cref="GetGeneratedFileAsync"/> and in UI thread operations.  Those should not
    /// generally run concurrently.  However, to be safe, we make this a concurrent dictionary to be safe to that
    /// potentially happening.
    /// </summary>
    private readonly ConcurrentDictionary<string, MetadataAsSourceFileMetadata> _generatedFilenameToInformation = new(StringComparer.OrdinalIgnoreCase);

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
            var persister = GetPersister(options);
            if (_workspace is null)
            {
                _workspace = new MetadataAsSourceWorkspace(this, sourceWorkspace.Services.HostServices);

                // We're being initialized the first time.  Use this time to clean up any stale metadata-as-source files
                // from previous VS sessions.
                persister.CleanupGeneratedDocuments();
            }

            Contract.ThrowIfNull(_workspace);

            // We don't want to track telemetry for signatures only requests, only where we try to show source
            using var telemetryMessage = signaturesOnly ? null : new TelemetryMessage(cancellationToken);

            foreach (var lazyProvider in _providers.Value)
            {
                var provider = lazyProvider.Value;
                var result = await provider.GetGeneratedFileAsync(_workspace, sourceWorkspace, sourceProject, symbol, signaturesOnly, options, telemetryMessage, persister, cancellationToken).ConfigureAwait(false);
                if (result is not null)
                {
                    _generatedFilenameToInformation[result.Value.file.FilePath] = result.Value.fileMetadata;
                    return result.Value.file;
                }
            }
        }

        // The decompilation provider can always return something
        throw ExceptionUtilities.Unreachable();

        IMetadataDocumentPersister GetPersister(MetadataAsSourceOptions options)
        {
            return options.NavigateToVirtualFile ? _virtualMetadataDocumentPersister.Value : _fileSystemMetadataDocumentPersister.Value;
        }
    }

    private static void AssertIsMainThread(MetadataAsSourceWorkspace workspace)
    {
        var threadingService = workspace.Services.GetRequiredService<IWorkspaceThreadingServiceProvider>().Service;
        Contract.ThrowIfFalse(threadingService.IsOnMainThread);
    }

    public bool ShouldCollapseOnOpen(string? filePath, BlockStructureOptions blockStructureOptions)
    {
        if (filePath is null)
            return false;

        if (_workspace == null)
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

        AssertIsMainThread(_workspace);

        if (_generatedFilenameToInformation.TryGetValue(filePath, out var metadata))
        {
            return metadata.SignaturesOnly
                ? blockStructureOptions.CollapseEmptyMetadataImplementationsWhenFirstOpened
                : blockStructureOptions.CollapseMetadataImplementationsWhenFirstOpened;
        }

        return false;
    }

    internal async Task<SymbolMappingResult?> MapSymbolAsync(Document document, SymbolKey symbolId, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(document.FilePath);

        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_generatedFilenameToInformation.TryGetValue(document.FilePath, out var metadata))
            {
                var solution = metadata.SourceWorkspace.CurrentSolution;
                var project = solution.GetProject(metadata.SourceProjectId);
                if (project is null)
                    return null;

                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var resolutionResult = symbolId.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken: cancellationToken);
                return resolutionResult.Symbol == null ? null : new SymbolMappingResult(project, resolutionResult.Symbol);
            }
            else
            {
                return null;
            }
        }
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

    internal TestAccessor GetTestAccessor() => new TestAccessor(this);

    internal struct TestAccessor(MetadataAsSourceFileService service)
    {
        public readonly MetadataAsSourceFileMetadata GetGeneratedFileMetadata(string filePath) => service._generatedFilenameToInformation[filePath];
    }
}
