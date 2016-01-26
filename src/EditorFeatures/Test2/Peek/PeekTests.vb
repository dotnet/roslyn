' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.Peek
Imports Microsoft.CodeAnalysis.Editor.Peek
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.Imaging.Interop
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Peek
    Public Class PeekTests
        <WpfFact, WorkItem(820706, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820706"), Trait(Traits.Feature, Traits.Features.Peek)>
        Public Async Function TestInvokeInEmptyFile() As Task
            Dim result = Await GetPeekResultCollectionAsync(<Workspace>
                                                                <Project Language="C#" CommonReferences="true">
                                                                    <Document>$$}</Document>
                                                                </Project>
                                                            </Workspace>)

            Assert.Null(result)
        End Function

        <WpfFact, WorkItem(827025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827025"), Trait(Traits.Feature, Traits.Features.Peek)>
        Public Async Function TestWorksAcrossLanguages() As Task
            Using workspace = Await TestWorkspace.CreateAsync(<Workspace>
                                                                                  <Project Language="C#" AssemblyName="Reference" CommonReferences="true">
                                                                                      <Document>public class {|Identifier:TestClass|} { }</Document>
                                                                                  </Project>
                                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                                      <ProjectReference>Reference</ProjectReference>
                                                                                      <Document>
                                                                                Public Class Blah : Inherits $$TestClass : End Class
                                                                          </Document>
                                                                                  </Project>
                                                                              </Workspace>)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                result.AssertNavigatesToIdentifier(index:=0, name:="Identifier")
            End Using
        End Function

        <WpfFact, WorkItem(824336, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824336"), Trait(Traits.Feature, Traits.Features.Peek)>
        Public Async Function TestPeekDefinitionWhenInvokedOnLiteral() As Task
            Using workspace = Await TestWorkspace.CreateAsync(<Workspace>
                                                                                  <Project Language="C#" CommonReferences="true">
                                                                                      <Document>class C { string s = $$"Foo"; }</Document>
                                                                                  </Project>
                                                                              </Workspace>)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                Assert.Equal($"String [{EditorFeaturesResources.FromMetadata}]", result(0).DisplayInfo.Label)
                Assert.Equal($"String [{EditorFeaturesResources.FromMetadata}]", result(0).DisplayInfo.Title)
                Assert.True(result.GetRemainingIdentifierLineTextOnDisk(index:=0).StartsWith("String", StringComparison.Ordinal))
            End Using
        End Function

        <WpfFact, WorkItem(824331, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824331"), WorkItem(820289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820289"), Trait(Traits.Feature, Traits.Features.Peek)>
        Public Async Function TestPeekDefinitionWhenExtensionMethodFromMetadata() As Task
            Using workspace = Await TestWorkspace.CreateAsync(<Workspace>
                                                                                  <Project Language="C#" CommonReferences="true">
                                                                                      <Document>
                                                                               using System.Linq;
                                                                               class C { void M() { int[] a; a.$$Distinct(); }</Document>
                                                                                  </Project>
                                                                              </Workspace>)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                Assert.Equal($"Enumerable [{EditorFeaturesResources.FromMetadata}]", result(0).DisplayInfo.Label)
                Assert.Equal($"Enumerable [{EditorFeaturesResources.FromMetadata}]", result(0).DisplayInfo.Title)
                Assert.True(result.GetRemainingIdentifierLineTextOnDisk(index:=0).StartsWith("Distinct", StringComparison.Ordinal))
            End Using
        End Function

        <WpfFact, WorkItem(819660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819660"), Trait(Traits.Feature, Traits.Features.Peek)>
        Public Async Function TestPeekDefinitionFromVisualBasicMetadataAsSource() As Task
            Using workspace = Await TestWorkspace.CreateAsync(<Workspace>
                                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                                      <Document><![CDATA[<System.$$Serializable()>
Class AA
End Class
</Document>
                                                          ]]></Document>
                                                                                  </Project>
                                                                              </Workspace>)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                Assert.Equal($"SerializableAttribute [{EditorFeaturesResources.FromMetadata}]", result(0).DisplayInfo.Label)
                Assert.Equal($"SerializableAttribute [{EditorFeaturesResources.FromMetadata}]", result(0).DisplayInfo.Title)
                Assert.True(result.GetRemainingIdentifierLineTextOnDisk(index:=0).StartsWith("New()", StringComparison.Ordinal)) ' Navigates to constructor
            End Using
        End Function

        <WpfFact, WorkItem(819602, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819602"), Trait(Traits.Feature, Traits.Features.Peek)>
        Public Async Function TestPeekDefinitionOnParamNameXmlDocComment() As Task
            Using workspace = Await TestWorkspace.CreateAsync(<Workspace>
                                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                                      <Document><![CDATA[
Class C
''' <param name="$$exePath"></param>
Public Sub ddd(ByVal {|Identifier:exePath|} As String)
End Sub
End Class
                                                          ]]></Document>
                                                                                  </Project>
                                                                              </Workspace>)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                result.AssertNavigatesToIdentifier(0, "Identifier")
            End Using
        End Function


        <WpfFact, WorkItem(820363, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820363"), Trait(Traits.Feature, Traits.Features.Peek)>
        Public Async Function TestPeekDefinitionOnLinqVariable() As Task
            Using workspace = Await TestWorkspace.CreateAsync(<Workspace>
                                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                                      <Document><![CDATA[
Module M
    Sub S()
        Dim arr = {3, 4, 5}
        Dim q = From i In arr Select {|Identifier:$$d|} = i.GetType
    End Sub
End Module
                                                          ]]></Document>
                                                                                  </Project>
                                                                              </Workspace>)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                result.AssertNavigatesToIdentifier(0, "Identifier")
            End Using
        End Function


        <WpfFact>
        <WorkItem(1091211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091211")>
        Public Async Function TestPeekAcrossProjectsInvolvingPortableReferences() As Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferencesPortable="true">
        <Document>
            namespace N
            {
                public class CSClass
                {
                    public void  {|Identifier:M|}(int i) { }
                }
            }
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
            Imports N

            Public Class VBClass
                Sub Test()
                    Dim x As New CSClass()
                    x.M$$(5)
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                result.AssertNavigatesToIdentifier(0, "Identifier")
            End Using

        End Function

        Private Async Function GetPeekResultCollectionAsync(element As XElement) As Task(Of PeekResultCollection)
            Using workspace = Await TestWorkspace.CreateAsync(element)
                Return GetPeekResultCollection(workspace)
            End Using
        End Function

        Private Function GetPeekResultCollection(workspace As TestWorkspace) As PeekResultCollection
            Dim document = workspace.Documents.FirstOrDefault(Function(d) d.CursorPosition.HasValue)

            If document Is Nothing Then
                AssertEx.Fail("The test is missing a $$ in the workspace.")
            End If

            Dim textBuffer = document.GetTextBuffer()
            Dim textView = document.GetTextView()

            Dim peekableItemSource As New PeekableItemSource(textBuffer,
                                                             workspace.GetService(Of IPeekableItemFactory),
                                                             New MockPeekResultFactory(workspace.GetService(Of IPersistentSpanFactory)),
                                                             workspace.GetService(Of IMetadataAsSourceFileService),
                                                             workspace.GetService(Of IWaitIndicator))

            Dim peekableSession As New Mock(Of IPeekSession)(MockBehavior.Strict)
            Dim triggerPoint = New SnapshotPoint(document.GetTextBuffer().CurrentSnapshot, document.CursorPosition.Value)
            peekableSession.Setup(Function(s) s.GetTriggerPoint(It.IsAny(Of ITextSnapshot))).Returns(triggerPoint)
            peekableSession.SetupGet(Function(s) s.RelationshipName).Returns("IsDefinedBy")

            Dim items As New List(Of IPeekableItem)

            peekableItemSource.AugmentPeekSession(peekableSession.Object, items)
            If Not items.Any Then
                Return Nothing
            End If

            Dim peekResult As New PeekResultCollection(workspace)
            Dim item = items.SingleOrDefault()

            If item IsNot Nothing Then
                Dim resultSource = item.GetOrCreateResultSource(PredefinedPeekRelationships.Definitions.Name)
                resultSource.FindResults(PredefinedPeekRelationships.Definitions.Name,
                                         peekResult,
                                         CancellationToken.None,
                                         New Mock(Of IFindPeekResultsCallback)(MockBehavior.Loose).Object)
            End If

            Return peekResult
        End Function

        Private Class MockPeekResultFactory
            Implements IPeekResultFactory

            Private ReadOnly _persistentSpanFactory As IPersistentSpanFactory

            Public Sub New(persistentSpanFactory As IPersistentSpanFactory)
                _persistentSpanFactory = persistentSpanFactory
            End Sub

            Public Function Create(displayInfo As IPeekResultDisplayInfo, browseAction As Action) As IExternallyBrowsablePeekResult Implements IPeekResultFactory.Create
                Throw New NotImplementedException()
            End Function

            Public Function Create(displayInfo As IPeekResultDisplayInfo, filePath As String, eoiSpan As Span, idPosition As Integer, isReadOnly As Boolean) As IDocumentPeekResult Implements IPeekResultFactory.Create
                Throw New NotImplementedException()
            End Function

            Public Function Create(displayInfo As IPeekResultDisplayInfo, filePath As String, startLine As Integer, startIndex As Integer, endLine As Integer, endIndex As Integer, idLine As Integer, idIndex As Integer) As IDocumentPeekResult Implements IPeekResultFactory.Create
                Throw New NotImplementedException()
            End Function

            Public Function Create(displayInfo As IPeekResultDisplayInfo, filePath As String, startLine As Integer, startIndex As Integer, endLine As Integer, endIndex As Integer, idLine As Integer, idIndex As Integer, isReadOnly As Boolean) As IDocumentPeekResult Implements IPeekResultFactory.Create
                Dim documentResult As New Mock(Of IDocumentPeekResult)(MockBehavior.Strict)

                documentResult.SetupGet(Function(d) d.DisplayInfo).Returns(displayInfo)
                documentResult.SetupGet(Function(d) d.FilePath).Returns(filePath)
                documentResult.SetupGet(Function(d) d.IdentifyingSpan).Returns(_persistentSpanFactory.Create(filePath, idLine, idIndex, idLine, idIndex, SpanTrackingMode.EdgeInclusive))
                documentResult.SetupGet(Function(d) d.Span).Returns(_persistentSpanFactory.Create(filePath, idLine, idIndex, idLine, idIndex, SpanTrackingMode.EdgeInclusive))
                documentResult.SetupGet(Function(d) d.IsReadOnly).Returns(isReadOnly)

                Return documentResult.Object
            End Function

            Public Function Create(displayInfo As IPeekResultDisplayInfo2, image As ImageMoniker, filePath As String, startLine As Integer, startIndex As Integer, endLine As Integer, endIndex As Integer, idStartLine As Integer, idStartIndex As Integer, idEndLine As Integer, idEndIndex As Integer) As IDocumentPeekResult Implements IPeekResultFactory.Create
                Throw New NotImplementedException()
            End Function

            Public Function Create(displayInfo As IPeekResultDisplayInfo2, image As ImageMoniker, filePath As String, startLine As Integer, startIndex As Integer, endLine As Integer, endIndex As Integer, idStartLine As Integer, idStartIndex As Integer, idEndLine As Integer, idEndIndex As Integer, isReadOnly As Boolean) As IDocumentPeekResult Implements IPeekResultFactory.Create
                Throw New NotImplementedException()
            End Function

            Public Function Create(displayInfo As IPeekResultDisplayInfo2, image As ImageMoniker, filePath As String, startLine As Integer, startIndex As Integer, endLine As Integer, endIndex As Integer, idStartLine As Integer, idStartIndex As Integer, idEndLine As Integer, idEndIndex As Integer, isReadOnly As Boolean, editorDestination As Guid) As IDocumentPeekResult Implements IPeekResultFactory.Create
                Throw New NotImplementedException()
            End Function

            Public Function Create(displayInfo As IPeekResultDisplayInfo2, image As ImageMoniker, filePath As String, startLine As Integer, startIndex As Integer, endLine As Integer, endIndex As Integer, idStartLine As Integer, idStartIndex As Integer, idEndLine As Integer, idEndIndex As Integer, isReadOnly As Boolean, editorDestination As Guid, postNavigationCallback As Action(Of IPeekResult, Object, Object)) As IDocumentPeekResult Implements IPeekResultFactory.Create
                Throw New NotImplementedException()
            End Function
        End Class

        Private Class PeekResultCollection
            Implements IPeekResultCollection

            Public ReadOnly Items As New List(Of IPeekResult)

            Private ReadOnly _workspace As TestWorkspace

            Public Sub New(workspace As TestWorkspace)
                _workspace = workspace
            End Sub

            Private ReadOnly Property Count As Integer Implements IPeekResultCollection.Count
                Get
                    Return Items.Count
                End Get
            End Property

            Default Public Property Item(index As Integer) As IPeekResult Implements IPeekResultCollection.Item
                Get
                    Return Items(index)
                End Get
                Set(value As IPeekResult)
                    Throw New NotImplementedException()
                End Set
            End Property

            Private Sub Add(peekResult As IPeekResult) Implements IPeekResultCollection.Add
                Items.Add(peekResult)
            End Sub

            Private Sub Clear() Implements IPeekResultCollection.Clear
                Throw New NotImplementedException()
            End Sub

            Private Sub Insert(index As Integer, peekResult As IPeekResult) Implements IPeekResultCollection.Insert
                Throw New NotImplementedException()
            End Sub

            Private Sub Move(oldIndex As Integer, newIndex As Integer) Implements IPeekResultCollection.Move
                Throw New NotImplementedException()
            End Sub

            Private Sub RemoveAt(index As Integer) Implements IPeekResultCollection.RemoveAt
                Throw New NotImplementedException()
            End Sub

            Private Function Contains(peekResult As IPeekResult) As Boolean Implements IPeekResultCollection.Contains
                Throw New NotImplementedException()
            End Function

            Private Function IndexOf(peekResult As IPeekResult, startAt As Integer) As Integer Implements IPeekResultCollection.IndexOf
                Throw New NotImplementedException()
            End Function

            Private Function Remove(item As IPeekResult) As Boolean Implements IPeekResultCollection.Remove
                Throw New NotImplementedException()
            End Function

            ''' <summary>
            ''' Returns the text of the identifier line, starting at the identifier and ending at end of the line.
            ''' </summary>
            ''' <param name="index"></param>
            ''' <returns></returns>
            Friend Function GetRemainingIdentifierLineTextOnDisk(index As Integer) As String
                Dim documentResult = DirectCast(Items(index), IDocumentPeekResult)
                Dim textBufferService = _workspace.GetService(Of ITextBufferFactoryService)
                Dim buffer = textBufferService.CreateTextBuffer(New StreamReader(documentResult.FilePath), textBufferService.InertContentType)

                Dim startLine As Integer
                Dim startIndex As Integer
                Assert.True(documentResult.IdentifyingSpan.TryGetStartLineIndex(startLine, startIndex), "Unable to get span for metadata file.")

                Dim line = buffer.CurrentSnapshot.GetLineFromLineNumber(startLine)

                Return buffer.CurrentSnapshot.GetText(line.Start + startIndex, line.Length - startIndex)
            End Function

            Friend Sub AssertNavigatesToIdentifier(index As Integer, name As String)
                Dim documentResult = DirectCast(Items(index), IDocumentPeekResult)
                Dim document = _workspace.Documents.FirstOrDefault(Function(d) d.FilePath = documentResult.FilePath)

                AssertEx.NotNull(document, "Peek didn't navigate to a document in source. Navigated to " + documentResult.FilePath + " instead.")

                Dim startLine As Integer
                Dim startIndex As Integer
                Assert.True(documentResult.IdentifyingSpan.TryGetStartLineIndex(startLine, startIndex), "Unable to get span for source file.")

                Dim snapshot = document.GetTextBuffer().CurrentSnapshot
                Dim expectedPosition = New SnapshotPoint(snapshot, document.AnnotatedSpans(name).Single().Start)
                Dim actualPosition = snapshot.GetLineFromLineNumber(startLine).Start + startIndex

                Assert.Equal(expectedPosition, actualPosition)
            End Sub
        End Class
    End Class
End Namespace
