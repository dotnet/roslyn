using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Caravela.Build.RenamePackage
{
    class Program
    {
        static bool RenamePackage(string inputPath)
        {
            Console.WriteLine("Processing " + inputPath);

            string outputPath = Path.Combine(Path.GetDirectoryName(inputPath), Path.GetFileName(inputPath).Replace("Microsoft", "Caravela.Roslyn"));

            File.Copy(inputPath, outputPath, true);

            using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Update);

            var oldNuspecEntry = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec"));

            if (oldNuspecEntry == null)
            {
                Console.Error.WriteLine("Usage: Cannot find the nuspec file.");
                return false;
            }

            XDocument nuspecXml;
            using (var nuspecStream = oldNuspecEntry.Open())
            {
                nuspecXml = XDocument.Load(nuspecStream);
            }

            var ns = nuspecXml.Root.Name.Namespace.NamespaceName;
            var packageIdElement = nuspecXml.Root.Element( XName.Get("metadata", ns)).Element(XName.Get("id", ns));
            var oldPackageId = packageIdElement.Value;
            var newPackageId = oldPackageId.Replace("Microsoft", "Caravela.Roslyn");
            packageIdElement.Value = newPackageId;

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
            if ( args.Length != 1 )
            {
                Console.Error.WriteLine("Usage: Caravela.Build.RenamePackage <directory>");
                return 1;
            }


            string directory = args[0];

            bool success = true;
            foreach ( var file in Directory.GetFiles(directory, "Microsoft.*.nupkg"))
            {
                success &= RenamePackage(file);
            }



            return success ? 0 : 2;
        }
    }
}
