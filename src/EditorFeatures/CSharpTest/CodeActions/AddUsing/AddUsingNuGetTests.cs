// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Test.Utilities;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing;

using FixProviderData = Tuple<IPackageInstallerService, ISymbolSearchService>;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
public sealed class AddUsingNuGetTests : AbstractAddUsingTests
{
    private static readonly ImmutableArray<PackageSource> NugetPackageSources =
        [new PackageSource(PackageSourceHelper.NugetOrgSourceName, "http://nuget.org/")];

    protected override void InitializeWorkspace(EditorTestWorkspace workspace, TestParameters parameters)
    {
        workspace.GlobalOptions.SetGlobalOption(SymbolSearchOptionsStorage.SearchNuGetPackages, LanguageNames.CSharp, true);
        workspace.GlobalOptions.SetGlobalOption(SymbolSearchOptionsStorage.SearchReferenceAssemblies, LanguageNames.CSharp, true);
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(
        Workspace workspace, TestParameters parameters)
    {
        var data = (FixProviderData)parameters.fixProviderData;
        return (null, new CSharpAddImportCodeFixProvider(data.Item1, data.Item2));
    }

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    [Fact]
    public async Task TestSearchPackageCustomFeedName()
    {
        var packageSources = ImmutableArray.Create(new PackageSource("My Custom Nuget Feed", "http://nuget.org/"));

        var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
        installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
        installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
        installerServiceMock.Setup(i => i.GetInstalledVersions("NuGetPackage")).Returns([]);
        installerServiceMock.Setup(i => i.TryGetPackageSources()).Returns(packageSources);
        installerServiceMock.Setup(s => s.TryInstallPackageAsync(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IProgress<CodeAnalysisProgress>>(), It.IsAny<CancellationToken>()))
                            .Returns(Task.FromResult(true));

        var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
        packageServiceMock.Setup(s => s.FindReferenceAssembliesAsync(new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async () => ImmutableArray<ReferenceAssemblyResult>.Empty);
        packageServiceMock.Setup(s => s.FindPackagesAsync(
            PackageSourceHelper.NugetOrgSourceName, new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """,
            """
            using NuGetNamespace;

            class C
            {
                NuGetType n;
            }
            """, new TestParameters(fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object)));
    }

    [Fact]
    public async Task TestSearchPackageFakeNugetFeed()
    {
        var packageSources = ImmutableArray.Create(new PackageSource("nuget.org", "http://fakenuget.org/"));

        var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
        installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
        installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
        installerServiceMock.Setup(i => i.GetInstalledVersions("NuGetPackage")).Returns([]);
        installerServiceMock.Setup(i => i.TryGetPackageSources()).Returns(packageSources);
        installerServiceMock.Setup(s => s.TryInstallPackageAsync(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IProgress<CodeAnalysisProgress>>(), It.IsAny<CancellationToken>()))
                            .Returns(Task.FromResult(true));

        var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
        packageServiceMock.Setup(s => s.FindReferenceAssembliesAsync(new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async () => ImmutableArray<ReferenceAssemblyResult>.Empty);
        packageServiceMock.Setup(s => s.FindPackagesAsync(
            "nuget.org", new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """,
            """
            using NuGetNamespace;

            class C
            {
                NuGetType n;
            }
            """, new TestParameters(fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object)));
    }

    [Fact]
    public async Task TestSearchPackageSingleName()
    {
        var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
        installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
        installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
        installerServiceMock.Setup(i => i.GetInstalledVersions("NuGetPackage")).Returns([]);
        installerServiceMock.Setup(i => i.TryGetPackageSources()).Returns(NugetPackageSources);
        installerServiceMock.Setup(s => s.TryInstallPackageAsync(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IProgress<CodeAnalysisProgress>>(), It.IsAny<CancellationToken>()))
                            .Returns(SpecializedTasks.True);

        var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
        packageServiceMock.Setup(s => s.FindReferenceAssembliesAsync(new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async () => ImmutableArray<ReferenceAssemblyResult>.Empty);
        packageServiceMock.Setup(s => s.FindPackagesAsync(
            PackageSourceHelper.NugetOrgSourceName, new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """,
            """
            using NuGetNamespace;

            class C
            {
                NuGetType n;
            }
            """, new TestParameters(fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object)));
    }

    [Fact]
    public async Task TestSearchPackageMultipleNames()
    {
        var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
        installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
        installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
        installerServiceMock.Setup(i => i.GetInstalledVersions("NuGetPackage")).Returns([]);
        installerServiceMock.Setup(i => i.TryGetPackageSources()).Returns(NugetPackageSources);
        installerServiceMock.Setup(s => s.TryInstallPackageAsync(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IProgress<CodeAnalysisProgress>>(), It.IsAny<CancellationToken>()))
                            .Returns(SpecializedTasks.True);

        var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
        packageServiceMock.Setup(s => s.FindReferenceAssembliesAsync(new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async () => ImmutableArray<ReferenceAssemblyResult>.Empty);
        packageServiceMock.Setup(s => s.FindPackagesAsync(
            PackageSourceHelper.NugetOrgSourceName, new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """,
            """
            using NS1.NS2;

            class C
            {
                NuGetType n;
            }
            """, new TestParameters(fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object)));
    }

    [Fact]
    public async Task TestMissingIfPackageAlreadyInstalled()
    {
        var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
        installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
        installerServiceMock.Setup(i => i.TryGetPackageSources()).Returns(NugetPackageSources);
        installerServiceMock.Setup(s => s.IsInstalled(It.IsAny<ProjectId>(), "NuGetPackage"))
            .Returns(true);

        var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
        packageServiceMock.Setup(s => s.FindReferenceAssembliesAsync(new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async () => ImmutableArray<ReferenceAssemblyResult>.Empty);
        packageServiceMock.Setup(s => s.FindPackagesAsync(
            PackageSourceHelper.NugetOrgSourceName, new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """, new TestParameters(fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object)));
    }

    [Fact]
    public async Task TestOptionsOffered()
    {
        var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
        installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
        installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
        installerServiceMock.Setup(i => i.GetProjectsWithInstalledPackage(It.IsAny<Solution>(), "NuGetPackage", "1.0")).Returns([]);
        installerServiceMock.Setup(i => i.GetProjectsWithInstalledPackage(It.IsAny<Solution>(), "NuGetPackage", "2.0")).Returns([]);
        installerServiceMock.Setup(i => i.TryGetPackageSources()).Returns(NugetPackageSources);
        installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
            .Returns(["1.0", "2.0"]);

        var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
        packageServiceMock.Setup(s => s.FindReferenceAssembliesAsync(new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async () => ImmutableArray<ReferenceAssemblyResult>.Empty);
        packageServiceMock.Setup(s => s.FindPackagesAsync(
            PackageSourceHelper.NugetOrgSourceName, new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

        var data = new FixProviderData(installerServiceMock.Object, packageServiceMock.Object);
        await TestSmartTagTextAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """,
            string.Format(FeaturesResources.Use_local_version_0, "1.0"),
            parameters: new TestParameters(fixProviderData: data));

        await TestSmartTagTextAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """,
            string.Format(FeaturesResources.Use_local_version_0, "2.0"),
            parameters: new TestParameters(index: 1, fixProviderData: data));

        await TestSmartTagTextAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """,
            FeaturesResources.Find_and_install_latest_version,
            parameters: new TestParameters(index: 2, fixProviderData: data));
    }

    [Fact]
    public async Task TestInstallGetsCalledNoVersion()
    {
        var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
        installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
        installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
        installerServiceMock.Setup(i => i.GetInstalledVersions("NuGetPackage")).Returns([]);
        installerServiceMock.Setup(i => i.TryGetPackageSources()).Returns(NugetPackageSources);
        installerServiceMock.Setup(s => s.TryInstallPackageAsync(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", /*versionOpt*/ null, It.IsAny<bool>(), It.IsAny<IProgress<CodeAnalysisProgress>>(), It.IsAny<CancellationToken>()))
                            .Returns(SpecializedTasks.True);

        var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
        packageServiceMock.Setup(s => s.FindReferenceAssembliesAsync(new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async () => ImmutableArray<ReferenceAssemblyResult>.Empty);
        packageServiceMock.Setup(s => s.FindPackagesAsync(
            PackageSourceHelper.NugetOrgSourceName, new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """,
            """
            using NuGetNamespace;

            class C
            {
                NuGetType n;
            }
            """, new TestParameters(fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object)));
        installerServiceMock.Verify();
    }

    [Fact]
    public async Task TestInstallGetsCalledWithVersion()
    {
        var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
        installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
        installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
        installerServiceMock.Setup(i => i.GetProjectsWithInstalledPackage(It.IsAny<Solution>(), "NuGetPackage", "1.0")).Returns([]);
        installerServiceMock.Setup(i => i.TryGetPackageSources()).Returns(NugetPackageSources);
        installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
            .Returns(["1.0"]);
        installerServiceMock.Setup(s => s.TryInstallPackageAsync(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", "1.0", It.IsAny<bool>(), It.IsAny<IProgress<CodeAnalysisProgress>>(), It.IsAny<CancellationToken>()))
                            .Returns(SpecializedTasks.True);

        var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
        packageServiceMock.Setup(s => s.FindReferenceAssembliesAsync(new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async () => ImmutableArray<ReferenceAssemblyResult>.Empty);
        packageServiceMock.Setup(s => s.FindPackagesAsync(PackageSourceHelper.NugetOrgSourceName, new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """,
            """
            using NuGetNamespace;

            class C
            {
                NuGetType n;
            }
            """, new TestParameters(fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object)));
        installerServiceMock.Verify();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/14516")]
    public async Task TestFailedInstallRollsBackFile()
    {
        var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
        installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
        installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
        installerServiceMock.Setup(i => i.GetProjectsWithInstalledPackage(It.IsAny<Solution>(), "NuGetPackage", "1.0")).Returns([]);
        installerServiceMock.Setup(i => i.TryGetPackageSources()).Returns(NugetPackageSources);
        installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
            .Returns(["1.0"]);
        installerServiceMock.Setup(s => s.TryInstallPackageAsync(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", "1.0", It.IsAny<bool>(), It.IsAny<IProgress<CodeAnalysisProgress>>(), It.IsAny<CancellationToken>()))
                            .Returns(SpecializedTasks.False);

        var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
        packageServiceMock.Setup(s => s.FindReferenceAssembliesAsync(new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async () => ImmutableArray<ReferenceAssemblyResult>.Empty);
        packageServiceMock.Setup(s => s.FindPackagesAsync(PackageSourceHelper.NugetOrgSourceName, new TypeQuery("NuGetType", 0), It.IsAny<NamespaceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|NuGetType|] n;
            }
            """,
            """
            class C
            {
                NuGetType n;
            }
            """, new TestParameters(fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object)));
        installerServiceMock.Verify();
    }

    private static ValueTask<ImmutableArray<PackageResult>> CreateSearchResult(
        string packageName, string typeName, ImmutableArray<string> containingNamespaceNames)
    {
        return CreateSearchResult(new PackageResult(
            packageName: packageName, rank: 0, typeName: typeName,
            version: null, containingNamespaceNames: containingNamespaceNames));
    }

    private static async ValueTask<ImmutableArray<PackageResult>> CreateSearchResult(params PackageResult[] results)
        => [.. results];

    private static ImmutableArray<string> CreateNameParts(params string[] parts)
        => [.. parts];
}
