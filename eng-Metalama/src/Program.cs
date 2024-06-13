using Build;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.NuGet;
using Spectre.Console.Cli;
using System.IO;
using Build.NuGetDependencies;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Dependencies.Definitions;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2024_2;

var product = new Product(MetalamaDependencies.MetalamaCompiler)
{
    VersionsFilePath = "eng\\Versions.props",
    GenerateArcadeProperties = true,
    AdditionalDirectoriesToClean = ["artifacts"],
    Solutions = [new RoslynSolution()],
    PublicArtifacts = Pattern.Create("Metalama.Compiler.$(PackageVersion).nupkg", "Metalama.Compiler.Sdk.$(PackageVersion).nupkg"),
    PrivateArtifacts = Pattern.Create(
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
    "Metalama.Roslyn.CodeAnalysis.VisualBasic.Workspaces.$(PackageVersion).nupkg"),
    Dependencies = [DevelopmentDependencies.PostSharpEngineering],
    SupportedProperties = new() { ["TestAll"] = "Supported by the 'test' command. Run all tests instead of just Metalama's unit tests." },
    ExportedProperties = { { @"eng\Versions.props", new[] { "RoslynVersion" } } },
    KeepEditorConfig = true,
    Configurations = Product.DefaultConfigurations.WithValue(BuildConfiguration.Release, c => c with { ExportsToTeamCityBuild = true }),
    DefaultTestsFilter = "Category!=OuterLoop"
};

product.BuildCompleted += OnBuildCompleted;
var commandApp = new CommandApp();
commandApp.AddProductCommands( product );
commandApp.Configure(delegate (IConfigurator root)
{
    root.AddCommand<PushNuGetDependenciesCommand>("push-nuget-dependencies").WithData(product).WithDescription("Pushes NuGet dependencies not coming from NuGet.org to Azure Artifacts repository. See See docs-Metalama/Merging.md for details.");
});

return commandApp.Run( args );

static void OnBuildCompleted( BuildCompletedEventArgs args )
{
    // Rename the packages as a post-build step.
    args.Context.Console.WriteHeading( "Renaming packages" );

    var success = RenamePackagesCommand.Execute( args.Context.Console, new RenamePackageCommandSettings { Directory = args.PrivateArtifactsDirectory } );

    if ( success )
    {
        // Delete original packages (those non-renamed) so they don't get uploaded.
        foreach ( var file in Directory.GetFiles( args.PrivateArtifactsDirectory, "Microsoft.*.nupkg" ) )
        {
            File.Delete( file );
        }

        args.Context.Console.WriteSuccess( "Renaming packages was successful." );
    }

    args.IsFailed = !success;
}
