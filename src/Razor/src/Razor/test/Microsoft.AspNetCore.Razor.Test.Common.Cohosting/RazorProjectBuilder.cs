// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.NET.Sdk.Razor.SourceGenerators;
using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal class RazorProjectBuilder(ProjectId? id = null)
{
    public ProjectId Id { get; } = id ?? ProjectId.CreateNewId();

    public string? ProjectName { get; set; }

    public string? ProjectFilePath
    {
        get;
        set
        {
            ProjectName ??= Path.GetFileNameWithoutExtension(value);

            field = value;
        }
    }

    public string? RootNamespace { get; set; } = "ASP";

    public bool ReferenceRazorSourceGenerator { get; set; } = true;
    public bool GenerateGlobalConfigFile { get; set; } = true;
    public bool GenerateMSBuildProjectDirectory { get; set; } = true;
    public bool GenerateAdditionalDocumentMetadata { get; set; } = true;

    public RazorLanguageVersion RazorLanguageVersion { get; set; } = RazorLanguageVersion.Preview;

    private readonly List<PortableExecutableReference> _references = [];
    private readonly List<(DocumentId id, string name, SourceText text, string filePath)> _documents = [];
    private readonly List<(DocumentId id, string name, SourceText text, string filePath)> _additionalDocuments = [];
    private readonly List<(string name, SourceText text, string filePath)> _analyzerConfigDocuments = [];

    internal void AddReferences(IEnumerable<PortableExecutableReference> enumerable)
    {
        _references.AddRange(enumerable);
    }

    internal DocumentId AddDocument(string filePath, SourceText text)
    {
        var name = Path.GetFileName(filePath);
        var id = DocumentId.CreateNewId(Id, name);
        _documents.Add((id, name, text, filePath));
        return id;
    }

    internal DocumentId AddAdditionalDocument(string filePath, SourceText text)
    {
        var name = Path.GetFileName(filePath);
        var id = DocumentId.CreateNewId(Id, name);
        AddAdditionalDocument(id, filePath, text);
        return id;
    }

    internal void AddAdditionalDocument(DocumentId id, string filePath, SourceText text)
    {
        var name = Path.GetFileName(filePath);
        _additionalDocuments.Add((id, name, text, filePath));
    }

    internal void AddAnalyzerConfigDocument(string filePath, SourceText text)
    {
        var name = Path.GetFileName(filePath);
        _analyzerConfigDocuments.Add((name, text, filePath));
    }

    public Solution Build(Solution solution)
    {
        var sgAssembly = typeof(RazorSourceGenerator).Assembly;

        var projectInfo = ProjectInfo
            .Create(
                Id,
                VersionStamp.Create(),
                name: ProjectName ?? "",
                assemblyName: ProjectName ?? "",
                LanguageNames.CSharp,
                ProjectFilePath,
                parseOptions: CSharpParseOptions.Default.WithFeatures([new("use-roslyn-tokenizer", "true")]),
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(_references)
            .WithDefaultNamespace(RootNamespace);

        if (ReferenceRazorSourceGenerator)
        {
            projectInfo = projectInfo.WithAnalyzerReferences([new AnalyzerFileReference(sgAssembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile)]);
        }

        solution = solution.AddProject(projectInfo);

        foreach (var document in _documents)
        {
            solution = solution.AddDocument(document.id, document.name, document.text, filePath: document.filePath);
        }

        foreach (var additionalDocument in _additionalDocuments)
        {
            solution = solution.AddAdditionalDocument(additionalDocument.id, additionalDocument.name, additionalDocument.text, filePath: additionalDocument.filePath);
        }

        if (GenerateGlobalConfigFile)
        {
            var globalConfigContent = new StringBuilder();

            globalConfigContent.AppendLine($"""
                is_global = true

                build_property.RazorLangVersion = {RazorLanguageVersion}
                build_property.RazorConfiguration = {FallbackRazorConfiguration.Latest.ConfigurationName}
                build_property.RootNamespace = {RootNamespace}

                # This might suprise you, but by suppressing the source generator here, we're mirroring what happens in the Razor SDK
                build_property.SuppressRazorSourceGenerator = true
                """);

            if (GenerateMSBuildProjectDirectory)
            {
                globalConfigContent.AppendLine($"""
                    build_property.MSBuildProjectDirectory = {Path.GetDirectoryName(ProjectFilePath).AssumeNotNull()}
                    """);
            }

            var projectBasePath = Path.GetDirectoryName(ProjectFilePath).AssumeNotNull();

            if (GenerateAdditionalDocumentMetadata)
            {
                foreach (var additionalDocument in _additionalDocuments)
                {
                    if (additionalDocument.filePath is not null &&
                        additionalDocument.filePath.StartsWith(projectBasePath))
                    {
                        var relativePath = additionalDocument.filePath[(projectBasePath.Length + 1)..];
                        globalConfigContent.AppendLine($"""

                            [{additionalDocument.filePath.AssumeNotNull().Replace('\\', '/')}]
                            build_metadata.AdditionalFiles.TargetPath = {Convert.ToBase64String(Encoding.UTF8.GetBytes(relativePath))}
                            """);
                    }
                }
            }

            solution = solution.AddAnalyzerConfigDocument(
                DocumentId.CreateNewId(Id),
                name: ".globalconfig",
                text: SourceText.From(globalConfigContent.ToString()),
                filePath: Path.Combine(projectBasePath, ".globalconfig"));
        }

        foreach (var analyzerConfigDocument in _analyzerConfigDocuments)
        {
            solution = solution.AddAnalyzerConfigDocument(
                DocumentId.CreateNewId(Id),
                name: analyzerConfigDocument.name,
                text: analyzerConfigDocument.text,
                filePath: analyzerConfigDocument.filePath);
        }

        return solution;
    }
}
