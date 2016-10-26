using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            var element = FindSingleProperty("RoslynProjectType");
            var type = element?.Value.Trim();

            var allGood = true;
            if (type != null && !IsValidRoslynProjectType(type))
            {
                allGood = false;
                textWriter.WriteLine($@"Value ""{type}"" is illegal for RoslynProjectType");
            }

            allGood &= IsVsixCorrectlySpecified(textWriter, type);
            allGood &= IsUnitTestCorrectlySpecified(textWriter, type);

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
            var element = FindSingleProperty("ProjectTypeGuids");
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

        private bool IsUnitTestCorrectlySpecified(TextWriter textWriter, string roslynProjectType)
        {
            if (ProjectType != ProjectType.CSharp && ProjectType != ProjectType.Basic)
            {
                return true;
            }

            if (roslynProjectType == "Dependency")
            {
                return true;
            }

            var element = FindSingleProperty("AssemblyName");
            if (element == null)
            {
                textWriter.WriteLine($"Need to specify AssemblyName");
                return false;
            }

            var name = element.Value.Trim();
            if (Regex.IsMatch(name, @"UnitTest(s?)\.dll", RegexOptions.IgnoreCase))
            {
                switch (roslynProjectType)
                {
                    case "UnitTest":
                    case "UnitTestNext":
                        // This is correct
                        break;
                    default:
                        textWriter.WriteLine($"Assembly named {name} is not marked as a unit test");
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

        private XElement FindSingleProperty(string localName) => GetAllPropertyGroupElements().SingleOrDefault(x => x.Name.LocalName == localName);
    }
}
