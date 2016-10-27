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
        private readonly ProjectData _data;
        private readonly XDocument _document;
        private readonly XmlNamespaceManager _manager;
        private readonly Dictionary<ProjectKey, ProjectData> _solutionMap;

        internal ProjectFileType ProjectType => _data.ProjectFileType;
        internal string ProjectFilePath => _data.FilePath;

        internal ProjectUtil(ProjectData data, Dictionary<ProjectKey, ProjectData> solutionMap)
        {
            _data = data;
            _document = data.Document;
            _solutionMap = solutionMap;
            _manager = new XmlNamespaceManager(new NameTable());
            _manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
        }

        internal bool CheckAll(TextWriter textWriter)
        {
            var allGood = true;
            if (ProjectType == ProjectFileType.CSharp || ProjectType == ProjectFileType.Basic)
            {
                allGood &= CheckForProperty(textWriter, "RestorePackages");
                allGood &= CheckForProperty(textWriter, "SolutionDir");
                allGood &= CheckForProperty(textWriter, "FileAlignment");
                allGood &= CheckForProperty(textWriter, "FileUpgradeFlags");
                allGood &= CheckForProperty(textWriter, "UpgradeBackupLocation");
                allGood &= CheckForProperty(textWriter, "OldToolsVersion");
                allGood &= CheckRoslynProjectType(textWriter);
                allGood &= CheckProjectReferences(textWriter);
            }

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
            RoslynProjectData data;
            if (!ParseRoslynProjectData(textWriter, out data))
            {
                return false;
            }

            var allGood = true;
            allGood &= IsVsixCorrectlySpecified(textWriter, data);
            allGood &= IsUnitTestCorrectlySpecified(textWriter, data);

            return allGood;
        }

        private bool ParseRoslynProjectData(TextWriter textWriter, out RoslynProjectData data)
        {
            data = default(RoslynProjectData);

            var typeElement = FindSingleProperty("RoslynProjectType");
            if (typeElement != null)
            {
                var value = typeElement.Value.Trim();
                var kind = RoslynProjectData.GetRoslynProjectKind(value);
                if (kind == null)
                {
                    textWriter.WriteLine($"Unrecognized RoslynProjectKnid value {value}");
                    return false;
                }

                data = new RoslynProjectData(kind.Value, kind.Value, value);
                return true;
            }
            else
            { 
                var outputType = FindSingleProperty("OutputType");
                switch (outputType?.Value.Trim())
                {
                    case "Exe":
                    case "WinExe":
                        data = new RoslynProjectData(RoslynProjectKind.Exe);
                        return true;
                    case "Library":
                        data = new RoslynProjectData(RoslynProjectKind.Dll);
                        return true;
                    default:
                        textWriter.WriteLine($"Unrecognized OutputType value {outputType?.Value.Trim()}");
                        return false;
                }
            }
        }

        private bool IsVsixCorrectlySpecified(TextWriter textWriter, RoslynProjectData data)
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
                if (guid == ProjectEntryUtil.VsixProjectType && data.EffectiveKind != RoslynProjectKind.Vsix)
                {
                    textWriter.WriteLine("Vsix projects must specify <RoslynProjectType>Vsix</RoslynProjectType>");
                    return false;
                }
            }

            return true;
        }

        private bool CheckProjectReferences(TextWriter textWriter)
        {
            var allGood = true;

            // It's important that every reference be included in the solution.  MSBuild does not necessarily
            // apply all configuration entries to projects which are compiled via referenes but not included
            // in the solution.
            var declaredList = GetDeclaredProjectReferences(_data);
            foreach (var key in declaredList)
            {
                if (!_solutionMap.ContainsKey(key))
                {
                    textWriter.WriteLine($"Project reference {key.FileName} is not included in the solution");
                    allGood = false;
                }
            }

            return allGood;
        }

        private bool IsUnitTestCorrectlySpecified(TextWriter textWriter, RoslynProjectData data)
        {
            if (ProjectType != ProjectFileType.CSharp && ProjectType != ProjectFileType.Basic)
            {
                return true;
            }

            if (data.EffectiveKind == RoslynProjectKind.Depedency)
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
                switch (data.EffectiveKind)
                {
                    case RoslynProjectKind.UnitTest:
                    case RoslynProjectKind.UnitTestNext:
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
            var groups = _document.XPathSelectElements("//mb:PropertyGroup", _manager);
            foreach (var group in groups)
            {
                foreach (var element in group.Elements())
                {
                    yield return element;
                }
            }
        }

        private List<ProjectKey> GetDeclaredProjectReferences(ProjectData data)
        {
            var references = data.Document.XPathSelectElements("//mb:ProjectReference", _manager);
            var list = new List<ProjectKey>();
            foreach (var r in references)
            {
                var relativePath = r.Attribute("Include").Value;
                var path = Path.Combine(data.Directory, relativePath);
                list.Add(new ProjectKey(path));
            }

            return list;
        }

        private XElement FindSingleProperty(string localName) => GetAllPropertyGroupElements().SingleOrDefault(x => x.Name.LocalName == localName);
    }
}
