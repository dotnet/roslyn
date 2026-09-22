' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToDefinition
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
    Public NotInheritable Class CrossLanguageSymbolNavigationTests
        Private Shared ReadOnly s_composition As TestComposition =
            EditorTestCompositions.EditorFeatures.AddParts(GetType(CrossLanguageSymbolNavigationService))

        <Theory>
        <InlineData("$$Counter c;", "T:Counter")>
        <InlineData("void M() => new Counter().$$Increment();", "M:Counter.Increment")>
        <InlineData("void M() => new Box<int>().$$Set(1);", "M:Box`1.Set(`0)")>
        Public Async Function TestAnotherLanguageIsAskedForTheDefinitionOfAMetadataSymbol(member As String, documentationCommentId As String) As Task
            Using workspace = EditorTestWorkspace.Create(WorkspaceReferencingOtherLanguageLibrary(member), composition:=s_composition)
                Dim service = GetCrossLanguageService(workspace)
                service.Span = New DocumentSpan(workspace.CurrentSolution.Projects.Single().Documents.Single(), New TextSpan(10, 5))

                Dim span = Await CrossLanguageSymbolNavigation.TryGetDefinitionSpanAsync(workspace.CurrentSolution, Await GetDefinitionItemAtCaretAsync(workspace), CancellationToken.None)

                Assert.Equal({$"{CrossLanguageSymbolNavigationService.OwnedAssemblyName}:{documentationCommentId}"}, service.Requests)
                Assert.Equal(service.Span, span)
            End Using
        End Function

        <Fact>
        Public Async Function TestNoSpanWhenNoOtherLanguageDefinesTheSymbol() As Task
            Using workspace = EditorTestWorkspace.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>class C { $$string s; }</Document>
                        </Project>
                    </Workspace>, composition:=s_composition)
                Dim span = Await CrossLanguageSymbolNavigation.TryGetDefinitionSpanAsync(workspace.CurrentSolution, Await GetDefinitionItemAtCaretAsync(workspace), CancellationToken.None)

                Assert.EndsWith(":T:System.String", Assert.Single(GetCrossLanguageService(workspace).Requests))
                Assert.Null(span)
            End Using
        End Function

        <Fact>
        Public Async Function TestAnotherLanguageIsNotAskedForASourceSymbol() As Task
            Using workspace = EditorTestWorkspace.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>public class D { } class C { $$D d; }</Document>
                        </Project>
                    </Workspace>, composition:=s_composition)
                Dim span = Await CrossLanguageSymbolNavigation.TryGetDefinitionSpanAsync(workspace.CurrentSolution, Await GetDefinitionItemAtCaretAsync(workspace), CancellationToken.None)

                Assert.Empty(GetCrossLanguageService(workspace).Requests)
                Assert.Null(span)
            End Using
        End Function

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

        Private Shared Async Function GetDefinitionItemAtCaretAsync(workspace As EditorTestWorkspace) As Task(Of DefinitionItem)
            Dim cursorDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
            Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
            Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, cursorDocument.CursorPosition.Value, CancellationToken.None)
            Return Await symbol.ToNonClassifiedDefinitionItemAsync(document.Project.Solution, includeHiddenLocations:=True, CancellationToken.None)
        End Function

        Private Shared Function GetCrossLanguageService(workspace As EditorTestWorkspace) As CrossLanguageSymbolNavigationService
            Return DirectCast(workspace.ExportProvider.GetExportedValue(Of ICrossLanguageSymbolNavigationService)(), CrossLanguageSymbolNavigationService)
        End Function

        <Export(GetType(ICrossLanguageSymbolNavigationService)), [Shared], PartNotDiscoverable>
        Private NotInheritable Class CrossLanguageSymbolNavigationService
            Implements ICrossLanguageSymbolNavigationService

            Public Const OwnedAssemblyName = "OtherLanguageLibrary"

            Public ReadOnly Property Requests As New List(Of String)
            Public Property Span As DocumentSpan?

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function TryGetNavigableLocationAsync(assemblyName As String, documentationCommentId As String, cancellationToken As CancellationToken) As Task(Of INavigableLocation) Implements ICrossLanguageSymbolNavigationService.TryGetNavigableLocationAsync
                Throw New NotImplementedException()
            End Function

            Public Function TryGetDefinitionSpanAsync(assemblyName As String, documentationCommentId As String, cancellationToken As CancellationToken) As Task(Of DocumentSpan?) Implements ICrossLanguageSymbolNavigationService.TryGetDefinitionSpanAsync
                Requests.Add($"{assemblyName}:{documentationCommentId}")
                Return Task.FromResult(If(assemblyName = OwnedAssemblyName, Span, Nothing))
            End Function
        End Class
    End Class
End Namespace
