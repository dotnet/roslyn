// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// Project context to initialize properties and items of a Workspace project created with <see cref="IProjectContextFactory.CreateProjectContext(string, string, string, Guid, string, Shell.Interop.IVsHierarchy, CommandLineArguments)"/>. 
    /// </summary>
    internal interface IProjectContext
    {
        // Project properties.
        ProjectId Id { get; }
        string DisplayName { get; set; }
        string ProjectFilePath { get; set; }
        Guid Guid { get; set; }
        string ProjectType { get; set; }

        // Options.
        void SetCommandLineArguments(CommandLineArguments commandLineArguments);

        // References.
        void AddMetadataReference(string referencePath, MetadataReferenceProperties properties);
        void RemoveMetadataReference(string referencePath);
        void AddProjectReference(IProjectContext project, MetadataReferenceProperties properties);
        void RemoveProjectReference(IProjectContext project);

        // Source files.
        void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular);
        void RemoveSourceFile(string filePath);
    }
}
