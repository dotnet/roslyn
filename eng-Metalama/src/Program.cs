using Build;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.NuGet;
using Spectre.Console.Cli;
using System.IO;
using Build.NuGetDependencies;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Dependencies.Definitions;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2025_0;

var product = new Product(MetalamaDependencies.MetalamaCompiler)
{
    VersionsFilePath = "eng\\Versions.props",
    GenerateArcadeProperties = true,
    AdditionalDirectoriesToClean = ["artifacts"],
    Solutions = [new RoslynSolution()],
    PublicArtifacts = Pattern.Create("Metalama.Compiler.$(PackageVersion).nupkg", "Metalama.Compiler.Sdk.$(PackageVersion).nupkg"),
    Dependencies = [DevelopmentDependencies.PostSharpEngineering],
    SupportedProperties = new() { ["TestAll"] = "Supported by the 'test' command. Run all tests instead of just Metalama's unit tests." },
    ExportedProperties = { { @"eng\Versions.props", new[] { "RoslynVersion" } } },
    KeepEditorConfig = true,
    Configurations = Product.DefaultConfigurations.WithValue(BuildConfiguration.Release, c => c with { ExportsToTeamCityBuild = true }),
    DefaultTestsFilter = "Category!=OuterLoop"
};


var app = new EngineeringApp(product);

app.Configure(delegate (IConfigurator root)
{
    root.AddCommand<PushNuGetDependenciesCommand>("push-nuget-dependencies")
        .WithData(new BaseCommandData(product))
        .WithDescription("Pushes NuGet dependencies not coming from NuGet.org to Azure Artifacts repository. See See docs-Metalama/Merging.md for details.");
});

return app.Run( args );
