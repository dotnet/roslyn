' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Packaging
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddImport
Imports Moq
Imports ProviderData = System.Tuple(Of Microsoft.CodeAnalysis.Packaging.IPackageInstallerService, Microsoft.CodeAnalysis.Packaging.IPackageSearchService)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.AddImport
    Partial Public Class AddImportTests
        Const NugetOrgSource = "nuget.org"

        Public Class NuGet
            Inherits AddImportTests

            Private Shared ReadOnly NugetPackageSources As ImmutableArray(Of PackageSource) =
                ImmutableArray.Create(New PackageSource(NugetOrgSource, "http://nuget.org"))

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, fixProviderData As Object) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Dim data = DirectCast(fixProviderData, ProviderData)
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    Nothing,
                    New VisualBasicAddImportCodeFixProvider(data.Item1, data.Item2))
            End Function

            Protected Overrides Function MassageActions(actions As IList(Of CodeAction)) As IList(Of CodeAction)
                Return FlattenActions(actions)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestSearchPackageSingleName() As Task
                ' Make a loose mock for the installer service.  We don't care what this test
                ' calls on it.
                Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Loose)
                installerServiceMock.SetupGet(Function(i) i.IsEnabled).Returns(True)
                installerServiceMock.SetupGet(Function(i) i.PackageSources).Returns(NugetPackageSources)

                Dim packageServiceMock = New Mock(Of IPackageSearchService)()
                packageServiceMock.Setup(Function(s) s.FindPackagesWithType(NugetOrgSource, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                    Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")))

                Await TestAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
"
Imports NuGetNamespace

Class C
    Dim n As NuGetType
End Class", fixProviderData:=New ProviderData(installerServiceMock.Object, packageServiceMock.Object))
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestSearchPackageMultipleNames() As Task
                ' Make a loose mock for the installer service.  We don't care what this test
                ' calls on it.
                Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Loose)
                installerServiceMock.SetupGet(Function(i) i.IsEnabled).Returns(True)
                installerServiceMock.SetupGet(Function(i) i.PackageSources).Returns(NugetPackageSources)

                Dim packageServiceMock = New Mock(Of IPackageSearchService)()
                packageServiceMock.Setup(Function(s) s.FindPackagesWithType(NugetOrgSource, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                    Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

                Await TestAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
"
Imports NS1.NS2

Class C
    Dim n As NuGetType
End Class", fixProviderData:=New ProviderData(installerServiceMock.Object, packageServiceMock.Object))
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestMissingIfPackageAlreadyInstalled() As Task
                ' Make a loose mock for the installer service.  We don't care what this test
                ' calls on it.
                Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Loose)
                installerServiceMock.SetupGet(Function(i) i.IsEnabled).Returns(True)
                installerServiceMock.SetupGet(Function(i) i.PackageSources).Returns(NugetPackageSources)
                installerServiceMock.Setup(Function(s) s.IsInstalled(It.IsAny(Of Workspace)(), It.IsAny(Of ProjectId)(), "NuGetPackage")).
                    Returns(True)

                Dim packageServiceMock = New Mock(Of IPackageSearchService)()
                packageServiceMock.Setup(Function(s) s.FindPackagesWithType(NugetOrgSource, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                    Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

                Await TestMissingAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
fixProviderData:=New ProviderData(installerServiceMock.Object, packageServiceMock.Object))
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestOptionsOffered() As Task
                ' Make a loose mock for the installer service.  We don't care what this test
                ' calls on it.
                Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Loose)
                installerServiceMock.SetupGet(Function(i) i.IsEnabled).Returns(True)
                installerServiceMock.SetupGet(Function(i) i.PackageSources).Returns(NugetPackageSources)
                installerServiceMock.Setup(Function(s) s.GetInstalledVersions("NuGetPackage")).
                    Returns({"1.0", "2.0"})

                Dim packageServiceMock = New Mock(Of IPackageSearchService)()
                packageServiceMock.Setup(Function(s) s.FindPackagesWithType(NugetOrgSource, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                    Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

                Dim data = New ProviderData(installerServiceMock.Object, packageServiceMock.Object)
                Await TestSmartTagTextAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
"Use local version '1.0'",
index:=0,
fixProviderData:=data)

                Await TestSmartTagTextAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
"Use local version '2.0'",
index:=1,
fixProviderData:=data)

                Await TestSmartTagTextAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
"Find and install latest version",
index:=2,
fixProviderData:=data)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestInstallGetsCalledNoVersion() As Task
                Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Loose)
                installerServiceMock.SetupGet(Function(i) i.IsEnabled).Returns(True)
                installerServiceMock.SetupGet(Function(i) i.PackageSources).Returns(NugetPackageSources)
                installerServiceMock.Setup(Function(s) s.TryInstallPackage(
                    It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", Nothing, It.IsAny(Of CancellationToken)))

                Dim packageServiceMock = New Mock(Of IPackageSearchService)()
                packageServiceMock.Setup(Function(s) s.FindPackagesWithType(NugetOrgSource, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                    Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")))

                Await TestAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
"
Imports NuGetNamespace

Class C
    Dim n As NuGetType
End Class", fixProviderData:=New ProviderData(installerServiceMock.Object, packageServiceMock.Object))
                installerServiceMock.Verify()
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestInstallGetsCalledWithVersion() As Task
                Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Loose)
                installerServiceMock.SetupGet(Function(i) i.IsEnabled).Returns(True)
                installerServiceMock.SetupGet(Function(i) i.PackageSources).Returns(NugetPackageSources)
                installerServiceMock.Setup(Function(s) s.GetInstalledVersions("NuGetPackage")).
                    Returns({"1.0"})
                installerServiceMock.Setup(Function(s) s.TryInstallPackage(
                    It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", "1.0", It.IsAny(Of CancellationToken)))

                Dim packageServiceMock = New Mock(Of IPackageSearchService)()
                packageServiceMock.Setup(Function(s) s.FindPackagesWithType(NugetOrgSource, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                    Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")))

                Await TestAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
"
Imports NuGetNamespace

Class C
    Dim n As NuGetType
End Class", fixProviderData:=New ProviderData(installerServiceMock.Object, packageServiceMock.Object))
                installerServiceMock.Verify()
            End Function

            Private Function CreateSearchResult(packageName As String, typeName As String, nameParts As IReadOnlyList(Of String)) As IEnumerable(Of PackageWithTypeResult)
                Return CreateSearchResult(New PackageWithTypeResult(
                    isDesktopFramework:=False,
                    packageName:=packageName,
                    assemblyName:=packageName,
                    typeName:=typeName,
                    version:=Nothing,
                    containingNamespaceNames:=nameParts))
            End Function

            Private Function CreateSearchResult(ParamArray results As PackageWithTypeResult()) As IEnumerable(Of PackageWithTypeResult)
                Return results
            End Function

            Private Function CreateNameParts(ParamArray parts As String()) As IReadOnlyList(Of String)
                Return parts
            End Function
        End Class
    End Class
End Namespace
