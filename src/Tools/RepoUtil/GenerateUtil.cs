using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RepoUtil
{
    internal class GenerateUtil
    {
        private readonly RepoData _repoData;
        private readonly RepoConfig _repoConfig;

        internal GenerateUtil(RepoData repoData)
        {
            _repoData = repoData;
            _repoConfig = repoData.RepoConfig;
        }

        internal void Go()
        {
            if (_repoConfig.MSBuildGenerateData.HasValue)
            {
                GenerateMSBuild(_repoConfig.MSBuildGenerateData.Value);
            }
        }

        private void GenerateMSBuild(GenerateData data)
        {
            var doc = GenerateMSBuildXml(data);
            var fileName = new FileName(_repoData.SourcesPath, data.RelativeFileName);
            using (var writer = XmlWriter.Create(fileName.FullPath, new XmlWriterSettings() { Indent = true }))
            {
                doc.WriteTo(writer);
            }
        }

        private XDocument GenerateMSBuildXml(GenerateData data)
        {
            var ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
            var doc = new XDocument(new XElement(ns + "Project"));
            doc.Root.Add(new XAttribute("ToolsVersion", "4.0"));

            var group = new XElement(ns + "PropertyGroup");
            foreach (var package in _repoData.AllPackages)
            {
                if (data.Packages.Any(x => x.IsMatch(package.Name)))
                {
                    var name = package.Name.Replace(".", "") + "Version";
                    var elem = new XElement(ns + name);
                    elem.Value = package.Version;
                    group.Add(elem);
                }
            }

            doc.Root.Add(group);
            return doc;
        }
    }
}
