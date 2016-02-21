// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddImport;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Packaging;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddUsing
{
    using FixProviderData = Tuple<IPackageInstallerService, IPackageSearchService>;

    public partial class AddUsingTests
    {
        const string NugetOrgSource = "nuget.org";

        public class NuGet : AddUsingTests
        {
            private static readonly ImmutableArray<string> NugetPackageSources =
                ImmutableArray.Create(NugetOrgSource);

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

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public async Task TestSearchPackageSingleName()
            {
                // Make a loose mock for the installer service.  We don't care what this test
                // calls on it.
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);

                var packageServiceMock = new Mock<IPackageSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithType(
                    NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

                await TestAsync(
@"
class C
{
    [|NuGetType|] n;
}",
@"
using NuGetNamespace;

class C
{
    NuGetType n;
}", systemSpecialCase: false, fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public async Task TestSearchPackageMultipleNames()
            {
                // Make a loose mock for the installer service.  We don't care what this test
                // calls on it.
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);

                var packageServiceMock = new Mock<IPackageSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithType(
                    NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

                await TestAsync(
@"
class C
{
    [|NuGetType|] n;
}",
@"
using NS1.NS2;

class C
{
    NuGetType n;
}", systemSpecialCase: false, fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public async Task TestMissingIfPackageAlreadyInstalled()
            {
                // Make a loose mock for the installer service.  We don't care what this test
                // calls on it.
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.IsInstalled(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), "NuGetPackage"))
                    .Returns(true);

                var packageServiceMock = new Mock<IPackageSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithType(
                    NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

                await TestMissingAsync(
@"
class C
{
    [|NuGetType|] n;
}", fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public async Task TestOptionsOffered()
            {
                // Make a loose mock for the installer service.  We don't care what this test
                // calls on it.
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
                    .Returns(new[] { "1.0", "2.0" });

                var packageServiceMock = new Mock<IPackageSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithType(
                    NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")));

                var data = new FixProviderData(installerServiceMock.Object, packageServiceMock.Object);
                await TestSmartTagTextAsync(
@"
class C
{
    [|NuGetType|] n;
}",
"Use local version '1.0'",
index: 0,
fixProviderData: data);

                await TestSmartTagTextAsync(
@"
class C
{
    [|NuGetType|] n;
}",
"Use local version '2.0'",
index: 1,
fixProviderData: data);

                await TestSmartTagTextAsync(
@"
class C
{
    [|NuGetType|] n;
}",
"Find and install latest version",
index: 2,
fixProviderData: data);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public async Task TestInstallGetsCalledNoVersion()
            {
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.TryInstallPackage(
                    It.IsAny<Workspace>(), It.IsAny<DocumentId>(), "NuGetPackage", /*versionOpt*/ null, It.IsAny<CancellationToken>()));

                var packageServiceMock = new Mock<IPackageSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithType(
                    NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

                await TestAsync(
@"
class C
{
    [|NuGetType|] n;
}",
@"
using NuGetNamespace;

class C
{
    NuGetType n;
}", systemSpecialCase: false, fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
                installerServiceMock.Verify();
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public async Task TestInstallGetsCalledWithVersion()
            {
                var installerServiceMock = new Mock<IPackageInstallerService>(MockBehavior.Loose);
                installerServiceMock.SetupGet(i => i.PackageSources).Returns(NugetPackageSources);
                installerServiceMock.Setup(s => s.GetInstalledVersions("NuGetPackage"))
                    .Returns(new[] { "1.0" });
                installerServiceMock.Setup(s => s.TryInstallPackage(
                    It.IsAny<Workspace>(), It.IsAny<DocumentId>(), "NuGetPackage", "1.0", It.IsAny<CancellationToken>()));

                var packageServiceMock = new Mock<IPackageSearchService>();
                packageServiceMock.Setup(s => s.FindPackagesWithType(NugetOrgSource, "NuGetType", 0, It.IsAny<CancellationToken>()))
                    .Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")));

                await TestAsync(
@"
class C
{
    [|NuGetType|] n;
}",
@"
using NuGetNamespace;

class C
{
    NuGetType n;
}", systemSpecialCase: false, fixProviderData: new FixProviderData(installerServiceMock.Object, packageServiceMock.Object));
                installerServiceMock.Verify();
            }

            private IEnumerable<PackageWithTypeResult> CreateSearchResult(
                string packageName, string typeName, IReadOnlyList<string> containingNamespaceNames)
            {
                return CreateSearchResult(new PackageWithTypeResult(
                    isDesktopFramework: false, packageName: packageName, assemblyName: packageName, typeName: typeName, containingNamespaceNames: containingNamespaceNames));
            }

            private IEnumerable<PackageWithTypeResult> CreateSearchResult(params PackageWithTypeResult[] results) => results;

            private IReadOnlyList<string> CreateNameParts(params string[] parts) => parts;
        }
    }
}
