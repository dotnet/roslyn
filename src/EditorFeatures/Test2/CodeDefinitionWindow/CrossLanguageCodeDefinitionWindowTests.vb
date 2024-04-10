' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeDefinitionWindow
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.GoToDefinition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.CodeDefinitionWindow.UnitTests
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
    Public Class CrossLanguageCodeDefinitionWindowTests

        Private Class FakeNavigableItem
            Implements INavigableItem

            Private ReadOnly _document As INavigableItem.NavigableDocument

            Public Sub New(document As Document)
                _document = INavigableItem.NavigableDocument.FromDocument(document)
            End Sub

            Public ReadOnly Property ChildItems As ImmutableArray(Of INavigableItem) Implements INavigableItem.ChildItems
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property DisplayFileLocation As Boolean Implements INavigableItem.DisplayFileLocation
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property DisplayTaggedParts As ImmutableArray(Of TaggedText) Implements INavigableItem.DisplayTaggedParts
                Get
                    Return ImmutableArray.Create(New TaggedText("", "DisplayText"))
                End Get
            End Property

            Public ReadOnly Property Document As INavigableItem.NavigableDocument Implements INavigableItem.Document
                Get
                    Return _document
                End Get
            End Property

            Public ReadOnly Property Glyph As Glyph Implements INavigableItem.Glyph
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property IsImplicitlyDeclared As Boolean Implements INavigableItem.IsImplicitlyDeclared
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property SourceSpan As TextSpan Implements INavigableItem.SourceSpan
                Get
                    Return New TextSpan(5, 2)
                End Get
            End Property

            Public ReadOnly Property IsStale As Boolean Implements INavigableItem.IsStale
                Get
                    Throw New NotImplementedException()
                End Get
            End Property
        End Class

        <ExportLanguageService(GetType(INavigableItemsService), NoCompilationConstants.LanguageName), [Shared]>
        Private Class FakeNavigableItemsService
            Implements INavigableItemsService

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetNavigableItemsAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of INavigableItem)) Implements INavigableItemsService.GetNavigableItemsAsync
                Return Task.FromResult(ImmutableArray.Create(Of INavigableItem)(New FakeNavigableItem(document)))
            End Function
        End Class

        <Fact>
        Public Async Function DocumentWithNoSemanticModel() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="NoCompilation">
                        <Document>
                            This is some language that doesn't have a $$compilation.
                        </Document>
                    </Project>
                </Workspace>,
                composition:=AbstractCodeDefinitionWindowTests.TestComposition.AddParts(GetType(FakeNavigableItemsService)))

                Dim hostDocument = workspace.Documents.Single()
                Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)

                Dim definitionContextTracker = workspace.ExportProvider.GetExportedValue(Of DefinitionContextTracker)
                Dim locations = Await definitionContextTracker.GetContextFromPointAsync(
                    workspace,
                    document,
                    hostDocument.CursorPosition.Value,
                    CancellationToken.None)

                Dim expectedLocation = New CodeDefinitionWindowLocation(
                    "DisplayText",
                    document.FilePath,
                    New LinePosition(1, 3))

                Assert.Equal(expectedLocation, Assert.Single(locations))
            End Using
        End Function

        <Fact>
        Public Async Function VisualBasicReferencingCSharp() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <ProjectReference>ReferencedProject</ProjectReference>
                        <Document>
                            Module Program
                                Sub Main()
                                    Dim c As New Class1
                                    c.$$M()
                                End Sub
                            End Module
                        </Document>
                    </Project>
                    <Project Language="C#" Name="ReferencedProject" CommonReferences="true">
                        <Document>
                            public class Class1
                            {
                                public void [|M|]() { }
                            }
                        </Document>
                    </Project>
                </Workspace>,
                composition:=AbstractCodeDefinitionWindowTests.TestComposition)

                Await AbstractCodeDefinitionWindowTests.VerifyContextLocationAsync("void Class1.M()", workspace)
            End Using
        End Function
    End Class
End Namespace

