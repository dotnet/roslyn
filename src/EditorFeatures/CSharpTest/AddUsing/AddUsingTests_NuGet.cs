// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Test.Utilities;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing
{
    using FixProviderData = Tuple<IPackageInstallerService, ISymbolSearchService>;

    public class AddUsingNuGetTests : AbstractAddUsingTests
    {
        const string NugetOrgSource = "nuget.org";

        private static readonly ImmutableArray<PackageSource> NugetPackageSources =
            ImmutableArray.Create(new PackageSource(NugetOrgSource, "http://nuget.org/"));

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
        {
            var workspace = base.CreateWorkspaceFromFile(initialMarkup, parameters);
            workspace.Options = workspace.Options
                .WithChangedOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages, LanguageNames.CSharp, true)
                .WithChangedOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, LanguageNames.CSharp, true);
            return workspace;
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(
            Workspace workspace, TestParameters parameters)
        {
            var data = (FixProviderData)parameters.fixProviderData;
            return (null, new CSharpAddImportCodeFixProvider(data.Item1, data.Item2));
        }

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestSearchPackageSingleName()
        {
            var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
            installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
            installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
            installerServiceMock.Setup(i => i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray<string>.Empty);
            installerServiceMock.Setup(i => i.GetPackageSources()).Returns(NugetPackageSources);
            installerServiceMock.Setup(s => s.TryInstallPackage(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                                .Returns(true);

            var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
            packageServiceMock.Setup(s => s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IList<ReferenceAssemblyWithTypeResult>>(new List<ReferenceAssemblyWithTypeResult>()));
            packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(
                NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

            await TestInRegularAndScriptAsync(
@"class C
{
    [|NuGetType|] n;
}",
@"using NuGetNamespace;

class C
{
    NuGetType n;
}", fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestSearchPackageMultipleNames()
        {
            var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
            installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
            installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
            installerServiceMock.Setup(i => i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray<string>.Empty);
            installerServiceMock.Setup(i => i.GetPackageSources()).Returns(NugetPackageSources);
            installerServiceMock.Setup(s => s.TryInstallPackage(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                                .Returns(true);

            var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
            packageServiceMock.Setup(s => s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IList<ReferenceAssemblyWithTypeResult>>(new List<ReferenceAssemblyWithTypeResult>()));
            packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(
                NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

            await TestInRegularAndScriptAsync(
@"class C
{
    [|NuGetType|] n;
}",
@"using NS1.NS2;

class C
{
    NuGetType n;
}", fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestMissingIfPackageAlreadyInstalled()
        {
            var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
            installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
            installerServiceMock.Setup(i => i.GetPackageSources()).Returns(NugetPackageSources);
            installerServiceMock.Setup(s => s.IsInstalled(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), "NuGetPackage"))
                .Returns(true);

            var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
            packageServiceMock.Setup(s => s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IList<ReferenceAssemblyWithTypeResult>>(new List<ReferenceAssemblyWithTypeResult>()));
            packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(
                NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

            await TestMissingInRegularAndScriptAsync(
@"class C
{
    [|NuGetType|] n;
}", new TestParameters(fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestOptionsOffered()
        {
            var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
            installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
            installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
            installerServiceMock.Setup(i => i.GetProjectsWithInstalledPackage(It.IsAny<Solution>(), "NuGetPackage", "1.0")).Returns(Enumerable.Empty<Project>());
            installerServiceMock.Setup(i => i.GetProjectsWithInstalledPackage(It.IsAny<Solution>(), "NuGetPackage", "2.0")).Returns(Enumerable.Empty<Project>());
            installerServiceMock.Setup(i => i.GetPackageSources()).Returns(NugetPackageSources);
            installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
                .Returns(ImmutableArray.Create("1.0", "2.0"));

            var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
            packageServiceMock.Setup(s => s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IList<ReferenceAssemblyWithTypeResult>>(new List<ReferenceAssemblyWithTypeResult>()));
            packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(
                NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

            var data = new FixProviderData(installerServiceMock.Object, packageServiceMock.Object);
            await TestSmartTagTextAsync(
@"class C
{
    [|NuGetType|] n;
}",
string.Format(FeaturesResources.Use_local_version_0, "1.0"),
parameters: new TestParameters(fixProviderData: data));

            await TestSmartTagTextAsync(
@"class C
{
    [|NuGetType|] n;
}",
string.Format(FeaturesResources.Use_local_version_0, "2.0"),
parameters: new TestParameters(index: 1, fixProviderData: data));

            await TestSmartTagTextAsync(
@"class C
{
    [|NuGetType|] n;
}",
FeaturesResources.Find_and_install_latest_version,
parameters: new TestParameters(index: 2, fixProviderData: data));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestInstallGetsCalledNoVersion()
        {
            var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
            installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
            installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
            installerServiceMock.Setup(i => i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray<string>.Empty);
            installerServiceMock.Setup(i => i.GetPackageSources()).Returns(NugetPackageSources);
            installerServiceMock.Setup(s => s.TryInstallPackage(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", /*versionOpt*/ null, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                                .Returns(true);

            var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
            packageServiceMock.Setup(s => s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IList<ReferenceAssemblyWithTypeResult>>(new List<ReferenceAssemblyWithTypeResult>()));
            packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(
                NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

            await TestInRegularAndScriptAsync(
@"class C
{
    [|NuGetType|] n;
}",
@"using NuGetNamespace;

class C
{
    NuGetType n;
}", fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
            installerServiceMock.Verify();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestInstallGetsCalledWithVersion()
        {
            var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
            installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
            installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
            installerServiceMock.Setup(i => i.GetProjectsWithInstalledPackage(It.IsAny<Solution>(), "NuGetPackage", "1.0")).Returns(Enumerable.Empty<Project>());
            installerServiceMock.Setup(i => i.GetPackageSources()).Returns(NugetPackageSources);
            installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
                .Returns(ImmutableArray.Create("1.0"));
            installerServiceMock.Setup(s => s.TryInstallPackage(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", "1.0", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                                .Returns(true);

            var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
            packageServiceMock.Setup(s => s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IList<ReferenceAssemblyWithTypeResult>>(new List<ReferenceAssemblyWithTypeResult>()));
            packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

            await TestInRegularAndScriptAsync(
@"class C
{
    [|NuGetType|] n;
}",
@"using NuGetNamespace;

class C
{
    NuGetType n;
}", fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
            installerServiceMock.Verify();
        }

        [WorkItem(14516, "https://github.com/dotnet/roslyn/pull/14516")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestFailedInstallRollsBackFile()
        {
            var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Strict);
            installerServiceMock.Setup(i => i.IsEnabled(It.IsAny<ProjectId>())).Returns(true);
            installerServiceMock.Setup(i => i.IsInstalled(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), "NuGetPackage")).Returns(false);
            installerServiceMock.Setup(i => i.GetProjectsWithInstalledPackage(It.IsAny<Solution>(), "NuGetPackage", "1.0")).Returns(Enumerable.Empty<Project>());
            installerServiceMock.Setup(i => i.GetPackageSources()).Returns(NugetPackageSources);
            installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
                .Returns(ImmutableArray.Create("1.0"));
            installerServiceMock.Setup(s => s.TryInstallPackage(It.IsAny<Workspace>(), It.IsAny<DocumentId>(), It.IsAny<string>(), "NuGetPackage", "1.0", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                                .Returns(false);

            var packageServiceMock = new Mock<ISymbolSearchService>(MockBehavior.Strict);
            packageServiceMock.Setup(s => s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IList<ReferenceAssemblyWithTypeResult>>(new List<ReferenceAssemblyWithTypeResult>()));
            packageServiceMock.Setup(s => s.FindPackagesWithTypeAsync(NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

            await TestInRegularAndScriptAsync(
@"class C
{
    [|NuGetType|] n;
}",
@"class C
{
    NuGetType n;
}", fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
            installerServiceMock.Verify();
        }

        private Task<IList<PackageWithTypeResult>> CreateSearchResult(
            string packageName, string typeName, ImmutableArray<string> containingNamespaceNames)
        {
            return CreateSearchResult(new PackageWithTypeResult(
                packageName: packageName, typeName: typeName, version: null,
                rank: 0, containingNamespaceNames: containingNamespaceNames));
        }

        private Task<IList<PackageWithTypeResult>> CreateSearchResult(params PackageWithTypeResult[] results)
            => Task.FromResult<IList<PackageWithTypeResult>>(ImmutableArray.Create(results));

        private ImmutableArray<string> CreateNameParts(params string[] parts) => parts.ToImmutableArray();
    }
}
