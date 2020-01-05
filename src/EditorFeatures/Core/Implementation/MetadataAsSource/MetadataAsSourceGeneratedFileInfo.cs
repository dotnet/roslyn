// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.MetadataAsSource
{
    internal sealed class MetadataAsSourceGeneratedFileInfo
    {
        public readonly ProjectId SourceProjectId;
        public readonly Workspace Workspace;

        public readonly AssemblyIdentity AssemblyIdentity;
        public readonly string LanguageName;
        public readonly ImmutableArray<MetadataReference> References;

        public readonly string TemporaryFilePath;

        private readonly ParseOptions? _parseOptions;

        public MetadataAsSourceGeneratedFileInfo(string rootPath, Project sourceProject, INamedTypeSymbol topLevelNamedType, bool allowDecompilation)
        {
            this.SourceProjectId = sourceProject.Id;
            this.Workspace = sourceProject.Solution.Workspace;
            this.LanguageName = allowDecompilation ? LanguageNames.CSharp : sourceProject.Language;
            if (sourceProject.Language == LanguageName)
            {
                _parseOptions = sourceProject.ParseOptions;
            }
            else
            {
                _parseOptions = Workspace.Services.GetLanguageServices(LanguageName).GetRequiredService<ISyntaxTreeFactoryService>().GetDefaultParseOptionsWithLatestLanguageVersion();
            }

            this.References = sourceProject.MetadataReferences.ToImmutableArray();
            this.AssemblyIdentity = topLevelNamedType.ContainingAssembly.Identity;

            var extension = LanguageName == LanguageNames.CSharp ? ".cs" : ".vb";

            var directoryName = Guid.NewGuid().ToString("N");
            this.TemporaryFilePath = Path.Combine(rootPath, directoryName, topLevelNamedType.Name + extension);
        }

        public Encoding Encoding => Encoding.UTF8;

        /// <summary>
        /// Creates a ProjectInfo to represent the fake project created for metadata as source documents.
        /// </summary>
        /// <param name="workspace">The containing workspace.</param>
        /// <param name="loadFileFromDisk">Whether the source file already exists on disk and should be included. If
        /// this is a false, a document is still created, but it's not backed by the file system and thus we won't
        /// try to load it.</param>
        public Tuple<ProjectInfo, DocumentId> GetProjectInfoAndDocumentId(Workspace workspace, bool loadFileFromDisk)
        {
            var projectId = ProjectId.CreateNewId();

            // Just say it's always a DLL since we probably won't have a Main method
            var compilationOptions = workspace.Services.GetLanguageServices(LanguageName).CompilationFactory!.GetDefaultCompilationOptions().WithOutputKind(OutputKind.DynamicallyLinkedLibrary);

            var extension = LanguageName == LanguageNames.CSharp ? ".cs" : ".vb";

            // We need to include the version information of the assembly so InternalsVisibleTo and stuff works
            var assemblyInfoDocumentId = DocumentId.CreateNewId(projectId);
            var assemblyInfoFileName = "AssemblyInfo" + extension;
            var assemblyInfoString = LanguageName == LanguageNames.CSharp
                ? string.Format(@"[assembly: System.Reflection.AssemblyVersion(""{0}"")]", AssemblyIdentity.Version)
                : string.Format(@"<Assembly: System.Reflection.AssemblyVersion(""{0}"")>", AssemblyIdentity.Version);

            var assemblyInfoSourceTextContainer = SourceText.From(assemblyInfoString, Encoding).Container;

            var assemblyInfoDocument = DocumentInfo.Create(
                assemblyInfoDocumentId,
                assemblyInfoFileName,
                loader: TextLoader.From(assemblyInfoSourceTextContainer, VersionStamp.Default));

            var generatedDocumentId = DocumentId.CreateNewId(projectId);
            var generatedDocument = DocumentInfo.Create(
                generatedDocumentId,
                Path.GetFileName(TemporaryFilePath),
                filePath: TemporaryFilePath,
                loader: loadFileFromDisk ? new FileTextLoader(TemporaryFilePath, Encoding) : null);

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                name: AssemblyIdentity.Name,
                assemblyName: AssemblyIdentity.Name,
                language: LanguageName,
                compilationOptions: compilationOptions,
                parseOptions: _parseOptions,
                documents: new[] { assemblyInfoDocument, generatedDocument },
                metadataReferences: References);

            return Tuple.Create(projectInfo, generatedDocumentId);
        }
    }
}
