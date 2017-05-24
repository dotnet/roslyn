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
    internal sealed class SolutionCheckerUtil : ICheckerUtil
    {
        private readonly string _solutionFilePath;

        internal SolutionCheckerUtil(string solutionFilePath)
        {
            _solutionFilePath = solutionFilePath;
        }

        public bool Check(TextWriter textWriter)
        {
            var solutionPath = Path.GetDirectoryName(_solutionFilePath);
            var projectDataList = SolutionUtil.ParseProjects(_solutionFilePath);
            var map = new Dictionary<ProjectKey, ProjectData>();
            var allGood = true;

            foreach (var projectEntry in projectDataList)
            {
                if (projectEntry.IsFolder)
                {
                    continue;
                }

                var projectFilePath = Path.Combine(solutionPath, projectEntry.RelativeFilePath);
                var projectData = new ProjectData(projectFilePath);
                if (map.ContainsKey(projectData.Key))
                {
                    textWriter.WriteLine($"Duplicate project detected {projectData.FileName}");
                    allGood = false;
                }
                else
                {
                    map.Add(projectData.Key, projectData);
                }
            }

            if (!TryReadPackageVersionMap(out var packageVersionMap))
            {
                textWriter.WriteLine($"Unable to find Packages.props");
                return false;
            }

            var count = 0;
            foreach (var projectData in map.Values.OrderBy(x => x.FileName))
            {
                var projectWriter = new StringWriter();
                projectWriter.WriteLine($"Processing {projectData.Key.FileName}");
                var util = new ProjectCheckerUtil(projectData, map, packageVersionMap);
                if (!util.Check(projectWriter))
                {
                    allGood = false;
                    textWriter.WriteLine(projectWriter.ToString());
                }
                count++;
            }

            textWriter.WriteLine($"Processed {count} projects");
            return allGood;
        }

        private bool TryReadPackageVersionMap(out Dictionary<string, string> packageVersionMap)
        {
            var path = Path.GetDirectoryName(_solutionFilePath);
            string getPackagesPropPath() => Path.Combine(path, @"build\Targets\Packages.props");
            while (path != null && !File.Exists(getPackagesPropPath()))
            {
                path = Path.GetDirectoryName(path);
            }

            if (path == null)
            {
                packageVersionMap = null;
                return false;
            }

            packageVersionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PopulatePackageVersionMap(getPackagesPropPath(), packageVersionMap);

            var fixedPropsPath = Path.Combine(Path.GetDirectoryName(getPackagesPropPath()), "FixedPackages.props");
            PopulatePackageVersionMap(fixedPropsPath, packageVersionMap);
            return true;
        }

        private static void PopulatePackageVersionMap(string path, Dictionary<string, string> packageVersionMap)
        {
            var doc = XDocument.Load(path);
            var manager = new XmlNamespaceManager(new NameTable());
            manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            var prop = doc.XPathSelectElements("//mb:PropertyGroup", manager).Single();
            foreach (var element in prop.Elements())
            {
                packageVersionMap.Add(element.Name.LocalName, element.Value.Trim());
            }
        }
    }
}
