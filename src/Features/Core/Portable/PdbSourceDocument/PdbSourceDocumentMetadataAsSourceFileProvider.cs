// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        private readonly Dictionary<string, ProjectId> _assemblyToProjectMap = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PdbSourceDocumentMetadataAsSourceFileProvider(IPdbFileLocatorService pdbFileLocatorService, IPdbSourceDocumentLoaderService pdbSourceDocumentLoaderService)
        {
            _pdbFileLocatorService = pdbFileLocatorService;
            _pdbSourceDocumentLoaderService = pdbSourceDocumentLoaderService;
        }

        public string Name => ProviderName;

        public async Task<MetadataAsSourceFile?> GetGeneratedFileAsync(Workspace workspace, Project project, ISymbol symbol, bool signaturesOnly, string tempPath, CancellationToken cancellationToken)
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

            var peReference = compilation.GetMetadataReference(symbol.ContainingAssembly) as PortableExecutableReference;
            if (peReference is null)
                return null;

            var dllPath = peReference.FilePath;
            if (dllPath is null)
                return null;

            // We know we have a DLL, call and see if we can find metadata readers for it, and for the PDB (whereever it may be)
            using var metadataReaderProvider = await _pdbFileLocatorService.GetMetadataReadersAsync(dllPath, cancellationToken).ConfigureAwait(false);
            if (metadataReaderProvider is null)
                return null;

            var dllReader = metadataReaderProvider.GetDllMetadataReader();
            var pdbReader = metadataReaderProvider.GetPdbMetadataReader();

            // Try to find some actual document information from the PDB
            var sourceDocuments = SymbolSourceDocumentFinder.FindSourceDocuments(symbol, dllReader, pdbReader);
            if (sourceDocuments.Length == 0)
                return null;

            // Get text loaders for our documents. We do this here because if we can't load any of the files, then
            // we can't provide any results.
            var filesAndPaths = (from sd in sourceDocuments
                                 let loader = _pdbSourceDocumentLoaderService.LoadSourceDocument(sd, pdbReader)
                                 where loader is not null
                                 select (sd.FilePath, loader)).ToImmutableArray();

            if (filesAndPaths.Length == 0)
                return null;

            var assemblyName = symbol.ContainingAssembly.Identity.Name;
            var symbolId = SymbolKey.Create(symbol, cancellationToken);

            if (!_assemblyToProjectMap.TryGetValue(assemblyName, out var projectId))
            {
                var projectInfo = CreateProjectInfo(workspace, project, pdbReader, assemblyName);

                if (projectInfo is null)
                    return null;

                projectId = projectInfo.Id;

                // TODO: Move to TryAddToWorkspace
                workspace.OnProjectAdded(projectInfo);
                _assemblyToProjectMap.Add(assemblyName, projectInfo.Id);
            }

            var documentInfos = CreateDocumentInfos(workspace, filesAndPaths, projectId);
            if (documentInfos.Length > 0)
            {
                // TODO: Move to TryAddToWorkspace
                workspace.OnDocumentsAdded(documentInfos);
            }

            var navigateProject = workspace.CurrentSolution.GetRequiredProject(projectId);

            var firstDocument = filesAndPaths[0].FilePath;
            var document = navigateProject.Documents.FirstOrDefault(d => d.FilePath?.Equals(firstDocument, StringComparison.OrdinalIgnoreCase) ?? false);

            var navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, document, cancellationToken).ConfigureAwait(false);
            var navigateDocument = navigateProject.GetDocument(navigateLocation.SourceTree);

            return new MetadataAsSourceFile(navigateDocument!.FilePath, navigateLocation, navigateDocument!.Name + " [from PDB]", navigateDocument.FilePath);
        }

        private static ProjectInfo? CreateProjectInfo(Workspace workspace, Project project, MetadataReader pdbReader, string assemblyName)
        {
            // If we don't already have a project for this assembly, we need to create one, and we want to use
            // the same compiler options for it that the DLL was created with.
            var commandLineArguments = RetreiveCompilerOptions(pdbReader, out var languageName);

            // TODO: Find language another way for non portable PDBs
            if (languageName is null)
                return null;

            var parser = workspace.Services.GetLanguageServices(languageName).GetRequiredService<ICommandLineParserService>();
            var arguments = parser.Parse(commandLineArguments, baseDirectory: null, isInteractive: false, sdkDirectory: null);

            var compilationOptions = arguments.CompilationOptions;
            var parseOptions = arguments.ParseOptions;

            var projectId = ProjectId.CreateNewId();
            return ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                name: assemblyName + "_FromPdb", // To distinguish it from a Metadata as Source project it might get
                assemblyName: assemblyName,
                language: languageName,
                compilationOptions: compilationOptions,
                parseOptions: parseOptions,
                metadataReferences: project.MetadataReferences.ToImmutableArray());
        }

        private static ImmutableArray<DocumentInfo> CreateDocumentInfos(Workspace workspace, ImmutableArray<(string FilePath, TextLoader Loader)> filePaths, ProjectId projectId)
        {
            var project = workspace.CurrentSolution.GetRequiredProject(projectId);

            using var _ = ArrayBuilder<DocumentInfo>.GetInstance(out var documents);

            foreach (var sourceDocument in filePaths)
            {
                // If a document has multiple symbols then we would already know about it
                if (project.Documents.Contains(doc => doc.FilePath?.Equals(sourceDocument.FilePath, StringComparison.OrdinalIgnoreCase) ?? false))
                    continue;

                documents.Add(DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId),
                    Path.GetFileName(sourceDocument.FilePath),
                    filePath: sourceDocument.FilePath,
                    loader: sourceDocument.Loader));
            }

            return documents.ToImmutable();
        }

        private static IEnumerable<string> RetreiveCompilerOptions(MetadataReader pdbReader, out string? languageName)
        {
            languageName = null;

            using var _ = ArrayBuilder<string>.GetInstance(out var options);
            foreach (var handle in pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
            {
                var customDebugInformation = pdbReader.GetCustomDebugInformation(handle);
                if (pdbReader.GetGuid(customDebugInformation.Kind) == PortableCustomDebugInfoKinds.CompilationOptions)
                {
                    var blobReader = pdbReader.GetBlobReader(customDebugInformation.Value);

                    // Compiler flag bytes are UTF-8 null-terminated key-value pairs
                    var nullIndex = blobReader.IndexOf(0);
                    while (nullIndex >= 0)
                    {
                        var key = blobReader.ReadUTF8(nullIndex);

                        // Skip the null terminator
                        blobReader.ReadByte();

                        nullIndex = blobReader.IndexOf(0);
                        var value = blobReader.ReadUTF8(nullIndex);

                        // key and value now have strings containing serialized compiler flag information
                        options.Add($"/{TranslateKey(key)}:{value}");

                        if (key == "language")
                        {
                            languageName = value;
                        }

                        // Skip the null terminator
                        blobReader.ReadByte();
                        nullIndex = blobReader.IndexOf(0);
                    }
                }
            }

            return options.ToImmutable();
        }

        private static string TranslateKey(string key)
            => key switch
            {
                "output-kind" => "target",
                _ => key
            };

        public bool TryAddDocumentToWorkspace(Workspace workspace, string filePath, SourceTextContainer sourceTextContainer)
        {
            throw new NotImplementedException();
        }

        public bool TryRemoveDocumentFromWorkspace(Workspace workspace, string filePath)
        {
            throw new NotImplementedException();
        }

        public Project? MapDocument(Document document)
        {
            throw new NotImplementedException();
        }

        public void CleanupGeneratedFiles(Workspace? workspace)
        {
            _assemblyToProjectMap.Clear();
        }
    }
}
