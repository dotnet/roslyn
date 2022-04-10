// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    [ExportMetadataAsSourceFileProvider(ProviderName), Shared]
    [ExtensionOrder(Before = DecompilationMetadataAsSourceFileProvider.ProviderName)]
    internal sealed class PdbSourceDocumentMetadataAsSourceFileProvider : IMetadataAsSourceFileProvider
    {
        internal const string ProviderName = "PdbSource";

        private readonly IPdbFileLocatorService _pdbFileLocatorService;
        private readonly IPdbSourceDocumentLoaderService _pdbSourceDocumentLoaderService;
        private readonly IPdbSourceDocumentLogger? _logger;

        private readonly Dictionary<string, ProjectId> _assemblyToProjectMap = new();
        private readonly Dictionary<string, SourceDocumentInfo> _fileToDocumentInfoMap = new();
        private readonly HashSet<ProjectId> _sourceLinkEnabledProjects = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PdbSourceDocumentMetadataAsSourceFileProvider(
            IPdbFileLocatorService pdbFileLocatorService,
            IPdbSourceDocumentLoaderService pdbSourceDocumentLoaderService,
            [Import(AllowDefault = true)] IPdbSourceDocumentLogger? logger)
        {
            _pdbFileLocatorService = pdbFileLocatorService;
            _pdbSourceDocumentLoaderService = pdbSourceDocumentLoaderService;
            _logger = logger;
        }

        public async Task<MetadataAsSourceFile?> GetGeneratedFileAsync(Workspace workspace, Project project, ISymbol symbol, bool signaturesOnly, MetadataAsSourceOptions options, string tempPath, CancellationToken cancellationToken)
        {
            // we don't support signatures only mode
            if (signaturesOnly)
                return null;

            using var telemetry = new TelemetryMessage(cancellationToken);

            var assemblyName = symbol.ContainingAssembly.Identity.Name;
            var assemblyVersion = symbol.ContainingAssembly.Identity.Version.ToString();

            // Clear the log so messages from the previously generated file don't confuse the user
            _logger?.Clear();
            _logger?.Log(FeaturesResources.Navigating_to_symbol_0_from_1, symbol, assemblyName);

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // The purpose of the logging is to help library authors, so we don't log things like this where something
            // else has gone wrong, so even though if this check fails we won't be able to show the source, it's not something
            // the user or a library author can control, so no log message.
            if (compilation.GetMetadataReference(symbol.ContainingAssembly) is not PortableExecutableReference { FilePath: not null and var dllPath })
                return null;

            // If this is a reference assembly then we won't have the right information available, so try to find
            // a better DLL, or bail out
            var isReferenceAssembly = symbol.ContainingAssembly.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == nameof(ReferenceAssemblyAttribute)
                && attribute.AttributeClass.ToNameDisplayString() == typeof(ReferenceAssemblyAttribute).FullName);
            if (isReferenceAssembly &&
                !MetadataAsSourceHelpers.TryGetImplementationAssemblyPath(dllPath, out dllPath))
            {
                _logger?.Log(FeaturesResources.Source_is_a_reference_assembly);
                return null;
            }

            _logger?.Log(FeaturesResources.Symbol_found_in_assembly_path_0, dllPath);

            ImmutableDictionary<string, string> pdbCompilationOptions;
            ImmutableArray<SourceDocument> sourceDocuments;
            // We know we have a DLL, call and see if we can find metadata readers for it, and for the PDB (whereever it may be)
            using (var documentDebugInfoReader = await _pdbFileLocatorService.GetDocumentDebugInfoReaderAsync(dllPath, options.AlwaysUseDefaultSymbolServers, telemetry, cancellationToken).ConfigureAwait(false))
            {
                if (documentDebugInfoReader is null)
                    return null;

                // If we don't already have a project for this assembly, we'll need to create one, and we want to use
                // the same compiler options for it that the DLL was created with. We also want to do that early so we
                // can dispose files sooner
                pdbCompilationOptions = documentDebugInfoReader.GetCompilationOptions();

                // Try to find some actual document information from the PDB
                sourceDocuments = documentDebugInfoReader.FindSourceDocuments(symbol);
                if (sourceDocuments.Length == 0)
                {
                    _logger?.Log(FeaturesResources.No_source_document_info_found_in_PDB);
                    return null;
                }
            }

            Encoding? defaultEncoding = null;
            if (pdbCompilationOptions.TryGetValue("default-encoding", out var encodingString))
            {
                defaultEncoding = Encoding.GetEncoding(encodingString);
            }
            else if (pdbCompilationOptions.TryGetValue("fallback-encoding", out var fallbackEncodingString))
            {
                defaultEncoding = Encoding.GetEncoding(fallbackEncodingString);
            }

            if (!_assemblyToProjectMap.TryGetValue(assemblyName, out var projectId))
            {
                // Get the project info now, so we can dispose the documentDebugInfoReader sooner
                var projectInfo = CreateProjectInfo(workspace, project, pdbCompilationOptions, assemblyName, assemblyVersion);

                if (projectInfo is null)
                    return null;

                projectId = projectInfo.Id;

                workspace.OnProjectAdded(projectInfo);
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
            var sourceFileInfoTasks = sourceDocuments.Select(sd => _pdbSourceDocumentLoaderService.LoadSourceDocumentAsync(tempFilePath, sd, encoding, telemetry, useExtendedTimeout, cancellationToken)).ToArray();
            var sourceFileInfos = await Task.WhenAll(sourceFileInfoTasks).ConfigureAwait(false);
            if (sourceFileInfos is null || sourceFileInfos.Where(t => t is null).Any())
                return null;

            var symbolId = SymbolKey.Create(symbol, cancellationToken);
            var navigateProject = workspace.CurrentSolution.GetRequiredProject(projectId);

            var documentInfos = CreateDocumentInfos(sourceFileInfos, encoding, navigateProject.Id, project);
            if (documentInfos.Length > 0)
            {
                workspace.OnDocumentsAdded(documentInfos);
                navigateProject = workspace.CurrentSolution.GetRequiredProject(projectId);
            }

            // TODO: Support results from multiple source files: https://github.com/dotnet/roslyn/issues/55834
            var firstSourceFileInfo = sourceFileInfos[0]!;
            var documentPath = firstSourceFileInfo.FilePath;
            var document = navigateProject.Documents.FirstOrDefault(d => d.FilePath?.Equals(documentPath, StringComparison.OrdinalIgnoreCase) ?? false);

            var navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, document, cancellationToken).ConfigureAwait(false);
            var navigateDocument = navigateProject.GetDocument(navigateLocation.SourceTree);

            var documentName = string.Format(
                "{0} [{1}]",
                navigateDocument!.Name,
                firstSourceFileInfo.SourceDescription);

            return new MetadataAsSourceFile(documentPath, navigateLocation, documentName, sourceDocuments[0].FilePath);
        }

        private ProjectInfo? CreateProjectInfo(Workspace workspace, Project project, ImmutableDictionary<string, string> pdbCompilationOptions, string assemblyName, string assemblyVersion)
        {
            // First we need the language name in order to get the services
            // TODO: Find language another way for non portable PDBs: https://github.com/dotnet/roslyn/issues/55834
            if (!pdbCompilationOptions.TryGetValue("language", out var languageName) || languageName is null)
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
                projectId,
                VersionStamp.Default,
                name: $"{assemblyName} ({assemblyVersion})",
                assemblyName: assemblyName,
                language: languageName,
                compilationOptions: compilationOptions,
                parseOptions: parseOptions,
                metadataReferences: project.MetadataReferences.ToImmutableArray()); // TODO: Read references from PDB info: https://github.com/dotnet/roslyn/issues/55834
        }

        private ImmutableArray<DocumentInfo> CreateDocumentInfos(SourceFileInfo?[] sourceFileInfos, Encoding encoding, ProjectId projectId, Project sourceProject)
        {
            using var _ = ArrayBuilder<DocumentInfo>.GetInstance(out var documents);

            foreach (var info in sourceFileInfos)
            {
                Contract.ThrowIfNull(info);

                // If a document has multiple symbols then we might already know about it
                if (_fileToDocumentInfoMap.ContainsKey(info.FilePath))
                {
                    continue;
                }

                var documentId = DocumentId.CreateNewId(projectId);

                documents.Add(DocumentInfo.Create(
                    documentId,
                    Path.GetFileName(info.FilePath),
                    filePath: info.FilePath,
                    loader: info.Loader));

                // If we successfully got something from SourceLink for this project then its nice to wait a bit longer
                // if the user performs subsequent navigation
                if (info.FromRemoteLocation)
                {
                    _sourceLinkEnabledProjects.Add(projectId);
                }

                // In order to open documents in VS we need to understand the link from temp file to document and its encoding etc.
                _fileToDocumentInfoMap[info.FilePath] = new(documentId, encoding, sourceProject.Id, sourceProject.Solution.Workspace);
            }

            return documents.ToImmutable();
        }

        public bool TryAddDocumentToWorkspace(Workspace workspace, string filePath, SourceTextContainer sourceTextContainer)
        {
            if (_fileToDocumentInfoMap.TryGetValue(filePath, out var info))
            {
                workspace.OnDocumentOpened(info.DocumentId, sourceTextContainer);

                return true;
            }

            return false;
        }

        public bool TryRemoveDocumentFromWorkspace(Workspace workspace, string filePath)
        {
            if (_fileToDocumentInfoMap.TryGetValue(filePath, out var info))
            {
                workspace.OnDocumentClosed(info.DocumentId, new FileTextLoader(filePath, info.Encoding));

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

        public void CleanupGeneratedFiles(Workspace? workspace)
        {
            if (workspace is not null)
            {
                var projectIds = _assemblyToProjectMap.Values;
                foreach (var projectId in projectIds)
                {
                    workspace.OnProjectRemoved(projectId);
                }
            }

            _assemblyToProjectMap.Clear();

            // The MetadataAsSourceFileService will clean up the entire temp folder so no need to do anything here
            _fileToDocumentInfoMap.Clear();
            _sourceLinkEnabledProjects.Clear();
        }
    }

    internal sealed record SourceDocument(string FilePath, SourceHashAlgorithm HashAlgorithm, ImmutableArray<byte> Checksum, byte[]? EmbeddedTextBytes, string? SourceLinkUrl);

    internal record struct SourceDocumentInfo(DocumentId DocumentId, Encoding Encoding, ProjectId SourceProjectId, Workspace SourceWorkspace);
}
