// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public class EditorTestHostProject : TestHostProject<EditorTestHostDocument>
{
    internal EditorTestHostProject(
        HostLanguageServices languageServices,
        CompilationOptions? compilationOptions,
        ParseOptions? parseOptions,
        string assemblyName,
        string projectName,
        IList<MetadataReference>? references,
        IList<EditorTestHostDocument> documents,
        IList<EditorTestHostDocument>? additionalDocuments = null,
        IList<EditorTestHostDocument>? analyzerConfigDocuments = null,
        Type? hostObjectType = null,
        bool isSubmission = false,
        string? filePath = null,
        IList<AnalyzerReference>? analyzerReferences = null,
        string? defaultNamespace = null)
        : base(
            languageServices,
            compilationOptions,
            parseOptions,
            assemblyName,
            projectName,
            references,
            documents,
            additionalDocuments,
            analyzerConfigDocuments,
            hostObjectType,
            isSubmission,
            filePath,
            analyzerReferences,
            defaultNamespace)
    {
    }

    public EditorTestHostProject(
        EditorTestWorkspace workspace,
        string? name = null,
        string? language = null,
        CompilationOptions? compilationOptions = null,
        ParseOptions? parseOptions = null,
        IEnumerable<EditorTestHostDocument>? documents = null,
        IEnumerable<EditorTestHostDocument>? additionalDocuments = null,
        IEnumerable<EditorTestHostDocument>? analyzerConfigDocuments = null,
        IEnumerable<EditorTestHostProject>? projectReferences = null,
        IEnumerable<MetadataReference>? metadataReferences = null,
        IEnumerable<AnalyzerReference>? analyzerReferences = null,
        string? assemblyName = null,
        string? defaultNamespace = null)
        : base(workspace.Services,
               name,
               language,
               compilationOptions,
               parseOptions,
               documents,
               additionalDocuments,
               analyzerConfigDocuments,
               projectReferences,
               metadataReferences,
               analyzerReferences,
               assemblyName,
               defaultNamespace)
    {
    }

    public EditorTestHostProject(
        EditorTestWorkspace workspace,
        EditorTestHostDocument document,
        string? name = null,
        string? language = null,
        CompilationOptions? compilationOptions = null,
        ParseOptions? parseOptions = null,
        IEnumerable<EditorTestHostProject>? projectReferences = null,
        IEnumerable<MetadataReference>? metadataReferences = null,
        IEnumerable<AnalyzerReference>? analyzerReferences = null,
        string? assemblyName = null,
        string? defaultNamespace = null)
        : base(workspace.Services, name, language, compilationOptions, parseOptions, [document], [], [], projectReferences, metadataReferences, analyzerReferences, assemblyName, defaultNamespace)
    {
    }
}
