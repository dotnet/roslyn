// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Utilities;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

#pragma warning disable 8765

namespace PostSharp.Engineering.BuildTools.NuGet
{
    public class RenamePackagesCommand : Command<RenamePackageSettings>
    {
        public override int Execute( CommandContext context, RenamePackageSettings settings )
        {
            return Execute( new ConsoleHelper(), settings ) ? 0 : 2;
        }

        public static bool Execute( ConsoleHelper console, RenamePackageSettings settings )
        {
            var success = true;

            var directory = new DirectoryInfo( settings.Directory );

            var files = Directory.GetFiles( directory.FullName, "Microsoft.*.nupkg" );

            if ( files.Length == 0 )
            {
                console.WriteError( $"No matching package found in '{directory.FullName}'." );

                return false;
            }

            foreach ( var file in files )
            {
                success &= RenamePackage( console, directory.FullName, file );
            }

            return success;
        }

        private static bool RenamePackage( ConsoleHelper console, string directory, string inputPath )
        {
            console.WriteMessage( "Processing " + inputPath );

            var outputPath = Path.Combine(
                Path.GetDirectoryName( inputPath )!,
                Path.GetFileName( inputPath ).Replace( "Microsoft", "Metalama.Roslyn", StringComparison.OrdinalIgnoreCase ) );

            File.Copy( inputPath, outputPath, true );

            using var archive = ZipFile.Open( outputPath, ZipArchiveMode.Update );

            var oldNuspecEntry = archive.Entries.SingleOrDefault( entry => entry.FullName.EndsWith( ".nuspec", StringComparison.OrdinalIgnoreCase ) );

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
            var newPackageId = oldPackageId.Replace( "Microsoft", "Metalama.Roslyn", StringComparison.OrdinalIgnoreCase );

            var packageVersion = nuspecXml.Root.Element( XName.Get( "metadata", ns ) )!
                .Element( XName.Get( "version", ns ) )!.Value;

            packageIdElement.Value = newPackageId;

            // Rename the dependencies.
            var namespaceManager = new XmlNamespaceManager( xmlReader.NameTable );
            namespaceManager.AddNamespace( "p", ns );

            foreach ( var dependency in nuspecXml.XPathSelectElements( "//p:dependency", namespaceManager ) )
            {
                var dependentId = dependency.Attribute( "id" )!.Value;

                if ( dependentId.StartsWith( "Microsoft", StringComparison.OrdinalIgnoreCase ) )
                {
                    var dependencyPath = Path.Combine( directory, dependentId + "." + packageVersion + ".nupkg" );

                    if ( File.Exists( dependencyPath ) )
                    {
                        dependency.Attribute( "id" )!.Value = dependentId.Replace( "Microsoft", "Metalama.Roslyn", StringComparison.OrdinalIgnoreCase );
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