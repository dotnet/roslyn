using System.Collections.Generic;
using BuildMetalamaCompiler;
using BuildMetalamaCompiler.NuGetDependencies;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Docker;
using Spectre.Console.Cli;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2026_0;

var product = new Product(MetalamaDependencies.MetalamaCompiler)
{
    OverriddenBuildAgentRequirements = new ContainerRequirements(ContainerHostKind.Windows)
    {
        Components =
        [
            // Must match global.json.
            new DotNetComponent("10.0.100-preview.6.25358.103", DotNetComponentKind.Sdk),
            new VisualStudioBuildToolsComponent(
                VisualStudioBuildToolsComponentVersion.v17_14_15,
            [
                "Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools",
                "Microsoft.VisualStudio.Workload.NetCoreBuildTools",
                "Microsoft.VisualStudio.Workload.MSBuildTools",
                "Microsoft.Net.Component.4.7.2.TargetingPack",
                "Microsoft.Net.Component.4.7.2.SDK",
                "Microsoft.NetCore.Component.SDK"
            ])
        ]
    },
    VersionsFilePath = "eng\\Versions.props",
    GenerateArcadeProperties = true,
    AdditionalDirectoriesToClean = ["artifacts"],
    Solutions = [new RoslynSolution()],
    PublicArtifacts =
        Pattern.Create("Metalama.Compiler.$(PackageVersion).nupkg",
            "Metalama.Compiler.Sdk.$(PackageVersion).nupkg"),
    SupportedProperties =
        new Dictionary<string, string>
        {
            ["TestAll"] =
                "Supported by the 'test' command. Run all tests instead of just Metalama's unit tests."
        },
    ExportedProperties = { { @"eng\Versions.props", ["RoslynVersion"] } },
    KeepEditorConfig = true,
    Configurations =
        Product.DefaultConfigurations.WithValue(BuildConfiguration.Release,
            c => c with { ExportsToTeamCityBuild = true }),
    DefaultTestsFilter = "Category!=OuterLoop"
};


var app = new EngineeringApp(product);

app.Configure(delegate(IConfigurator root)
{
    root.AddCommand<PushNuGetDependenciesCommand>("push-nuget-dependencies")
        .WithData(new BaseCommandData(product))
        .WithDescription(
            "Pushes NuGet dependencies not coming from NuGet.org to Azure Artifacts repository. See See docs-Metalama/Merging.md for details.");
});

return app.Run(args);
