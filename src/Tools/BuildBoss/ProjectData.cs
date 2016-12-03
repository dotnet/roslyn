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
        internal ProjectKey Key { get; }
        internal string FilePath { get; }
        internal XDocument Document { get; }
        internal ProjectUtil ProjectUtil { get; }

        internal string FileName => Path.GetFileName(FilePath);
        internal string Directory => Path.GetDirectoryName(FilePath);
        internal ProjectFileType ProjectFileType => ProjectEntryUtil.GetProjectFileType(FilePath);

        internal ProjectData(string filePath)
        {
            Key = new ProjectKey(filePath);
            FilePath = Key.FilePath;
            Document = XDocument.Load(FilePath);
            ProjectUtil = new ProjectUtil(Key, Document);
        }
    }
}
