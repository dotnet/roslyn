using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBoss
{
    /// <summary>
    /// All of the project entry contained in a solution file.
    /// </summary>
    internal struct ProjectEntry
    {
        internal string RelativeFilePath { get; }
        internal string Name { get; }
        internal Guid Guid { get; }
        internal Guid TypeGuid { get; }

        internal bool IsFolder => TypeGuid == ProjectEntryUtil.FolderProjectType;
        internal ProjectFileType ProjectType => ProjectEntryUtil.GetProjectFileType(RelativeFilePath);

        internal ProjectEntry(
            string relativeFilePath,
            string name,
            Guid guid,
            Guid typeGuid)
        {
            RelativeFilePath = relativeFilePath;
            Name = name;
            Guid = guid;
            TypeGuid = typeGuid;
        }

        public override string ToString() => Name;
    }

    internal static class ProjectEntryUtil
    {
        internal static readonly Guid FolderProjectType = new Guid("{2150E333-8FDC-42A3-9474-1A3956D46DE8}");
        internal static readonly Guid VsixProjectType = new Guid("{82B43B9B-A64C-4715-B499-D71E9CA2BD60}");

        internal static ProjectFileType GetProjectFileType(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".csproj": return ProjectFileType.CSharp;
                case ".vbproj": return ProjectFileType.Basic;
                case ".shproj": return ProjectFileType.Shared;
                default:
                    return ProjectFileType.Unknown;
            }
        }

    }
}
