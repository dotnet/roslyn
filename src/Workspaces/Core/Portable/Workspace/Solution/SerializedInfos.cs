// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Serialization
{
    /// <summary>
    /// types to hold information in solution info/project info or document info that remote host will have as well.
    /// 
    /// TODO: right now, any kind of version is not synced to remote host since it requires some changes in workspace.
    ///       but we should sync versions to remote host as well when we make workspace available in remote host.
    ///       for now, even if remote host uses workspace for things like resolving p2p references, only public service
    ///       clients can use are APIs compiler layer expose.
    /// </summary>
    internal sealed class SerializedSolutionInfo
    {
        public readonly SolutionId Id;
        public readonly VersionStamp Version;
        public readonly string FilePath;

        public SerializedSolutionInfo(SolutionId id, VersionStamp version, string filePath)
        {
            Id = id;
            Version = version;
            FilePath = filePath;
        }
    }

    internal sealed class SerializedProjectInfo
    {
        // REVIEW: do we need this?
        // HostObjectType, HasAllInformation, Top Level Version, Latest Document Version

        public readonly ProjectId Id;
        public readonly VersionStamp Version;
        public readonly string Name;
        public readonly string AssemblyName;
        public readonly string Language;
        public readonly string FilePath;
        public readonly string OutputFilePath;
        public readonly bool IsSubmission;

        public SerializedProjectInfo(ProjectId id, VersionStamp version, string name, string assemblyName, string language, string filePath, string outputFilePath, bool isSubmission)
        {
            Id = id;
            Version = version;
            Name = name;
            AssemblyName = assemblyName;
            Language = language;
            FilePath = filePath;
            OutputFilePath = outputFilePath;
            IsSubmission = isSubmission;
        }
    }

    internal sealed class SerializedDocumentInfo
    {
        // REVIEW: do we need this?
        // Tree Version, Text Version

        public readonly DocumentId Id;
        public readonly string Name;
        public readonly IReadOnlyList<string> Folders;
        public readonly SourceCodeKind SourceCodeKind;
        public readonly string FilePath;
        public readonly bool IsGenerated;

        public SerializedDocumentInfo(DocumentId id, string name, IReadOnlyList<string> folders, SourceCodeKind sourceCodeKind, string filePath, bool isGenerated)
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
