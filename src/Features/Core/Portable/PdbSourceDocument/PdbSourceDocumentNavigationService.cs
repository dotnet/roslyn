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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    [Export(typeof(IPdbSourceDocumentNavigationService)), Shared]
    internal partial class PdbSourceDocumentNavigationService : IPdbSourceDocumentNavigationService
    {
        private MetadataAsSourceWorkspace? _workspace;
        private readonly IPdbFileLocatorService _pdbFileLocatorService;
        private readonly IPdbSourceDocumentLoaderService _pdbSourceDocumentLoaderService;

        private readonly Dictionary<string, ProjectId> _assemblyToProjectMap = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PdbSourceDocumentNavigationService(IPdbFileLocatorService pdbFileLocatorService, IPdbSourceDocumentLoaderService pdbSourceDocumentLoaderService)
        {
            _pdbFileLocatorService = pdbFileLocatorService;
            _pdbSourceDocumentLoaderService = pdbSourceDocumentLoaderService;
        }

        public async Task<MetadataAsSourceFile?> GetPdbSourceDocumentAsync(Project project, ISymbol symbol, CancellationToken cancellationToken)
        {
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            var peReference = compilation.GetMetadataReference(symbol.ContainingAssembly) as PortableExecutableReference;
            if (peReference is null)
                return null;

            var dllPath = peReference.FilePath;
            if (dllPath is null)
                return null;

            // We know we have a DLL, call and see if we can find metadata readers for it, and for the PDB (whereever it may be)
            var readers = await _pdbFileLocatorService.GetMetadataReadersAsync(dllPath, cancellationToken).ConfigureAwait(false);
            if (readers is null)
                return null;

            var (dllReader, pdbReader) = readers.Value;

            // Try to find some actual document information from the PDB
            var sourceDocuments = SymbolSourceDocumentFinder.FindSourceDocuments(symbol, dllReader, pdbReader);
            if (sourceDocuments.Length == 0)
                return null;

            // Each assembly gets its own project, so we need a workspace
            if (_workspace == null)
            {
                _workspace = new MetadataAsSourceWorkspace(null!, project.Solution.Workspace.Services.HostServices);
            }

            var assemblyName = symbol.ContainingAssembly.Identity.Name;
            var symbolId = SymbolKey.Create(symbol, cancellationToken);

            if (!_assemblyToProjectMap.TryGetValue(assemblyName, out var projectId))
            {
                var projectInfo = CreateProjectInfo(project, pdbReader, assemblyName);

                if (projectInfo is null)
                    return null;

                projectId = projectInfo.Id;

                _workspace.OnProjectAdded(projectInfo);
                _assemblyToProjectMap.Add(assemblyName, projectInfo.Id);
            }

            var documentInfos = CreateDocumentInfos(sourceDocuments, projectId, pdbReader);
            if (documentInfos.Length > 0)
            {
                _workspace.OnDocumentsAdded(documentInfos);
            }

            var navigateProject = _workspace.CurrentSolution.GetRequiredProject(projectId);

            var firstDocument = sourceDocuments.First().FilePath;
            var document = navigateProject.Documents.FirstOrDefault(d => d.FilePath?.Equals(firstDocument, StringComparison.OrdinalIgnoreCase) ?? false);

            var navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, document, cancellationToken).ConfigureAwait(false);
            var navigateDocument = navigateProject.GetDocument(navigateLocation.SourceTree);

            return new MetadataAsSourceFile(navigateDocument!.FilePath, navigateLocation, navigateDocument!.Name + " [from PDB]", navigateDocument.FilePath);
        }

        private ProjectInfo? CreateProjectInfo(Project project, MetadataReader pdbReader, string assemblyName)
        {
            // If we don't already have a project for this assembly, we need to create one, and we want to use
            // the same compiler options for it that the DLL was created with.
            var commandLineArguments = RetreiveCompilerOptions(pdbReader, out var languageName);

            // TODO: Find language another way for non portable PDBs
            if (languageName is null)
                return null;

            var parser = _workspace!.Services.GetLanguageServices(languageName).GetRequiredService<ICommandLineParserService>();
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

        private ImmutableArray<DocumentInfo> CreateDocumentInfos(ImmutableArray<SourceDocument> filePaths, ProjectId projectId, MetadataReader pdbReader)
        {
            var project = _workspace!.CurrentSolution.GetRequiredProject(projectId);

            using var _ = ArrayBuilder<DocumentInfo>.GetInstance(out var documents);

            foreach (var sourceDocument in filePaths)
            {
                // If a document has multiple symbols then we'll already know about it
                if (project.Documents.Contains(doc => doc.FilePath?.Equals(sourceDocument.FilePath, StringComparison.OrdinalIgnoreCase) ?? false))
                    continue;

                documents.Add(DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId),
                    Path.GetFileName(sourceDocument.FilePath),
                    filePath: sourceDocument.FilePath,
                    loader: _pdbSourceDocumentLoaderService.LoadSourceDocument(sourceDocument, pdbReader)));
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

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal class TestAccessor
        {
            private readonly PdbSourceDocumentNavigationService _service;

            public TestAccessor(PdbSourceDocumentNavigationService service)
                => _service = service;

            public MetadataAsSourceWorkspace? Workspace => _service._workspace;
        }
    }
}
