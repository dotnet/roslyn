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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    [Export(typeof(IMetadataAsSourceFileService)), Shared]
    internal class MetadataAsSourceFileService : IMetadataAsSourceFileService
    {
        /// <summary>
        /// A lock to guard parallel accesses to this type. In practice, we presume that it's not 
        /// an important scenario that we can be generating multiple documents in parallel, and so 
        /// we simply take this lock around all public entrypoints to enforce sequential access.
        /// </summary>
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        private MetadataAsSourceWorkspace? _workspace;

        /// <summary>
        /// We create a mutex so other processes can see if our directory is still alive. We destroy the mutex when
        /// we purge our generated files.
        /// </summary>
        private Mutex? _mutex;
        private string? _rootTemporaryPathWithGuid;
        private readonly string _rootTemporaryPath;
        private readonly ImmutableArray<Lazy<IMetadataAsSourceFileProvider, MetadataAsSourceFileProviderMetadata>> _providers;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MetadataAsSourceFileService([ImportMany] IEnumerable<Lazy<IMetadataAsSourceFileProvider, MetadataAsSourceFileProviderMetadata>> providers)
        {
            _providers = ExtensionOrderer.Order(providers).ToImmutableArray();
            _rootTemporaryPath = Path.Combine(Path.GetTempPath(), "MetadataAsSource");
        }

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

        public async Task<MetadataAsSourceFile> GetGeneratedFileAsync(Project project, ISymbol symbol, bool signaturesOnly, MetadataAsSourceOptions options, CancellationToken cancellationToken = default)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (symbol.Kind == SymbolKind.Namespace)
            {
                throw new ArgumentException(FeaturesResources.symbol_cannot_be_a_namespace, nameof(symbol));
            }

            symbol = symbol.GetOriginalUnreducedDefinition();

            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                InitializeWorkspace(project);
                Contract.ThrowIfNull(_workspace);
                var tempPath = GetRootPathWithGuid_NoLock();

                foreach (var lazyProvider in _providers)
                {
                    var provider = lazyProvider.Value;
                    var providerTempPath = Path.Combine(tempPath, provider.GetType().Name);
                    var result = await provider.GetGeneratedFileAsync(_workspace, project, symbol, signaturesOnly, options, providerTempPath, cancellationToken).ConfigureAwait(false);
                    if (result is not null)
                    {
                        return result;
                    }
                }
            }

            // The decompilation provider can always return something
            throw ExceptionUtilities.Unreachable;
        }

        public bool TryAddDocumentToWorkspace(string filePath, SourceTextContainer sourceTextContainer)
        {
            using (_gate.DisposableWait())
            {
                foreach (var provider in _providers)
                {
                    if (!provider.IsValueCreated)
                        continue;

                    Contract.ThrowIfNull(_workspace);

                    if (provider.Value.TryAddDocumentToWorkspace(_workspace, filePath, sourceTextContainer))
                        return true;
                }
            }

            return false;
        }

        public bool TryRemoveDocumentFromWorkspace(string filePath)
        {
            using (_gate.DisposableWait())
            {
                foreach (var provider in _providers)
                {
                    if (!provider.IsValueCreated)
                        continue;

                    Contract.ThrowIfNull(_workspace);

                    if (provider.Value.TryRemoveDocumentFromWorkspace(_workspace, filePath))
                        return true;
                }
            }

            return false;
        }

        private void InitializeWorkspace(Project project)
        {
            if (_workspace == null)
            {
                _workspace = new MetadataAsSourceWorkspace(this, project.Solution.Workspace.Services.HostServices);
            }
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

                // Only cleanup for providers that have actually generated a file. This keeps us from
                // accidentally loading lazy providers on cleanup that weren't used
                foreach (var provider in _providers)
                {
                    if (!provider.IsValueCreated)
                        continue;

                    provider.Value.CleanupGeneratedFiles(_workspace);
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
            if (!symbol.Locations.Any(l => l.IsInMetadata))
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
