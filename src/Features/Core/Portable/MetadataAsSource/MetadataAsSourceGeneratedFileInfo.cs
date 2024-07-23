// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal sealed class MetadataAsSourceGeneratedFileInfo
{
    public readonly ProjectId SourceProjectId;
    public readonly Workspace Workspace;

    public readonly AssemblyIdentity AssemblyIdentity;
    public readonly string LanguageName;
    public readonly bool SignaturesOnly;
    public readonly ImmutableArray<MetadataReference> References;

    public readonly string TemporaryFilePath;

    private readonly ParseOptions? _parseOptions;

    public MetadataAsSourceGeneratedFileInfo(string rootPath, Workspace sourceWorkspace, Project sourceProject, INamedTypeSymbol topLevelNamedType, bool signaturesOnly)
    {
        this.SourceProjectId = sourceProject.Id;
        this.Workspace = sourceWorkspace;
        this.LanguageName = signaturesOnly ? sourceProject.Language : LanguageNames.CSharp;
        this.SignaturesOnly = signaturesOnly;

        _parseOptions = sourceProject.Language == LanguageName
            ? sourceProject.ParseOptions
            : sourceProject.Solution.Services.GetLanguageServices(LanguageName).GetRequiredService<ISyntaxTreeFactoryService>().GetDefaultParseOptionsWithLatestLanguageVersion();

        this.References = [.. sourceProject.MetadataReferences];
        this.AssemblyIdentity = topLevelNamedType.ContainingAssembly.Identity;

        var extension = LanguageName == LanguageNames.CSharp ? ".cs" : ".vb";

        var directoryName = Guid.NewGuid().ToString("N");
        this.TemporaryFilePath = Path.Combine(rootPath, directoryName, topLevelNamedType.Name + extension);
    }

    public static Encoding Encoding => Encoding.UTF8;
    public static SourceHashAlgorithm ChecksumAlgorithm => SourceHashAlgorithms.Default;

    /// <summary>
    /// Creates a ProjectInfo to represent the fake project created for metadata as source documents.
    /// </summary>
    /// <param name="services">Solution services.</param>
    /// <param name="loadFileFromDisk">Whether the source file already exists on disk and should be included. If
    /// this is a false, a document is still created, but it's not backed by the file system and thus we won't
    /// try to load it.</param>
    public (ProjectInfo, DocumentId) GetProjectInfoAndDocumentId(SolutionServices services, bool loadFileFromDisk)
    {
        var projectId = ProjectId.CreateNewId();

        // Just say it's always a DLL since we probably won't have a Main method
        var compilationOptions = services.GetRequiredLanguageService<ICompilationFactoryService>(LanguageName).GetDefaultCompilationOptions().WithOutputKind(OutputKind.DynamicallyLinkedLibrary);

        var extension = LanguageName == LanguageNames.CSharp ? ".cs" : ".vb";

        // We need to include the version information of the assembly so InternalsVisibleTo and stuff works
        var assemblyInfoDocumentId = DocumentId.CreateNewId(projectId);
        var assemblyInfoFileName = "AssemblyInfo" + extension;
        var assemblyInfoString = LanguageName == LanguageNames.CSharp
            ? string.Format(@"[assembly: System.Reflection.AssemblyVersion(""{0}"")]", AssemblyIdentity.Version)
            : string.Format(@"<Assembly: System.Reflection.AssemblyVersion(""{0}"")>", AssemblyIdentity.Version);

        var assemblyInfoSourceText = SourceText.From(assemblyInfoString, Encoding, ChecksumAlgorithm);

        var assemblyInfoDocument = DocumentInfo.Create(
            assemblyInfoDocumentId,
            assemblyInfoFileName,
            loader: TextLoader.From(assemblyInfoSourceText.Container, VersionStamp.Default),
            filePath: null,
            isGenerated: true)
            .WithDesignTimeOnly(true);

        var generatedDocumentId = DocumentId.CreateNewId(projectId);
        var generatedDocument = DocumentInfo.Create(
            generatedDocumentId,
            Path.GetFileName(TemporaryFilePath),
            loader: loadFileFromDisk ? new WorkspaceFileTextLoader(services, TemporaryFilePath, Encoding) : null,
            filePath: TemporaryFilePath,
            isGenerated: true)
            .WithDesignTimeOnly(true);

        var projectInfo = ProjectInfo.Create(
            new ProjectInfo.ProjectAttributes(
                id: projectId,
                version: VersionStamp.Default,
                name: AssemblyIdentity.Name,
                assemblyName: AssemblyIdentity.Name,
                language: LanguageName,
                compilationOutputFilePaths: default,
                checksumAlgorithm: ChecksumAlgorithm),
            compilationOptions: compilationOptions,
            parseOptions: _parseOptions,
            documents: [assemblyInfoDocument, generatedDocument],
            metadataReferences: References);

        return (projectInfo, generatedDocumentId);
    }
}
