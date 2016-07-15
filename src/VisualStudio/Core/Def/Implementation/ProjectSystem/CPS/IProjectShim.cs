// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal interface IProjectShim : IAnalyzerHost
    {
        ProjectId Id { get; }
        string DisplayName { get; }
        string ProjectFilePath { get; }

        // Options.
        void SetCommandLineArguments(CommandLineArguments commandLineArguments);

        // References.
        void AddMetadataReference(string referencePath, MetadataReferenceProperties properties);
        void RemoveMetadataReference(string referencePath);
        void AddProjectReference(ProjectId projectId, MetadataReferenceProperties properties);
        void RemoveProjectReference(ProjectId projectId);

        // Source files.
        void AddSourceFile(string filePath, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular);
        void RemoveSourceFile(string filePath);

        // Project property changes.
        void SetProjectGuid(Guid guid);
        void SetProjectFilePath(string projectFilePath);
        void SetIsWebsiteProject();
    }
}
