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

                persister = GetPersister(_workspace, options);
                // We're being initialized the first time.  Use this time to clean up any stale metadata-as-source files
                // from previous VS sessions.
                persister.CleanupGeneratedDocuments();
            }
            else
            {
                persister = GetPersister(_workspace, options);
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

        static IMetadataDocumentPersister GetPersister(MetadataAsSourceWorkspace workspace, MetadataAsSourceOptions options)
        {
            if (options.NavigateToVirtualFile)
            {
                // If we're configured to use virtual files, return the persister that only saves documents to the workspace.
                return workspace.Services.GetRequiredService<WorkspaceMetadataDocumentPersister>();
            }
            else
            {
                return workspace.Services.GetRequiredService<FileSystemMetadataDocumentPersister>();
            }
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

        foreach (var provider in _providers.Value)
        {
            if (!provider.IsValueCreated)
                continue;

            if (provider.Value.ShouldCollapseOnOpen(_workspace, filePath, blockStructureOptions))
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
