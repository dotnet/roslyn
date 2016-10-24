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
                allGood &= CheckForProperty(textWriter, "RestorePackages");
                allGood &= CheckForProperty(textWriter, "SolutionDir");
                allGood &= CheckForProperty(textWriter, "FileAlignment");
                allGood &= CheckForProperty(textWriter, "FileUpgradeFlags");
                allGood &= CheckForProperty(textWriter, "UpgradeBackupLocation");
                allGood &= CheckForProperty(textWriter, "OldToolsVersion");
            }

            allGood &= CheckRoslynProjectType(textWriter);

            return allGood;
        }

        private bool CheckForProperty(TextWriter textWriter, string propertyName)
        {
            foreach (var element in GetAllPropertyGroupElements())
            {
                if (element.Name.LocalName == propertyName)
                {
                    textWriter.WriteLine($"\tDo not use {propertyName}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validate the content of RoslynProjectType is one of the supported values.
        /// </summary>
        private bool CheckRoslynProjectType(TextWriter textWriter)
        {
            var element = GetAllPropertyGroupElements().FirstOrDefault(x => x.Name.LocalName == "RoslynProjectType");
            if (element == null)
            {
                return true;
            }

            var value = element.Value.Trim();
            switch (value)
            {
                case "Dll":
                case "ExeDesktop":
                case "ToolDesktop":
                case "ExeCoreClr":
                case "UnitTest":
                case "UnitTestNext":
                case "CompilerGeneratorTool":
                case "DeploymentCompilerGeneratorTools":
                case "Deployment":
                case "Vsix":
                case "Ignore":
                case "Dependency":
                case "Custom":
                    return true;
                default:
                    textWriter.WriteLine($@"Value ""{value}"" is illegal for RoslynProjectType");
                    return false;
            }
        }

        private IEnumerable<XElement> GetAllPropertyGroupElements()
        {
            var groups = _projectData.XPathSelectElements("//mb:PropertyGroup", _manager);
            foreach (var group in groups)
            {
                foreach (var element in group.Elements())
                {
                    yield return element;
                }
            }
        }
    }
}
