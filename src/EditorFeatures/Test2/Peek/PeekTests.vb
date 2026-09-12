' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Editor.Implementation.Peek
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Imaging.Interop
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Utilities
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Peek
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Peek)>
    Public Class PeekTests

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820706")>
        Public Sub TestInvokeInEmptyFile()
            Dim result = GetPeekResultCollection(<Workspace>
                                                     <Project Language="C#" CommonReferences="true">
                                                         <Document>$$}</Document>
                                                     </Project>
                                                 </Workspace>)

            Assert.Null(result)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827025")>
        Public Sub TestWorksAcrossLanguages()
            Using workspace = CreateTestWorkspace(<Workspace>
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
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824336")>
        Public Sub TestPeekDefinitionWhenInvokedOnLiteral()
            Using workspace = CreateTestWorkspace(<Workspace>
                                                      <Project Language="C#" CommonReferences="true">
                                                          <Document>class C { string s = $$"Goo"; }</Document>
                                                      </Project>
                                                  </Workspace>)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                Assert.Equal($"String [{FeaturesResources.Decompiled}]", result(0).DisplayInfo.Label)
                Assert.Equal($"String [{FeaturesResources.Decompiled}]", result(0).DisplayInfo.Title)
                Assert.True(result.GetRemainingIdentifierLineTextOnDisk(index:=0).StartsWith("String", StringComparison.Ordinal))
            End Using
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824331"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820289")>
        Public Sub TestPeekDefinitionWhenExtensionMethodFromMetadata()
            Using workspace = CreateTestWorkspace(<Workspace>
                                                      <Project Language="C#" CommonReferences="true">
                                                          <Document>
                                                                               using System.Linq;
                                                                               class C { void M() { int[] a; a.$$Distinct(); }</Document>
                                                      </Project>
                                                  </Workspace>)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                Assert.Equal($"Enumerable [{FeaturesResources.from_metadata}]", result(0).DisplayInfo.Label)
                Assert.Equal($"Enumerable [{FeaturesResources.from_metadata}]", result(0).DisplayInfo.Title)
                Assert.True(result.GetRemainingIdentifierLineTextOnDisk(index:=0).StartsWith("Distinct", StringComparison.Ordinal))
            End Using
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819660")>
        Public Sub TestPeekDefinitionFromVisualBasicMetadataAsSource()
            Using workspace = CreateTestWorkspace(<Workspace>
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
                Assert.Equal($"SerializableAttribute [{FeaturesResources.Decompiled}]", result(0).DisplayInfo.Label)
                Assert.Equal($"SerializableAttribute [{FeaturesResources.Decompiled}]", result(0).DisplayInfo.Title)
            End Using
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819602")>
        Public Sub TestPeekDefinitionOnParamNameXmlDocComment()
            Using workspace = CreateTestWorkspace(<Workspace>
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
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820363")>
        Public Sub TestPeekDefinitionOnLinqVariable()
            Using workspace = CreateTestWorkspace(<Workspace>
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
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091211")>
        Public Sub TestPeekAcrossProjectsInvolvingPortableReferences()
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
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
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

            Using workspace = CreateTestWorkspace(workspaceDefinition)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                result.AssertNavigatesToIdentifier(0, "Identifier")
            End Using
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820363")>
        Public Sub TestFileMapping()
            Using workspace = CreateTestWorkspace(<Workspace>
                                                      <Project Language="C#" CommonReferences="true">
                                                          <Document><![CDATA[
public class D
{
    public void M()
    {
        new Component().$$M();
    }
}
                                                          ]]></Document>
                                                          <Document FilePath="Test.razor"><![CDATA[
@code
{
    public void {|Identifier:M|}()
    {
    }
}
                                                          ]]></Document>
                                                          <Document FilePath="Test.razor.g.cs">
public class Component
{
#line 4 "<%= Path.Combine(TestWorkspace.RootDirectory, "Test.razor") %>"
    public void M()
    {
    }
}
                                                          </Document>
                                                      </Project>
                                                  </Workspace>)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                result.AssertNavigatesToIdentifier(0, "Identifier")
            End Using
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64615")>
        Public Sub TestPartialMethods()
            Using workspace = CreateTestWorkspace(<Workspace>
                                                      <Project Language="C#" CommonReferences="true">
                                                          <Document><![CDATA[
public partial class D
{
    public void M()
    {
        $$PartialMethod();
    }

    partial void {|Identifier:PartialMethod|}();
}
                                                          ]]></Document>
                                                          <Document><![CDATA[
public partial class D
{
    partial void PartialMethod() { }
}
                                                          ]]></Document>
                                                      </Project>
                                                  </Workspace>)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                result.AssertNavigatesToIdentifier(0, "Identifier")
            End Using
        End Sub

        <WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/71680")>
        <InlineData("ValueTuple<int> valueTuple1;")>
        <InlineData("ValueTuple<int, int> valueTuple2;")>
        <InlineData("ValueTuple<int, int, int> valueTuple3;")>
        <InlineData("ValueTuple<int, int, int, int> valueTuple4;")>
        <InlineData("ValueTuple<int, int, int, int, int> valueTuple5;")>
        <InlineData("ValueTuple<int, int, int, int, int, int> valueTuple6;")>
        <InlineData("ValueTuple<int, int, int, int, int, int, int> valueTuple7;")>
        <InlineData("ValueTuple<int, int, int, int, int, int, int, int> valueTuple8;")>
        Public Sub TestPeekDefinitionWithValueType(expression As String)
            Dim workspace =
               <Workspace>
                   <Project Language="C#" CommonReferences="true" AssemblyName="CSProj">
                       <Document FilePath="C.cs">
                            using System;

                            class C
                            {
                                void M()
                                {
                                    $$<%= expression %>
                                }
                            }
                        </Document>
                   </Project>
               </Workspace>

            Using testWorkspace = CreateTestWorkspace(workspace)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal(1, result.Items.Count)
                Assert.Equal($"ValueTuple [{FeaturesResources.from_metadata}]", result(0).DisplayInfo.Label)
                Assert.Equal($"ValueTuple [{FeaturesResources.from_metadata}]", result(0).DisplayInfo.Title)
            End Using
        End Sub

        <WpfTheory>
        <InlineData("$$Counter c;", "T:Counter")>
        <InlineData("void M() => new Box<int>().$$Set(1);", "M:Box`1.Set(`0)")>
        <InlineData("void M() => new Counter().$$Increment();", "M:Counter.Increment")>
        Public Sub TestPeekDefinitionShowsTheFileAnotherLanguageOwnsForAMetadataSymbol(member As String, documentationCommentId As String)
            Using workspace = CreateTestWorkspace(WorkspaceReferencingOtherLanguageLibrary(member), s_crossLanguageComposition)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Equal({$"{CrossLanguageSymbolNavigationService.OwnedAssemblyName}:{documentationCommentId}"}, GetCrossLanguageService(workspace).Requests)
                Assert.Equal(1, result.Items.Count)
                result.AssertShowsFile(index:=0, CrossLanguageSymbolNavigationService.FilePath, CrossLanguageSymbolNavigationService.Position)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestPeekDefinitionShowsMetadataAsSourceWhenNoOtherLanguageOwnsTheSymbol()
            Using workspace = CreateTestWorkspace(<Workspace>
                                                      <Project Language="C#" CommonReferences="true">
                                                          <Document>class C { string s = $$"Goo"; }</Document>
                                                      </Project>
                                                  </Workspace>, s_crossLanguageComposition)
                Dim result = GetPeekResultCollection(workspace)

                Assert.EndsWith(":T:System.String", Assert.Single(GetCrossLanguageService(workspace).Requests))
                Assert.Equal(1, result.Items.Count)
                Assert.Equal($"String [{FeaturesResources.Decompiled}]", result(0).DisplayInfo.Label)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestPeekDefinitionDoesNotAskAnotherLanguageForASourceSymbol()
            Using workspace = CreateTestWorkspace(<Workspace>
                                                      <Project Language="C#" CommonReferences="true">
                                                          <Document>public class {|Identifier:D|} { } class C { $$D d; }</Document>
                                                      </Project>
                                                  </Workspace>, s_crossLanguageComposition)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Empty(GetCrossLanguageService(workspace).Requests)
                Assert.Equal(1, result.Items.Count)
                result.AssertNavigatesToIdentifier(index:=0, name:="Identifier")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestPeekDefinitionDoesNotAskAnotherLanguageForAMetadataSymbolItCannotShow()
            Using workspace = CreateTestWorkspace(<Workspace>
                                                      <Project Language="C#" CommonReferences="true">
                                                          <Document>using $$System; class C { }</Document>
                                                      </Project>
                                                  </Workspace>, s_crossLanguageComposition)
                Dim result = GetPeekResultCollection(workspace)

                Assert.Empty(GetCrossLanguageService(workspace).Requests)
                Assert.Null(result)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestPeekDefinitionPrefersTheExternalNavigationLocationToAnotherLanguage()
            Using workspace = CreateTestWorkspace(
                    WorkspaceReferencingOtherLanguageLibrary("$$Counter c;"),
                    s_crossLanguageComposition.AddParts(GetType(ExternalNavigationSymbolNavigationService)))
                Dim result = GetPeekResultCollection(workspace)

                Assert.Empty(GetCrossLanguageService(workspace).Requests)
                Assert.Equal(1, result.Items.Count)
                result.AssertShowsFile(index:=0, ExternalNavigationSymbolNavigationService.FilePath, ExternalNavigationSymbolNavigationService.Position)
            End Using
        End Sub

        Private Shared ReadOnly s_crossLanguageComposition As TestComposition =
            EditorTestCompositions.EditorFeatures.AddParts(GetType(CrossLanguageSymbolNavigationService))

        ''' <summary>
        ''' A C# project referencing, as metadata, an assembly <see cref="CrossLanguageSymbolNavigationService"/> owns
        ''' the source of, the way F# owns the source of the F# assemblies a C# project references.
        ''' </summary>
        Private Shared Function WorkspaceReferencingOtherLanguageLibrary(member As String) As XElement
            Return <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <MetadataReferenceFromSource Language="C#" AssemblyName=<%= CrossLanguageSymbolNavigationService.OwnedAssemblyName %> CommonReferences="true">
                               <Document>public class Counter { public void Increment() { } } public class Box&lt;T&gt; { public void Set(T value) { } }</Document>
                           </MetadataReferenceFromSource>
                           <Document>class C { <%= member %> }</Document>
                       </Project>
                   </Workspace>
        End Function

        Private Shared Function GetCrossLanguageService(workspace As EditorTestWorkspace) As CrossLanguageSymbolNavigationService
            Return DirectCast(workspace.ExportProvider.GetExportedValue(Of ICrossLanguageSymbolNavigationService)(), CrossLanguageSymbolNavigationService)
        End Function

        Private Shared Function CreateTestWorkspace(element As XElement, Optional composition As TestComposition = Nothing) As EditorTestWorkspace
            Return EditorTestWorkspace.Create(element, composition:=If(composition, EditorTestCompositions.EditorFeatures))
        End Function

        Private Shared Function GetPeekResultCollection(element As XElement) As PeekResultCollection
            Using workspace = CreateTestWorkspace(element)
                Return GetPeekResultCollection(workspace)
            End Using
        End Function

        Private Shared Function GetPeekResultCollection(workspace As EditorTestWorkspace) As PeekResultCollection
            Dim document = workspace.Documents.FirstOrDefault(Function(d) d.CursorPosition.HasValue)

            If document Is Nothing Then
                Assert.Fail("The test is missing a $$ in the workspace.")
            End If

            Dim textBuffer = document.GetTextBuffer()
            Dim textView = document.GetTextView()

            Dim peekableItemSource As New PeekableItemSource(
                textBuffer,
                workspace.GetService(Of PeekableItemFactory),
                New MockPeekResultFactory(workspace.GetService(Of IPersistentSpanFactory)),
                workspace.GetService(Of IThreadingContext),
                workspace.GetService(Of IUIThreadOperationExecutor))

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
                Dim callbackMock = New Mock(Of IFindPeekResultsCallback)(MockBehavior.Strict)
                callbackMock.Setup(Sub(s) s.ReportProgress(It.IsAny(Of Integer)))

                Dim resultSource = item.GetOrCreateResultSource(PredefinedPeekRelationships.Definitions.Name)
                resultSource.FindResults(PredefinedPeekRelationships.Definitions.Name,
                                         peekResult,
                                         CancellationToken.None,
                                         callbackMock.Object)
            End If

            Return peekResult
        End Function

        <Export(GetType(ICrossLanguageSymbolNavigationService)), [Shared], PartNotDiscoverable>
        Private NotInheritable Class CrossLanguageSymbolNavigationService
            Implements ICrossLanguageSymbolNavigationService

            Public Const OwnedAssemblyName = "OtherLanguageLibrary"
            Public Shared ReadOnly FilePath As String = Path.Combine(TestWorkspace.RootDirectory, "Library.fs")
            Public Shared ReadOnly Position As New LinePosition(2, 4)

            Public ReadOnly Property Requests As New List(Of String)

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function TryGetNavigableLocationAsync(assemblyName As String, documentationCommentId As String, cancellationToken As CancellationToken) As Task(Of INavigableLocation) Implements ICrossLanguageSymbolNavigationService.TryGetNavigableLocationAsync
                Throw New NotImplementedException()
            End Function

            Public Function TryGetNavigableFileLocationAsync(assemblyName As String, documentationCommentId As String, cancellationToken As CancellationToken) As Task(Of (filePath As String, linePosition As LinePosition)?) Implements ICrossLanguageSymbolNavigationService.TryGetNavigableFileLocationAsync
                Requests.Add($"{assemblyName}:{documentationCommentId}")

                Dim location As (filePath As String, linePosition As LinePosition)? = Nothing
                If assemblyName = OwnedAssemblyName Then
                    location = (FilePath, Position)
                End If

                Return Task.FromResult(location)
            End Function
        End Class

        <ExportWorkspaceService(GetType(ISymbolNavigationService), ServiceLayer.Test), [Shared], PartNotDiscoverable>
        Private NotInheritable Class ExternalNavigationSymbolNavigationService
            Implements ISymbolNavigationService

            Public Shared ReadOnly FilePath As String = Path.Combine(TestWorkspace.RootDirectory, "External.cs")
            Public Shared ReadOnly Position As New LinePosition(5, 1)

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetNavigableLocationAsync(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Task(Of INavigableLocation) Implements ISymbolNavigationService.GetNavigableLocationAsync
                Throw New NotImplementedException()
            End Function

            Public Function TrySymbolNavigationNotifyAsync(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Task(Of Boolean) Implements ISymbolNavigationService.TrySymbolNavigationNotifyAsync
                Throw New NotImplementedException()
            End Function

            Public Function GetExternalNavigationSymbolLocationAsync(definitionItem As DefinitionItem, cancellationToken As CancellationToken) As Task(Of (filePath As String, linePosition As LinePosition)?) Implements ISymbolNavigationService.GetExternalNavigationSymbolLocationAsync
                Return Task.FromResult(Of (filePath As String, linePosition As LinePosition)?)((FilePath, Position))
            End Function
        End Class

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

            Private ReadOnly _workspace As EditorTestWorkspace

            Public Sub New(workspace As EditorTestWorkspace)
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

            Friend Function GetText() As String
                Dim documentResult = DirectCast(Items(0), IDocumentPeekResult)
                Dim textBufferService = _workspace.GetService(Of ITextBufferFactoryService)
                Dim buffer = textBufferService.CreateTextBuffer(New StreamReader(documentResult.FilePath), textBufferService.InertContentType)

                Return buffer.CurrentSnapshot.GetText()
            End Function

            ''' <summary>
            ''' Returns the text of the identifier line, starting at the identifier and ending at end of the line.
            ''' </summary>
            ''' <param name="index"></param>
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

            Friend Sub AssertShowsFile(index As Integer, filePath As String, position As LinePosition)
                Dim documentResult = DirectCast(Items(index), IDocumentPeekResult)
                Assert.Equal(filePath, documentResult.FilePath)

                Dim startLine As Integer
                Dim startIndex As Integer
                Assert.True(documentResult.IdentifyingSpan.TryGetStartLineIndex(startLine, startIndex), "Unable to get span for the file.")
                Assert.Equal(position, New LinePosition(startLine, startIndex))
            End Sub

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
