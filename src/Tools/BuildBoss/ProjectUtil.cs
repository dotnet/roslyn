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
            var type = element?.Value.Trim();

            var allGood = true;
            if (type != null && !IsValidRoslynProjectType(type))
            {
                allGood = false;
                textWriter.WriteLine($@"Value ""{type}"" is illegal for RoslynProjectType");
            }

            allGood &= IsVsixCorrectlySpecified(textWriter, type);

            return allGood;
        }

        private static bool IsValidRoslynProjectType(string type)
        {
            switch (type)
            {
                case "Dll":
                case "ExeDesktop":
                case "ExeCoreClr":
                case "UnitTest":
                case "UnitTestNext":
                case "CompilerGeneratorTool":
                case "DeploymentCompilerGeneratorTools":
                case "Deployment":
                case "Vsix":
                case "Dependency":
                case "Custom":
                    return true;
                default:
                    return false;
            }
        }

        private bool IsVsixCorrectlySpecified(TextWriter textWriter, string roslynProjectType)
        {
            var element = GetAllPropertyGroupElements().FirstOrDefault(x => x.Name.LocalName == "ProjectTypeGuids");
            if (element == null)
            {
                return true;
            }

            foreach (var rawValue in element.Value.Split(';'))
            {
                var value = rawValue.Trim();
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                var guid = Guid.Parse(value);
                if (guid == ProjectDataUtil.VsixProjectType && roslynProjectType != "Vsix")
                {
                    textWriter.WriteLine("Vsix projects must specify <RoslynProjectType>Vsix</RoslynProjectType>");
                    return false;
                }
            }

            return true;
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
