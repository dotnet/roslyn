// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal interface IProjectContext
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
        void AddSourceFile(string filePath, bool isFromSharedProject = false, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular);
        void RemoveSourceFile(string filePath);

        // Project property changes.
        void SetProjectGuid(Guid guid);
        void SetProjectTypeGuid(string projectTypeGuid);
        void SetProjectDisplayName(string projectDisplayName);
        void SetProjectFilePath(string projectFilePath);
    }
}
