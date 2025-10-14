// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public partial class TestWorkspace : TestWorkspace<TestHostDocument, TestHostProject, TestHostSolution>
{
    public static string RootDirectory => TempRoot.Root;

    internal TestWorkspace(
        TestComposition? composition = null,
        string? workspaceKind = WorkspaceKind.Host,
        Guid solutionTelemetryId = default,
        bool disablePartialSolutions = true,
        bool ignoreUnchangeableDocumentsWhenApplyingChanges = true,
        WorkspaceConfigurationOptions? configurationOptions = null)
        : base(composition,
               workspaceKind,
               solutionTelemetryId,
               disablePartialSolutions,
               ignoreUnchangeableDocumentsWhenApplyingChanges,
               configurationOptions)
    {
    }

    internal TestWorkspace(
        ExportProvider exportProvider,
        string? workspaceKind = WorkspaceKind.Host,
        Guid solutionTelemetryId = default,
        bool disablePartialSolutions = true,
        bool ignoreUnchangeableDocumentsWhenApplyingChanges = true,
        WorkspaceConfigurationOptions? configurationOptions = null)
        : base(exportProvider,
               workspaceKind,
               solutionTelemetryId,
               disablePartialSolutions,
               ignoreUnchangeableDocumentsWhenApplyingChanges,
               configurationOptions)
    {
    }

    private protected override TestHostDocument CreateDocument(
        string text = "",
        string displayName = "",
        SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
        DocumentId? id = null,
        string? filePath = null,
        IReadOnlyList<string>? folders = null,
        ExportProvider? exportProvider = null,
        IDocumentServiceProvider? documentServiceProvider = null)
        => new(text, displayName, sourceCodeKind, id, filePath, folders, exportProvider, documentServiceProvider);

    private protected override TestHostDocument CreateDocument(
        ExportProvider exportProvider,
        HostLanguageServices? languageServiceProvider,
        string code,
        string name,
        string filePath,
        int? cursorPosition,
        IDictionary<string, ImmutableArray<TextSpan>> spans,
        SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
        IReadOnlyList<string>? folders = null,
        bool isLinkFile = false,
        IDocumentServiceProvider? documentServiceProvider = null,
        ISourceGenerator? generator = null)
        => new(exportProvider, languageServiceProvider, code, name, filePath, cursorPosition, spans, sourceCodeKind, folders, isLinkFile, documentServiceProvider, generator);

    private protected override TestHostProject CreateProject(
        HostLanguageServices languageServices,
        CompilationOptions? compilationOptions,
        ParseOptions? parseOptions,
        string assemblyName,
        string projectName,
        IList<MetadataReference>? references,
        IList<TestHostDocument> documents,
        IList<TestHostDocument>? additionalDocuments = null,
        IList<TestHostDocument>? analyzerConfigDocuments = null,
        Type? hostObjectType = null,
        bool isSubmission = false,
        string? filePath = null,
        IList<AnalyzerReference>? analyzerReferences = null,
        string? defaultNamespace = null)
        => new(languageServices,
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
               defaultNamespace);

    private protected override TestHostSolution CreateSolution(TestHostProject[] projects)
        => new(projects);
}
