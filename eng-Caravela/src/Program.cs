using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Dependencies.Model;
using PostSharp.Engineering.BuildTools.NuGet;
using Spectre.Console.Cli;
using System.Collections.Immutable;
using System.IO;

namespace Build
{
    internal class Program
    {
        private static int Main( string[] args )
        {

            var product = new Product
            {
                PrivateArtifactsDirectory = "artifacts\\packages\\$(Configuration)\\Shipping",
                ProductName = "Caravela.Compiler",
                EngineeringDirectory = "eng-Caravela",
                VersionsFile = "eng\\Versions.props",
                GenerateArcadeProperties = true,
                AdditionalDirectoriesToClean = ImmutableArray.Create( "artifacts" ),
                Solutions = ImmutableArray.Create<Solution>( new RoslynSolution() ),
                PublicArtifacts = Pattern.Create( "Caravela.Compiler.$(PackageVersion).nupkg", "Caravela.Compiler.Sdk.$(PackageVersion).nupkg" ),
                PrivateArtifacts = Pattern.Create(
                     "Caravela.RoslynUtilities.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.Common.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.CSharp.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.CSharp.Features.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.CSharp.Scripting.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.CSharp.Workspaces.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.Features.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.Scripting.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.Scripting.Common.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.Workspaces.Common.$(PackageVersion).nupkg",

                // Visual Basic is needed by Caravela.Try
                "Caravela.Roslyn.CodeAnalysis.VisualBasic.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.VisualBasic.Features.$(PackageVersion).nupkg",
                "Caravela.Roslyn.CodeAnalysis.VisualBasic.Workspaces.$(PackageVersion).nupkg" ),
                Dependencies = ImmutableArray.Create( Dependencies.PostSharpEngineering, Dependencies.PostSharpBackstageSettings ),
                SupportedProperties = ImmutableDictionary.Create<string, string>().Add( "TestAll", "Supported by the 'test' command. Run all tests instead of just Caravela's unit tests." ),
                KeepEditorConfig = true,
            };
            product.BuildCompleted += OnBuildCompleted;
            var commandApp = new CommandApp();
            commandApp.AddProductCommands( product );

            return commandApp.Run( args );
        }

        private static bool OnBuildCompleted( (BuildContext Context, BuildOptions Options, string Directory) args )
        {
            // Rename the packages as a post-build step.
            args.Context.Console.WriteHeading( "Renaming packages" );

            var success = RenamePackagesCommand.Execute( args.Context.Console, new RenamePackageSettings { Directory = args.Directory } );

            if ( success )
            {
                // Delete original packages (those non-renamed) so they don't get uploaded.
                foreach ( var file in Directory.GetFiles( args.Directory, "Microsoft.*.nupkg" ) )
                {
                    File.Delete( file );
                }

                args.Context.Console.WriteSuccess( "Renaming packages was successful." );
            }

            return success;
        }
    }
}
