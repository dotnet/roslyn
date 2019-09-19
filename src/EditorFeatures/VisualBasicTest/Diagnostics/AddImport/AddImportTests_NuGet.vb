' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Packaging
Imports Microsoft.CodeAnalysis.SymbolSearch
Imports Microsoft.CodeAnalysis.VisualBasic.AddImport
Imports Moq
Imports ProviderData = System.Tuple(Of Microsoft.CodeAnalysis.Packaging.IPackageInstallerService, Microsoft.CodeAnalysis.SymbolSearch.ISymbolSearchService)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.AddImport
    Public Class AddImportNuGetTests
        Inherits AbstractAddImportTests

        Const NugetOrgSourceName = "nuget.org"
        Const NugetOrgUri = "https://api.nuget.org/v3/index.json"

        Private Shared ReadOnly NugetPackageSources As ImmutableArray(Of PackageSource) =
            ImmutableArray.Create(New PackageSource(NugetOrgSourceName, NugetOrgUri))

        Protected Overrides Function CreateWorkspaceFromFile(initialMarkup As String, parameters As TestParameters) As TestWorkspace
            Dim workspace = MyBase.CreateWorkspaceFromFile(initialMarkup, parameters)
            workspace.Options = workspace.Options.
                WithChangedOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages, LanguageNames.VisualBasic, True).
                WithChangedOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, LanguageNames.VisualBasic, True)
            Return workspace
        End Function

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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestSearchPackageSingleName() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray(Of String).Empty)
            installerServiceMock.Setup(Function(i) i.GetPackageSources()).Returns(NugetPackageSources)
            installerServiceMock.Setup(Function(s) s.TryInstallPackage(It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", It.IsAny(Of String), It.IsAny(Of Boolean), It.IsAny(Of CancellationToken))).
                                 Returns(True)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Task.FromResult(Of IList(Of ReferenceAssemblyWithTypeResult))(New List(Of ReferenceAssemblyWithTypeResult)))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(NugetOrgUri, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")))

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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestSearchPackageMultipleNames() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray(Of String).Empty)
            installerServiceMock.Setup(Function(i) i.GetPackageSources()).Returns(NugetPackageSources)
            installerServiceMock.Setup(Function(s) s.TryInstallPackage(It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", It.IsAny(Of String), It.IsAny(Of Boolean), It.IsAny(Of CancellationToken))).
                                 Returns(True)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Task.FromResult(Of IList(Of ReferenceAssemblyWithTypeResult))(New List(Of ReferenceAssemblyWithTypeResult)))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(NugetOrgUri, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestFailedInstallDoesNotChangeFile() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray(Of String).Empty)
            installerServiceMock.Setup(Function(i) i.GetPackageSources()).Returns(NugetPackageSources)
            installerServiceMock.Setup(Function(s) s.TryInstallPackage(It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", It.IsAny(Of String), It.IsAny(Of Boolean), It.IsAny(Of CancellationToken))).
                                 Returns(False)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Task.FromResult(Of IList(Of ReferenceAssemblyWithTypeResult))(New List(Of ReferenceAssemblyWithTypeResult)))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(NugetOrgUri, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestMissingIfPackageAlreadyInstalled() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.GetPackageSources()).Returns(NugetPackageSources)
            installerServiceMock.Setup(Function(s) s.IsInstalled(It.IsAny(Of Workspace)(), It.IsAny(Of ProjectId)(), "NuGetPackage")).
                Returns(True)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Task.FromResult(Of IList(Of ReferenceAssemblyWithTypeResult))(New List(Of ReferenceAssemblyWithTypeResult)))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(NugetOrgUri, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

            Await TestMissingInRegularAndScriptAsync(
"
Class C
    Dim n As [|NuGetType|]
End Class",
New TestParameters(fixProviderData:=New ProviderData(installerServiceMock.Object, packageServiceMock.Object)))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestOptionsOffered() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetProjectsWithInstalledPackage(It.IsAny(Of Solution), "NuGetPackage", "1.0")).Returns(Enumerable.Empty(Of Project))
            installerServiceMock.Setup(Function(i) i.GetProjectsWithInstalledPackage(It.IsAny(Of Solution), "NuGetPackage", "2.0")).Returns(Enumerable.Empty(Of Project))
            installerServiceMock.Setup(Function(i) i.GetPackageSources()).Returns(NugetPackageSources)
            installerServiceMock.Setup(Function(s) s.GetInstalledVersions("NuGetPackage")).
                Returns(ImmutableArray.Create("1.0", "2.0"))

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Task.FromResult(Of IList(Of ReferenceAssemblyWithTypeResult))(New List(Of ReferenceAssemblyWithTypeResult)))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(NugetOrgUri, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NS1", "NS2")))

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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestInstallGetsCalledNoVersion() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetInstalledVersions("NuGetPackage")).Returns(ImmutableArray(Of String).Empty)
            installerServiceMock.Setup(Function(i) i.GetPackageSources()).Returns(NugetPackageSources)
            installerServiceMock.Setup(Function(s) s.TryInstallPackage(It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", Nothing, It.IsAny(Of Boolean), It.IsAny(Of CancellationToken))).
                                 Returns(True)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Task.FromResult(Of IList(Of ReferenceAssemblyWithTypeResult))(New List(Of ReferenceAssemblyWithTypeResult)))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(NugetOrgUri, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")))

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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestInstallGetsCalledWithVersion() As Task
            Dim installerServiceMock = New Mock(Of IPackageInstallerService)(MockBehavior.Strict)
            installerServiceMock.Setup(Function(i) i.IsEnabled(It.IsAny(Of ProjectId))).Returns(True)
            installerServiceMock.Setup(Function(i) i.IsInstalled(It.IsAny(Of Workspace), It.IsAny(Of ProjectId), "NuGetPackage")).Returns(False)
            installerServiceMock.Setup(Function(i) i.GetProjectsWithInstalledPackage(It.IsAny(Of Solution), "NuGetPackage", "1.0")).Returns(Enumerable.Empty(Of Project))
            installerServiceMock.Setup(Function(i) i.GetPackageSources()).Returns(NugetPackageSources)
            installerServiceMock.Setup(Function(s) s.GetInstalledVersions("NuGetPackage")).
                Returns(ImmutableArray.Create("1.0"))
            installerServiceMock.Setup(Function(s) s.TryInstallPackage(It.IsAny(Of Workspace), It.IsAny(Of DocumentId), It.IsAny(Of String), "NuGetPackage", "1.0", It.IsAny(Of Boolean), It.IsAny(Of CancellationToken))).
                                 Returns(True)

            Dim packageServiceMock = New Mock(Of ISymbolSearchService)(MockBehavior.Strict)
            packageServiceMock.Setup(Function(s) s.FindReferenceAssembliesWithTypeAsync("NuGetType", 0, It.IsAny(Of CancellationToken))).
                Returns(Task.FromResult(Of IList(Of ReferenceAssemblyWithTypeResult))(New List(Of ReferenceAssemblyWithTypeResult)))
            packageServiceMock.Setup(Function(s) s.FindPackagesWithTypeAsync(NugetOrgUri, "NuGetType", 0, It.IsAny(Of CancellationToken)())).
                Returns(CreateSearchResult("NuGetPackage", "NuGetType", CreateNameParts("NuGetNamespace")))

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

        Private Function CreateSearchResult(packageName As String, typeName As String, nameParts As ImmutableArray(Of String)) As Task(Of IList(Of PackageWithTypeResult))
            Return CreateSearchResult(New PackageWithTypeResult(
                packageName:=packageName,
                typeName:=typeName,
                version:=Nothing,
                rank:=0,
                containingNamespaceNames:=nameParts))
        End Function

        Private Function CreateSearchResult(ParamArray results As PackageWithTypeResult()) As Task(Of IList(Of PackageWithTypeResult))
            Return Task.FromResult(Of IList(Of PackageWithTypeResult))(ImmutableArray.Create(results))
        End Function

        Private Function CreateNameParts(ParamArray parts As String()) As ImmutableArray(Of String)
            Return parts.ToImmutableArray()
        End Function
    End Class
End Namespace
