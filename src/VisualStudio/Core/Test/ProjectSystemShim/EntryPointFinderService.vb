' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <UseExportProvider>
    Public Class EntryPointFinderService
        Private Shared Async Function FindEntryPointsAsync(file As String, findFormsOnly As Boolean) As Task(Of IEnumerable(Of INamedTypeSymbol))
            Using vsWorkspace = TestWorkspace.CreateVisualBasic(file, composition:=VisualStudioTestCompositions.LanguageServices)
                Dim entryPointFinder = vsWorkspace.Projects.Single.LanguageServiceProvider.GetRequiredService(Of IEntryPointFinderService)
                Dim workspace = vsWorkspace.Projects.Single.LanguageServiceProvider.WorkspaceServices.Workspace
                Dim compilation = Await workspace.CurrentSolution.Projects.Single.GetCompilationAsync()
                Assert.NotNull(compilation)
                Return entryPointFinder.FindEntryPoints(compilation, findFormsOnly)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Async Function FindMainEntryPointForStaticMethodInClass() As Task
            Dim file = "
Imports System
Imports System.Threading.Tasks
Public Class Program
    Public Shared Sub Main(args as String())
    End Sub
End Class
"
            Dim entryPoints = Await FindEntryPointsAsync(file, False)
            Dim entryPoint = Assert.Single(entryPoints)
            Assert.Equal("Program", entryPoint.Name)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Async Function FindMainEntryPointForModule() As Task
            Dim file = "
Imports System
Imports System.Threading.Tasks
Module Program
    Sub Main(args As String())
    End Sub
End Module
"
            Dim entryPoints = Await FindEntryPointsAsync(file, False)
            Dim entryPoint = Assert.Single(entryPoints)
            Assert.Equal("Program", entryPoint.Name)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Async Function DoNotFindEntryPointForTaskReturningMethod() As Task
            Dim file = "
Imports System
Imports System.Threading.Tasks
Module Program
    Function Main(args As String()) As Task
    End Function
End Module
"
            Dim entryPoints = Await FindEntryPointsAsync(file, False)
            Assert.Empty(entryPoints)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Async Function FindMainEntryPointForWinForm() As Task
            Dim file = "
Imports System
Imports System.Threading.Tasks
Public Class MyForm
    Inherits System.Windows.Forms.Form
End Class
"
            Dim entryPoints = Await FindEntryPointsAsync(file, True)
            Dim entryPoint = Assert.Single(entryPoints)
            Assert.Equal("MyForm", entryPoint.Name)
        End Function
    End Class
End Namespace
