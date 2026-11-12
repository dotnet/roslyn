// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace BuildBoss
{
    /// <summary>
    /// Verifies our NuGet packages can actually be consumed: that a customer can add one to a project
    /// and compile against its API.
    ///
    /// The generated project calls into the package it installs, so the build proves the package
    /// really surfaces a usable reference rather than just restoring successfully.
    ///
    /// Everything here uses SDK style projects and PackageReference. That covers .NET Framework as well
    /// because an SDK style project can target net472. Legacy packages.config is deliberately not
    /// tested: install would require us to hand author the Reference / HintPath / Import edits that
    /// Visual Studio normally makes, which would test our emulation of NuGet rather than NuGet itself.
    ///
    /// This is the one checker that spawns processes and needs network access, so it is opt in via the
    /// --check-package-install switch rather than running on every BuildBoss invocation.
    /// </summary>
    internal sealed class PackageInstallChecker : ICheckerUtil
    {
        /// <summary>
        /// The packages validated by this checker, each paired with source that consumes it. The source
        /// is compiled but never run, and uses fully qualified names so it works on any target framework.
        /// </summary>
        private static readonly (string PackageId, string SourceCode)[] s_packages = new[]
        {
            ("Microsoft.CodeAnalysis",
@"internal static class PackageUsage
{
    internal static void Use()
    {
        Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(""class C { }"").GetRoot();
        Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree.ParseText("""").GetRoot();
    }
}"),

            ("Microsoft.CodeAnalysis.Workspaces.MSBuild",
@"internal static class PackageUsage
{
    internal static void Use()
    {
        using (var workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create())
        {
            var solution = workspace.OpenSolutionAsync(""Test.sln"");
        }
    }
}"),
        };

        internal string ArtifactsDirectory { get; }
        internal string Configuration { get; }

        internal PackageInstallChecker(string artifactsDirectory, string configuration)
        {
            ArtifactsDirectory = artifactsDirectory;
            Configuration = configuration;
        }

        public bool Check(TextWriter textWriter)
        {
            try
            {
                var packagesDirectory = Path.Combine(ArtifactsDirectory, "packages", Configuration, "Shipping");
                if (!Directory.Exists(packagesDirectory))
                {
                    textWriter.WriteLine($"Package directory '{packagesDirectory}' does not exist; was the build run with -pack?");
                    return false;
                }

                // Build outside the repository so the generated projects behave like a customer's: no
                // Directory.Build.props, central package management, or NuGet.config walked up into.
                var scratchDirectory = Path.Combine(Path.GetTempPath(), "RoslynPackageInstallValidation");
                var globalPackagesDirectory = Path.Combine(scratchDirectory, ".nuget");
                PrepareScratchDirectory(scratchDirectory, packagesDirectory, globalPackagesDirectory);

                var allGood = true;
                foreach (var (packageId, sourceCode) in s_packages)
                {
                    var packageFilePath = SharedUtil.FindNuGetPackage(packagesDirectory, packageId);
                    var packageVersion = SharedUtil.GetNuGetPackageVersion(packageFilePath, packageId);

                    foreach (var (name, targetFramework) in s_targetFrameworks)
                    {
                        allGood &= CheckPackage(textWriter, scratchDirectory, packageId, sourceCode, packageVersion, name, targetFramework);
                    }
                }

                return allGood;
            }
            catch (Exception ex)
            {
                textWriter.WriteLine($"Error verifying: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Installs the package into a freshly generated project that compiles against it. Returns false,
        /// having written the details, if any step misbehaves.
        /// </summary>
        private static bool CheckPackage(
            TextWriter textWriter,
            string scratchDirectory,
            string packageId,
            string sourceCode,
            string packageVersion,
            string targetFrameworkName,
            string targetFramework)
        {
            var projectDirectory = Path.Combine(scratchDirectory, $"{packageId}.{targetFrameworkName}");
            var projectFilePath = Path.Combine(projectDirectory, "PackageValidation.csproj");

            Directory.CreateDirectory(projectDirectory);
            File.WriteAllText(projectFilePath, GenerateProjectFile(targetFramework), SharedUtil.Encoding);
            File.WriteAllText(Path.Combine(projectDirectory, "PackageUsage.cs"), sourceCode, SharedUtil.Encoding);

            var context = $"{packageId} ({targetFrameworkName})";

            if (!RunDotnetAndReport(textWriter, $"{context}: install", $"add package {packageId} --version {packageVersion}", projectDirectory))
            {
                return false;
            }

            // The source calls into the package, so this only succeeds if the package really surfaced a
            // usable reference for this target framework. --disable-build-servers keeps the compiler
            // server from holding the scratch directory's assemblies open, which would block the delete
            // at the start of the next run.
            return RunDotnetAndReport(textWriter, $"{context}: build", "build --disable-build-servers", projectDirectory);
        }

        private static bool RunDotnetAndReport(TextWriter textWriter, string description, string arguments, string workingDirectory)
        {
            var result = ProcessUtil.Run("dotnet", arguments, workingDirectory);
            textWriter.WriteLine($"{description}: 'dotnet {arguments}' exited with code {result.ExitCode}");
            textWriter.WriteLine(result.Output);

            return result.Succeeded;
        }

        /// <summary>
        /// Creates the scratch directory the generated projects build in.
        /// </summary>
        private void PrepareScratchDirectory(string scratchDirectory, string packagesDirectory, string globalPackagesDirectory)
        {
            if (Directory.Exists(scratchDirectory))
            {
                Directory.Delete(scratchDirectory, recursive: true);
            }

            Directory.CreateDirectory(scratchDirectory);

            // RestorePackagesPath outranks both the NUGET_PACKAGES environment variable the CI job sets
            // and the globalPackagesFolder in the generated NuGet.config, so it is what actually pins
            // restore to the throwaway folder.
            File.WriteAllText(
                Path.Combine(scratchDirectory, "Directory.Build.props"),
                    $"""
                    <Project>
                        <PropertyGroup>
                            <RestorePackagesPath>{globalPackagesDirectory}</RestorePackagesPath>
                        </PropertyGroup>
                    </Project>
                    """,
                    SharedUtil.Encoding);

            File.WriteAllText(Path.Combine(scratchDirectory, "NuGet.config"), GenerateNuGetConfig(packagesDirectory, globalPackagesDirectory), SharedUtil.Encoding);
        }

        /// <summary>
        /// Builds a NuGet.config exposing the just built packages alongside the nuget.org mirror.
        /// The local feed supplies our packages and their Roslyn dependencies; the mirror supplies
        /// third party dependencies such as Microsoft.Build.Framework.
        /// </summary>
        private static string GenerateNuGetConfig(string packagesDirectory, string globalPackagesDirectory)
        {
            var document = new XDocument(
                new XElement("configuration",
                    // A dedicated packages folder, wiped with the scratch directory, keeps this honest.
                    // Sharing the machine's global folder would let an already extracted package of the
                    // same version silently stand in for the one we just built.
                    new XElement("config",
                        new XElement("add", new XAttribute("key", "globalPackagesFolder"), new XAttribute("value", globalPackagesDirectory))),
                    new XElement("packageSources",
                        new XElement("clear"),
                        new XElement("add", new XAttribute("key", "package-install-validation-local"), new XAttribute("value", packagesDirectory)),
                        new XElement("add",
                            new XAttribute("key", "dotnet-public"),
                            new XAttribute("value", "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json"))),
                    new XElement("packageSourceMapping",
                        new XElement("clear"),
                        new XElement("packageSource",
                            new XAttribute("key", "package-install-validation-local"),
                            GetLocalPackageMappings(packagesDirectory)),
                        new XElement("packageSource",
                            new XAttribute("key", "dotnet-public"),
                            new XElement("package", new XAttribute("pattern", "*")))),
                    new XElement("disabledPackageSources",
                        new XElement("clear"))));

            return document.ToString();
        }

        private static IEnumerable<XElement> GetLocalPackageMappings(string packagesDirectory)
        {
            foreach (var packagePath in Directory.EnumerateFiles(packagesDirectory, "*.nupkg"))
            {
                var packageFileName = Path.GetFileName(packagePath);
                if (!SharedUtil.TryGetNuGetPackageId(packagePath, out var packageId))
                {
                    throw new Exception($"Unexpected package file name '{packageFileName}'");
                }

                // The product packages intentionally depend on an older published analyzer package,
                // not the analyzer package produced by the current build.
                if (packageId == "Microsoft.CodeAnalysis.Analyzers")
                {
                    continue;
                }

                yield return new XElement("package", new XAttribute("pattern", packageId));
            }
        }

        private static string GenerateProjectFile(string targetFramework) =>
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>{targetFramework}</TargetFramework>
                    <OutputType>Library</OutputType>
                </PropertyGroup>
            </Project>
            """;

        /// <summary>
        /// The target frameworks a customer could consume these packages from, paired with a name safe
        /// to use in a directory. The .NET Core case is spelled as a property the SDK defines so it
        /// always matches whichever SDK is running rather than needing to be updated here.
        /// </summary>
        private static readonly (string Name, string TargetFramework)[] s_targetFrameworks = new[]
        {
            ("net472", "net472"),
            ("netcore", "net$(BundledNETCoreAppTargetFrameworkVersion)"),
        };

    }
}
