// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.MetadataAsSource
{
    [Export(typeof(IMetadataAsSourceFileService))]
    internal class MetadataAsSourceFileService : IMetadataAsSourceFileService
    {
        /// <summary>
        /// A lock to guard parallel accesses to this type. In practice, we presume that it's not 
        /// an important scenario that we can be generating multiple documents in parallel, and so 
        /// we simply take this lock around all public entrypoints to enforce sequential access.
        /// </summary>
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

        /// <summary>
        /// For a description of the key, see GetKeyAsync.
        /// </summary>
        private readonly Dictionary<UniqueDocumentKey, MetadataAsSourceGeneratedFileInfo> _keyToInformation = new Dictionary<UniqueDocumentKey, MetadataAsSourceGeneratedFileInfo>();

        private readonly Dictionary<string, MetadataAsSourceGeneratedFileInfo> _generatedFilenameToInformation = new Dictionary<string, MetadataAsSourceGeneratedFileInfo>(StringComparer.OrdinalIgnoreCase);
        private IBidirectionalMap<MetadataAsSourceGeneratedFileInfo, DocumentId> _openedDocumentIds = BidirectionalMap<MetadataAsSourceGeneratedFileInfo, DocumentId>.Empty;

        private MetadataAsSourceWorkspace? _workspace;

        /// <summary>
        /// We create a mutex so other processes can see if our directory is still alive. We destroy the mutex when
        /// we purge our generated files.
        /// </summary>
        private Mutex? _mutex;
        private string? _rootTemporaryPathWithGuid;
        private readonly string _rootTemporaryPath;

        [ImportingConstructor]
        public MetadataAsSourceFileService()
        {
            _rootTemporaryPath = Path.Combine(Path.GetTempPath(), "MetadataAsSource");
        }

        private static string CreateMutexName(string directoryName)
        {
            return "MetadataAsSource-" + directoryName;
        }

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

        public async Task<MetadataAsSourceFile> GetGeneratedFileAsync(Project project, ISymbol symbol, bool allowDecompilation, CancellationToken cancellationToken = default)
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
                throw new ArgumentException(EditorFeaturesResources.symbol_cannot_be_a_namespace, nameof(symbol));
            }

            symbol = symbol.GetOriginalUnreducedDefinition();

            MetadataAsSourceGeneratedFileInfo fileInfo;
            Location? navigateLocation = null;
            var topLevelNamedType = MetadataAsSourceHelpers.GetTopLevelContainingNamedType(symbol);
            var symbolId = SymbolKey.Create(symbol, cancellationToken);
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                InitializeWorkspace(project);
                Contract.ThrowIfNull(_workspace);

                var infoKey = await GetUniqueDocumentKey(project, topLevelNamedType, allowDecompilation, cancellationToken).ConfigureAwait(false);
                fileInfo = _keyToInformation.GetOrAdd(infoKey, _ => new MetadataAsSourceGeneratedFileInfo(GetRootPathWithGuid_NoLock(), project, topLevelNamedType, allowDecompilation));

                _generatedFilenameToInformation[fileInfo.TemporaryFilePath] = fileInfo;

                if (!File.Exists(fileInfo.TemporaryFilePath))
                {
                    // We need to generate this. First, we'll need a temporary project to do the generation into. We
                    // avoid loading the actual file from disk since it doesn't exist yet.
                    var temporaryProjectInfoAndDocumentId = fileInfo.GetProjectInfoAndDocumentId(_workspace, loadFileFromDisk: false);
                    var temporaryDocument = _workspace.CurrentSolution.AddProject(temporaryProjectInfoAndDocumentId.Item1)
                                                                     .GetDocument(temporaryProjectInfoAndDocumentId.Item2);

                    Contract.ThrowIfNull(temporaryDocument, "The temporary ProjectInfo didn't contain the document it said it would.");

                    var useDecompiler = allowDecompilation;
                    if (useDecompiler)
                    {
                        useDecompiler = !symbol.ContainingAssembly.GetAttributes().Any(attribute => attribute.AttributeClass.Name == nameof(SuppressIldasmAttribute)
                            && attribute.AttributeClass.ToNameDisplayString() == typeof(SuppressIldasmAttribute).FullName);
                    }

                    if (useDecompiler)
                    {
                        try
                        {
                            var decompiledSourceService = temporaryDocument.GetLanguageService<IDecompiledSourceService>();
                            if (decompiledSourceService != null)
                            {
                                temporaryDocument = await decompiledSourceService.AddSourceToAsync(temporaryDocument, compilation, symbol, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                useDecompiler = false;
                            }
                        }
                        catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                        {
                            useDecompiler = false;
                        }
                    }

                    if (!useDecompiler)
                    {
                        var sourceFromMetadataService = temporaryDocument.Project.LanguageServices.GetRequiredService<IMetadataAsSourceService>();
                        temporaryDocument = await sourceFromMetadataService.AddSourceToAsync(temporaryDocument, compilation, symbol, cancellationToken).ConfigureAwait(false);
                    }

                    // We have the content, so write it out to disk
                    var text = await temporaryDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    // Create the directory. It's possible a parallel deletion is happening in another process, so we may have
                    // to retry this a few times.
                    var directoryToCreate = Path.GetDirectoryName(fileInfo.TemporaryFilePath);
                    while (!Directory.Exists(directoryToCreate))
                    {
                        try
                        {
                            Directory.CreateDirectory(directoryToCreate);
                        }
                        catch (DirectoryNotFoundException)
                        {
                        }
                        catch (UnauthorizedAccessException)
                        {
                        }
                    }

                    using (var textWriter = new StreamWriter(fileInfo.TemporaryFilePath, append: false, encoding: fileInfo.Encoding))
                    {
                        text.Write(textWriter);
                    }

                    // Mark read-only
                    new FileInfo(fileInfo.TemporaryFilePath).IsReadOnly = true;

                    // Locate the target in the thing we just created
                    navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, temporaryDocument, cancellationToken).ConfigureAwait(false);
                }

                // If we don't have a location yet, then that means we're re-using an existing file. In this case, we'll want to relocate the symbol.
                if (navigateLocation == null)
                {
                    navigateLocation = await RelocateSymbol_NoLock(fileInfo, symbolId, cancellationToken).ConfigureAwait(false);
                }
            }

            var documentName = string.Format(
                "{0} [{1}]",
                topLevelNamedType.Name,
                EditorFeaturesResources.from_metadata);

            var documentTooltip = topLevelNamedType.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

            return new MetadataAsSourceFile(fileInfo.TemporaryFilePath, navigateLocation, documentName, documentTooltip);
        }

        private async Task<Location> RelocateSymbol_NoLock(MetadataAsSourceGeneratedFileInfo fileInfo, SymbolKey symbolId, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_workspace);

            // We need to relocate the symbol in the already existing file. If the file is open, we can just
            // reuse that workspace. Otherwise, we have to go spin up a temporary project to do the binding.
            if (_openedDocumentIds.TryGetValue(fileInfo, out var openDocumentId))
            {
                // Awesome, it's already open. Let's try to grab a document for it
                var document = _workspace.CurrentSolution.GetDocument(openDocumentId);

                return await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, document, cancellationToken).ConfigureAwait(false);
            }

            // Annoying case: the file is still on disk. Only real option here is to spin up a fake project to go and bind in.
            var temporaryProjectInfoAndDocumentId = fileInfo.GetProjectInfoAndDocumentId(_workspace, loadFileFromDisk: true);
            var temporaryDocument = _workspace.CurrentSolution.AddProject(temporaryProjectInfoAndDocumentId.Item1)
                                                             .GetDocument(temporaryProjectInfoAndDocumentId.Item2);

            return await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, temporaryDocument, cancellationToken).ConfigureAwait(false);
        }

        public bool TryAddDocumentToWorkspace(string filePath, ITextBuffer buffer)
        {
            using (_gate.DisposableWait())
            {
                Contract.ThrowIfNull(_workspace);

                if (_generatedFilenameToInformation.TryGetValue(filePath, out var fileInfo))
                {
                    Contract.ThrowIfTrue(_openedDocumentIds.ContainsKey(fileInfo));

                    // We do own the file, so let's open it up in our workspace
                    var newProjectInfoAndDocumentId = fileInfo.GetProjectInfoAndDocumentId(_workspace, loadFileFromDisk: true);

                    _workspace.OnProjectAdded(newProjectInfoAndDocumentId.Item1);
                    _workspace.OnDocumentOpened(newProjectInfoAndDocumentId.Item2, buffer.AsTextContainer());

                    _openedDocumentIds = _openedDocumentIds.Add(fileInfo, newProjectInfoAndDocumentId.Item2);

                    return true;
                }
            }

            return false;
        }

        public bool TryRemoveDocumentFromWorkspace(string filePath)
        {
            using (_gate.DisposableWait())
            {
                if (_generatedFilenameToInformation.TryGetValue(filePath, out var fileInfo))
                {
                    if (_openedDocumentIds.ContainsKey(fileInfo))
                    {
                        RemoveDocumentFromWorkspace_NoLock(fileInfo);

                        return true;
                    }
                }
            }

            return false;
        }

        private void RemoveDocumentFromWorkspace_NoLock(MetadataAsSourceGeneratedFileInfo fileInfo)
        {
            var documentId = _openedDocumentIds.GetValueOrDefault(fileInfo);
            Contract.ThrowIfNull(documentId);
            Contract.ThrowIfNull(_workspace);

            _workspace.OnDocumentClosed(documentId, new FileTextLoader(fileInfo.TemporaryFilePath, fileInfo.Encoding));
            _workspace.OnProjectRemoved(documentId.ProjectId);

            _openedDocumentIds = _openedDocumentIds.RemoveKey(fileInfo);
        }

        private async Task<UniqueDocumentKey> GetUniqueDocumentKey(Project project, INamedTypeSymbol topLevelNamedType, bool allowDecompilation, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(compilation, "We are trying to produce a key for a language that doesn't support compilations.");

            var peMetadataReference = compilation.GetMetadataReference(topLevelNamedType.ContainingAssembly) as PortableExecutableReference;

            if (peMetadataReference?.FilePath != null)
            {
                return new UniqueDocumentKey(peMetadataReference.FilePath, project.Language, SymbolKey.Create(topLevelNamedType, cancellationToken), allowDecompilation);
            }
            else
            {
                return new UniqueDocumentKey(topLevelNamedType.ContainingAssembly.Identity, project.Language, SymbolKey.Create(topLevelNamedType, cancellationToken), allowDecompilation);
            }
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
            MetadataAsSourceGeneratedFileInfo fileInfo;

            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!_openedDocumentIds.TryGetKey(document.Id, out fileInfo))
                {
                    return null;
                }
            }

            // WARANING: do not touch any state fields outside the lock.
            var solution = fileInfo.Workspace.CurrentSolution;
            var project = solution.GetProject(fileInfo.SourceProjectId);
            if (project == null)
            {
                return null;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var resolutionResult = symbolId.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken: cancellationToken);
            if (resolutionResult.Symbol == null)
            {
                return null;
            }

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

                // Clone the list so we don't break our own enumeration
                var generatedFileInfoList = _generatedFilenameToInformation.Values.ToList();

                foreach (var generatedFileInfo in generatedFileInfoList)
                {
                    if (_openedDocumentIds.ContainsKey(generatedFileInfo))
                    {
                        RemoveDocumentFromWorkspace_NoLock(generatedFileInfo);
                    }
                }

                _generatedFilenameToInformation.Clear();
                _keyToInformation.Clear();
                Contract.ThrowIfFalse(_openedDocumentIds.IsEmpty);

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

        private class UniqueDocumentKey : IEquatable<UniqueDocumentKey>
        {
            private static readonly IEqualityComparer<SymbolKey> s_symbolIdComparer = SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: true);

            /// <summary>
            /// The path to the assembly. Null in the case of in-memory assemblies, where we then use assembly identity.
            /// </summary>
            private readonly string? _filePath;

            /// <summary>
            /// Assembly identity. Only non-null if <see cref="_filePath"/> is null, where it's an in-memory assembly.
            /// </summary>
            private readonly AssemblyIdentity? _assemblyIdentity;
            private readonly string _language;
            private readonly SymbolKey _symbolId;
            private readonly bool _allowDecompilation;

            public UniqueDocumentKey(string filePath, string language, SymbolKey symbolId, bool allowDecompilation)
            {
                Contract.ThrowIfNull(filePath);

                _filePath = filePath;
                _language = language;
                _symbolId = symbolId;
                _allowDecompilation = allowDecompilation;
            }

            public UniqueDocumentKey(AssemblyIdentity assemblyIdentity, string language, SymbolKey symbolId, bool allowDecompilation)
            {
                Contract.ThrowIfNull(assemblyIdentity);

                _assemblyIdentity = assemblyIdentity;
                _language = language;
                _symbolId = symbolId;
                _allowDecompilation = allowDecompilation;
            }

            public bool Equals(UniqueDocumentKey? other)
            {
                if (other == null)
                {
                    return false;
                }

                return StringComparer.OrdinalIgnoreCase.Equals(_filePath, other._filePath) &&
                    object.Equals(_assemblyIdentity, other._assemblyIdentity) &&
                    _language == other._language &&
                    s_symbolIdComparer.Equals(_symbolId, other._symbolId) &&
                    _allowDecompilation == other._allowDecompilation;
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as UniqueDocumentKey);
            }

            public override int GetHashCode()
            {
                return
                    Hash.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(_filePath ?? string.Empty),
                        Hash.Combine(_assemblyIdentity != null ? _assemblyIdentity.GetHashCode() : 0,
                            Hash.Combine(_language.GetHashCode(),
                                Hash.Combine(s_symbolIdComparer.GetHashCode(_symbolId),
                                    _allowDecompilation.GetHashCode()))));
            }
        }
    }
}
