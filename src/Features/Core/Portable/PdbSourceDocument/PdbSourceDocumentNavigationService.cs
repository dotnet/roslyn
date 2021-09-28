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
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
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
    internal class PdbSourceDocumentNavigationService : IPdbSourceDocumentNavigationService
    {
        private MetadataAsSourceWorkspace? _workspace;
        private readonly IPdbFileLocatorService _pdbFileLocatorService;
        private readonly IPdbSourceDocumentLoaderService _pdbSourceDocumentLoaderService;

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

            using var pdbStream = await _pdbFileLocatorService.GetPdbPathAsync(dllPath, cancellationToken).ConfigureAwait(false);

            MetadataReader dllReader;
            MetadataReader pdbReader;

            using var dllStream = File.OpenRead(dllPath);
            if (pdbStream is null)
            {
                // Otherwise lets see if its an embedded PDB. We'll need to read the DLL to get info
                // for the debugger anyway
                var peReader = new PEReader(dllStream, PEStreamOptions.LeaveOpen);

                var entry = peReader.ReadDebugDirectory().SingleOrDefault(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                if (entry.Type == DebugDirectoryEntryType.Unknown)
                {
                    return null;
                }

                using var provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
                pdbReader = provider.GetMetadataReader();

                dllReader = peReader.GetMetadataReader();
            }
            else
            {
                //using var dllReaderProvider = MetadataReaderProvider.FromMetadataStream(dllStream, leaveOpen: true);   // TODO: Fails with "System.BadImageFormatException : Invalid COR20 header signature.", from tests at least
                using var dllReaderProvider = ModuleMetadata.CreateFromStream(dllStream, leaveOpen: true);
                dllReader = dllReaderProvider.GetMetadataReader();

                using var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream, MetadataStreamOptions.LeaveOpen);
                pdbReader = pdbReaderProvider.GetMetadataReader();
            }

            var filePaths = GetSourcePaths(symbol, dllReader, pdbReader);
            if (filePaths.Length == 0)
                return null;

            // If we have something to navigate to, then we need to construct a project etc. so for that we'll need compiler options
            var commandLineArguments = RetreiveCompilerOptions(pdbReader, out var languageName);

            if (languageName is null)
                return null;

            // TODO: Do we need our own workspace? - No, use metadata as source
            //       We should probably cache things from the same document? - yes, and assembly etc.
            //       Does each assembly get its own project added to that workspace? - each assembly gets its own project, and has to be distinct from the MAS project it might get
            if (_workspace == null)
            {
                _workspace = new MetadataAsSourceWorkspace(null!, project.Solution.Workspace.Services.HostServices);
            }

            var symbolId = SymbolKey.Create(symbol, cancellationToken);
            var projectId = ProjectId.CreateNewId();
            var languageServices = _workspace.Services.GetLanguageServices(languageName!);

            var parser = languageServices.GetRequiredService<ICommandLineParserService>();
            var arguments = parser.Parse(commandLineArguments, baseDirectory: null, isInteractive: false, sdkDirectory: null);

            var compilationOptions = arguments.CompilationOptions;
            var parseOptions = arguments.ParseOptions;
            var assemblyName = symbol.ContainingAssembly.Identity.Name;

            var documentInfos = filePaths.Select(filePath => DocumentInfo.Create(
                DocumentId.CreateNewId(projectId),
                Path.GetFileName(filePath),
                filePath: filePath,
                loader: _pdbSourceDocumentLoaderService.LoadSourceFile(filePath)));

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                name: assemblyName,
                assemblyName: assemblyName,
                language: languageName,
                compilationOptions: compilationOptions,
                parseOptions: parseOptions,
                documents: documentInfos,
                metadataReferences: project.MetadataReferences.ToImmutableArray());

            var temporarySolution = _workspace.CurrentSolution.AddProject(projectInfo);
            var temporaryProject = temporarySolution.GetRequiredProject(projectId);

            var navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, temporaryProject.Documents.First(), cancellationToken).ConfigureAwait(false);
            var navigateDocument = temporaryProject.GetDocument(navigateLocation.SourceTree);

            return new MetadataAsSourceFile(navigateDocument!.FilePath, navigateLocation, navigateDocument!.Name + " [from PDB]", navigateDocument.FilePath);
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

        // TODO: Move everything below this point to a service? Or just a static class?
        private static string[] GetSourcePaths(ISymbol symbol, MetadataReader dllReader, MetadataReader pdbReader)
        {
            var documentHandles = FindSourceDocuments(symbol, dllReader, pdbReader);

            var result = documentHandles.Select(h => pdbReader.GetString(pdbReader.GetDocument(h).Name)).ToArray();
            return result;
        }

        private static HashSet<DocumentHandle> FindSourceDocuments(ISymbol symbol, MetadataReader dllReader, MetadataReader pdbReader)
        {
            var docList = new HashSet<DocumentHandle>();

            // There is no way to go from parameter metadata to its containing method or type, so we need use the symbol API first to
            // get the method it belongs to.
            var token = symbol is IParameterSymbol parameterSymbol ? parameterSymbol.ContainingSymbol.MetadataToken : symbol.MetadataToken;
            var handle = MetadataTokens.EntityHandle(token);

            switch (handle.Kind)
            {
                case HandleKind.MethodDefinition:
                    ProcessMethodDef((MethodDefinitionHandle)handle, dllReader, pdbReader, docList, processDeclaringType: true);
                    break;
                case HandleKind.TypeDefinition:
                    ProcessTypeDef((TypeDefinitionHandle)handle, dllReader, pdbReader, docList);
                    break;
                case HandleKind.FieldDefinition:
                    ProcessFieldDef((FieldDefinitionHandle)handle, dllReader, pdbReader, docList);
                    break;
                case HandleKind.PropertyDefinition:
                    ProcessPropertyDef((PropertyDefinitionHandle)handle, dllReader, pdbReader, docList);
                    break;
                case HandleKind.EventDefinition:
                    ProcessEventDef((EventDefinitionHandle)handle, dllReader, pdbReader, docList);
                    break;
            }

            return docList;
        }

        private static void ProcessMethodDef(MethodDefinitionHandle methodDefHandle, MetadataReader dllReader, MetadataReader pdbReader, HashSet<DocumentHandle> docList, bool processDeclaringType)
        {
            var mdi = pdbReader.GetMethodDebugInformation(methodDefHandle);
            if (!mdi.Document.IsNil)
            {
                docList.Add(mdi.Document);
                return;
            }

            if (!mdi.SequencePointsBlob.IsNil)
            {
                foreach (var point in mdi.GetSequencePoints())
                {
                    if (!point.Document.IsNil)
                    {
                        docList.Add(point.Document);
                        // No need to check the type if we found a document
                        processDeclaringType = false;
                    }
                }
            }

            // Not all methods have document info, for example synthesized constructors, so we also want
            // to get any documents from the declaring type
            if (processDeclaringType)
            {
                var methodDef = dllReader.GetMethodDefinition(methodDefHandle);
                var typeDefHandle = methodDef.GetDeclaringType();
                ProcessTypeDef(typeDefHandle, dllReader, pdbReader, docList);
            }
        }

        private static void ProcessEventDef(EventDefinitionHandle eventDefHandle, MetadataReader dllReader, MetadataReader pdbReader, HashSet<DocumentHandle> docList)
        {
            var eventDef = dllReader.GetEventDefinition(eventDefHandle);
            var accessors = eventDef.GetAccessors();
            if (!accessors.Adder.IsNil)
            {
                ProcessMethodDef(accessors.Adder, dllReader, pdbReader, docList, processDeclaringType: true);
            }

            if (!accessors.Remover.IsNil)
            {
                ProcessMethodDef(accessors.Remover, dllReader, pdbReader, docList, processDeclaringType: true);
            }

            if (!accessors.Raiser.IsNil)
            {
                ProcessMethodDef(accessors.Raiser, dllReader, pdbReader, docList, processDeclaringType: true);
            }

            foreach (var other in accessors.Others)
            {
                ProcessMethodDef(other, dllReader, pdbReader, docList, processDeclaringType: true);
            }
        }

        private static void ProcessPropertyDef(PropertyDefinitionHandle propertyDefHandle, MetadataReader dllReader, MetadataReader pdbReader, HashSet<DocumentHandle> docList)
        {
            var propertyDef = dllReader.GetPropertyDefinition(propertyDefHandle);
            var accessors = propertyDef.GetAccessors();
            if (!accessors.Getter.IsNil)
            {
                ProcessMethodDef(accessors.Getter, dllReader, pdbReader, docList, processDeclaringType: true);
            }

            if (!accessors.Setter.IsNil)
            {
                ProcessMethodDef(accessors.Setter, dllReader, pdbReader, docList, processDeclaringType: true);
            }

            foreach (var other in accessors.Others)
            {
                ProcessMethodDef(other, dllReader, pdbReader, docList, processDeclaringType: true);
            }
        }

        private static void ProcessFieldDef(FieldDefinitionHandle fieldDefHandle, MetadataReader dllReader, MetadataReader pdbReader, HashSet<DocumentHandle> docList)
        {
            var fieldDef = dllReader.GetFieldDefinition(fieldDefHandle);
            var typeDefHandle = fieldDef.GetDeclaringType();
            ProcessTypeDef(typeDefHandle, dllReader, pdbReader, docList);
        }

        private static void ProcessTypeDef(TypeDefinitionHandle typeDefHandle, MetadataReader dllReader, MetadataReader pdbReader, HashSet<DocumentHandle> docList, bool processContainingType = true)
        {
            AddDocumentsFromTypeDefinitionDocuments(typeDefHandle, pdbReader, docList);

            // We don't necessarily have all of the documents associated with the type
            var typeDef = dllReader.GetTypeDefinition(typeDefHandle);
            foreach (var methodDefHandle in typeDef.GetMethods())
            {
                ProcessMethodDef(methodDefHandle, dllReader, pdbReader, docList, processDeclaringType: false);
            }

            if (processContainingType)
            {
                // If this is a nested type, then we want to check the outer type too
                var containingType = typeDef.GetDeclaringType();
                if (!containingType.IsNil)
                {
                    ProcessTypeDef(containingType, dllReader, pdbReader, docList);
                }
            }

            // And of course if this is an outer type, the only document info might be from methods in
            // nested types
            var nestedTypes = typeDef.GetNestedTypes();
            foreach (var nestedType in nestedTypes)
            {
                ProcessTypeDef(nestedType, dllReader, pdbReader, docList, processContainingType: false);
            }
        }

        private static void AddDocumentsFromTypeDefinitionDocuments(TypeDefinitionHandle typeDefHandle, MetadataReader pdbReader, HashSet<DocumentHandle> docList)
        {
            var handles = pdbReader.GetCustomDebugInformation(typeDefHandle);
            foreach (var cdiHandle in handles)
            {
                var cdi = pdbReader.GetCustomDebugInformation(cdiHandle);
                var guid = pdbReader.GetGuid(cdi.Kind);
                if (guid == PortableCustomDebugInfoKinds.TypeDefinitionDocuments)
                {
                    if (((TypeDefinitionHandle)cdi.Parent).Equals(typeDefHandle))
                    {
                        var reader = pdbReader.GetBlobReader(cdi.Value);
                        while (reader.RemainingBytes > 0)
                        {
                            docList.Add(MetadataTokens.DocumentHandle(reader.ReadCompressedInteger()));
                        }
                    }
                }
            }
        }
    }
}
