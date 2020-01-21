using System.IO;
using System.Xml.Linq;

namespace BuildBoss
{
    internal sealed class ProjectData
    {
        internal string FilePath { get; }
        internal ProjectUtil ProjectUtil { get; }

        internal XDocument Document => ProjectUtil.Document;
        internal ProjectKey Key => ProjectUtil.Key;
        internal string FileName => Path.GetFileName(FilePath);
        internal string Directory => Path.GetDirectoryName(FilePath);
        internal ProjectFileType ProjectFileType => ProjectEntryUtil.GetProjectFileType(FilePath);

        internal bool IsTestProject => IsUnitTestProject || IsIntegrationTestProject;
        internal bool IsUnitTestProject => Path.GetFileNameWithoutExtension(FilePath).EndsWith(".UnitTests");
        internal bool IsIntegrationTestProject => Path.GetFileNameWithoutExtension(FilePath).EndsWith(".IntegrationTests");

        internal ProjectData(string filePath)
        {
            FilePath = filePath;
            ProjectUtil = new ProjectUtil(FilePath);
        }
    }
}
