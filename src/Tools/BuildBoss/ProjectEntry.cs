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

        internal static readonly Guid LegacyProjectSystemCSharp = new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
        internal static readonly Guid ManagedProjectSystemCSharp = new Guid("9A19103F-16F7-4668-BE54-9A1E7A4F7556");
        internal static readonly Guid LegacyProjectSystemVisualBasic = new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F");
        internal static readonly Guid ManagedProjectSystemVisualBasic = new Guid("778DAE3C-4631-46EA-AA77-85C1314464D9");

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
