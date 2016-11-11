// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddUsing
{
    using FixProviderData = Tuple<IPackageInstallerService, ISymbolSearchService>;

    public partial class AddUsingTests
    {
        const string NugetOrgSource = "nuget.org";

        public class NuGet : AddUsingTests
        {
            private static readonly ImmutableArray<PackageSource> NugetPackageSources =
                ImmutableArray.Create(new PackageSource(NugetOrgSource, "http://nuget.org/"));

            protected override async Task<TestWorkspace> CreateWorkspaceFromFileAsync(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions)
            {
                var workspace = await base.CreateWorkspaceFromFileAsync(definition, parseOptions, compilationOptions);
                workspace.Options = workspace.Options
                    .WithChangedOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages, LanguageNames.CSharp, true)
                    .WithChangedOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, LanguageNames.CSharp, true);
                return workspace;
            }

            internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(
                Workspace workspace, object fixProviderData)
            {
                var data = (FixProviderData)fixProviderData;
                return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(
                        null,
                        new CSharpAddImportCodeFixProvider(data.Item1, data.Item2));
            }

            protected override IList<CodeAction> MassageActions(IList<CodeAction> actions)
            {
                return FlattenActions(actions);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
            public async Task TestSearchPackageSingleName()
            {
                // Make a loose mock for the installer service.  We don't care what this test
                // calls on it.
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.IsEnabled).Returns(true);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.TryInstallPackage(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .Returns(true);

                var packageServiceMock = new Mock<ISymbolSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(
                    NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

                await TestAsync(
@"class C
{
    [|NuGetType|] n;
}",
@"using NuGetNamespace;

class C
{
    NuGetType n;
}", systemSpecialCase: false, fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
            public async Task TestSearchPackageMultipleNames()
            {
                // Make a loose mock for the installer service.  We don't care what this test
                // calls on it.
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.IsEnabled).Returns(true);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.TryInstallPackage(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .Returns(true);

                var packageServiceMock = new Mock<ISymbolSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(
                    NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

                await TestAsync(
@"class C
{
    [|NuGetType|] n;
}",
@"using NS1.NS2;

class C
{
    NuGetType n;
}", systemSpecialCase: false, fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
            public async Task TestMissingIfPackageAlreadyInstalled()
            {
                // Make a loose mock for the installer service.  We don't care what this test
                // calls on it.
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.IsEnabled).Returns(true);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.IsInstalled(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), "NuGetPackage"))
                    .Returns(true);

                var packageServiceMock = new Mock<ISymbolSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(
                    NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

                await TestMissingAsync(
@"class C
{
    [|NuGetType|] n;
}", fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
            public async Task TestOptionsOffered()
            {
                // Make a loose mock for the installer service.  We don't care what this test
                // calls on it.
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.IsEnabled).Returns(true);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
                    .Returns(ImmutableArray.Create("1.0", "2.0"));

                var packageServiceMock = new Mock<ISymbolSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(
                    NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

                var data = new FixProviderData(installerServiceMock.Object, packageServiceMock.Object);
                await TestSmartTagTextAsync(
@"class C
{
    [|NuGetType|] n;
}",
"Use local version '1.0'",
index: 0,
fixProviderData: data);

                await TestSmartTagTextAsync(
@"class C
{
    [|NuGetType|] n;
}",
"Use local version '2.0'",
index: 1,
fixProviderData: data);

                await TestSmartTagTextAsync(
@"class C
{
    [|NuGetType|] n;
}",
"Find and install latest version",
index: 2,
fixProviderData: data);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
            public async Task TestInstallGetsCalledNoVersion()
            {
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.IsEnabled).Returns(true);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.TryInstallPackage(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", /*versionOpt*/ null, It.IsAny<CancellationToken>()))
                                    .Returns(true);

                var packageServiceMock = new Mock<ISymbolSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(
                    NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

                await TestAsync(
@"class C
{
    [|NuGetType|] n;
}",
@"using NuGetNamespace;

class C
{
    NuGetType n;
}", systemSpecialCase: false, fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
                installerServiceMock.Verify();
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
            public async Task TestInstallGetsCalledWithVersion()
            {
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.IsEnabled).Returns(true);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
                    .Returns(ImmutableArray.Create("1.0"));
                installerServiceMock.Setup(s => s.TryInstallPackage(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", "1.0", It.IsAny<CancellationToken>()))
                                    .Returns(true);

                var packageServiceMock = new Mock<ISymbolSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

                await TestAsync(
@"class C
{
    [|NuGetType|] n;
}",
@"using NuGetNamespace;

class C
{
    NuGetType n;
}", systemSpecialCase: false, fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
                installerServiceMock.Verify();
            }

            [WorkItem(14516, "https://github.com/dotnet/roslyn/pull/14516")]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
            public async Task TestFailedInstallRollsBackFile()
            {
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.IsEnabled).Returns(true);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
                    .Returns(ImmutableArray.Create("1.0"));
                installerServiceMock.Setup(s => s.TryInstallPackage(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", "1.0", It.IsAny<CancellationToken>()))
                                    .Returns(false);

                var packageServiceMock = new Mock<ISymbolSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

                await TestAsync(
@"class C
{
    [|NuGetType|] n;
}",
@"class C
{
    NuGetType n;
}", systemSpecialCase: false, fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
                installerServiceMock.Verify();
            }

            private Task<ImmutableArray<PackageWithTypeResult>> CreateSearchResult(
                string packageName, string typeName, IReadOnlyList<string> containingNamespaceNames)
            {
                return CreateSearchResult(new PackageWithTypeResult(
                    packageName: packageName, typeName: typeName, version: null,
                    rank: 0, containingNamespaceNames: containingNamespaceNames));
            }

            private Task<ImmutableArray<PackageWithTypeResult>> CreateSearchResult(params PackageWithTypeResult[] results)
                => Task.FromResult(ImmutableArray.Create(results));

            private IReadOnlyList<string> CreateNameParts(params string[] parts) => parts;
        }
    }
}