using PostSharp.Engineering.BuildTools.Utilities;
using Spectre.Console.Cli;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

#pragma warning disable 8765

namespace PostSharp.Engineering.BuildTools.Commands.NuGet
{
    public class RenamePackagesCommand : Command<RenamePackageSettings>
    {
        public override int Execute( CommandContext context, RenamePackageSettings settings )
        {
            var console = new ConsoleHelper();

            var success = true;

            var directory = new DirectoryInfo( settings.Directory );

            var files = Directory.GetFiles( directory.FullName, "Microsoft.*.nupkg" );

            if ( files.Length == 0 )
            {
                console.WriteError( $"No matching package found in '{directory.FullName}'." );
                return 1;
            }

            foreach ( var file in files )
            {
                success &= RenamePackage( console, directory.FullName, file );
            }

            return success ? 0 : 2;
        }

        private static bool RenamePackage( ConsoleHelper console, string directory, string inputPath )
        {
            console.WriteMessage( "Processing " + inputPath );

            var outputPath = Path.Combine(
                Path.GetDirectoryName( inputPath )!,
                Path.GetFileName( inputPath ).Replace( "Microsoft", "Caravela.Roslyn" ) );

            File.Copy( inputPath, outputPath, true );

            using var archive = ZipFile.Open( outputPath, ZipArchiveMode.Update );

            var oldNuspecEntry = archive.Entries.SingleOrDefault( entry => entry.FullName.EndsWith( ".nuspec" ) );

            if ( oldNuspecEntry == null )
            {
                console.WriteError( "Usage: Cannot find the nuspec file." );
                return false;
            }

            XDocument nuspecXml;
            XmlReader xmlReader;
            using ( var nuspecStream = oldNuspecEntry.Open() )
            {
                xmlReader = new XmlTextReader( nuspecStream );
                nuspecXml = XDocument.Load( xmlReader );
            }

            var ns = nuspecXml.Root!.Name.Namespace.NamespaceName;

            // Rename the packageId.
            var packageIdElement =
                nuspecXml.Root.Element( XName.Get( "metadata", ns ) )!.Element( XName.Get( "id", ns ) )!;
            var oldPackageId = packageIdElement.Value;
            var newPackageId = oldPackageId.Replace( "Microsoft", "Caravela.Roslyn" );
            var packageVersion = nuspecXml.Root.Element( XName.Get( "metadata", ns ) )!
                .Element( XName.Get( "version", ns ) )!.Value;
            packageIdElement.Value = newPackageId;

            // Rename the dependencies.
            var namespaceManager = new XmlNamespaceManager( xmlReader.NameTable );
            namespaceManager.AddNamespace( "p", ns );

            foreach ( var dependency in nuspecXml.XPathSelectElements( "//p:dependency", namespaceManager ) )
            {
                var dependentId = dependency.Attribute( "id" )!.Value;

                if ( dependentId.StartsWith( "Microsoft" ) )
                {
                    var dependencyPath = Path.Combine( directory, dependentId + "." + packageVersion + ".nupkg" );

                    if ( File.Exists( dependencyPath ) )
                    {
                        dependency.Attribute( "id" )!.Value = dependentId.Replace( "Microsoft", "Caravela.Roslyn" );
                    }
                    else
                    {
                        // The dependency is not produced by this repo.
                    }
                }
            }

            oldNuspecEntry.Delete();
            var newNuspecEntry = archive.CreateEntry( newPackageId + ".nuspec", CompressionLevel.Optimal );

            using ( var outputStream = newNuspecEntry.Open() )
            {
                nuspecXml.Save( outputStream );
            }

            return true;
        }
    }
}