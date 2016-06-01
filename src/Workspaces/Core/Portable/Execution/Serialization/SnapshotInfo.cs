// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// type that to hold information in solution info/project state or document state
    /// </summary>
    internal sealed class SolutionSnapshotInfo
    {
        public readonly SolutionId Id;
        public readonly VersionStamp Version;
        public readonly string FilePath;

        public SolutionSnapshotInfo(SolutionId id, VersionStamp version, string filePath)
        {
            Id = id;
            Version = version;
            FilePath = filePath;
        }
    }

    internal sealed class ProjectSnapshotInfo
    {
        // REVIEW: do we need this?
        // IsSubmission, HostObjectType, HasAllInformation, Top Level Version, Latest Document Version

        public readonly ProjectId Id;
        public readonly VersionStamp Version;
        public readonly string Name;
        public readonly string AssemblyName;
        public readonly string Language;
        public readonly string FilePath;
        public readonly string OutputFilePath;

        public ProjectSnapshotInfo(ProjectId id, VersionStamp version, string name, string assemblyName, string language, string filePath, string outputFilePath)
        {
            Id = id;
            Version = version;
            Name = name;
            AssemblyName = assemblyName;
            Language = language;
            FilePath = filePath;
            OutputFilePath = outputFilePath;
        }
    }

    internal sealed class DocumentSnapshotInfo
    {
        // REVIEW: do we need this?
        // Tree Version, Text Version

        public readonly DocumentId Id;
        public readonly string Name;
        public readonly IReadOnlyList<string> Folders;
        public readonly int SourceCodeKind;
        public readonly string FilePath;
        public readonly bool IsGenerated;

        public DocumentSnapshotInfo(DocumentId id, string name, IReadOnlyList<string> folders, int sourceCodeKind, string filePath, bool isGenerated)
        {
            Id = id;
            Name = name;
            Folders = folders;
            SourceCodeKind = sourceCodeKind;
            FilePath = filePath;
            IsGenerated = isGenerated;
        }
    }
}
