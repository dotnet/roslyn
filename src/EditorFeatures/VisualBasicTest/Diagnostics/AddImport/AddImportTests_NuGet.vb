' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Packaging
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.CodeAnalysis.SymbolSearch
Imports Microsoft.CodeAnalysis.VisualBasic.AddImport
Imports Moq
Imports ProviderData = System.Tuple(Of Microsoft.CodeAnalysis.Packaging.IPackageInstallerService, Microsoft.CodeAnalysis.SymbolSearch.ISymbolSearchService)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.AddImport
    <Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
    Public Class AddImportNuGetTests
        Inherits AbstractAddImportTests

        Private Shared ReadOnly NuGetPackageSources As ImmutableArray(Of PackageSource) =
            ImmutableArray.Create(New PackageSource(PackageSourceHelper.NugetOrgSourceName, "http://nuget.org"))

        Protected Overrides Sub InitializeWorkspace(workspace As TestWorkspace, parameters As TestParameters)
            workspace.GlobalOptions.SetGlobalOption(New OptionKey(SymbolSearchOptionsStorage.SearchNuGetPackages, LanguageNames.VisualBasic), True)
            workspace.GlobalOptions.SetGlobalOption(New OptionKey(SymbolSearchOptionsStorage.SearchReferenceAssemblies, LanguageNames.VisualBasic), True)
        End Sub

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            ' This is used by inherited tests to ensure the properties of diagnostic analyzers are correct. It's not
            ' needed by the tests in this class, but can't throw an exception.
            Return (Nothing, Nothing)
        End Function

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, parameters As TestParameters) As (DiagnosticAnalyzer, CodeFixProvider)
            Dim data = DirectCast(parameters.fixProviderData, ProviderData)
            Return (Nothing, New VisualBasicAddImportCodeFixProvider(data.Item1, data.Item2))
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Fact>
        Public Async Function TestSearchPackageSingleName() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray(Of String).Empty)
            installerServiceMock.Setup(Function(i) i.TryGetPackageSources()).Returns(NuGetPackageSources)
            installerServiceMock.Setup(Function(s) s.TryInstallPackageAsync(It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", It.IsAny(Of String), It.IsAny(Of Boolean), It.IsAny(Of IProgressTracker)(), It.IsAny(Of CancellationToken))).
                                 Returns(SpecializedTasks.True)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Function() ValueTaskFactory.FromResult(ImmutableArray(Of ReferenceAssemblyWithTypeResult).Empty))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(PackageSourceHelper.NugetOrgSourceName, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(Function() CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")))

            Await TestInRegularAndScriptAsync(
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

        <Fact>
        Public Async Function TestSearchPackageMultipleNames() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray(Of String).Empty)
            installerServiceMock.Setup(Function(i) i.TryGetPackageSources()).Returns(NuGetPackageSources)
            installerServiceMock.Setup(Function(s) s.TryInstallPackageAsync(It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", It.IsAny(Of String), It.IsAny(Of Boolean), It.IsAny(Of IProgressTracker)(), It.IsAny(Of CancellationToken))).
                                 Returns(SpecializedTasks.True)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Function() ValueTaskFactory.FromResult(ImmutableArray(Of ReferenceAssemblyWithTypeResult).Empty))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(PackageSourceHelper.NugetOrgSourceName, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(Function() CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

            Await TestInRegularAndScriptAsync(
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

        <Fact>
        Public Async Function TestFailedInstallDoesNotChangeFile() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray(Of String).Empty)
            installerServiceMock.Setup(Function(i) i.TryGetPackageSources()).Returns(NuGetPackageSources)
            installerServiceMock.Setup(Function(s) s.TryInstallPackageAsync(It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", It.IsAny(Of String), It.IsAny(Of Boolean), It.IsAny(Of IProgressTracker)(), It.IsAny(Of CancellationToken))).
                                 Returns(SpecializedTasks.False)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Function() ValueTaskFactory.FromResult(ImmutableArray(Of ReferenceAssemblyWithTypeResult).Empty))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(PackageSourceHelper.NugetOrgSourceName, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(Function() CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

            Await TestInRegularAndScriptAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
"
Class C
    Dim n As NuGetType
End Class", fixProviderData:=New ProviderData(installerServiceMock.Object, packageServiceMock.Object))
        End Function

        <Fact>
        Public Async Function TestMissingIfPackageAlreadyInstalled() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.TryGetPackageSources()).Returns(NuGetPackageSources)
            installerServiceMock.Setup(Function(s) s.IsInstalled(It.IsAny(Of Workspace)(), It.IsAny(Of ProjectId)(), "NuGetPackage")).
                Returns(True)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Function() ValueTaskFactory.FromResult(ImmutableArray(Of ReferenceAssemblyWithTypeResult).Empty))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(PackageSourceHelper.NugetOrgSourceName, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(Function() CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

            Await TestMissingInRegularAndScriptAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
New TestParameters(fixProviderData:=New ProviderData(installerServiceMock.Object, packageServiceMock.Object)))
        End Function

        <Fact>
        Public Async Function TestOptionsOffered() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetProjectsWithInstalledPackage(It.IsAny(Of Solution), "NuGetPackage", "1.0")).Returns(ImmutableArray(Of Project).Empty)
            installerServiceMock.Setup(Function(i) i.GetProjectsWithInstalledPackage(It.IsAny(Of Solution), "NuGetPackage", "2.0")).Returns(ImmutableArray(Of Project).Empty)
            installerServiceMock.Setup(Function(i) i.TryGetPackageSources()).Returns(NuGetPackageSources)
            installerServiceMock.Setup(Function(s) s.GetInstalledVersions("NuGetPackage")).
                Returns(ImmutableArray.Create("1.0", "2.0"))

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Function() ValueTaskFactory.FromResult(ImmutableArray(Of ReferenceAssemblyWithTypeResult).Empty))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(PackageSourceHelper.NugetOrgSourceName, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(Function() CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

            Dim data = New ProviderData(installerServiceMock.Object, packageServiceMock.Object)
            Await TestSmartTagTextAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
String.Format(FeaturesResources.Use_local_version_0, "1.0"),
parameters:=New TestParameters(fixProviderData:=data))

            Await TestSmartTagTextAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
String.Format(FeaturesResources.Use_local_version_0, "2.0"),
parameters:=New TestParameters(index:=1, fixProviderData:=data))

            Await TestSmartTagTextAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
FeaturesResources.Find_and_install_latest_version,
parameters:=New TestParameters(index:=2, fixProviderData:=data))
        End Function

        <Fact>
        Public Async Function TestInstallGetsCalledNoVersion() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray(Of String).Empty)
            installerServiceMock.Setup(Function(i) i.TryGetPackageSources()).Returns(NuGetPackageSources)
            installerServiceMock.Setup(Function(s) s.TryInstallPackageAsync(It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", Nothing, It.IsAny(Of Boolean), It.IsAny(Of IProgressTracker)(), It.IsAny(Of CancellationToken))).
                                 Returns(SpecializedTasks.True)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Function() ValueTaskFactory.FromResult(ImmutableArray(Of ReferenceAssemblyWithTypeResult).Empty))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(PackageSourceHelper.NugetOrgSourceName, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(Function() CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")))

            Await TestInRegularAndScriptAsync(
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

        <Fact>
        Public Async Function TestInstallGetsCalledWithVersion() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetProjectsWithInstalledPackage(It.IsAny(Of Solution), "NuGetPackage", "1.0")).Returns(ImmutableArray(Of Project).Empty)
            installerServiceMock.Setup(Function(i) i.TryGetPackageSources()).Returns(NuGetPackageSources)
            installerServiceMock.Setup(Function(s) s.GetInstalledVersions("NuGetPackage")).
                Returns(ImmutableArray.Create("1.0"))
            installerServiceMock.Setup(Function(s) s.TryInstallPackageAsync(It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", "1.0", It.IsAny(Of Boolean), It.IsAny(Of IProgressTracker)(), It.IsAny(Of CancellationToken))).
                                 Returns(SpecializedTasks.True)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Function() ValueTaskFactory.FromResult(ImmutableArray(Of ReferenceAssemblyWithTypeResult).Empty))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(PackageSourceHelper.NugetOrgSourceName, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(Function() CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")))

            Await TestInRegularAndScriptAsync(
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

        Private Shared Function CreateSearchResult(packageName As String, typeName As String, nameParts As ImmutableArray(Of String)) As ValueTask(Of ImmutableArray(Of PackageWithTypeResult))
            Return CreateSearchResult(New PackageWithTypeResult(
                packageName:=packageName,
                rank:=0,
                typeName:=typeName,
                version:=Nothing,
                containingNamespaceNames:=nameParts))
        End Function

        Private Shared Function CreateSearchResult(ParamArray results As PackageWithTypeResult()) As ValueTask(Of ImmutableArray(Of PackageWithTypeResult))
            Return ValueTaskFactory.FromResult(ImmutableArray.Create(results))
        End Function

        Private Shared Function CreateNameParts(ParamArray parts As String()) As ImmutableArray(Of String)
            Return parts.ToImmutableArray()
        End Function
    End Class
End Namespace
