using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Caravela.Build.RenamePackage
{
    class Program
    {
        static bool RenamePackage(string directory, string inputPath)
        {
            Console.WriteLine("Processing " + inputPath);

            string outputPath = Path.Combine(Path.GetDirectoryName(inputPath),
                Path.GetFileName(inputPath).Replace("Microsoft", "Caravela.Roslyn"));

            File.Copy(inputPath, outputPath, true);

            using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Update);

            var oldNuspecEntry = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec"));

            if (oldNuspecEntry == null)
            {
                Console.Error.WriteLine("Usage: Cannot find the nuspec file.");
                return false;
            }

            XDocument nuspecXml;
            XmlReader xmlReader;
            using (var nuspecStream = oldNuspecEntry.Open())
            {
                xmlReader = new XmlTextReader(nuspecStream);
                nuspecXml = XDocument.Load(xmlReader);
            }


            var ns = nuspecXml.Root.Name.Namespace.NamespaceName;

            // Rename the packageId.
            var packageIdElement = nuspecXml.Root.Element(XName.Get("metadata", ns)).Element(XName.Get("id", ns));
            var oldPackageId = packageIdElement.Value;
            var newPackageId = oldPackageId.Replace("Microsoft", "Caravela.Roslyn");
            var packageVersion = nuspecXml.Root.Element(XName.Get("metadata", ns)).Element(XName.Get("version", ns)).Value;
            packageIdElement.Value = newPackageId;

            // Rename the dependencies.
            var namespaceManager = new XmlNamespaceManager(xmlReader.NameTable);
            namespaceManager.AddNamespace("p", ns);

            foreach (var dependency in nuspecXml.XPathSelectElements("//p:dependency", namespaceManager))
            {
                var dependentId = dependency.Attribute("id").Value;

                if (dependentId.StartsWith("Microsoft"))
                {
                    var dependencyPath = Path.Combine(directory, dependentId + "." + packageVersion + ".nupkg");

                    if (File.Exists(dependencyPath))
                    {
                        dependency.Attribute("id").Value = dependentId.Replace("Microsoft", "Caravela.Roslyn");
                    }
                    else
                    {
                        // The dependency is not produced by this repo.

                    }
                }
            }

            oldNuspecEntry.Delete();
            var newNuspecEntry = archive.CreateEntry(newPackageId + ".nuspec", CompressionLevel.Optimal);


            using (var outputStream = newNuspecEntry.Open())
            {
                nuspecXml.Save(outputStream);
            }

            return true;
        }

        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: Caravela.Build.RenamePackage <directory>");
                return 1;
            }


            string directory = args[0];
            bool success = true;
            foreach (var file in Directory.GetFiles(directory, "Microsoft.*.nupkg"))
            {
                success &= RenamePackage( directory, file);
            }


            return success ? 0 : 2;
        }
    }
}
