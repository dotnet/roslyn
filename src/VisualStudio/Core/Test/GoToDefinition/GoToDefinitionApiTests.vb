' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.UnitTests.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.GoToDefinition
    Public Class GoToDefinitionApiTests

        Private Async Function TestAsync(workspaceDefinition As XElement, expectSuccess As Boolean) As Tasks.Task
            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(workspaceDefinition, exportProvider:=GoToTestHelpers.ExportProvider)
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

                Dim presenter = New MockNavigableItemsPresenter(Sub() Exit Sub)

                WpfTestCase.RequireWpfFact($"{NameOf(GoToDefinitionHelpers)}.{NameOf(GoToDefinitionHelpers.TryGoToDefinition)} assumes it's on the UI thread with a WaitAndGetResult call")
                Dim success = GoToDefinitionHelpers.TryGoToDefinition(
                    symbolInfo.Symbol, document.Project, {New Lazy(Of INavigableItemsPresenter)(Function() presenter)}, thirdPartyNavigationAllowed:=True, throwOnHiddenDefinition:=False, cancellationToken:=CancellationToken.None)

                Assert.Equal(expectSuccess, success)
            End Using
        End Function

        Private Function TestSuccessAsync(workspaceDefinition As XElement) As Tasks.Task
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