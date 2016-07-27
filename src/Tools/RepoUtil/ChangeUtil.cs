using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RepoUtil
{
    /// <summary>
    /// Responsible for changing the repo to use a new set of NuGet packages.
    /// </summary>
    internal sealed class ChangeUtil
    {
        private readonly RepoData _repoData;

        internal ChangeUtil(RepoData repoData)
        {
            _repoData = repoData;
        }

        internal void ChangeAll()
        {
            // TODO: actually take an URL
            var list = new List<NuGetPackage>();
            foreach (var line in File.ReadAllLines(@"e:\temp\test.txt"))
            {
                var item = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var package = new NuGetPackage(item[0], item[1]);
                list.Add(package);
            }

            ChangeAll(list);
        }

        /// <summary>
        /// Change the state of the repo to use the specified packages.
        /// </summary>
        internal void ChangeAll(IEnumerable<NuGetPackage> packages)
        {
            var changeList = CalculateChanges(packages);
            var map = ImmutableDictionary<string, NuGetPackage>.Empty.WithComparers(Constants.NugetPackageNameComparer);
            foreach (var package in changeList)
            {
                map = map.Add(package.Name, package.NewPackage);
            }
            ChangeAllCore(map);
        }

        private List<NuGetPackageChange> CalculateChanges(IEnumerable<NuGetPackage> packages)
        {
            var map = _repoData
                .FloatingPackages
                .ToDictionary(x => x.Name, Constants.NugetPackageNameComparer);
            var list = new List<NuGetPackageChange>();

            Console.WriteLine("Calculating the changes");
            foreach (var package in packages)
            {
                NuGetPackage existingPackage;
                if (!map.TryGetValue(package.Name, out existingPackage))
                {
                    Console.WriteLine($"\tSkipping {package.Name} as it's not a floating package in this repo.");
                }

                list.Add(new NuGetPackageChange(package.Name, oldVersion: existingPackage.Version, newVersion: package.Version));
            }

            return list;
        }

        /// <summary>
        /// Change the repo to respect the new version for the specified set of NuGet packages
        /// </summary>
        private void ChangeAllCore(ImmutableDictionary<string, NuGetPackage> changeMap)
        {
            ChangeProjectJsonFiles(changeMap);

            // Calculate the new set of packages based on the changed information.
            var list = new List<NuGetPackage>();
            foreach (var cur in _repoData.AllPackages)
            {
                NuGetPackage newPackage;
                if (changeMap.TryGetValue(cur.Name, out newPackage))
                {
                    list.Add(newPackage);
                }
                else
                {
                    list.Add(cur);
                }
            }

            ChangeGeneratedFiles(list);
        }

        private void ChangeProjectJsonFiles(ImmutableDictionary<string, NuGetPackage> changeMap)
        {
            Console.WriteLine("Changing project.json files");
            foreach (var filePath in ProjectJsonUtil.GetProjectJsonFiles(_repoData.SourcesPath))
            {
                if (ProjectJsonUtil.ChangeDependencies(filePath, changeMap))
                {
                    Console.WriteLine($"\t{filePath} updated");
                }
            }
        }

        private void ChangeGeneratedFiles(IEnumerable<NuGetPackage> allPackages)
        {
            var msbuildData = _repoData.RepoConfig.MSBuildGenerateData;
            if (msbuildData.HasValue)
            {
                GenerateMSBuild(msbuildData.Value, allPackages);
            }
        }

        private void GenerateMSBuild(GenerateData data, IEnumerable<NuGetPackage> allPackages)
        {
            Console.WriteLine($"Generating MSBuild props file {data.RelativeFileName}");
            var doc = GenerateMSBuildXml(data, allPackages);
            var fileName = new FileName(_repoData.SourcesPath, data.RelativeFileName);
            using (var writer = XmlWriter.Create(fileName.FullPath, new XmlWriterSettings() { Indent = true }))
            {
                doc.WriteTo(writer);
            }
        }

        /// <summary>
        /// Generate the MSBuild props file which contains named values for the NuGet versions.
        /// </summary>
        private XDocument GenerateMSBuildXml(GenerateData data, IEnumerable<NuGetPackage> allPackages)
        {
            var ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
            var doc = new XDocument(new XElement(ns + "Project"));
            doc.Root.Add(new XAttribute("ToolsVersion", "4.0"));

            var group = new XElement(ns + "PropertyGroup");
            foreach (var package in allPackages)
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
