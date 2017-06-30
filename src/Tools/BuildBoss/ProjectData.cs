using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        internal ProjectData(string filePath)
        {
            FilePath = filePath;
            ProjectUtil = new ProjectUtil(FilePath);
        }
    }
}
