using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBoss
{
    /// <summary>
    /// All of the project information contained in a solution file.
    /// </summary>
    internal struct ProjectData
    {
        internal string RelativeFilePath { get; }
        internal string Name { get; }
        internal Guid Guid { get; }
        internal Guid TypeGuid { get; }

        internal bool IsFolder => TypeGuid == ProjectDataUtil.FolderProjectType;
        internal ProjectType ProjectType => ProjectDataUtil.GetProjectType(RelativeFilePath);

        internal ProjectData(
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

    internal static class ProjectDataUtil
    {
        internal static readonly Guid FolderProjectType = new Guid("{2150E333-8FDC-42A3-9474-1A3956D46DE8}");

        internal static ProjectType GetProjectType(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".csproj": return ProjectType.CSharp;
                case ".vbproj": return ProjectType.Basic;
                case ".shproj": return ProjectType.Shared;
                default:
                    return ProjectType.Unknown;
            }
        }

    }
}
