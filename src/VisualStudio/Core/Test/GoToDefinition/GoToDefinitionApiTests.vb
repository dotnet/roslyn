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

        Private Sub Test(workspaceDefinition As XElement, expectSuccess As Boolean)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceDefinition, exportProvider:=GoToTestHelpers.ExportProvider)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = solution.GetDocument(cursorDocument.Id)
                Dim root = document.GetSyntaxRootAsync(CancellationToken.None).Result
                Dim semanticModel = document.GetSemanticModelAsync(CancellationToken.None).Result
                Dim symbol = root.FindToken(cursorPosition).Parent _
                    .AncestorsAndSelf() _
                    .Select(Function(n) semanticModel.GetDeclaredSymbol(n, CancellationToken.None)) _
                    .FirstOrDefault()

                Assert.NotNull(symbol)

                Dim symbolId = symbol.GetSymbolKey()
                Dim project = document.Project
                Dim compilation = project.GetCompilationAsync(CancellationToken.None).Result
                Dim symbolInfo = symbolId.Resolve(compilation)

                Assert.NotNull(symbolInfo.Symbol)

                Dim presenter = New MockNavigableItemsPresenter(Sub() Exit Sub)

                Dim success = GoToDefinitionHelpers.TryGoToDefinition(
                    symbolInfo.Symbol, document.Project, {New Lazy(Of INavigableItemsPresenter)(Function() presenter)}, thirdPartyNavigationAllowed:=True, throwOnHiddenDefinition:=False, cancellationToken:=CancellationToken.None)

                Assert.Equal(expectSuccess, success)
            End Using
        End Sub

        Private Sub TestSuccess(workspaceDefinition As XElement)
            Test(workspaceDefinition, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVBOperator()
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

            TestSuccess(workspaceDefinition)
        End Sub

    End Class
End Namespace
