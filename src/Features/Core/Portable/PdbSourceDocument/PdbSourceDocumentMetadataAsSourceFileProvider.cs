// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    [ExportMetadataAsSourceFileProvider(ProviderName), Shared]
    [ExtensionOrder(Before = DecompilationMetadataAsSourceFileProvider.ProviderName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class PdbSourceDocumentMetadataAsSourceFileProvider(
        IPdbFileLocatorService pdbFileLocatorService,
        IPdbSourceDocumentLoaderService pdbSourceDocumentLoaderService,
        IImplementationAssemblyLookupService implementationAssemblyLookupService,
        [Import(AllowDefault = true)] IPdbSourceDocumentLogger? logger) : IMetadataAsSourceFileProvider
    {
        internal const string ProviderName = "PdbSource";

        private readonly IPdbFileLocatorService _pdbFileLocatorService = pdbFileLocatorService;
        private readonly IPdbSourceDocumentLoaderService _pdbSourceDocumentLoaderService = pdbSourceDocumentLoaderService;
        private readonly IImplementationAssemblyLookupService _implementationAssemblyLookupService = implementationAssemblyLookupService;
        private readonly IPdbSourceDocumentLogger? _logger = logger;

        /// <summary>
        /// Accessed only in <see cref="GetGeneratedFileAsync"/> and <see cref="CleanupGeneratedFiles"/>, both of which
        /// are called under a lock in <see cref="MetadataAsSourceFileService"/>.  So this is safe as a plain
        /// dictionary.
        /// </summary>
        private readonly Dictionary<string, ProjectId> _assemblyToProjectMap = new();

        /// <summary>
        /// Accessed only in <see cref="GetGeneratedFileAsync"/> and <see cref="CleanupGeneratedFiles"/>, both of which
        /// are called under a lock in <see cref="MetadataAsSourceFileService"/>.  So this is safe as a plain
        /// set.
        /// </summary>
        private readonly HashSet<ProjectId> _sourceLinkEnabledProjects = new();

        /// <summary>
        /// Accessed both in <see cref="GetGeneratedFileAsync"/> and in UI thread operations.  Those should not
        /// generally run concurrently.  However, to be safe, we make this a concurrent dictionary to be safe to that
        /// potentially happening.
        /// </summary>
        private readonly ConcurrentDictionary<string, SourceDocumentInfo> _fileToDocumentInfoMap = new();

        public async Task<MetadataAsSourceFile?> GetGeneratedFileAsync(
            MetadataAsSourceWorkspace metadataWorkspace,
            Workspace sourceWorkspace,
            Project sourceProject,
            ISymbol symbol,
            bool signaturesOnly,
            MetadataAsSourceOptions options,
            string tempPath,
            TelemetryMessage? telemetryMessage,
            CancellationToken cancellationToken)
        {
            // Check if the user wants to look for PDB source documents at all
            if (!options.NavigateToSourceLinkAndEmbeddedSources)
                return null;

            // we don't support signatures only mode
            if (signaturesOnly)
                return null;

            // telemetryMessage is only null if signaturesOnly is true
            Contract.ThrowIfNull(telemetryMessage);
            var assemblyName = symbol.ContainingAssembly.Identity.Name;
            var assemblyVersion = symbol.ContainingAssembly.Identity.Version.ToString();

            // Clear the log so messages from the previously generated file don't confuse the user
            _logger?.Clear();
            _logger?.Log(FeaturesResources.Navigating_to_symbol_0_from_1, symbol, assemblyName);

            var compilation = await sourceProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // The purpose of the logging is to help library authors, so we don't log things like this where something
            // else has gone wrong, so even though if this check fails we won't be able to show the source, it's not something
            // the user or a library author can control, so no log message.
            if (compilation.GetMetadataReference(symbol.ContainingAssembly) is not PortableExecutableReference { FilePath: not null and var dllPath })
                return null;

            telemetryMessage.SetDll(Path.GetFileName(dllPath));

            _logger?.Log(FeaturesResources.Symbol_found_in_assembly_path_0, dllPath);

            // There is no way to go from parameter metadata to its containing method or type, so we need use the symbol API first to
            // get the method it belongs to.
            var symbolToFind = symbol is IParameterSymbol parameterSymbol ? parameterSymbol.ContainingSymbol : symbol;
            var handle = MetadataTokens.EntityHandle(symbolToFind.MetadataToken);

            // If this is a reference assembly then we won't have the right information available, so try to find
            // a better DLL, or bail out
            var isReferenceAssembly = MetadataAsSourceHelpers.IsReferenceAssembly(symbol.ContainingAssembly);
            if (isReferenceAssembly)
            {
                telemetryMessage.SetReferenceAssembly("yes");
                if (_implementationAssemblyLookupService.TryFindImplementationAssemblyPath(dllPath, out dllPath))
                {
                    _logger?.Log(FeaturesResources.Symbol_found_in_assembly_path_0, dllPath);

                    // If the original assembly was a reference assembly, we can't trust that the implementation assembly
                    // we found actually contains the types, so we need to find it, following any type forwards.

                    dllPath = _implementationAssemblyLookupService.FollowTypeForwards(symbolToFind, dllPath, _logger);
                    if (dllPath is null)
                    {
                        _logger?.Log(FeaturesResources.Could_not_find_implementation_of_symbol_0, symbolToFind.MetadataName);
                        return null;
                    }

                    // Now that we have the right DLL, we need to look up the symbol in this DLL, because the one
                    // we have is from the reference assembly. To do this we create an empty compilation,
                    // add our DLL as a reference, and use SymbolKey to map the type across.
                    var documentationProvider = sourceWorkspace.Services.GetRequiredService<IDocumentationProviderService>();
                    var dllReference = IOUtilities.PerformIO(() => MetadataReference.CreateFromFile(dllPath, documentation: documentationProvider.GetDocumentationProvider(dllPath)));
                    if (dllReference is null)
                    {
                        _logger?.Log(FeaturesResources.Could_not_find_implementation_of_symbol_0, symbolToFind.MetadataName);
                        return null;
                    }

                    var compilationFactory = sourceProject.Services.GetRequiredService<ICompilationFactoryService>();
                    var tmpCompilation = compilationFactory
                        .CreateCompilation("tmp", compilationFactory.GetDefaultCompilationOptions())
                        .AddReferences(dllReference);

                    var key = SymbolKey.Create(symbolToFind, cancellationToken);
                    var resolution = key.Resolve(tmpCompilation, ignoreAssemblyKey: true, cancellationToken);
                    var newSymbol = resolution.Symbol;
                    if (newSymbol is null)
                    {
                        _logger?.Log(FeaturesResources.Could_not_find_implementation_of_symbol_0, symbolToFind.MetadataName);
                        return null;
                    }

                    telemetryMessage.SetReferenceAssembly("resolved");

                    handle = MetadataTokens.EntityHandle(newSymbol.MetadataToken);
                }
                else
                {
                    _logger?.Log(FeaturesResources.Source_is_a_reference_assembly);
                    return null;
                }
            }

            ImmutableDictionary<string, string> pdbCompilationOptions;
            ImmutableArray<SourceDocument> sourceDocuments;
            // We know we have a DLL, call and see if we can find metadata readers for it, and for the PDB (whereever it may be)
            using (var documentDebugInfoReader = await _pdbFileLocatorService.GetDocumentDebugInfoReaderAsync(dllPath, options.AlwaysUseDefaultSymbolServers, telemetryMessage, cancellationToken).ConfigureAwait(false))
            {
                if (documentDebugInfoReader is null)
                    return null;

                // If we don't already have a project for this assembly, we'll need to create one, and we want to use
                // the same compiler options for it that the DLL was created with. We also want to do that early so we
                // can dispose files sooner
                pdbCompilationOptions = documentDebugInfoReader.GetCompilationOptions();

                // Try to find some actual document information from the PDB
                sourceDocuments = documentDebugInfoReader.FindSourceDocuments(handle);
                if (sourceDocuments.Length == 0)
                {
                    _logger?.Log(FeaturesResources.No_source_document_info_found_in_PDB);
                    return null;
                }
            }

            Encoding? defaultEncoding = null;
            if (pdbCompilationOptions.TryGetValue(Cci.CompilationOptionNames.DefaultEncoding, out var encodingString))
            {
                defaultEncoding = Encoding.GetEncoding(encodingString);
            }
            else if (pdbCompilationOptions.TryGetValue(Cci.CompilationOptionNames.FallbackEncoding, out var fallbackEncodingString))
            {
                defaultEncoding = Encoding.GetEncoding(fallbackEncodingString);
            }

            if (!_assemblyToProjectMap.TryGetValue(assemblyName, out var projectId))
            {
                // Use the first document's checksum algorithm as a default, project-level value.
                // The compiler doesn't persist the actual value of /checksumalgorithm in the PDB.
                var projectChecksumAlgorithm = sourceDocuments[0].ChecksumAlgorithm;

                // Get the project info now, so we can dispose the documentDebugInfoReader sooner
                var projectInfo = CreateProjectInfo(metadataWorkspace, sourceProject, pdbCompilationOptions, assemblyName, assemblyVersion, projectChecksumAlgorithm);

                if (projectInfo is null)
                    return null;

                projectId = projectInfo.Id;

                metadataWorkspace.OnProjectAdded(projectInfo);
                _assemblyToProjectMap.Add(assemblyName, projectId);
            }

            var tempFilePath = Path.Combine(tempPath, projectId.Id.ToString());
            // Create the directory. It's possible a parallel deletion is happening in another process, so we may have
            // to retry this a few times.
            var loopCount = 0;
            while (!Directory.Exists(tempFilePath))
            {
                // Protect against infinite loops.
                if (loopCount++ > 10)
                    return null;

                IOUtilities.PerformIO(() => Directory.CreateDirectory(tempFilePath));
            }

            // Get text loaders for our documents. We do this here because if we can't load any of the files, then
            // we can't provide any results, so there is no point adding a project to the workspace etc.
            var useExtendedTimeout = _sourceLinkEnabledProjects.Contains(projectId);
            var encoding = defaultEncoding ?? Encoding.UTF8;
            var sourceFileInfoTasks = sourceDocuments.Select(sd => _pdbSourceDocumentLoaderService.LoadSourceDocumentAsync(tempFilePath, sd, encoding, telemetryMessage, useExtendedTimeout, cancellationToken)).ToArray();
            var sourceFileInfos = await Task.WhenAll(sourceFileInfoTasks).ConfigureAwait(false);
            if (sourceFileInfos is null || sourceFileInfos.Where(t => t is null).Any())
                return null;

            var symbolId = SymbolKey.Create(symbol, cancellationToken);
            var navigateProject = metadataWorkspace.CurrentSolution.GetRequiredProject(projectId);

            var documentInfos = CreateDocumentInfos(sourceFileInfos, encoding, navigateProject.Id, sourceWorkspace, sourceProject);
            if (documentInfos.Length > 0)
            {
                metadataWorkspace.OnDocumentsAdded(documentInfos);
                navigateProject = metadataWorkspace.CurrentSolution.GetRequiredProject(projectId);
            }

            // If MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync can't find the actual document to navigate to, it will fall back
            // to the document passed in, which we just use the first document for.
            // TODO: Support results from multiple source files: https://github.com/dotnet/roslyn/issues/55834
            var firstDocumentFilePath = sourceFileInfos[0]!.FilePath;
            var firstDocument = navigateProject.Documents.First(d => d.FilePath?.Equals(firstDocumentFilePath, StringComparison.OrdinalIgnoreCase) ?? false);
            var navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, firstDocument, cancellationToken).ConfigureAwait(false);

            // In the case of partial classes, finding the location in the generated source may return a location in a different document, so we
            // have to make sure to look it up again.
            var navigateDocument = navigateProject.GetDocument(navigateLocation.SourceTree);
            Contract.ThrowIfNull(navigateDocument);
            var sourceDescription = sourceFileInfos.FirstOrDefault(sfi => sfi!.FilePath?.Equals(navigateDocument.FilePath, StringComparison.OrdinalIgnoreCase) ?? false)?.SourceDescription ?? FeaturesResources.from_metadata;

            var documentName = string.Format(
                "{0} [{1}]",
                navigateDocument.Name,
                sourceDescription);
            var documentTooltip = navigateDocument.FilePath + Environment.NewLine + dllPath;

            return new MetadataAsSourceFile(navigateDocument.FilePath, navigateLocation, documentName, documentTooltip);
        }

        private ProjectInfo? CreateProjectInfo(Workspace workspace, Project project, ImmutableDictionary<string, string> pdbCompilationOptions, string assemblyName, string assemblyVersion, SourceHashAlgorithm checksumAlgorithm)
        {
            // First we need the language name in order to get the services
            // TODO: Find language another way for non portable PDBs: https://github.com/dotnet/roslyn/issues/55834
            if (!pdbCompilationOptions.TryGetValue(Cci.CompilationOptionNames.Language, out var languageName) || languageName is null)
            {
                _logger?.Log(FeaturesResources.Source_code_language_information_was_not_found_in_PDB);
                return null;
            }

            var languageServices = workspace.Services.GetLanguageServices(languageName);

            // TODO: Use compiler API when available: https://github.com/dotnet/roslyn/issues/57356
            var compilationOptions = languageServices.GetRequiredService<ICompilationFactoryService>().TryParsePdbCompilationOptions(pdbCompilationOptions);
            var parseOptions = languageServices.GetRequiredService<ISyntaxTreeFactoryService>().TryParsePdbParseOptions(pdbCompilationOptions);

            var projectId = ProjectId.CreateNewId();
            return ProjectInfo.Create(
                new ProjectInfo.ProjectAttributes(
                    id: projectId,
                    version: VersionStamp.Default,
                    name: $"{assemblyName} ({assemblyVersion})",
                    assemblyName: assemblyName,
                    language: languageName,
                    compilationOutputFilePaths: default,
                    checksumAlgorithm: checksumAlgorithm),
                compilationOptions: compilationOptions,
                parseOptions: parseOptions,
                metadataReferences: project.MetadataReferences.ToImmutableArray()); // TODO: Read references from PDB info: https://github.com/dotnet/roslyn/issues/55834
        }

        private ImmutableArray<DocumentInfo> CreateDocumentInfos(
            SourceFileInfo?[] sourceFileInfos, Encoding encoding, ProjectId projectId, Workspace sourceWorkspace, Project sourceProject)
        {
            using var _ = ArrayBuilder<DocumentInfo>.GetInstance(out var documents);

            foreach (var info in sourceFileInfos)
            {
                if (info is null)
                {
                    continue;
                }

                // If a document has multiple symbols then we might already know about it
                if (_fileToDocumentInfoMap.ContainsKey(info.FilePath))
                {
                    continue;
                }

                var documentId = DocumentId.CreateNewId(projectId);

                documents.Add(DocumentInfo.Create(
                    documentId,
                    name: Path.GetFileName(info.FilePath),
                    loader: info.Loader,
                    filePath: info.FilePath,
                    isGenerated: true)
                    .WithDesignTimeOnly(true));

                // If we successfully got something from SourceLink for this project then its nice to wait a bit longer
                // if the user performs subsequent navigation
                if (info.FromRemoteLocation)
                {
                    _sourceLinkEnabledProjects.Add(projectId);
                }

                // In order to open documents in VS we need to understand the link from temp file to document and its encoding etc.
                _fileToDocumentInfoMap[info.FilePath] = new(documentId, encoding, info.ChecksumAlgorithm, sourceProject.Id, sourceWorkspace);
            }

            return documents.ToImmutable();
        }

        private static void AssertIsMainThread(MetadataAsSourceWorkspace workspace)
        {
            Contract.ThrowIfNull(workspace);
            var threadingService = workspace.Services.GetRequiredService<IWorkspaceThreadingServiceProvider>().Service;
            Contract.ThrowIfFalse(threadingService.IsOnMainThread);
        }

        public bool ShouldCollapseOnOpen(MetadataAsSourceWorkspace workspace, string filePath, BlockStructureOptions blockStructureOptions)
        {
            AssertIsMainThread(workspace);
            return _fileToDocumentInfoMap.TryGetValue(filePath, out _) && blockStructureOptions.CollapseMetadataImplementationsWhenFirstOpened;
        }

        public bool TryAddDocumentToWorkspace(MetadataAsSourceWorkspace workspace, string filePath, SourceTextContainer sourceTextContainer)
        {
            AssertIsMainThread(workspace);

            if (_fileToDocumentInfoMap.TryGetValue(filePath, out var info))
            {
                workspace.OnDocumentOpened(info.DocumentId, sourceTextContainer);
                return true;
            }

            return false;
        }

        public bool TryRemoveDocumentFromWorkspace(MetadataAsSourceWorkspace workspace, string filePath)
        {
            AssertIsMainThread(workspace);

            if (_fileToDocumentInfoMap.TryGetValue(filePath, out var info))
            {
                workspace.OnDocumentClosed(info.DocumentId, new WorkspaceFileTextLoader(workspace.Services.SolutionServices, filePath, info.Encoding));
                return true;
            }

            return false;
        }

        public Project? MapDocument(Document document)
        {
            if (document.FilePath is not null &&
                _fileToDocumentInfoMap.TryGetValue(document.FilePath, out var info))
            {
                // We always want to do symbol look ups in the context of the source project, not in
                // our temporary project. This is so that source symbols in our source project don't
                // get incorrectly found, as they might not represent the whole picture. For example
                // given the following in two different files:
                //
                // File1.cs
                // public partial class C { void M1(); }
                // File2.cs
                // public partial class C { void M2(); }
                //
                // A go-to-def on M1() would find File1.cs. If a subsequent go-to-def is done on C
                // it would find the source definition from the downloaded File1.cs, and use that
                // rather than doing a probably symbol search to find both possible locations for C

                var solution = info.SourceWorkspace.CurrentSolution;
                return solution.GetProject(info.SourceProjectId);
            }

            return null;
        }

        public void CleanupGeneratedFiles(MetadataAsSourceWorkspace workspace)
        {
            foreach (var projectId in _assemblyToProjectMap.Values)
                workspace.OnProjectRemoved(projectId);

            _assemblyToProjectMap.Clear();

            // The MetadataAsSourceFileService will clean up the entire temp folder so no need to do anything here
            _fileToDocumentInfoMap.Clear();
            _sourceLinkEnabledProjects.Clear();
            _implementationAssemblyLookupService.Clear();
        }
    }

    internal sealed record SourceDocument(string FilePath, SourceHashAlgorithm ChecksumAlgorithm, ImmutableArray<byte> Checksum, byte[]? EmbeddedTextBytes, string? SourceLinkUrl);

    internal record struct SourceDocumentInfo(DocumentId DocumentId, Encoding Encoding, SourceHashAlgorithm ChecksumAlgorithm, ProjectId SourceProjectId, Workspace SourceWorkspace);
}
