' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.GoToDefinition
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.GoToDefinition
    <[UseExportProvider]>
    Public Class GoToDefinitionApiTests

        Private Shared Async Function TestAsync(workspaceDefinition As XElement, expectSuccess As Boolean) As Tasks.Task
            Using workspace = TestWorkspace.Create(workspaceDefinition, composition:=GoToTestHelpers.Composition)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = solution.GetDocument(cursorDocument.Id)
                Dim root = Await document.GetSyntaxRootAsync()
                Dim semanticModel = Await document.GetSemanticModelAsync()
                Dim symbol = root.FindToken(cursorPosition).Parent _
                    .AncestorsAndSelf() _
                    .Select(Function(n) semanticModel.GetDeclaredSymbol(n, CancellationToken.None)) _
                    .FirstOrDefault()

                Assert.NotNull(symbol)

                Dim symbolId = symbol.GetSymbolKey()
                Dim project = document.Project
                Dim compilation = Await project.GetCompilationAsync()
                Dim symbolInfo = symbolId.Resolve(compilation)

                Assert.NotNull(symbolInfo.Symbol)

                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)()
                Dim presenter = New MockStreamingFindUsagesPresenter(workspace.GlobalOptions, Sub() Exit Sub)

                WpfTestRunner.RequireWpfFact($"{NameOf(GoToDefinitionHelpers)}.{NameOf(GoToDefinitionHelpers.GetDefinitionLocationAsync)} assumes it's on the UI thread with a {NameOf(TaskExtensions.WaitAndGetResult)} call")
                Dim location = Await GoToDefinitionHelpers.GetDefinitionLocationAsync(
                    symbolInfo.Symbol, document.Project.Solution,
                    threadingContext,
                    presenter,
                    thirdPartyNavigationAllowed:=True,
                    cancellationToken:=CancellationToken.None)
                Dim success = location IsNot Nothing AndAlso
                    Await location.NavigateToAsync(NavigationOptions.Default, CancellationToken.None)

                Assert.Equal(expectSuccess, success)
            End Using
        End Function

        Private Shared Function TestSuccessAsync(workspaceDefinition As XElement) As Tasks.Task
            Return TestAsync(workspaceDefinition, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVBOperator() As Tasks.Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
''' &lt;summary&gt;
''' thin wrapper around a single int to test operators
''' &lt;/summary&gt;
Structure IntWrapper
    Private Value As Integer

    Public Sub New(i As Integer)
        Value = i
    End Sub

    Public Shared Widening Operator CType(i As IntWrapper) As Integer
        Return i.Value
    End Operator

    ' Repro operator 
    $$Public Shared Narrowing Operator CType(i As IntWrapper) As Byte
        Return CByte(i.Value)
    End Operator
 
    Public Shared Narrowing Operator CType(i As IntWrapper) As String
        Return String.Format("{0}", i.Value)
    End Operator
End Structure
        </Document>
    </Project>
</Workspace>

            Await TestSuccessAsync(workspaceDefinition)
        End Function
    End Class
End Namespace
