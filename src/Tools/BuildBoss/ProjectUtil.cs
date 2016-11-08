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
    internal class ProjectUtil
    {
        private readonly ProjectKey _key;
        private readonly XDocument _document;
        private readonly XmlNamespaceManager _manager;

        internal ProjectUtil(string filePath) :this(new ProjectKey(filePath), XDocument.Load(filePath))
        {

        }

        internal ProjectUtil(ProjectKey key, XDocument document)
        {
            _key = key;
            _document = document;
            _manager = new XmlNamespaceManager(new NameTable());
            _manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
        }

        internal RoslynProjectData GetRoslynProjectData()
        {
            var typeElement = FindSingleProperty("RoslynProjectType");
            if (typeElement != null)
            {
                var value = typeElement.Value.Trim();
                var kind = RoslynProjectKindUtil.GetRoslynProjectKind(value);
                if (kind == null)
                {
                    throw new Exception($"Unrecognized RoslynProjectKind value {value}");
                }

                return new RoslynProjectData(kind.Value, kind.Value, value);
            }
            else
            { 
                var outputType = FindSingleProperty("OutputType");
                switch (outputType?.Value.Trim())
                {
                    case "Exe":
                    case "WinExe":
                        return new RoslynProjectData(RoslynProjectKind.Exe);
                    case "Library":
                        return new RoslynProjectData(RoslynProjectKind.Dll);
                    default:
                        throw new Exception($"Unrecognized OutputType value {outputType?.Value.Trim()}");
                }
            }
        }

        internal Guid? GetProjectGuid()
        {
            var elem = _document.XPathSelectElements("//mb:ProjectGuid", _manager).FirstOrDefault();
            if (elem == null)
            {
                return null;
            }

            return Guid.Parse(elem.Value);
        }

        internal RoslynProjectData? TryGetRoslynProjectData()
        {
            try
            {
                return GetRoslynProjectData();
            }
            catch
            {
                return null;
            }
        }

        internal IEnumerable<XElement> GetAllPropertyGroupElements()
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

        internal IEnumerable<XElement> GetTargets()
        {
            return _document.XPathSelectElements("//mb:Target", _manager);
        }

        internal IEnumerable<XElement> GetImports()
        {
            return _document.XPathSelectElements("//mb:Import", _manager);
        }

        internal IEnumerable<string> GetImportProjects()
        {
            return GetImports()
                .Select(x => x.Attribute("Project")?.Value)
                .Where(x => !string.IsNullOrEmpty(x));
        }

        internal IEnumerable<XElement> GetItemGroup()
        {
            return _document.XPathSelectElements("//mb:ItemGroup", _manager);
        }

        internal List<ProjectKey> GetDeclaredProjectReferences()
        {
            var references = _document.XPathSelectElements("//mb:ProjectReference", _manager);
            var list = new List<ProjectKey>();
            var directory = Path.GetDirectoryName(_key.FilePath);
            foreach (var r in references)
            {
                // Make sure to check for references that exist only for ordering purposes.  They don't count as 
                // actual references.
                var refOutputAssembly = r.Element(SharedUtil.MSBuildNamespace.GetName("ReferenceOutputAssembly"));
                if (refOutputAssembly != null)
                {
                    bool isRealReference;
                    if (bool.TryParse(refOutputAssembly.Value.Trim().ToLower(), out isRealReference) && !isRealReference)
                    {
                        continue;
                    }
                }

                var relativePath = r.Attribute("Include").Value;
                var path = Path.Combine(directory, relativePath);
                list.Add(new ProjectKey(path));
            }

            return list;
        }

        internal XElement FindSingleProperty(string localName) => GetAllPropertyGroupElements().SingleOrDefault(x => x.Name.LocalName == localName);
    }
}
