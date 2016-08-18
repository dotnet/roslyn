' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Navigation
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Composition
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.CodeDefinitionWindow.UnitTests
    Public Class CrossLanguageCodeDefinitionWindowTests
        Inherits AbstractCodeDefinitionWindowTests

        Private Class FakeNavigableItem
            Implements INavigableItem

            Private ReadOnly _document As Document

            Public Sub New(document As Document)
                _document = document
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

            Public ReadOnly Property Document As Document Implements INavigableItem.Document
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
        End Class

        <ExportLanguageService(GetType(IGoToDefinitionService), "NoCompilation"), [Shared]>
        Private Class FakeGoToDefinitionService
            Implements IGoToDefinitionService

            Public Function FindDefinitionsAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of INavigableItem)) Implements IGoToDefinitionService.FindDefinitionsAsync
                Return Task.FromResult(SpecializedCollections.SingletonEnumerable(Of INavigableItem)(New FakeNavigableItem(document)))
            End Function

            Public Function TryGoToDefinition(document As Document, position As Integer, cancellationToken As CancellationToken) As Boolean Implements IGoToDefinitionService.TryGoToDefinition
                Throw New NotImplementedException()
            End Function
        End Class

        <Fact, Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
        Public Async Function DocumentWithNoSemanticModel() As Task
            Using workspace = Await TestWorkspace.CreateAsync(
                <Workspace>
                    <Project Language="NoCompilation">
                        <Document>
                            This is some language that doesn't have a $$compilation.
                        </Document>
                    </Project>
                </Workspace>,
                exportProvider:=MinimalTestExportProvider.CreateExportProvider(
                    MinimalTestExportProvider.LanguageNeutralCatalog.WithPart(GetType(FakeGoToDefinitionService))))

                Dim hostDocument = workspace.Documents.Single()
                Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)

                Dim definitionContextTracker As New DefinitionContextTracker(Nothing, Nothing)
                Dim locations = Await definitionContextTracker.GetContextFromPointAsync(
                    document,
                    hostDocument.CursorPosition.Value,
                    TaskScheduler.Current,
                    CancellationToken.None)

                Dim expectedLocation = New CodeDefinitionWindowLocation(
                    "DisplayText",
                    New FileLinePositionSpan("document1", New LinePositionSpan(start:=New LinePosition(0, 5), [end]:=New LinePosition(0, 7))))
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
        Public Async Function CSharpReferencingVisualBasic() As Task
            Using workspace = Await TestWorkspace.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" ProjectReferences="ReferencedProject">
                        <Document>
                            Module Program
                                Sub Main()
                                    Dim c As New Class1
                                    c.$$M()
                                End Sub
                            End Module
                        </Document>
                    </Project>
                    <Project Language="C#" Name="ReferencedProject">
                        <Document>
                            public class Class1
                            {
                                public void [|M|]() { }
                            }
                        </Document>
                    </Project>
                </Workspace>)
                Dim vbHostDocument = workspace.Documents.Single(Function(d) d.Project.Language = LanguageNames.VisualBasic)
                Dim document As Document = workspace.CurrentSolution.GetDocument(vbHostDocument.Id)

                Dim definitionContextTracker As New DefinitionContextTracker(Nothing, Nothing)
                Dim locations = Await definitionContextTracker.GetContextFromPointAsync(
                    document,
                    vbHostDocument.CursorPosition.Value,
                    TaskScheduler.Current,
                    CancellationToken.None)

                Dim csHostDocument = workspace.Documents.Single(Function(d) d.Project.Language = LanguageNames.CSharp)
                Dim tree = Await workspace.CurrentSolution.GetDocument(csHostDocument.Id).GetSyntaxTreeAsync()
                Dim expectedLocation = New CodeDefinitionWindowLocation(
                    "Class1.M()",
                    tree.GetLocation(csHostDocument.SelectedSpans.Single()).GetLineSpan())
            End Using
        End Function

        Protected Overrides Function CreateWorkspaceAsync(code As String, Optional exportProvider As ExportProvider = Nothing) As Task(Of TestWorkspace)
            Assert.False(True)
            Return Nothing
        End Function
    End Class
End Namespace

