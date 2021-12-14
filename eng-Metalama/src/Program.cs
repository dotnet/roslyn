﻿using Build;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Dependencies.Model;
using PostSharp.Engineering.BuildTools.NuGet;
using Spectre.Console.Cli;
using System.Collections.Immutable;
using System.IO;

var product = new Product
{
    PrivateArtifactsDirectory = "artifacts\\packages\\$(Configuration)\\Shipping",
    ProductName = "Metalama.Compiler",
    EngineeringDirectory = "eng-Metalama",
    VersionsFile = "eng\\Versions.props",
    GenerateArcadeProperties = true,
    AdditionalDirectoriesToClean = ImmutableArray.Create( "artifacts" ),
    Solutions = ImmutableArray.Create<Solution>( new RoslynSolution() ),
    PublicArtifacts = Pattern.Create( "Metalama.Compiler.$(PackageVersion).nupkg", "Metalama.Compiler.Sdk.$(PackageVersion).nupkg" ),
    PrivateArtifacts = Pattern.Create(
         "Metalama.RoslynUtilities.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.Common.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.CSharp.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.CSharp.Features.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.CSharp.Scripting.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.CSharp.Workspaces.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.Features.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.Scripting.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.Scripting.Common.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.Workspaces.Common.$(PackageVersion).nupkg",

    // Visual Basic is needed by Metalama.Try
    "Metalama.Roslyn.CodeAnalysis.VisualBasic.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.VisualBasic.Features.$(PackageVersion).nupkg",
    "Metalama.Roslyn.CodeAnalysis.VisualBasic.Workspaces.$(PackageVersion).nupkg" ),
    Dependencies = ImmutableArray.Create( Dependencies.PostSharpEngineering, Dependencies.PostSharpBackstageSettings, Dependencies.Roslyn ),
    SupportedProperties = ImmutableDictionary.Create<string, string>().Add( "TestAll", "Supported by the 'test' command. Run all tests instead of just Metalama's unit tests." ),
    KeepEditorConfig = true,
};
product.BuildCompleted += OnBuildCompleted;
var commandApp = new CommandApp();
commandApp.AddProductCommands( product );

return commandApp.Run( args );

static bool OnBuildCompleted( (BuildContext Context, BuildSettings Settings, string Directory) args )
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
