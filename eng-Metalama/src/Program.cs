using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.NuGet;
using Spectre.Console.Cli;
using System;
using System.Collections.Immutable;
using System.IO;

namespace Build
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var privateSource = new NugetSource("%INTERNAL_NUGET_PUSH_URL%", "%INTERNAL_NUGET_API_KEY%");
            var publicSource = new NugetSource("https://api.nuget.org/v3/index.json", "%NUGET_ORG_API_KEY%");


            // These packages are published to internal and private feeds.
            var publicPackages = new ParametricString[]
            {
                "Metalama.Compiler.$(PackageVersion).nupkg",
                "Metalama.Compiler.Sdk.$(PackageVersion).nupkg"
            };

            var publicPublishing = new NugetPublishTarget(
                Pattern.Empty.Add(publicPackages),
                privateSource,
                publicSource);


            // These packages are published to private feeds only.
            var privatePackages = new ParametricString[]
            {
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
                "Metalama.Roslyn.CodeAnalysis.VisualBasic.Workspaces.$(PackageVersion).nupkg",
            };


            var privatePublishing = new NugetPublishTarget(
                Pattern.Empty.Add(publicPackages).Add(privatePackages),
                privateSource);

            var product = new Product
            {
                PrivateArtifactsDirectory = "artifacts\\packages\\$(Configuration)\\Shipping",
                ProductName = "Metalama.Compiler",
                EngineeringDirectory = "eng-Metalama",
                GenerateArcadeProperties = true,
                AdditionalDirectoriesToClean = ImmutableArray.Create("artifacts"),
                Solutions = ImmutableArray.Create<Solution>(
                    new RoslynSolution()),
                PublishingTargets = ImmutableArray.Create<PublishingTarget>(publicPublishing, privatePublishing),
                Dependencies = ImmutableArray.Create(new ProductDependency("PostSharp.Backstage.Settings")),
                SupportedProperties = ImmutableDictionary.Create<string, string>().Add("TestAll", "Supported by the 'test' command. Run all tests instead of just Metalama's unit tests.")
            };
            product.BuildCompleted += OnBuildCompleted;
            var commandApp = new CommandApp();
            commandApp.AddProductCommands(product);

            return commandApp.Run(args);
        }

        private static bool OnBuildCompleted((BuildContext Context, BuildOptions Options, string Directory) args)
        {
            // Rename the packages as a post-build step.
            args.Context.Console.WriteHeading("Renaming packages");

            var success = RenamePackagesCommand.Execute(args.Context.Console, new RenamePackageSettings { Directory = args.Directory });

            if (success)
            {
                // Delete original packages (those non-renamed) so they don't get uploaded.
                foreach (var file in Directory.GetFiles(args.Directory, "Microsoft.*.nupkg"))
                {
                    File.Delete(file);
                }

                args.Context.Console.WriteSuccess("Renaming packages was successful.");
            }

            return success;
        }
    }
}
