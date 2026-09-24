// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public sealed class TestHostProject : TestHostProject<TestHostDocument>
{
    public TestHostProject(
        HostLanguageServices languageServices,
        CompilationOptions compilationOptions,
        ParseOptions parseOptions,
        params MetadataReference[] references)
        : this(languageServices, compilationOptions, parseOptions, "Test", references)
    {
    }

    public TestHostProject(
        HostLanguageServices languageServices,
        CompilationOptions compilationOptions,
        ParseOptions parseOptions,
        string assemblyName,
        params MetadataReference[] references)
        : this(languageServices,
               compilationOptions,
               parseOptions,
               assemblyName: assemblyName,
               projectName: assemblyName,
               references: references,
               documents: [])
    {
    }

    public TestHostProject(
        TestWorkspace workspace,
        TestHostDocument document,
        string name = null,
        string language = null,
        CompilationOptions compilationOptions = null,
        ParseOptions parseOptions = null,
        IEnumerable<TestHostProject> projectReferences = null,
        IEnumerable<MetadataReference> metadataReferences = null,
        IEnumerable<AnalyzerReference> analyzerReferences = null,
        string assemblyName = null,
        string defaultNamespace = null)
        : this(workspace, name, language, compilationOptions, parseOptions, [document], [], [], projectReferences, metadataReferences, analyzerReferences, assemblyName, defaultNamespace)
    {
    }

    internal TestHostProject(
        HostLanguageServices languageServices,
        CompilationOptions compilationOptions,
        ParseOptions parseOptions,
        string assemblyName,
        string projectName,
        IList<MetadataReference> references,
        IList<TestHostDocument> documents,
        IList<TestHostDocument> additionalDocuments = null,
        IList<TestHostDocument> analyzerConfigDocuments = null,
        Type hostObjectType = null,
        bool isSubmission = false,
        string filePath = null,
        IList<AnalyzerReference> analyzerReferences = null,
        string defaultNamespace = null)
        : base(languageServices,
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

    public TestHostProject(
        TestWorkspace workspace,
        string name = null,
        string language = null,
        CompilationOptions compilationOptions = null,
        ParseOptions parseOptions = null,
        IEnumerable<TestHostDocument> documents = null,
        IEnumerable<TestHostDocument> additionalDocuments = null,
        IEnumerable<TestHostDocument> analyzerConfigDocuments = null,
        IEnumerable<TestHostProject> projectReferences = null,
        IEnumerable<MetadataReference> metadataReferences = null,
        IEnumerable<AnalyzerReference> analyzerReferences = null,
        string assemblyName = null,
        string defaultNamespace = null)
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
}
