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
        private readonly Dictionary<string, (DocumentId documentId, Encoding encoding)> _fileToDocumentMap = new();

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

        public async Task<MetadataAsSourceFile?> GetGeneratedFileAsync(Workspace workspace, Project project, ISymbol symbol, bool signaturesOnly, bool allowDecompilation, string tempPath, CancellationToken cancellationToken)
        {
            // we don't support signatures only mode
            if (signaturesOnly)
                return null;

            // If this is a reference assembly then we won't have the right information available, so bail out
            // TODO: find the implementation assembly for the reference assembly, and keep going: https://github.com/dotnet/roslyn/issues/55834
            var isReferenceAssembly = symbol.ContainingAssembly.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == nameof(ReferenceAssemblyAttribute)
                && attribute.AttributeClass.ToNameDisplayString() == typeof(ReferenceAssemblyAttribute).FullName);
            if (isReferenceAssembly)
                return null;

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            if (compilation.GetMetadataReference(symbol.ContainingAssembly) is not PortableExecutableReference { FilePath: not null and var dllPath })
            {
                return null;
            }

            var assemblyName = symbol.ContainingAssembly.Identity.Name;

            ImmutableDictionary<string, string> pdbCompilationOptions;
            ImmutableArray<SourceDocument> sourceDocuments;
            // We know we have a DLL, call and see if we can find metadata readers for it, and for the PDB (whereever it may be)
            using (var documentDebugInfoReader = await _pdbFileLocatorService.GetDocumentDebugInfoReaderAsync(dllPath, _logger, cancellationToken).ConfigureAwait(false))
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
                    return null;
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
                var projectInfo = CreateProjectInfo(workspace, project, pdbCompilationOptions, assemblyName);

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
            var encoding = defaultEncoding ?? Encoding.UTF8;
            var sourceFileInfoTasks = sourceDocuments.Select(sd => _pdbSourceDocumentLoaderService.LoadSourceDocumentAsync(tempFilePath, sd, encoding, _logger, cancellationToken)).ToArray();
            var sourceFileInfos = await Task.WhenAll(sourceFileInfoTasks).ConfigureAwait(false);
            if (sourceFileInfos is null || sourceFileInfos.Where(t => t is null).Any())
                return null;

            var symbolId = SymbolKey.Create(symbol, cancellationToken);
            var navigateProject = workspace.CurrentSolution.GetRequiredProject(projectId);

            var documentInfos = CreateDocumentInfos(sourceFileInfos, navigateProject);
            if (documentInfos.Length > 0)
            {
                workspace.OnDocumentsAdded(documentInfos);
                navigateProject = workspace.CurrentSolution.GetRequiredProject(projectId);
            }

            var documentPath = sourceFileInfos[0]!.FilePath;
            var document = navigateProject.Documents.FirstOrDefault(d => d.FilePath?.Equals(documentPath, StringComparison.OrdinalIgnoreCase) ?? false);

            // In order to open documents in VS we need to understand the link from temp file to document and its encoding
            if (!_fileToDocumentMap.ContainsKey(documentPath))
            {
                _fileToDocumentMap[documentPath] = (document.Id, encoding);
            }

            var navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, document, cancellationToken).ConfigureAwait(false);
            var navigateDocument = navigateProject.GetDocument(navigateLocation.SourceTree);

            // TODO: "from metadata" is technically correct, but could be confusing. From PDB? From Source? https://github.com/dotnet/roslyn/issues/55834
            var documentName = string.Format(
                "{0} [{1}]",
                navigateDocument!.Name,
                FeaturesResources.from_metadata);

            return new MetadataAsSourceFile(documentPath, navigateLocation, documentName, navigateDocument.FilePath);
        }

        private static ProjectInfo? CreateProjectInfo(Workspace workspace, Project project, ImmutableDictionary<string, string> pdbCompilationOptions, string assemblyName)
        {
            // First we need the language name in order to get the services
            // TODO: Find language another way for non portable PDBs: https://github.com/dotnet/roslyn/issues/55834
            if (!pdbCompilationOptions.TryGetValue("language", out var languageName) || languageName is null)
                return null;

            var languageServices = workspace.Services.GetLanguageServices(languageName);

            // TODO: Use compiler API when available: https://github.com/dotnet/roslyn/issues/57356
            var compilationOptions = languageServices.GetRequiredService<ICompilationFactoryService>().TryParsePdbCompilationOptions(pdbCompilationOptions);
            var parseOptions = languageServices.GetRequiredService<ISyntaxTreeFactoryService>().TryParsePdbParseOptions(pdbCompilationOptions);

            var projectId = ProjectId.CreateNewId();
            return ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                name: assemblyName + ProviderName, // Distinguish this project from any decompilation projects that might be created
                assemblyName: assemblyName,
                language: languageName,
                compilationOptions: compilationOptions,
                parseOptions: parseOptions,
                metadataReferences: project.MetadataReferences.ToImmutableArray()); // TODO: Read references from PDB info: https://github.com/dotnet/roslyn/issues/55834
        }

        private static ImmutableArray<DocumentInfo> CreateDocumentInfos(SourceFileInfo?[] sourceFileInfos, Project project)
        {
            using var _ = ArrayBuilder<DocumentInfo>.GetInstance(out var documents);

            foreach (var info in sourceFileInfos)
            {
                Contract.ThrowIfNull(info);

                // If a document has multiple symbols then we would already know about it
                if (project.Documents.Contains(d => d.FilePath?.Equals(info.FilePath, StringComparison.OrdinalIgnoreCase) ?? false))
                    continue;

                var documentId = DocumentId.CreateNewId(project.Id);

                documents.Add(DocumentInfo.Create(
                    documentId,
                    Path.GetFileName(info.FilePath),
                    filePath: info.FilePath,
                    loader: info.Loader));
            }

            return documents.ToImmutable();
        }

        public bool TryAddDocumentToWorkspace(Workspace workspace, string filePath, SourceTextContainer sourceTextContainer)
        {
            if (_fileToDocumentMap.TryGetValue(filePath, out var value))
            {
                workspace.OnDocumentOpened(value.documentId, sourceTextContainer);

                return true;
            }

            return false;
        }

        public bool TryRemoveDocumentFromWorkspace(Workspace workspace, string filePath)
        {
            if (_fileToDocumentMap.TryGetValue(filePath, out var value))
            {
                workspace.OnDocumentClosed(value.documentId, new FileTextLoader(filePath, value.encoding));

                return true;
            }

            return false;
        }

        public Project? MapDocument(Document document)
        {
            return document.Project;
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
            _fileToDocumentMap.Clear();
        }
    }

    internal sealed record SourceDocument(string FilePath, SourceHashAlgorithm HashAlgorithm, ImmutableArray<byte> Checksum, byte[]? EmbeddedTextBytes, string? SourceLinkUrl);
}
