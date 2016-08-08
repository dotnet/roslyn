using System;
using System.Collections.Generic;
using System.IO;
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
        internal static Encoding Encoding { get; } = Encoding.UTF8;

        /// <summary>
        /// Get the subset of packages which match the specified filter for the generated file.
        /// </summary>
        internal static List<NuGetPackage> GetFilteredPackages(GenerateData generateData, RepoData repoData)
        {
            // Fixed packages are never included in generated output.  Doing so would create conflicts because it's
            // possible for two versions to exist for the same package.  Take for example System.Collections.Immuatble
            // which is both fixed and floating in Roslyn.
            return repoData
                .FloatingPackages
                .Where(x => generateData.Packages.Any(y => y.IsMatch(x.Name)))
                .ToList();
        }

        /// <summary>
        /// Get any regex entries that match no packages.  Such entries are stale.
        /// </summary>
        internal static List<Regex> GetStaleRegex(GenerateData generateData, RepoData repoData)
        {
            return generateData
                .Packages
                .Where(r => repoData.FloatingPackages.All(p => !r.IsMatch(p.Name)))
                .ToList();
        }

        internal static void WriteMSBuildContent(FileName fileName, IEnumerable<NuGetPackage> packages)
        {
            Console.WriteLine($"Generating MSBuild props file {fileName}");
            using (var stream = File.Open(fileName.FullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                WriteMSBuildContent(stream, packages);
            }
        }

        private static void WriteMSBuildContent(Stream stream, IEnumerable<NuGetPackage> packages)
        {
            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings() { Indent = true, Encoding = Encoding }))
            {
                var document = GenerateMSBuildXml(packages);
                document.WriteTo(writer);
            }
        }

        internal static string GenerateMSBuildContent(IEnumerable<NuGetPackage> allPackages)
        {
            using (var stream = new MemoryStream())
            {
                WriteMSBuildContent(stream, allPackages);

                stream.Position = 0;
                using (var reader = new StreamReader(stream, Encoding))
                {
                    return reader.ReadToEnd();
                }
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
            doc.Root.Add(new XComment(@"Generated file, do not directly edit.  Run ""RepoUtil change"" to regenerate"));

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
