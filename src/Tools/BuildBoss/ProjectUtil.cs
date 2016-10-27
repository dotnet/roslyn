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

        internal ProjectUtil(ProjectKey key, XDocument document)
        {
            _document = document;
            _manager = new XmlNamespaceManager(new NameTable());
            _manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
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

        internal List<ProjectKey> GetDeclaredProjectReferences()
        {
            var references = _document.XPathSelectElements("//mb:ProjectReference", _manager);
            var list = new List<ProjectKey>();
            var directory = Path.GetDirectoryName(_key.FilePath);
            foreach (var r in references)
            {
                var relativePath = r.Attribute("Include").Value;
                var path = Path.Combine(directory, relativePath);
                list.Add(new ProjectKey(path));
            }

            return list;
        }

        internal XElement FindSingleProperty(string localName) => GetAllPropertyGroupElements().SingleOrDefault(x => x.Name.LocalName == localName);
    }
}
