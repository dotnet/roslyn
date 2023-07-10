// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
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

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    [Export(typeof(IMetadataAsSourceFileService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal class MetadataAsSourceFileService([ImportMany] IEnumerable<Lazy<IMetadataAsSourceFileProvider, MetadataAsSourceFileProviderMetadata>> providers) : IMetadataAsSourceFileService
    {
        /// <summary>
        /// Set of providers that can be used to generate source for a symbol (for example, by decompiling, or by
        /// extracting it from a pdb).
        /// </summary>
        private readonly ImmutableArray<Lazy<IMetadataAsSourceFileProvider, MetadataAsSourceFileProviderMetadata>> _providers = ExtensionOrderer.Order(providers).ToImmutableArray();

        /// <summary>
        /// Workspace created the first time we generate any metadata for any symbol.
        /// </summary>
        private MetadataAsSourceWorkspace? _workspace;

        /// <summary>
        /// A lock to guard the mutex and filesystem data below.  We want to ensure we generate into that and clean that
        /// up safely.  
        /// </summary>
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        /// <summary>
        /// We create a mutex so other processes can see if our directory is still alive. We destroy the mutex when
        /// we purge our generated files.
        /// </summary>
        private Mutex? _mutex;
        private string? _rootTemporaryPathWithGuid;
        private readonly string _rootTemporaryPath = Path.Combine(Path.GetTempPath(), "MetadataAsSource");

        private static string CreateMutexName(string directoryName)
            => "MetadataAsSource-" + directoryName;

        private string GetRootPathWithGuid_NoLock()
        {
            if (_rootTemporaryPathWithGuid == null)
            {
                var guidString = Guid.NewGuid().ToString("N");
                _rootTemporaryPathWithGuid = Path.Combine(_rootTemporaryPath, guidString);
                _mutex = new Mutex(initiallyOwned: true, name: CreateMutexName(guidString));
            }

            return _rootTemporaryPathWithGuid;
        }

        public async Task<MetadataAsSourceFile> GetGeneratedFileAsync(
            Workspace sourceWorkspace,
            Project sourceProject,
            ISymbol symbol,
            bool signaturesOnly,
            MetadataAsSourceOptions options,
            CancellationToken cancellationToken = default)
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
                _workspace ??= new MetadataAsSourceWorkspace(this, sourceWorkspace.Services.HostServices);

                Contract.ThrowIfNull(_workspace);
                var tempPath = GetRootPathWithGuid_NoLock();

                // We don't want to track telemetry for signatures only requests, only where we try to show source
                using var telemetryMessage = signaturesOnly ? null : new TelemetryMessage(cancellationToken);

                foreach (var lazyProvider in _providers)
                {
                    var provider = lazyProvider.Value;
                    var providerTempPath = Path.Combine(tempPath, provider.GetType().Name);
                    var result = await provider.GetGeneratedFileAsync(_workspace, sourceWorkspace, sourceProject, symbol, signaturesOnly, options, providerTempPath, telemetryMessage, cancellationToken).ConfigureAwait(false);
                    if (result is not null)
                    {
                        return result;
                    }
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

        public bool TryAddDocumentToWorkspace(string filePath, SourceTextContainer sourceTextContainer)
        {
            // If we haven't even created a MetadataAsSource workspace yet, then this file definitely cannot be added to
            // it. This happens when the MiscWorkspace calls in to just see if it can attach this document to the
            // MetadataAsSource instead of itself.
            var workspace = _workspace;
            if (workspace != null)
            {
                AssertIsMainThread(workspace);

                foreach (var provider in _providers)
                {
                    if (!provider.IsValueCreated)
                        continue;

                    if (provider.Value.TryAddDocumentToWorkspace(workspace, filePath, sourceTextContainer))
                        return true;
                }
            }

            return false;
        }

        public bool TryRemoveDocumentFromWorkspace(string filePath)
        {
            // If we haven't even created a MetadataAsSource workspace yet, then this file definitely cannot be removed
            // from it. This happens when the MiscWorkspace is hearing about a doc closing, and calls into the
            // MetadataAsSource system to see if it owns the file and should handle that event.
            var workspace = _workspace;
            if (workspace != null)
            {
                AssertIsMainThread(workspace);

                foreach (var provider in _providers)
                {
                    if (!provider.IsValueCreated)
                        continue;

                    if (provider.Value.TryRemoveDocumentFromWorkspace(workspace, filePath))
                        return true;
                }
            }

            return false;
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

            foreach (var provider in _providers)
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
                foreach (var provider in _providers)
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

        public void CleanupGeneratedFiles()
        {
            using (_gate.DisposableWait())
            {
                // Release our mutex to indicate we're no longer using our directory and reset state
                if (_mutex != null)
                {
                    _mutex.Dispose();
                    _mutex = null;
                    _rootTemporaryPathWithGuid = null;
                }

                // Only cleanup for providers that have actually generated a file. This keeps us from accidentally loading
                // lazy providers on cleanup that weren't used
                var workspace = _workspace;
                if (workspace != null)
                {
                    foreach (var provider in _providers)
                    {
                        if (!provider.IsValueCreated)
                            continue;

                        provider.Value.CleanupGeneratedFiles(workspace);
                    }
                }

                try
                {
                    if (Directory.Exists(_rootTemporaryPath))
                    {
                        var deletedEverything = true;

                        // Let's look through directories to delete.
                        foreach (var directoryInfo in new DirectoryInfo(_rootTemporaryPath).EnumerateDirectories())
                        {
                            // Is there a mutex for this one?
                            if (Mutex.TryOpenExisting(CreateMutexName(directoryInfo.Name), out var acquiredMutex))
                            {
                                acquiredMutex.Dispose();
                                deletedEverything = false;
                                continue;
                            }

                            TryDeleteFolderWhichContainsReadOnlyFiles(directoryInfo.FullName);
                        }

                        if (deletedEverything)
                        {
                            Directory.Delete(_rootTemporaryPath);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static void TryDeleteFolderWhichContainsReadOnlyFiles(string directoryPath)
        {
            try
            {
                foreach (var fileInfo in new DirectoryInfo(directoryPath).EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    fileInfo.IsReadOnly = false;
                }

                Directory.Delete(directoryPath, recursive: true);
            }
            catch (Exception)
            {
            }
        }

        public bool IsNavigableMetadataSymbol(ISymbol symbol)
        {
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
}
