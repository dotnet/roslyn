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
        internal ProjectKey Key { get; }
        internal XDocument Document { get; }
        internal XmlNamespaceManager Manager { get; }
        internal XNamespace Namespace { get; }

        public bool IsNewSdk => Document.XPathSelectElements("//mb:TargetFramework", Manager).FirstOrDefault() != null;
        public bool IsDesktopProject => Document.XPathSelectElements("//mb:TargetFrameworkVersion", Manager).FirstOrDefault() != null;

        public bool IsPclProject
        {
            get
            {
                var elem = Document.XPathSelectElements("//mb:TargetFrameworkIdentifier", Manager).FirstOrDefault();
                if (elem == null)
                {
                    return false;
                }

                return StringComparer.OrdinalIgnoreCase.Equals(elem.Value, ".NETPortable");
            }
        }

        internal ProjectUtil(string filePath) : this(new ProjectKey(filePath), XDocument.Load(filePath))
        {
        }

        internal ProjectUtil(ProjectKey key, XDocument document)
        {
            Key = key;
            Document = document;
            Manager = new XmlNamespaceManager(new NameTable());
            Manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            Namespace = SharedUtil.MSBuildNamespace;
        }

        internal RoslynProjectData GetRoslynProjectData()
        {
            RoslynProjectData data;
            string error;
            if (!TryGetRoslynProjectData(out data, out error))
            {
                throw new Exception(error);
            }

            return data;
        }

        internal bool TryGetRoslynProjectData(out RoslynProjectData data, out string error)
        {
            data = default(RoslynProjectData);
            error = null;

            var typeElement = FindSingleProperty("RoslynProjectType");
            if (typeElement != null)
            {
                var value = typeElement.Value.Trim();
                var kind = RoslynProjectKindUtil.GetRoslynProjectKind(value);
                if (kind == null)
                {
                    error = $"The value {value} is illegal for element <RoslynProjectType>";
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
                        error = $"The value {outputType?.Value.Trim()} is not a recognized value of OutputType";
                        return false;
                }
            }
        }

        internal Guid? GetProjectGuid()
        {
            var elem = Document.XPathSelectElements("//mb:ProjectGuid", Manager).FirstOrDefault();
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
            var groups = Document.XPathSelectElements("//mb:PropertyGroup", Manager);
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
            return Document.XPathSelectElements("//mb:Target", Manager);
        }

        internal IEnumerable<XElement> GetImports()
        {
            return Document.XPathSelectElements("//mb:Import", Manager);
        }

        internal IEnumerable<string> GetImportProjects()
        {
            return GetImports()
                .Select(x => x.Attribute("Project")?.Value)
                .Where(x => !string.IsNullOrEmpty(x));
        }

        internal IEnumerable<XElement> GetItemGroup()
        {
            return Document.XPathSelectElements("//mb:ItemGroup", Manager);
        }

        internal List<ProjectReferenceEntry> GetDeclaredProjectReferences()
        {
            var references = Document.XPathSelectElements("//mb:ProjectReference", Manager);
            var list = new List<ProjectReferenceEntry>();
            var directory = Path.GetDirectoryName(Key.FilePath);
            foreach (var r in references)
            {
                // Make sure to check for references that exist only for ordering purposes.  They don't count as 
                // actual references.
                var refOutputAssembly = r.Element(Namespace.GetName("ReferenceOutputAssembly"));
                if (refOutputAssembly != null)
                {
                    bool isRealReference;
                    if (bool.TryParse(refOutputAssembly.Value.Trim().ToLower(), out isRealReference) && !isRealReference)
                    {
                        continue;
                    }
                }

                Guid? project = null;
                var projectElement = r.Element(Namespace.GetName("Project"));
                if (projectElement != null)
                {
                    project = Guid.Parse(projectElement.Value.Trim());
                }

                var relativePath = r.Attribute("Include").Value;
                var path = Path.Combine(directory, relativePath);
                list.Add(new ProjectReferenceEntry(path, project));
            }

            return list;
        }


        internal List<PackageReference> GetPackageReferences()
        {
            var list = new List<PackageReference>();
            foreach (var packageRef in Document.XPathSelectElements("//mb:PackageReference", Manager))
            {
                list.Add(GetPackageReference(packageRef));
            }

            return list;
        }

        internal PackageReference GetPackageReference(XElement element)
        {
            var name = element.Attribute("Include")?.Value ?? "";
            var version = element.Attribute("Version");
            if (version != null)
            {
                return new PackageReference(name, version.Value);
            }

            var elem = element.Element(Namespace.GetName("Version"));
            if (element == null)
            {
                throw new Exception($"Could not find a Version for package reference {name}");
            }

            return new PackageReference(name, elem.Value.Trim());
        }

        internal XElement FindSingleProperty(string localName) => GetAllPropertyGroupElements().SingleOrDefault(x => x.Name.LocalName == localName);
    }
}
