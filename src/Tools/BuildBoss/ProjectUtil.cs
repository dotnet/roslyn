using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BuildBoss
{
    internal sealed class ProjectUtil
    {
        private readonly XDocument _projectData;
        private readonly XmlNamespaceManager _manager;

        internal ProjectType ProjectType { get; }
        internal string ProjectFilePath { get; }

        internal ProjectUtil(ProjectType projectType, string projectFilePath, XDocument projectData)
        {
            ProjectType = projectType;
            ProjectFilePath = projectFilePath;
            _projectData = projectData;
            _manager = new XmlNamespaceManager(new NameTable());
            _manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
        }

        internal bool CheckAll(TextWriter textWriter)
        {
            var allGood = true;
            if (ProjectType == ProjectType.CSharp || ProjectType == ProjectType.Basic)
            {
                allGood &= CheckRestorePackages(textWriter);
            }

            return allGood;
        }

        private bool CheckRestorePackages(TextWriter textWriter)
        {
            var groups = _projectData.XPathSelectElements("//mb:PropertyGroup", _manager);
            foreach(var group in groups)
            {
                foreach (var element in group.Elements())
                {
                    if (element.Name.LocalName == "RestorePackages")
                    {
                        textWriter.WriteLine($"\tDo not use RestorePackages");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
