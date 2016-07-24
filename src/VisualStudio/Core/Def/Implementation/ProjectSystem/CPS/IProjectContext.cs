// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// Project context to initialize properties and items of a Workspace project created with <see cref="IProjectContextFactory.CreateProjectContext(string, string, string, Shell.Interop.IVsHierarchy)"/>. 
    /// </summary>
    internal interface IProjectContext : IDisposable
    {
        // Project properties.
        string DisplayName { get; set; }
        string ProjectFilePath { get; set; }
        Guid Guid { get; set; }
        string ProjectType { get; set; }
        bool DesignTimeBuildStatus { get; set; }

        // Options.
        void SetCommandLineArguments(CommandLineArguments commandLineArguments);
        void SetCommandLineArguments(string commandLineForOptions);

        // References.
        void AddMetadataReference(string referencePath, MetadataReferenceProperties properties);
        void RemoveMetadataReference(string referencePath);
        void AddProjectReference(IProjectContext project, MetadataReferenceProperties properties);
        void RemoveProjectReference(IProjectContext project);

        // Analyzers.
        void AddAnalyzerAssembly(string analyzerAssemblyFullPath);
        void RemoveAnalyzerAssembly(string analyzerAssemblyFullPath);
        void SetRuleSetFile(string ruleSetFileFullPath);
        void AddAdditionalFile(string additionalFilePath);
        void RemoveAdditionalFile(string additionalFilePath);

        // Source files.
        void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular);
        void RemoveSourceFile(string filePath);
    }
}
