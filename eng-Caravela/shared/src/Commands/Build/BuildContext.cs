using PostSharp.Engineering.BuildTools.Utilities;
using Spectre.Console.Cli;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace PostSharp.Engineering.BuildTools.Commands.Build
{
    public class BuildContext
    {
        public ConsoleHelper Console { get; }

        public string RepoDirectory { get; }

        public Product Product { get; }

        public string VersionFilePath =>
            Path.Combine( this.RepoDirectory, $"artifacts\\private\\{this.Product.ProductName}Version.props" );

        private BuildContext( ConsoleHelper console, string repoDirectory, Product product )
        {
            this.Console = console;
            this.RepoDirectory = repoDirectory;
            this.Product = product;
        }

        public static bool TryCreate( CommandContext commandContext,
            [NotNullWhen( true )] out BuildContext? buildContext )
        {
            var repoDirectory = FindRepoDirectory( Environment.CurrentDirectory );
            var console = new ConsoleHelper();

            if ( repoDirectory == null )
            {
                console.WriteError( "This tool must be called from a git repository." );
                buildContext = null;
                return false;
            }

            buildContext = new BuildContext( console, repoDirectory, (Product) commandContext.Data! );
            return true;
        }

        private static string? FindRepoDirectory( string directory )
        {
            if ( Directory.Exists( Path.Combine( directory, ".git" ) ) )
            {
                return directory;
            }
            else
            {
                var parentDirectory = Path.GetDirectoryName( directory );
                if ( parentDirectory != null )
                {
                    return FindRepoDirectory( parentDirectory );
                }
                else
                {
                    return null;
                }
            }
        }
    }
}