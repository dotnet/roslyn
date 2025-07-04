// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;

namespace BuildBoss
{
    /// <summary>
    /// All of the project entry contained in a solution file.
    /// </summary>
    internal readonly struct ProjectEntry
    {
        internal string RelativeFilePath { get; }
        internal string Name { get; }
        internal Guid ProjectGuid { get; }
        internal Guid TypeGuid { get; }

        internal bool IsFolder => TypeGuid == ProjectEntryUtil.FolderProjectType;
        internal ProjectFileType ProjectType => ProjectEntryUtil.GetProjectFileType(RelativeFilePath);

        internal ProjectEntry(
            string relativeFilePath,
            string name,
            Guid projectGuid,
            Guid typeGuid)
        {
            RelativeFilePath = relativeFilePath;
            Name = name;
            ProjectGuid = projectGuid;
            TypeGuid = typeGuid;
        }

        public override string ToString() => Name;
    }

    internal static class ProjectEntryUtil
    {
        internal static readonly Guid FolderProjectType = new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8");
        internal static readonly Guid VsixProjectType = new Guid("82B43B9B-A64C-4715-B499-D71E9CA2BD60");
        internal static readonly Guid SharedProject = new Guid("D954291E-2A0B-460D-934E-DC6B0785DB48");

        internal static readonly Guid ManagedProjectSystemCSharp = new Guid("9A19103F-16F7-4668-BE54-9A1E7A4F7556");
        internal static readonly Guid ManagedProjectSystemVisualBasic = new Guid("778DAE3C-4631-46EA-AA77-85C1314464D9");

        internal static ProjectFileType GetProjectFileType(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".csproj": return ProjectFileType.CSharp;
                case ".vbproj": return ProjectFileType.Basic;
                case ".shproj": return ProjectFileType.Shared;
                case ".proj": return ProjectFileType.Tool;
                default:
                    return ProjectFileType.Unknown;
            }
        }
    }
}
