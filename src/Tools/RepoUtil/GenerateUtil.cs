using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RepoUtil
{
    /// <summary>
    /// Used for generating supporting files in the repo.  It will spit out named constants in props file 
    /// instead of having developers hard code version numbers.
    /// </summary>
    internal static class GenerateUtil
    {
        internal static XNamespace MSBuildNamespace { get; } = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

        /// <summary>
        /// Get the subset of packages which match the specified filter for the generated file.
        /// </summary>
        internal static IEnumerable<NuGetPackage> GetFilteredPackages(GenerateData generateData, IEnumerable<NuGetPackage> allPackages)
        {
            return allPackages
                .Where(x => generateData.Packages.Any(y => y.IsMatch(x.Name)))
                .ToList();
        }

        internal static void WriteMSBuildContent(FileName fileName, IEnumerable<NuGetPackage> packages)
        {
            Console.WriteLine($"Generating MSBuild props file {fileName}");
            var doc = GenerateMSBuildXml(packages);
            using (var writer = XmlWriter.Create(fileName.FullPath, new XmlWriterSettings() { Indent = true }))
            {
                doc.WriteTo(writer);
            }
        }

        /// <summary>
        /// Generate the MSBuild props file which contains named values for the NuGet versions.
        /// </summary>
        internal static XDocument GenerateMSBuildXml(IEnumerable<NuGetPackage> allPackages)
        {
            var ns = MSBuildNamespace;
            var doc = new XDocument(new XElement(ns + "Project"));
            doc.Root.Add(new XAttribute("ToolsVersion", "4.0"));

            var group = new XElement(ns + "PropertyGroup");
            foreach (var package in allPackages)
            {
                var name = PackageNameToXElementName(package.Name);
                var elem = new XElement(ns + name);
                elem.Value = package.Version;
                group.Add(elem);
            }

            doc.Root.Add(group);
            return doc;
        }

        private static string PackageNameToXElementName(string name)
        {
            return name.Replace(".", "") + "Version";
        }
    }
}
